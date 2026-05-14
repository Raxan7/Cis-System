using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Api.Configuration;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Identity;
using Cis.Contracts.Operations;
using Cis.Domain.Audit;
using Cis.Domain.Identity;
using Cis.Domain.Operations;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.Operations;

[Collection(IntegrationTestCollection.Name)]
public sealed class OperationsReadinessTests
{
    private readonly CisApiFactory _factory;

    public OperationsReadinessTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void ProductionConfigurationValidation_FailsForMissingOrPlaceholderSecrets()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CisDb"] = "Host=localhost;Database=cis;Username=cis;Password=cis_password",
                ["Jwt:Issuer"] = "Victory.CIS",
                ["Jwt:Audience"] = "Victory.CIS.Api",
                ["Jwt:SigningKey"] = "development-signing-key-change-before-production-32bytes",
                ["OperationalReadiness:Environment"] = "Production",
                ["OperationalReadiness:RtoMinutes"] = "60",
                ["OperationalReadiness:RpoMinutes"] = "15",
                ["OperationalReadiness:BackupDirectory"] = "__REQUIRED_DURABLE_STORAGE_PATH__",
                ["Storage:LocalPath"] = "__REQUIRED_DURABLE_STORAGE_PATH__",
                ["BackgroundJobs:Provider"] = "Quartz",
                ["BackgroundJobs:DashboardAdminRole"] = "SystemAdmin",
                ["Identity:BootstrapAdmin:Password"] = "ChangeMe123!"
            })
            .Build();

        var action = () => OperationalReadinessConfigurationValidator.Validate(configuration, "Production");

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Production ConnectionStrings:CisDb*Production Jwt:SigningKey*Production backup directory*Production Security:Cors:AllowedOrigins*Production Security:Mfa:RequireForPrivilegedRoles*");
    }

    [Fact]
    public async Task HealthEndpoints_ReturnLivenessReadinessAndDeepDependencyStatus()
    {
        using var client = _factory.CreateClient();

        var live = await client.GetAsync("/health/live");
        live.StatusCode.Should().Be(HttpStatusCode.OK);

        var ready = await client.GetAsync("/health/ready");
        var readyBody = await ready.Content.ReadAsStringAsync();
        ready.StatusCode.Should().Be(HttpStatusCode.OK, readyBody);
        readyBody.Should().Contain("database").And.Contain("storage").And.Contain("background_jobs");

        var unauthorizedDeep = await client.GetAsync("/api/operations/health/deep");
        unauthorizedDeep.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await AuthenticateAsBootstrapAdminAsync(client);
        var deep = await client.GetAsync("/api/operations/health/deep");
        var deepBody = await deep.Content.ReadAsStringAsync();
        deep.StatusCode.Should().Be(HttpStatusCode.OK, deepBody);
        var health = await ReadResponseAsync<DeepHealthDto>(deep);
        health.Entries.Select(entry => entry.Name).Should().Contain(["database", "storage", "background_jobs"]);
    }

    [Fact]
    public async Task OperationsApis_EnforcePermissionsPersistRecordsAndAuditDrTestCreation()
    {
        using var client = _factory.CreateClient();

        var unauthorized = await client.GetAsync("/api/operations/backup-runs");
        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var portalUser = await CreateUserWithRoleDirectlyAsync(UniqueEmail("ops-portal"), "Operations Portal", "Portal123!", RoleNames.InvestorPortalUser);
        await AuthenticateAsync(client, portalUser.Email, portalUser.Password);
        var forbidden = await client.GetAsync("/api/operations/backup-runs");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        client.DefaultRequestHeaders.Authorization = null;
        await AuthenticateAsBootstrapAdminAsync(client);
        await SeedBackupRunAsync();

        var backupRuns = await client.GetAsync("/api/operations/backup-runs");
        backupRuns.StatusCode.Should().Be(HttpStatusCode.OK);
        var backups = await ReadResponseAsync<IReadOnlyCollection<BackupRunRecordDto>>(backupRuns);
        backups.Should().Contain(record => record.DatabaseName == "cis_tests" && record.Status == OperationalRecordStatus.Completed.ToString());

        var invalidDrTest = await client.PostAsJsonAsync("/api/operations/dr-tests", new CreateDrTestRequest(
            string.Empty,
            "Production",
            "Missing test name",
            DateTime.UtcNow.AddDays(1),
            null));
        invalidDrTest.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var plannedAt = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(14), DateTimeKind.Utc);
        var create = await client.PostAsJsonAsync("/api/operations/dr-tests", new CreateDrTestRequest(
            $"Quarterly DR Test {Guid.NewGuid():N}"[..40],
            "Production",
            "Restore the latest backup into an isolated environment and verify readiness probes.",
            plannedAt,
            "evidence/dr/planned-test.md"));
        var createBody = await create.Content.ReadAsStringAsync();
        create.StatusCode.Should().Be(HttpStatusCode.Created, createBody);
        var drTest = await ReadResponseAsync<DrTestRecordDto>(create);
        drTest.Status.Should().Be(OperationalRecordStatus.Scheduled.ToString());

        var list = await client.GetAsync("/api/operations/dr-tests");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var records = await ReadResponseAsync<IReadOnlyCollection<DrTestRecordDto>>(list);
        records.Should().Contain(record => record.Id == drTest.Id);

        var metrics = await client.GetAsync("/metrics");
        metrics.StatusCode.Should().Be(HttpStatusCode.OK);
        var metricsBody = await metrics.Content.ReadAsStringAsync();
        metricsBody.Should().Contain("cis_process_uptime_seconds");

        var jobs = await client.GetAsync("/jobs");
        jobs.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await dbContext.AuditLogs.AnyAsync(log => log.Module == "Operations" && log.Action == "DRTestScheduled" && log.EventType == AuditEventType.Created)).Should().BeTrue();
    }

    private async Task SeedBackupRunAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;
        var backup = BackupRunRecord.Start(OperationalEnvironment.Test, BackupType.Logical, "cis_tests", "ops/backups/cis-tests.dump", now.AddMinutes(-5), "test-fixture");
        backup.Complete(now, 2048, new string('a', 64));
        dbContext.BackupRunRecords.Add(backup);
        await dbContext.SaveChangesAsync();
    }

    private async Task AuthenticateAsBootstrapAdminAsync(HttpClient client)
    {
        await AuthenticateAsync(client, "admin.tests@victoryfs.local", "ChangeMe123!");
    }

    private static async Task<AuthTokenResponse> AuthenticateAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        var token = await ReadResponseAsync<AuthTokenResponse>(response);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return token;
    }

    private async Task<(Guid Id, string Email, string Password)> CreateUserWithRoleDirectlyAsync(string email, string displayName, string password, string roleName)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var role = await dbContext.Roles.SingleAsync(role => role.Name == roleName);
        var now = DateTime.UtcNow;
        var user = User.Create(email, displayName, "pending", now);
        user.SetPasswordHash(passwordHasher.HashPassword(user, password), requirePasswordChange: false);
        user.AssignRole(role.Id, "test-fixture", "test-fixture", now);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return (user.Id, email, password);
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response)
    {
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        apiResponse.Should().NotBeNull();
        return apiResponse!.Data;
    }

    private static string UniqueEmail(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}@victoryfs.local";
    }
}
