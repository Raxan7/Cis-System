using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.ComplianceRisk;
using Cis.Contracts.Identity;
using Cis.Domain.ComplianceRisk;
using Cis.Domain.Identity;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.ComplianceRisk;

[Collection(IntegrationTestCollection.Name)]
public sealed class ComplianceRiskModuleTests
{
    private readonly CisApiFactory _factory;

    public ComplianceRiskModuleTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedRiskEndpoint_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/risk/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ProtectedRiskEndpoint_WithRoleWithoutPermission_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var portalUser = await CreateUserWithRoleDirectlyAsync(UniqueEmail("risk-portal"), "Risk Portal", "Portal123!", RoleNames.InvestorPortalUser);
        await AuthenticateAsync(client, portalUser.Email, portalUser.Password);

        var response = await client.GetAsync("/api/risk/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LimitRun_AutomaticallyLogsBreachesSourceDataExposuresAndAuditLog()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);

        var response = await client.PostAsJsonAsync("/api/compliance/limit-runs", LimitRunRequest("auto-breach"));
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
        var run = await ReadResponseAsync<LimitCheckRunDto>(response);

        run.AssetsUnderManagement.Should().Be(3_000m);
        run.NetFlow.Should().Be(325m);
        run.AverageNav.Should().Be(1_100m);
        run.ExpenseToAumRatio.Should().Be(0.02m);
        run.AnnualizedYield.Should().Be(0.272727m);
        run.Breaches.Should().HaveCount(3);
        run.Breaches.Should().Contain(breach => breach.LimitType == LimitType.Counterparty.ToString() && breach.Status == BreachStatus.Open.ToString());
        run.RelatedPartyExposures.Should().ContainSingle();
        run.RelatedPartyExposures.Single().ExposurePercentOfAum.Should().Be(10m);
        run.CounterpartyUsages.Should().ContainSingle();
        run.CounterpartyUsages.Single().UsagePercent.Should().Be(120m);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var statutoryLimits = await dbContext.StatutoryLimits.CountAsync(limit => limit.Reference.Contains("auto-breach"));
        var internalLimits = await dbContext.InternalPolicyLimits.CountAsync(limit => limit.Reference.Contains("auto-breach"));
        statutoryLimits.Should().BeGreaterThan(0);
        internalLimits.Should().BeGreaterThan(0);
        var actions = await dbContext.AuditLogs.Where(log => log.Module == "ComplianceRisk").Select(log => log.Action).ToListAsync();
        actions.Should().Contain("LimitCheckRunCreated");
        actions.Should().Contain("LimitBreachLogged");
    }

    [Fact]
    public async Task BreachWorkflow_RequiresRemediationEvidenceAndSeparateClosureApprover()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var runResponse = await client.PostAsJsonAsync("/api/compliance/limit-runs", LimitRunRequest("workflow"));
        runResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var run = await ReadResponseAsync<LimitCheckRunDto>(runResponse);
        var breach = run.Breaches.First();

        var prematureClose = await client.PostAsJsonAsync($"/api/compliance/breaches/{breach.Id}/close", new CloseBreachRequest("evidence/early.pdf"));
        prematureClose.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var assignResponse = await client.PostAsJsonAsync($"/api/compliance/breaches/{breach.Id}/assign", new AssignBreachRequest("risk-owner", "Assign for remediation."));
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var assigned = await ReadResponseAsync<LimitBreachDto>(assignResponse);
        assigned.Status.Should().Be(BreachStatus.Assigned.ToString());

        var remediateResponse = await client.PostAsJsonAsync($"/api/compliance/breaches/{breach.Id}/remediate", new RemediateBreachRequest("Reduce exposure below policy limit.", "evidence/remediation.pdf"));
        remediateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var remediated = await ReadResponseAsync<LimitBreachDto>(remediateResponse);
        remediated.Status.Should().Be(BreachStatus.PendingClosureApproval.ToString());

        var selfClose = await client.PostAsJsonAsync($"/api/compliance/breaches/{breach.Id}/close", new CloseBreachRequest("evidence/closure.pdf"));
        selfClose.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("breach-closure"), "Breach Closure", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var closeResponse = await client.PostAsJsonAsync($"/api/compliance/breaches/{breach.Id}/close", new CloseBreachRequest("evidence/closure.pdf"));
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var closed = await ReadResponseAsync<LimitBreachDto>(closeResponse);
        closed.Status.Should().Be(BreachStatus.Closed.ToString());
        closed.ClosureEvidenceReference.Should().Be("evidence/closure.pdf");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var remediation = await dbContext.RemediationActions.SingleOrDefaultAsync(action => action.LimitBreachId == breach.Id);
        remediation.Should().NotBeNull();
        var exceptionRegister = await dbContext.BreachExceptionRegisters.SingleOrDefaultAsync(record => record.LimitBreachId == breach.Id);
        exceptionRegister.Should().NotBeNull();
        var actions = await dbContext.AuditLogs.Where(log => log.Module == "ComplianceRisk" && log.EntityId == breach.Id.ToString()).Select(log => log.Action).ToListAsync();
        actions.Should().Contain(["LimitBreachAssigned", "LimitBreachRemediationSubmitted", "LimitBreachClosed"]);
    }

    [Fact]
    public async Task LiquidityStressLiquidationAndDashboard_AreCalculatedAndPersisted()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);

        var limitRunResponse = await client.PostAsJsonAsync("/api/compliance/limit-runs", LimitRunRequest("dashboard"));
        limitRunResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var liquidityResponse = await client.PostAsJsonAsync("/api/risk/liquidity-coverage-runs", new CreateLiquidityCoverageRunRequest(
            null,
            null,
            new DateOnly(2026, 9, 1),
            "RISK-1.0",
            500m,
            250m,
            null));
        liquidityResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var liquidity = await ReadResponseAsync<LiquidityCoverageRunDto>(liquidityResponse);
        liquidity.LiquidityCoverageRatio.Should().Be(2m);

        var scenarioResponse = await client.PostAsJsonAsync("/api/risk/stress-scenarios", new CreateStressScenarioRequest(
            null,
            $"Heavy Redemption {Guid.NewGuid():N}",
            800m,
            "{\"shock\":\"40pct_redemption\"}"));
        scenarioResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var scenario = await ReadResponseAsync<RedemptionStressScenarioDto>(scenarioResponse);

        var stressRunResponse = await client.PostAsJsonAsync("/api/risk/stress-test-runs", new CreateStressTestRunRequest(
            scenario.Id,
            new DateOnly(2026, 9, 1),
            "RISK-1.0",
            400m,
            null));
        stressRunResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var stressRun = await ReadResponseAsync<RedemptionStressTestRunDto>(stressRunResponse);
        stressRun.StressCoverage.Should().Be(0.5m);

        var liquidationResponse = await client.PostAsJsonAsync("/api/risk/liquidation-time-analysis", new CreateLiquidationTimeAnalysisRequest(
            null,
            null,
            new DateOnly(2026, 9, 1),
            "RISK-1.0",
            [
                new WeightedMaturityInputRequest(600m, 30),
                new WeightedMaturityInputRequest(400m, 90)
            ],
            "{\"liquidationAssumption\":\"sell_shortest_first\"}"));
        liquidationResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var liquidation = await ReadResponseAsync<LiquidationTimeAnalysisRunDto>(liquidationResponse);
        liquidation.WeightedAverageMaturityDays.Should().Be(54m);

        var dashboardResponse = await client.GetAsync("/api/risk/dashboard");
        dashboardResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dashboard = await ReadResponseAsync<RiskDashboardSnapshotDto>(dashboardResponse);
        dashboard.AssetsUnderManagement.Should().Be(3_000m);
        dashboard.LatestLiquidityCoverageRatio.Should().Be(2m);
        dashboard.LatestStressCoverage.Should().Be(0.5m);
        dashboard.OpenBreaches.Should().BeGreaterThan(0);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await dbContext.LiquidityCoverageRuns.AnyAsync()).Should().BeTrue();
        (await dbContext.RedemptionStressTestRuns.AnyAsync()).Should().BeTrue();
        (await dbContext.LiquidationTimeAnalysisRuns.AnyAsync()).Should().BeTrue();
        (await dbContext.RiskDashboardSnapshots.AnyAsync()).Should().BeTrue();
    }

    private static CreateLimitCheckRunRequest LimitRunRequest(string suffix)
    {
        return new CreateLimitCheckRunRequest(
            null,
            null,
            new DateOnly(2026, 9, 1),
            "RISK-1.0",
            [
                new NavSourceRequest(Guid.NewGuid(), Guid.NewGuid(), 1_000m),
                new NavSourceRequest(Guid.NewGuid(), Guid.NewGuid(), 2_000m)
            ],
            500m,
            50m,
            200m,
            25m,
            [1_000m, 1_100m, 1_200m],
            22m,
            25m,
            12m,
            [
                new LimitRuleRequest("Statutory", "Counterparty", $"CP-{suffix}", 100m, 120m, "Counterparty exposure exceeds statutory limit."),
                new LimitRuleRequest("InternalPolicy", "Issuer", $"ISS-{suffix}", 200m, 350m, "Issuer exposure exceeds internal policy limit."),
                new LimitRuleRequest("Statutory", "Tenor", $"TENOR-{suffix}", 365m, 500m, "Instrument tenor exceeds statutory tenor rule."),
                new LimitRuleRequest("InternalPolicy", "AssetClass", $"ASSET-{suffix}", 500m, 450m, "Asset class within limit.")
            ],
            [
                new RelatedPartyExposureRequest($"Related Party {suffix}", 300m, $"related/{suffix}.csv")
            ],
            [
                new CounterpartyUsageRequest($"Counterparty {suffix}", 120m, 100m, $"counterparty/{suffix}.csv")
            ]);
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

    private async Task<(Guid Id, string Email, string Password)> CreateSystemAdminDirectlyAsync(string email, string displayName, string password)
    {
        return await CreateUserWithRoleDirectlyAsync(email, displayName, password, RoleNames.SystemAdmin);
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
