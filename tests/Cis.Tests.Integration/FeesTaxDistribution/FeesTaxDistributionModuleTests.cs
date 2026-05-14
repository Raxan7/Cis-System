using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.FeesTaxDistribution;
using Cis.Contracts.Identity;
using Cis.Domain.Common;
using Cis.Domain.FeesTaxDistribution;
using Cis.Domain.Identity;
using Cis.Domain.Investors;
using Cis.Domain.Schemes;
using Cis.Domain.UnitRegister;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.FeesTaxDistribution;

[Collection(IntegrationTestCollection.Name)]
public sealed class FeesTaxDistributionModuleTests
{
    private readonly CisApiFactory _factory;

    public FeesTaxDistributionModuleTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedFeesEndpoint_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/fees/accrual-runs/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ProtectedFeesEndpoint_WithRoleWithoutPermission_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var portalUser = await CreateUserWithRoleDirectlyAsync(UniqueEmail("fees-portal"), "Fees Portal", "Portal123!", RoleNames.InvestorPortalUser);
        await AuthenticateAsync(client, portalUser.Email, portalUser.Password);

        var response = await client.GetAsync($"/api/fees/accrual-runs/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FeeAccrualRun_UsesSchemeFeeScheduleApprovedWaiverTaxesAndWritesAuditLogs()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var scheme = await CreateActiveSchemeFixtureAsync();
        await CreateTaxRulesAsync(client);

        var waiverResponse = await client.PostAsJsonAsync("/api/fees/waivers", new CreateFeeWaiverRequest(
            scheme.SchemeId,
            scheme.ClassId,
            null,
            "ManagementFee",
            new DateOnly(2026, 1, 1),
            null,
            10m,
            null,
            "Temporary management fee waiver."));
        waiverResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var waiver = await ReadResponseAsync<FeeWaiverRequestDto>(waiverResponse);

        var selfApprove = await client.PostAsJsonAsync($"/api/fees/waivers/{waiver.Id}/approve", new ApproveFeeWaiverRequest("Self approval attempt."));
        selfApprove.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("fee-waiver-approver"), "Fee Waiver Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync($"/api/fees/waivers/{waiver.Id}/approve", new ApproveFeeWaiverRequest("Approved."));
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var runResponse = await client.PostAsJsonAsync("/api/fees/accrual-runs", FeeAccrualRequest(scheme.SchemeId, scheme.ClassId, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31)));
        var runBody = await runResponse.Content.ReadAsStringAsync();
        runResponse.StatusCode.Should().Be(HttpStatusCode.Created, runBody);
        var run = await ReadResponseAsync<FeeAccrualRunDto>(runResponse);

        run.Calculations.Should().Contain(calculation => calculation.FormulaCode == "D-FML-014");
        var periodManagementFee = run.Calculations.Single(calculation => calculation.FormulaCode == "D-FML-015");
        periodManagementFee.AnnualRate.Should().Be(2m);
        periodManagementFee.WaiverAmount.Should().BeGreaterThan(0m);
        run.TotalVatAmount.Should().BeGreaterThan(0m);
        run.TotalWithholdingTaxAmount.Should().BeGreaterThan(0m);
        run.TotalExpenseRatio.Should().BeGreaterThan(0m);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var actions = await dbContext.AuditLogs
            .Where(log => log.Module == "FeesTaxDistribution")
            .Select(log => log.Action)
            .ToListAsync();
        actions.Should().Contain(["FeeWaiverRequested", "FeeWaiverApproved", "FeeAccrualRunCreated"]);
    }

    [Fact]
    public async Task DistributionDeclaration_CoverageFailureRequiresApprovedOverrideAndSeparateApprover()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var scheme = await CreateActiveSchemeFixtureAsync();

        var declarationResponse = await client.PostAsJsonAsync("/api/distributions/declarations", DistributionDeclarationRequest(scheme.SchemeId, scheme.ClassId, new DateOnly(2026, 6, 1), availableCash: 100m, coverageOverrideRequested: false));
        declarationResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var declaration = await ReadResponseAsync<DistributionDeclarationDto>(declarationResponse);
        declaration.DistributionCoverage.Should().BeLessThan(1m);

        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("distribution-approver"), "Distribution Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var failedApproval = await client.PostAsJsonAsync($"/api/distributions/declarations/{declaration.Id}/approve", new ApproveDistributionDeclarationRequest("No override.", false));
        failedApproval.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var failedBody = await failedApproval.Content.ReadAsStringAsync();
        failedBody.Should().Contain("coverage");

        await AuthenticateAsBootstrapAdminAsync(client);
        var overrideResponse = await client.PostAsJsonAsync("/api/distributions/declarations", DistributionDeclarationRequest(scheme.SchemeId, scheme.ClassId, new DateOnly(2026, 7, 1), availableCash: 100m, coverageOverrideRequested: true));
        overrideResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var overrideDeclaration = await ReadResponseAsync<DistributionDeclarationDto>(overrideResponse);

        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveOverride = await client.PostAsJsonAsync($"/api/distributions/declarations/{overrideDeclaration.Id}/approve", new ApproveDistributionDeclarationRequest("Override approved.", true));
        approveOverride.StatusCode.Should().Be(HttpStatusCode.OK);
        var approved = await ReadResponseAsync<DistributionDeclarationDto>(approveOverride);
        approved.Status.Should().Be(DistributionDeclarationStatus.Approved.ToString());
        approved.CoverageOverrideApproved.Should().BeTrue();
    }

    [Fact]
    public async Task DistributionRun_PublishCreatesInvestorTaxRecordsReinvestmentUnitMovementAndAuditLog()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var scheme = await CreateActiveSchemeFixtureAsync();
        var investor = await CreateInvestorFixtureAsync();
        await CreateTaxRulesAsync(client);

        var declarationResponse = await client.PostAsJsonAsync("/api/distributions/declarations", DistributionDeclarationRequest(scheme.SchemeId, scheme.ClassId, new DateOnly(2026, 8, 1), availableCash: 5_000m, coverageOverrideRequested: false));
        declarationResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var declaration = await ReadResponseAsync<DistributionDeclarationDto>(declarationResponse);
        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("distribution-publish-approver"), "Distribution Publish Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveDeclaration = await client.PostAsJsonAsync($"/api/distributions/declarations/{declaration.Id}/approve", new ApproveDistributionDeclarationRequest("Approved.", false));
        approveDeclaration.StatusCode.Should().Be(HttpStatusCode.OK);
        declaration = await ReadResponseAsync<DistributionDeclarationDto>(approveDeclaration);

        await AuthenticateAsBootstrapAdminAsync(client);
        var runResponse = await client.PostAsJsonAsync("/api/distributions/runs", new CreateDistributionRunRequest(
            declaration.Id,
            1.25m,
            [
                new CreateInvestorDistributionRequest(
                    investor.Id,
                    100m,
                    "Reinvest",
                    1_000m,
                    1_100m,
                    25m,
                    50m,
                    "KE",
                    "DISTRIBUTION")
            ]));
        var runBody = await runResponse.Content.ReadAsStringAsync();
        runResponse.StatusCode.Should().Be(HttpStatusCode.Created, runBody);
        var run = await ReadResponseAsync<DistributionRunDto>(runResponse);
        run.InvestorDistributions.Should().ContainSingle();
        run.InvestorDistributions.Single().InvestorTaxAmount.Should().BeGreaterThan(0m);
        run.ReinvestmentAllocations.Should().ContainSingle();

        var selfPublish = await client.PostAsJsonAsync($"/api/distributions/runs/{run.Id}/publish", new PublishDistributionRunRequest("Self publish attempt."));
        selfPublish.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await AuthenticateAsync(client, approver.Email, approver.Password);
        var publishResponse = await client.PostAsJsonAsync($"/api/distributions/runs/{run.Id}/publish", new PublishDistributionRunRequest("Published."));
        var publishBody = await publishResponse.Content.ReadAsStringAsync();
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK, publishBody);
        var published = await ReadResponseAsync<DistributionRunDto>(publishResponse);
        published.Status.Should().Be(DistributionRunStatus.Published.ToString());
        published.ReinvestmentAllocations.Single().UnitLedgerEntryId.Should().NotBeNull();

        var investorDistributionsResponse = await client.GetAsync($"/api/distributions/investor/{investor.Id}");
        investorDistributionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var investorDistributions = await ReadResponseAsync<IReadOnlyCollection<InvestorDistributionDto>>(investorDistributionsResponse);
        investorDistributions.Should().ContainSingle(distribution => distribution.Id == run.InvestorDistributions.Single().Id);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var ledgerEntry = await dbContext.UnitLedgerEntries.SingleOrDefaultAsync(entry => entry.SourceType == UnitMovementSourceType.ReinvestmentInstruction && entry.InvestorId == investor.Id);
        ledgerEntry.Should().NotBeNull();
        ledgerEntry!.MovementType.Should().Be(UnitMovementType.Issued);

        var taxCalculation = await dbContext.TaxCalculations.SingleOrDefaultAsync(calculation => calculation.InvestorDistributionId == run.InvestorDistributions.Single().Id);
        taxCalculation.Should().NotBeNull();
        taxCalculation!.CalculationType.Should().Be(TaxCalculationType.InvestorDistributionTax);

        var actions = await dbContext.AuditLogs.Where(log => log.Module == "FeesTaxDistribution").Select(log => log.Action).ToListAsync();
        actions.Should().Contain(["DistributionDeclarationCreated", "DistributionDeclarationApproved", "DistributionRunCreated", "DistributionRunPublished", "ReinvestmentUnitMovementPosted"]);
    }

    private static CreateFeeAccrualRunRequest FeeAccrualRequest(Guid schemeId, Guid classId, DateOnly periodStart, DateOnly periodEnd)
    {
        return new CreateFeeAccrualRunRequest(
            schemeId,
            classId,
            periodStart,
            periodEnd,
            "FEE-1.0",
            FeeDayCountBasis.Actual365.ToString(),
            30,
            100_000m,
            100_000m,
            99m,
            99m,
            99m,
            99m,
            null,
            10_000m,
            99m,
            null,
            5_000m,
            99m,
            null,
            2_500m,
            99m,
            null,
            120_000m,
            100_000m,
            110_000m,
            99m,
            "KE",
            "FUND");
    }

    private static CreateDistributionDeclarationRequest DistributionDeclarationRequest(Guid schemeId, Guid classId, DateOnly recordDate, decimal availableCash, bool coverageOverrideRequested)
    {
        return new CreateDistributionDeclarationRequest(
            schemeId,
            classId,
            recordDate,
            recordDate,
            recordDate.AddDays(5),
            "DIST-1.0",
            1_000m,
            200m,
            0m,
            0m,
            100m,
            50m,
            25m,
            25m,
            1_000m,
            availableCash,
            coverageOverrideRequested,
            coverageOverrideRequested ? "Board-approved liquidity override." : null);
    }

    private async Task CreateTaxRulesAsync(HttpClient client)
    {
        var vatResponse = await client.PostAsJsonAsync("/api/taxes/rules", new CreateTaxRuleRequest("KE", "FUND", "VAT", 16m, new DateOnly(2026, 1, 1), null));
        vatResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict);
        var whtResponse = await client.PostAsJsonAsync("/api/taxes/rules", new CreateTaxRuleRequest("KE", "FUND", "WHT", 5m, new DateOnly(2026, 1, 1), null));
        whtResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict);
        var distributionWhtResponse = await client.PostAsJsonAsync("/api/taxes/rules", new CreateTaxRuleRequest("KE", "DISTRIBUTION", "WHT", 10m, new DateOnly(2026, 1, 1), null));
        distributionWhtResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict);
    }

    private async Task<(Guid SchemeId, Guid ClassId)> CreateActiveSchemeFixtureAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;
        var code = $"FTD{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        var scheme = Scheme.Create(code, $"Fees Tax Distribution Scheme {code}", "Unit Trust", "KES", "fixture-maker", now);
        var schemeClass = scheme.AddClass("A", "Class A", "KES", SchemeFrequency.Daily, SchemeFrequency.Daily, new TimeOnly(14, 0), 1000m, 500m, 0, 1);
        scheme.AddConfiguration("ForwardPricing", "Accrual");
        scheme.AddFeeSchedule(schemeClass.Id, "ManagementFee", "AverageNAV", 2m, null, BusinessDate.From(new DateOnly(2026, 1, 1)), null, []);
        scheme.AddFeeSchedule(schemeClass.Id, "CustodyFee", "AUM", 0.4m, null, BusinessDate.From(new DateOnly(2026, 1, 1)), null, []);
        scheme.AddFeeSchedule(schemeClass.Id, "TrusteeFee", "AUM", 0.2m, null, BusinessDate.From(new DateOnly(2026, 1, 1)), null, []);
        scheme.AddFeeSchedule(schemeClass.Id, "AdminFee", "AUM", 0.3m, null, BusinessDate.From(new DateOnly(2026, 1, 1)), null, []);
        scheme.AddFeeSchedule(schemeClass.Id, "EntryFee", "Contribution", 1m, null, BusinessDate.From(new DateOnly(2026, 1, 1)), null, []);
        scheme.AddFeeSchedule(schemeClass.Id, "ExitFee", "Redemption", 2m, null, BusinessDate.From(new DateOnly(2026, 1, 1)), null, []);
        scheme.AddFeeSchedule(schemeClass.Id, "SwitchFee", "Switch", 0.5m, null, BusinessDate.From(new DateOnly(2026, 1, 1)), null, []);
        scheme.AddFeeSchedule(schemeClass.Id, "PerformanceFee", "HighWaterMark", 20m, null, BusinessDate.From(new DateOnly(2026, 1, 1)), null, []);
        scheme.AddApprovedInstrumentRule("TreasuryBill", 365, 20m, 25m, 80m);
        scheme.AddBankAccount("Victory Bank", $"0400{Random.Shared.Next(100000, 999999)}", "Collection", "KES", "VICBKENA");
        scheme.Submit("fixture-maker", now);
        scheme.Check("fixture-checker", now.AddMinutes(1));
        scheme.Approve("fixture-approver", now.AddMinutes(2));
        dbContext.Schemes.Add(scheme);
        await dbContext.SaveChangesAsync();
        return (scheme.Id, schemeClass.Id);
    }

    private async Task<(Guid Id, string InvestorNumber)> CreateInvestorFixtureAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;
        var investor = Investor.Create($"INV-FTD-{Guid.NewGuid():N}"[..30], InvestorType.Individual, "Approved Distribution Investor", UniqueEmail("distribution-investor"), $"+2547{Random.Shared.Next(10000000, 99999999)}", "fixture-maker", now);
        investor.AttachIndividualProfile("Diana", "Distribution", $"ID-{Guid.NewGuid():N}"[..20], new DateOnly(1990, 1, 1), "Kenyan");
        investor.AddDefaultKycRequirements(["NationalId", "TaxCertificate", "ProofOfAddress"]);
        investor.SetTaxProfile($"TAX-{Guid.NewGuid():N}"[..20], "Kenya");
        investor.AssignRiskClassification(InvestorRiskCategory.Low, "Fixture risk.", "fixture-maker", now);
        foreach (var documentType in new[] { "NationalId", "TaxCertificate", "ProofOfAddress" })
        {
            investor.AddDocument(documentType, $"{documentType}.pdf", "application/pdf", 2000, $"fixture/{Guid.NewGuid():N}.pdf", BusinessDate.From(new DateOnly(2026, 1, 1)), BusinessDate.From(new DateOnly(2030, 1, 1)), "fixture-maker", now, BusinessDate.From(new DateOnly(2026, 5, 12)));
        }

        investor.AddAmlScreeningCase("Fixture", $"AML-{Guid.NewGuid():N}"[..20], [], now);
        investor.SubmitKyc("fixture-maker", now);
        investor.Approve("fixture-approver", now.AddMinutes(1), BusinessDate.From(new DateOnly(2026, 5, 12)), "Fixture approved.");
        dbContext.Investors.Add(investor);
        await dbContext.SaveChangesAsync();
        return (investor.Id, investor.InvestorNumber);
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
