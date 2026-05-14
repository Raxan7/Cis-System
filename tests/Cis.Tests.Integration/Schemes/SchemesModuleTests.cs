using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Identity;
using Cis.Contracts.Schemes;
using Cis.Domain.Identity;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.Schemes;

[Collection(IntegrationTestCollection.Name)]
public sealed class SchemesModuleTests
{
    private readonly CisApiFactory _factory;

    public SchemesModuleTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedSchemesEndpoint_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/schemes");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ProtectedSchemesEndpoint_WithAuthenticatedUserMissingPermission_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var createdUser = await CreateUserAsync(client, UniqueEmail("scheme-no-permission"), "Scheme No Permission", "NoPermission123!");

        var token = await LoginAsync(client, createdUser.Email, "NoPermission123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var response = await client.GetAsync("/api/schemes");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task SchemeSetup_WhenCheckedAndApprovedBySeparateUsers_BecomesActiveAndWritesAuditAndVersionHistory()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var scheme = await CreateSchemeAsync(client, UniqueCode("MMF"));
        scheme = await AddMinimumActivationDataAsync(client, scheme);

        var submitResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/submit",
            new SchemeWorkflowActionRequest("Submit scheme setup for checking."));
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        scheme = await ReadResponseAsync<SchemeDto>(submitResponse);
        scheme.Status.Should().Be("Submitted");

        var checker = await CreateSystemAdminDirectlyAsync(UniqueEmail("scheme-checker"), "Scheme Checker", "Checker123!");
        await AuthenticateAsync(client, checker.Email, checker.Password);
        var checkResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/check",
            new SchemeWorkflowActionRequest("Checked scheme setup controls."));
        checkResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("scheme-approver"), "Scheme Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/approve",
            new SchemeWorkflowActionRequest("Approved scheme activation."));
        var approveBody = await approveResponse.Content.ReadAsStringAsync();
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK, approveBody);

        var activeScheme = await ReadResponseAsync<SchemeDto>(approveResponse);
        activeScheme.Status.Should().Be("Active");
        activeScheme.Classes.Should().HaveCount(1);
        activeScheme.FeeSchedules.Should().ContainSingle().Which.Status.Should().Be("Active");
        activeScheme.ApprovedInstrumentRules.Should().ContainSingle().Which.Status.Should().Be("Active");
        activeScheme.BankAccounts.Should().ContainSingle();
        activeScheme.Configurations.Should().ContainSingle();
        activeScheme.RiskProfiles.Should().ContainSingle();
        activeScheme.LiquidityThresholds.Should().ContainSingle();
        activeScheme.DistributionRules.Should().ContainSingle();
        activeScheme.TemplateMappings.Should().ContainSingle();
        activeScheme.VersionHistory.Should().NotBeEmpty();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var auditActions = await dbContext.AuditLogs
            .Where(auditLog => auditLog.Module == "Schemes" && auditLog.EntityId == scheme.Id.ToString())
            .Select(auditLog => auditLog.Action)
            .ToListAsync();
        auditActions.Should().Contain("SchemeCreated");
        auditActions.Should().Contain("SchemeClassAdded");
        auditActions.Should().Contain("FeeScheduleAdded");
        auditActions.Should().Contain("ApprovedInstrumentRuleAdded");
        auditActions.Should().Contain("SchemeBankAccountAdded");
        auditActions.Should().Contain("SchemeSubmitted");
        auditActions.Should().Contain("SchemeChecked");
        auditActions.Should().Contain("SchemeApproved");
    }

    [Fact]
    public async Task SchemeApproval_WithoutActivationData_ReturnsValidationProblemDetails()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var scheme = await CreateSchemeAsync(client, UniqueCode("BAL"));
        var checker = await CreateSystemAdminDirectlyAsync(UniqueEmail("scheme-validation-checker"), "Scheme Validation Checker", "Checker123!");
        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("scheme-validation-approver"), "Scheme Validation Approver", "Approver123!");

        var submitResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/submit",
            new SchemeWorkflowActionRequest("Submit incomplete scheme."));
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await AuthenticateAsync(client, checker.Email, checker.Password);
        var checkResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/check",
            new SchemeWorkflowActionRequest("Checked incomplete scheme."));
        checkResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/approve",
            new SchemeWorkflowActionRequest("Try approve incomplete scheme."));

        approveResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        approveResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var body = await approveResponse.Content.ReadAsStringAsync();
        body.Should().Contain("requires at least one class");
    }

    [Fact]
    public async Task FeeSchedule_WhenEffectiveDatesOverlapForSameFeeTypeAndClass_ReturnsValidationProblemDetails()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var scheme = await CreateSchemeAsync(client, UniqueCode("INC"));
        scheme = await AddClassAsync(client, scheme);
        var classId = scheme.Classes.Single().Id;

        var configurationResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/configuration",
            new AddSchemeConfigurationRequest("ForwardPricing", "Accrual"));
        configurationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        scheme = await ReadResponseAsync<SchemeDto>(configurationResponse);

        var riskProfileResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/risk-profiles",
            new AddSchemeRiskProfileRequest("Moderate", 20m));
        riskProfileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        scheme = await ReadResponseAsync<SchemeDto>(riskProfileResponse);

        var liquidityResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/liquidity-thresholds",
            new AddLiquidityThresholdRequest(20m, 15m, 10m));
        liquidityResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        scheme = await ReadResponseAsync<SchemeDto>(liquidityResponse);

        var firstResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/fee-schedules",
            FeeScheduleRequest(classId, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)));
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var overlapResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/fee-schedules",
            FeeScheduleRequest(classId, new DateOnly(2026, 6, 1), new DateOnly(2026, 12, 31)));

        overlapResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        overlapResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var body = await overlapResponse.Content.ReadAsStringAsync();
        body.Should().Contain("effective dates cannot overlap");
    }

    [Fact]
    public async Task SchemeAmendment_WhenActiveSchemeIsUpdated_PreservesVersionHistoryAndRequiresApprovalAgain()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var scheme = await CreateSchemeAsync(client, UniqueCode("AMN"));
        scheme = await AddMinimumActivationDataAsync(client, scheme);
        scheme = await ApproveSchemeWithSeparateUsersAsync(client, scheme);

        await AuthenticateAsBootstrapAdminAsync(client);
        var amendResponse = await client.PutAsJsonAsync(
            $"/api/schemes/{scheme.Id}",
            new UpdateSchemeRequest(
                $"{scheme.Name} Amended",
                scheme.LegalType,
                scheme.BaseCurrency,
                new DateOnly(2026, 7, 1)));

        var amendBody = await amendResponse.Content.ReadAsStringAsync();
        amendResponse.StatusCode.Should().Be(HttpStatusCode.OK, amendBody);
        var amendedScheme = await ReadResponseAsync<SchemeDto>(amendResponse);
        amendedScheme.Status.Should().Be("AmendmentDraft");
        amendedScheme.PendingEffectiveDate.Should().Be(new DateOnly(2026, 7, 1));
        amendedScheme.VersionHistory.Should().NotBeEmpty();
        amendedScheme.VersionHistory.Should().Contain(history => history.ChangeType == "SchemeAmended");
    }

    private async Task<SchemeDto> CreateSchemeAsync(HttpClient client, string code)
    {
        var response = await client.PostAsJsonAsync(
            "/api/schemes",
            new CreateSchemeRequest(code, $"Victory {code} Fund", "Unit Trust", "KES"));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        return await ReadResponseAsync<SchemeDto>(response);
    }

    private async Task<SchemeDto> AddMinimumActivationDataAsync(HttpClient client, SchemeDto scheme)
    {
        scheme = await AddClassAsync(client, scheme);
        var classId = scheme.Classes.Single().Id;

        var configurationResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/configuration",
            new AddSchemeConfigurationRequest("ForwardPricing", "Accrual"));
        configurationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        scheme = await ReadResponseAsync<SchemeDto>(configurationResponse);

        var riskProfileResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/risk-profiles",
            new AddSchemeRiskProfileRequest("Moderate", 20m));
        riskProfileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        scheme = await ReadResponseAsync<SchemeDto>(riskProfileResponse);

        var liquidityResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/liquidity-thresholds",
            new AddLiquidityThresholdRequest(20m, 15m, 10m));
        liquidityResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        scheme = await ReadResponseAsync<SchemeDto>(liquidityResponse);

        var feeResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/fee-schedules",
            FeeScheduleRequest(classId, new DateOnly(2026, 1, 1), null));
        feeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        scheme = await ReadResponseAsync<SchemeDto>(feeResponse);

        var instrumentResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/approved-instruments",
            new AddApprovedInstrumentRuleRequest("TreasuryBill", 365, 20m, 25m, 80m));
        instrumentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        scheme = await ReadResponseAsync<SchemeDto>(instrumentResponse);

        var bankResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/bank-accounts",
            new AddSchemeBankAccountRequest("Victory Bank", $"0100{Random.Shared.Next(100000, 999999)}", $"{scheme.Name} Collection", "KES", "VICBKENA"));
        bankResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        scheme = await ReadResponseAsync<SchemeDto>(bankResponse);

        var custodianResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/custodian-mappings",
            new AddSchemeCustodianMappingRequest("Victory Custody", $"CUST-{Guid.NewGuid():N}"[..20], $"SETT-{Guid.NewGuid():N}"[..20]));
        custodianResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        scheme = await ReadResponseAsync<SchemeDto>(custodianResponse);

        var distributionResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/distribution-rules",
            new AddDistributionRuleRequest("Monthly", true, 25));
        distributionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        scheme = await ReadResponseAsync<SchemeDto>(distributionResponse);

        var templateResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/template-mappings",
            new AddTemplateMappingRequest("Statement", $"STMT-{Guid.NewGuid():N}"[..16]));
        templateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return await ReadResponseAsync<SchemeDto>(templateResponse);
    }

    private async Task<SchemeDto> AddClassAsync(HttpClient client, SchemeDto scheme)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/classes",
            new AddSchemeClassRequest("A", "Class A", "KES", "Daily", "Daily", new TimeOnly(14, 0), 1000m, 500m, 0, 1));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        return await ReadResponseAsync<SchemeDto>(response);
    }

    private static AddFeeScheduleRequest FeeScheduleRequest(Guid classId, DateOnly effectiveFrom, DateOnly? effectiveTo)
    {
        return new AddFeeScheduleRequest(classId, "ManagementFee", "AUM", 1.5m, null, effectiveFrom, effectiveTo, null);
    }

    private async Task<SchemeDto> ApproveSchemeWithSeparateUsersAsync(HttpClient client, SchemeDto scheme)
    {
        var checker = await CreateSystemAdminDirectlyAsync(UniqueEmail("scheme-amend-checker"), "Scheme Amend Checker", "Checker123!");
        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("scheme-amend-approver"), "Scheme Amend Approver", "Approver123!");

        var submitResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/submit",
            new SchemeWorkflowActionRequest("Submit for approval."));
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await AuthenticateAsync(client, checker.Email, checker.Password);
        var checkResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/check",
            new SchemeWorkflowActionRequest("Checked."));
        checkResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync(
            $"/api/schemes/{scheme.Id}/approve",
            new SchemeWorkflowActionRequest("Approved."));
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        return await ReadResponseAsync<SchemeDto>(approveResponse);
    }

    private async Task AuthenticateAsBootstrapAdminAsync(HttpClient client)
    {
        await AuthenticateAsync(client, "admin.tests@victoryfs.local", "ChangeMe123!");
    }

    private static async Task<AuthTokenResponse> AuthenticateAsync(HttpClient client, string email, string password)
    {
        var token = await LoginAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return token;
    }

    private static async Task<AuthTokenResponse> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        return await ReadResponseAsync<AuthTokenResponse>(response);
    }

    private static async Task<UserDto> CreateUserAsync(HttpClient client, string email, string displayName, string password)
    {
        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequest(email, displayName, password));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await ReadResponseAsync<UserDto>(response);
    }

    private async Task<(string Email, string Password)> CreateSystemAdminDirectlyAsync(string email, string displayName, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var role = await dbContext.Roles.SingleAsync(candidate => candidate.Name == RoleNames.SystemAdmin);
        var now = DateTime.UtcNow;
        var user = User.Create(email, displayName, "pending", now);
        user.SetPasswordHash(passwordHasher.HashPassword(user, password), requirePasswordChange: false);
        user.AssignRole(role.Id, "test-fixture", "test-fixture", now);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return (email, password);
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response)
    {
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        apiResponse.Should().NotBeNull();
        return apiResponse!.Data;
    }

    private static string UniqueCode(string prefix)
    {
        return $"{prefix}{Guid.NewGuid():N}"[..12].ToUpperInvariant();
    }

    private static string UniqueEmail(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}@victoryfs.local";
    }
}
