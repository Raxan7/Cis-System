using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Dealing;
using Cis.Contracts.Identity;
using Cis.Domain.Common;
using Cis.Domain.Dealing;
using Cis.Domain.Identity;
using Cis.Domain.Investors;
using Cis.Domain.Schemes;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.Dealing;

[Collection(IntegrationTestCollection.Name)]
public sealed class DealingModuleTests
{
    private readonly CisApiFactory _factory;

    public DealingModuleTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedDealingEndpoint_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/dealing/pending");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ProtectedDealingEndpoint_WithAuthenticatedUserMissingPermission_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var createdUser = await CreateUserAsync(client, UniqueEmail("dealing-no-permission"), "Dealing No Permission", "NoPermission123!");
        var token = await LoginAsync(client, createdUser.Email, "NoPermission123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var response = await client.GetAsync("/api/dealing/pending");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Subscription_ForUnapprovedInvestor_ReturnsValidationProblemDetails()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateInvestorFixtureAsync(approved: false);
        var scheme = await CreateActiveSchemeFixtureAsync();

        var response = await client.PostAsJsonAsync("/api/dealing/subscriptions", SubscriptionRequest(investor.Id, scheme.SchemeId, scheme.ClassId, fundsCleared: true, approvedNav: true));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("unapproved investor");
    }

    [Fact]
    public async Task Subscription_WithClearedFundsAndApprovedNav_AllocatesUnitsAndWritesAuditAndStatusHistory()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateInvestorFixtureAsync(approved: true);
        var scheme = await CreateActiveSchemeFixtureAsync();

        var createResponse = await client.PostAsJsonAsync("/api/dealing/subscriptions", SubscriptionRequest(investor.Id, scheme.SchemeId, scheme.ClassId, fundsCleared: true, approvedNav: true));
        var createBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, createBody);
        var instruction = await ReadResponseAsync<DealingInstructionDto>(createResponse);
        instruction.Status.Should().Be("Draft");
        instruction.StatusHistory.Should().Contain(history => history.Status == "Draft");

        var submitResponse = await client.PostAsJsonAsync($"/api/dealing/instructions/{instruction.Id}/submit", new DealingInstructionActionRequest("Submit subscription."));
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        instruction = await ReadResponseAsync<DealingInstructionDto>(submitResponse);
        instruction.Status.Should().Be("PendingApproval");

        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("dealing-approver"), "Dealing Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync($"/api/dealing/instructions/{instruction.Id}/approve", new DealingInstructionActionRequest("Approve subscription."));
        var approveBody = await approveResponse.Content.ReadAsStringAsync();
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK, approveBody);
        var approved = await ReadResponseAsync<DealingInstructionDto>(approveResponse);

        approved.Status.Should().Be("Allocated");
        approved.Subscriptions.Should().ContainSingle().Which.AllocatedUnits.Should().Be(100m);
        approved.Subscriptions.Single().ConfirmationNumber.Should().NotBeNullOrWhiteSpace();
        approved.StatusHistory.Select(history => history.Status).Should().Contain(["Draft", "Submitted", "PendingApproval", "Approved", "Allocated"]);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var actions = await dbContext.AuditLogs
            .Where(auditLog => auditLog.EntityId == instruction.Id.ToString())
            .Select(auditLog => auditLog.Action)
            .ToListAsync();
        actions.Should().Contain("SubscriptionInstructionCreated");
        actions.Should().Contain("DealingInstructionSubmitted");
        actions.Should().Contain("DealingInstructionApproved");
    }

    [Fact]
    public async Task Subscription_WithUnclearedFunds_CannotBeAllocated()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateInvestorFixtureAsync(approved: true);
        var scheme = await CreateActiveSchemeFixtureAsync();
        var createResponse = await client.PostAsJsonAsync("/api/dealing/subscriptions", SubscriptionRequest(investor.Id, scheme.SchemeId, scheme.ClassId, fundsCleared: false, approvedNav: true));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var instruction = await ReadResponseAsync<DealingInstructionDto>(createResponse);

        var submitResponse = await client.PostAsJsonAsync($"/api/dealing/instructions/{instruction.Id}/submit", new DealingInstructionActionRequest("Submit pending funds."));
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        instruction = await ReadResponseAsync<DealingInstructionDto>(submitResponse);
        instruction.Status.Should().Be("PendingFunds");

        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("pending-funds-approver"), "Pending Funds Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync($"/api/dealing/instructions/{instruction.Id}/approve", new DealingInstructionActionRequest("Try approve pending funds."));

        approveResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await approveResponse.Content.ReadAsStringAsync();
        body.Should().Contain("funds are cleared");
    }

    [Fact]
    public async Task Subscription_WithoutApprovedNav_CannotBeAllocated()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateInvestorFixtureAsync(approved: true);
        var scheme = await CreateActiveSchemeFixtureAsync();
        var createResponse = await client.PostAsJsonAsync("/api/dealing/subscriptions", SubscriptionRequest(investor.Id, scheme.SchemeId, scheme.ClassId, fundsCleared: true, approvedNav: false));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var instruction = await ReadResponseAsync<DealingInstructionDto>(createResponse);
        var submitResponse = await client.PostAsJsonAsync($"/api/dealing/instructions/{instruction.Id}/submit", new DealingInstructionActionRequest("Submit no NAV."));
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("no-nav-approver"), "No NAV Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync($"/api/dealing/instructions/{instruction.Id}/approve", new DealingInstructionActionRequest("Try approve no NAV."));

        approveResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await approveResponse.Content.ReadAsStringAsync();
        body.Should().Contain("approved NAV");
    }

    [Fact]
    public async Task Redemption_WhenLienReducesRedeemableBalance_ReturnsValidationProblemDetails()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateInvestorFixtureAsync(approved: true);
        var scheme = await CreateActiveSchemeFixtureAsync();
        var lienResponse = await client.PostAsJsonAsync("/api/dealing/liens", new CreateLienRequest(investor.Id, scheme.SchemeId, scheme.ClassId, 75m, null, "KES", "DOC-001", "APPROVER-EVIDENCE-001"));
        lienResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var redemptionResponse = await client.PostAsJsonAsync("/api/dealing/redemptions", RedemptionRequest(investor.Id, scheme.SchemeId, scheme.ClassId, amount: null, units: 50m, availableUnits: 100m));

        redemptionResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await redemptionResponse.Content.ReadAsStringAsync();
        body.Should().Contain("redeemable balance");
    }

    [Fact]
    public async Task LargeRedemption_RequiresApprovalThresholdWorkflowAndGeneratesAdvice()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateInvestorFixtureAsync(approved: true);
        var scheme = await CreateActiveSchemeFixtureAsync();

        var createResponse = await client.PostAsJsonAsync("/api/dealing/redemptions", RedemptionRequest(investor.Id, scheme.SchemeId, scheme.ClassId, amount: 1_200_000m, units: null, availableUnits: 20_000m));
        var createBody = await createResponse.Content.ReadAsStringAsync();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, createBody);
        var instruction = await ReadResponseAsync<DealingInstructionDto>(createResponse);
        instruction.Redemptions.Should().ContainSingle().Which.RequiresApprovalThreshold.Should().BeTrue();
        instruction.ValidationResults.Should().Contain(result => result.RuleCode == "LargeRedemptionThreshold");

        var submitResponse = await client.PostAsJsonAsync($"/api/dealing/instructions/{instruction.Id}/submit", new DealingInstructionActionRequest("Submit large redemption."));
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("large-redemption-approver"), "Large Redemption Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync($"/api/dealing/instructions/{instruction.Id}/approve", new DealingInstructionActionRequest("Approve large redemption."));
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var approved = await ReadResponseAsync<DealingInstructionDto>(approveResponse);

        approved.Status.Should().Be("Settled");
        approved.Redemptions.Single().PayoutAuthorized.Should().BeTrue();
        approved.Redemptions.Single().RedemptionAdviceNumber.Should().NotBeNullOrWhiteSpace();
        approved.Redemptions.Single().NetPayoutAmount.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task CutOffBreach_IsLoggedAndRequiresApproval()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateInvestorFixtureAsync(approved: true);
        var scheme = await CreateActiveSchemeFixtureAsync(cutOffTime: new TimeOnly(12, 0));
        var receivedAtUtc = DateTime.SpecifyKind(new DateTime(2026, 5, 12, 15, 0, 0), DateTimeKind.Utc);

        var createResponse = await client.PostAsJsonAsync("/api/dealing/subscriptions", SubscriptionRequest(investor.Id, scheme.SchemeId, scheme.ClassId, fundsCleared: true, approvedNav: true, receivedAtUtc: receivedAtUtc));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var instruction = await ReadResponseAsync<DealingInstructionDto>(createResponse);
        instruction.CutOffBreaches.Should().ContainSingle().Which.RequiresApproval.Should().BeTrue();

        var breachesResponse = await client.GetAsync("/api/dealing/cutoff-breaches");
        breachesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var breaches = await ReadResponseAsync<IReadOnlyCollection<CutOffBreachDto>>(breachesResponse);
        breaches.Should().Contain(breach => breach.Reason.Contains("cut-off", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SwitchTransferLienReleaseAndRecurringPlan_CreateRealRecordsAndAuditLogs()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateInvestorFixtureAsync(approved: true);
        var targetInvestor = await CreateInvestorFixtureAsync(approved: true);
        var sourceScheme = await CreateActiveSchemeFixtureAsync();
        var targetScheme = await CreateActiveSchemeFixtureAsync();

        var switchResponse = await client.PostAsJsonAsync("/api/dealing/switches", new CreateSwitchRequest(
            investor.Id,
            sourceScheme.SchemeId,
            sourceScheme.ClassId,
            targetScheme.SchemeId,
            targetScheme.ClassId,
            "Operations",
            new DateOnly(2026, 5, 12),
            DateTime.SpecifyKind(new DateTime(2026, 5, 12, 10, 0, 0), DateTimeKind.Utc),
            "Units",
            null,
            10m,
            15m,
            "{\"source\":\"test\"}"));
        switchResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var transferResponse = await client.PostAsJsonAsync("/api/dealing/transfers", new CreateTransferRequest(
            investor.Id,
            targetInvestor.Id,
            sourceScheme.SchemeId,
            sourceScheme.ClassId,
            "Branch",
            new DateOnly(2026, 5, 12),
            DateTime.SpecifyKind(new DateTime(2026, 5, 12, 10, 30, 0), DateTimeKind.Utc),
            5m,
            "{\"from\":\"test\",\"to\":\"test\"}"));
        transferResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var lienResponse = await client.PostAsJsonAsync("/api/dealing/liens", new CreateLienRequest(investor.Id, sourceScheme.SchemeId, sourceScheme.ClassId, 2m, null, "KES", "LIEN-DOC-001", "LIEN-APPROVAL-001"));
        lienResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var lien = await ReadResponseAsync<LienDto>(lienResponse);
        var releaseResponse = await client.PostAsJsonAsync($"/api/dealing/liens/{lien.Id}/release", new ReleaseLienRequest("LIEN-RELEASE-001"));
        releaseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var releasedLien = await ReadResponseAsync<LienDto>(releaseResponse);
        releasedLien.Status.Should().Be("Released");

        var planResponse = await client.PostAsJsonAsync("/api/dealing/recurring-plans", new CreateRecurringContributionPlanRequest(
            investor.Id,
            sourceScheme.SchemeId,
            sourceScheme.ClassId,
            "Monthly",
            5000m,
            "KES",
            "InvestorPortal",
            new DateOnly(2026, 6, 1),
            null,
            "StandingInstruction"));
        planResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var plan = await ReadResponseAsync<RecurringContributionPlanDto>(planResponse);
        plan.Status.Should().Be("Active");

        var amendResponse = await client.PutAsJsonAsync($"/api/dealing/recurring-plans/{plan.Id}", new AmendRecurringContributionPlanRequest(
            "Quarterly",
            7500m,
            "Operations",
            new DateOnly(2026, 7, 1),
            null,
            "Payroll"));
        amendResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        plan = await ReadResponseAsync<RecurringContributionPlanDto>(amendResponse);
        plan.Frequency.Should().Be("Quarterly");
        plan.Amount.Should().Be(7500m);

        var failedDebitResponse = await client.PostAsync($"/api/dealing/recurring-plans/{plan.Id}/failed-debit", null);
        failedDebitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        plan = await ReadResponseAsync<RecurringContributionPlanDto>(failedDebitResponse);
        plan.FailedDebits.Should().Be(1);

        var missedCollectionResponse = await client.PostAsync($"/api/dealing/recurring-plans/{plan.Id}/missed-collection", null);
        missedCollectionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        plan = await ReadResponseAsync<RecurringContributionPlanDto>(missedCollectionResponse);
        plan.MissedCollections.Should().Be(1);

        var pauseResponse = await client.PostAsync($"/api/dealing/recurring-plans/{plan.Id}/pause", null);
        pauseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        plan = await ReadResponseAsync<RecurringContributionPlanDto>(pauseResponse);
        plan.Status.Should().Be("Paused");

        var resumeResponse = await client.PostAsync($"/api/dealing/recurring-plans/{plan.Id}/resume", null);
        resumeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        plan = await ReadResponseAsync<RecurringContributionPlanDto>(resumeResponse);
        plan.Status.Should().Be("Active");

        var cancelResponse = await client.PostAsync($"/api/dealing/recurring-plans/{plan.Id}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        plan = await ReadResponseAsync<RecurringContributionPlanDto>(cancelResponse);
        plan.Status.Should().Be("Cancelled");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var actions = await dbContext.AuditLogs
            .Where(auditLog => auditLog.Module == "Dealing")
            .Select(auditLog => auditLog.Action)
            .ToListAsync();
        actions.Should().Contain("SwitchInstructionCreated");
        actions.Should().Contain("TransferInstructionCreated");
        actions.Should().Contain("LienPlaced");
        actions.Should().Contain("LienReleased");
        actions.Should().Contain("RecurringContributionPlanCreated");
        actions.Should().Contain("RecurringContributionPlanAmended");
        actions.Should().Contain("RecurringContributionPlanFailedDebitRegistered");
        actions.Should().Contain("RecurringContributionPlanMissedCollectionRegistered");
        actions.Should().Contain("RecurringContributionPlanPaused");
        actions.Should().Contain("RecurringContributionPlanResumed");
        actions.Should().Contain("RecurringContributionPlanCancelled");
    }

    private static CreateSubscriptionRequest SubscriptionRequest(Guid investorId, Guid schemeId, Guid classId, bool fundsCleared, bool approvedNav, DateTime? receivedAtUtc = null)
    {
        return new CreateSubscriptionRequest(
            investorId,
            schemeId,
            classId,
            "Branch",
            new DateOnly(2026, 5, 12),
            receivedAtUtc ?? DateTime.SpecifyKind(new DateTime(2026, 5, 12, 10, 0, 0), DateTimeKind.Utc),
            "Amount",
            10_000m,
            null,
            "KES",
            fundsCleared,
            approvedNav,
            approvedNav ? 100m : null,
            approvedNav ? new DateOnly(2026, 5, 12) : null);
    }

    private static CreateRedemptionRequest RedemptionRequest(Guid investorId, Guid schemeId, Guid classId, decimal? amount, decimal? units, decimal availableUnits)
    {
        return new CreateRedemptionRequest(
            investorId,
            schemeId,
            classId,
            "Operations",
            new DateOnly(2026, 5, 12),
            DateTime.SpecifyKind(new DateTime(2026, 5, 12, 10, 0, 0), DateTimeKind.Utc),
            amount.HasValue ? "Amount" : "Units",
            amount,
            units,
            false,
            availableUnits,
            0m,
            5m,
            0,
            1,
            true,
            100m,
            new DateOnly(2026, 5, 12),
            1m,
            5m,
            "KES");
    }

    private async Task<(Guid Id, string InvestorNumber)> CreateInvestorFixtureAsync(bool approved)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;
        var investor = Investor.Create($"INV-TST-{Guid.NewGuid():N}"[..30], InvestorType.Individual, "Approved Dealing Investor", UniqueEmail("dealing-investor"), $"+2547{Random.Shared.Next(10000000, 99999999)}", "fixture-maker", now);
        investor.AttachIndividualProfile("Jane", "Dealing", $"ID-{Guid.NewGuid():N}"[..20], new DateOnly(1990, 1, 1), "Kenyan");
        investor.AddDefaultKycRequirements(["NationalId", "TaxCertificate", "ProofOfAddress"]);
        investor.SetTaxProfile($"TAX-{Guid.NewGuid():N}"[..20], "Kenya");
        investor.AssignRiskClassification(InvestorRiskCategory.Low, "Fixture risk.", "fixture-maker", now);
        if (approved)
        {
            foreach (var documentType in new[] { "NationalId", "TaxCertificate", "ProofOfAddress" })
            {
                investor.AddDocument(
                    documentType,
                    $"{documentType}.pdf",
                    "application/pdf",
                    2000,
                    $"fixture/{Guid.NewGuid():N}.pdf",
                    BusinessDate.From(new DateOnly(2026, 1, 1)),
                    BusinessDate.From(new DateOnly(2030, 1, 1)),
                    "fixture-maker",
                    now,
                    BusinessDate.From(new DateOnly(2026, 5, 12)));
            }

            investor.AddAmlScreeningCase("Fixture", $"AML-{Guid.NewGuid():N}"[..20], [], now);
            investor.SubmitKyc("fixture-maker", now);
            investor.Approve("fixture-approver", now.AddMinutes(1), BusinessDate.From(new DateOnly(2026, 5, 12)), "Fixture approved.");
        }

        dbContext.Investors.Add(investor);
        await dbContext.SaveChangesAsync();
        return (investor.Id, investor.InvestorNumber);
    }

    private async Task<(Guid SchemeId, Guid ClassId)> CreateActiveSchemeFixtureAsync(TimeOnly? cutOffTime = null)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;
        var code = $"DS{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        var scheme = Scheme.Create(code, $"Dealing Scheme {code}", "Unit Trust", "KES", "fixture-maker", now);
        var schemeClass = scheme.AddClass("A", "Class A", "KES", SchemeFrequency.Daily, SchemeFrequency.Daily, cutOffTime ?? new TimeOnly(14, 0), 1000m, 500m, 0, 1);
        scheme.AddConfiguration("ForwardPricing", "Accrual");
        scheme.AddFeeSchedule(schemeClass.Id, "ManagementFee", "AUM", 1m, null, BusinessDate.From(new DateOnly(2026, 1, 1)), null, []);
        scheme.AddApprovedInstrumentRule("TreasuryBill", 365, 20m, 25m, 80m);
        scheme.AddBankAccount("Victory Bank", $"0100{Random.Shared.Next(100000, 999999)}", "Collection", "KES", "VICBKENA");
        scheme.Submit("fixture-maker", now);
        scheme.Check("fixture-checker", now.AddMinutes(1));
        scheme.Approve("fixture-approver", now.AddMinutes(2));
        dbContext.Schemes.Add(scheme);
        await dbContext.SaveChangesAsync();
        return (scheme.Id, schemeClass.Id);
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

    private static string UniqueEmail(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}@victoryfs.local";
    }
}
