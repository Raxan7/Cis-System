using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Accounting;
using Cis.Contracts.Identity;
using Cis.Domain.Accounting;
using Cis.Domain.Schemes;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.Accounting;

[Collection(IntegrationTestCollection.Name)]
public sealed class AccountingModuleTests
{
    private static readonly DateOnly PostingDate = new(2026, 5, 12);
    private readonly CisApiFactory _factory;

    public AccountingModuleTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedAccountingEndpoint_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/accounting/trial-balance?schemeId={Guid.NewGuid()}&asOfDate={PostingDate:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task AccountingEndpoint_WithRoleWithoutPermission_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var portalUser = await CreateUserWithRoleDirectlyAsync(UniqueEmail("accounting-portal"), "Portal User", "Portal123!", RoleNames.InvestorPortalUser);
        await AuthenticateAsync(client, portalUser.Email, portalUser.Password);

        var response = await client.GetAsync($"/api/accounting/journal-register?schemeId={Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateChartOfAccounts_WithValidAccounts_CreatesLedgerPeriodTemplatesAndAuditLog()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var scheme = await CreateActiveSchemeFixtureAsync();

        var response = await client.PostAsJsonAsync("/api/accounting/chart-of-accounts", ChartRequest(scheme.SchemeId, scheme.ClassId));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var chart = await ReadResponseAsync<ChartOfAccountsDto>(response);
        chart.Accounts.Should().HaveCount(6);
        chart.Ledger.SchemeId.Should().Be(scheme.SchemeId);
        chart.InitialPeriod.Status.Should().Be(AccountingPeriodStatus.Open.ToString());
        chart.JournalTemplates.Should().Contain(template => template.JournalType == AutomatedJournalType.Subscription.ToString());

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var audit = await dbContext.AuditLogs.FirstOrDefaultAsync(log => log.EntityId == chart.Id.ToString() && log.Action == "ChartOfAccountsCreated");
        audit.Should().NotBeNull();
    }

    [Fact]
    public async Task ManualJournal_WithUnbalancedLines_IsRejected()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var fixture = await CreateAccountingFixtureAsync(client);
        var cash = fixture.Account("1000");
        var equity = fixture.Account("3000");

        var response = await client.PostAsJsonAsync("/api/accounting/journals/manual", new CreateManualJournalRequest(
            fixture.SchemeId,
            fixture.ClassId,
            PostingDate,
            "KES",
            "Unbalanced journal",
            false,
            null,
            [
                new CreateJournalLineRequest(cash.Id, "Debit cash", 100m, 0m),
                new CreateJournalLineRequest(equity.Id, "Credit units", 0m, 90m)
            ]));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("balance");
    }

    [Fact]
    public async Task ManualJournal_CanBeSubmittedApprovedPostedAndIncludedInTrialBalance()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var fixture = await CreateAccountingFixtureAsync(client);
        var cash = fixture.Account("1000");
        var equity = fixture.Account("3000");

        var created = await CreateBalancedManualJournalAsync(client, fixture.SchemeId, fixture.ClassId, cash.Id, equity.Id, 250m);
        created.Status.Should().Be(JournalStatus.Draft.ToString());

        var submitResponse = await client.PostAsJsonAsync($"/api/accounting/journals/{created.Id}/submit", new JournalWorkflowActionRequest("Submitted."));
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var selfApproveResponse = await client.PostAsJsonAsync($"/api/accounting/journals/{created.Id}/approve", new JournalWorkflowActionRequest("Self approve."));
        selfApproveResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("accounting-approver"), "Accounting Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveResponse = await client.PostAsJsonAsync($"/api/accounting/journals/{created.Id}/approve", new JournalWorkflowActionRequest("Approved."));
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var posted = await ReadResponseAsync<JournalDto>(approveResponse);
        posted.Status.Should().Be(JournalStatus.Posted.ToString());
        posted.TotalDebits.Should().Be(250m);
        posted.TotalCredits.Should().Be(250m);

        var ledgerResponse = await client.GetAsync($"/api/accounting/general-ledger?schemeId={fixture.SchemeId}&schemeClassId={fixture.ClassId}");
        ledgerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var ledger = await ReadResponseAsync<GeneralLedgerDto>(ledgerResponse);
        ledger.Entries.Should().HaveCount(2);
        ledger.TotalDebits.Should().Be(250m);
        ledger.TotalCredits.Should().Be(250m);

        var trialBalanceResponse = await client.GetAsync($"/api/accounting/trial-balance?schemeId={fixture.SchemeId}&schemeClassId={fixture.ClassId}&asOfDate={PostingDate:yyyy-MM-dd}");
        trialBalanceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var trialBalance = await ReadResponseAsync<TrialBalanceDto>(trialBalanceResponse);
        trialBalance.IsBalanced.Should().BeTrue();
        trialBalance.TotalDebits.Should().Be(250m);
        trialBalance.TotalCredits.Should().Be(250m);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var actions = await dbContext.AuditLogs.Where(log => log.EntityId == created.Id.ToString()).Select(log => log.Action).ToListAsync();
        actions.Should().Contain(["ManualJournalCreated", "JournalSubmitted", "JournalApprovedAndPosted"]);
    }

    [Fact]
    public async Task ClosedPeriod_BlocksNormalPostingButAllowsApprovedCorrectionJournal()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var fixture = await CreateAccountingFixtureAsync(client);
        var cash = fixture.Account("1000");
        var equity = fixture.Account("3000");
        var expense = fixture.Account("5000");
        var payable = fixture.Account("2000");

        var original = await CreateAndApproveManualJournalAsync(client, fixture, cash.Id, equity.Id, 100m, "original");
        var closeResponse = await client.PostAsJsonAsync($"/api/accounting/periods/{fixture.PeriodId}/close", new JournalWorkflowActionRequest("Close period."));
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await AuthenticateAsBootstrapAdminAsync(client);
        var blocked = await CreateBalancedManualJournalAsync(client, fixture.SchemeId, fixture.ClassId, cash.Id, equity.Id, 50m);
        var submitBlocked = await client.PostAsJsonAsync($"/api/accounting/journals/{blocked.Id}/submit", new JournalWorkflowActionRequest("Submit blocked."));
        submitBlocked.StatusCode.Should().Be(HttpStatusCode.OK);
        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("closed-period-approver"), "Closed Period Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var blockedApprove = await client.PostAsJsonAsync($"/api/accounting/journals/{blocked.Id}/approve", new JournalWorkflowActionRequest("Should fail."));
        blockedApprove.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await blockedApprove.Content.ReadAsStringAsync();
        body.Should().Contain("Closed period");

        await AuthenticateAsBootstrapAdminAsync(client);
        var correctionResponse = await client.PostAsJsonAsync("/api/accounting/journals/manual", new CreateManualJournalRequest(
            fixture.SchemeId,
            fixture.ClassId,
            PostingDate,
            "KES",
            "Correction journal",
            true,
            original.Id,
            [
                new CreateJournalLineRequest(expense.Id, "Debit correction expense", 10m, 0m),
                new CreateJournalLineRequest(payable.Id, "Credit correction payable", 0m, 10m)
            ]));
        correctionResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var correction = await ReadResponseAsync<JournalDto>(correctionResponse);
        var submitCorrection = await client.PostAsJsonAsync($"/api/accounting/journals/{correction.Id}/submit", new JournalWorkflowActionRequest("Submit correction."));
        submitCorrection.StatusCode.Should().Be(HttpStatusCode.OK);
        var correctionApprover = await CreateSystemAdminDirectlyAsync(UniqueEmail("correction-approver"), "Correction Approver", "Approver123!");
        await AuthenticateAsync(client, correctionApprover.Email, correctionApprover.Password);
        var approveCorrection = await client.PostAsJsonAsync($"/api/accounting/journals/{correction.Id}/approve", new JournalWorkflowActionRequest("Approve correction."));
        approveCorrection.StatusCode.Should().Be(HttpStatusCode.OK);
        var postedCorrection = await ReadResponseAsync<JournalDto>(approveCorrection);
        postedCorrection.Status.Should().Be(JournalStatus.Posted.ToString());
        postedCorrection.CorrectedJournalId.Should().Be(original.Id);
    }

    [Fact]
    public async Task AutomatedJournal_IsPostedAndLinkedToOriginatingBusinessEvent()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var fixture = await CreateAccountingFixtureAsync(client);
        var cash = fixture.Account("1000");
        var income = fixture.Account("4000");
        var eventId = Guid.NewGuid().ToString();

        var response = await client.PostAsJsonAsync("/api/accounting/journals/automated", new CreateAutomatedJournalRequest(
            fixture.SchemeId,
            fixture.ClassId,
            PostingDate,
            "KES",
            AutomatedJournalType.IncomeReceipt.ToString(),
            "Income receipt",
            "IncomeReceipt",
            eventId,
            [
                new CreateJournalLineRequest(cash.Id, "Debit cash", 75m, 0m),
                new CreateJournalLineRequest(income.Id, "Credit income", 0m, 75m)
            ]));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var journal = await ReadResponseAsync<JournalDto>(response);
        journal.Source.Should().Be(JournalSource.Automated.ToString());
        journal.Status.Should().Be(JournalStatus.Posted.ToString());
        journal.OriginatingEventType.Should().Be("IncomeReceipt");
        journal.OriginatingEventId.Should().Be(eventId);

        var ledgerResponse = await client.GetAsync($"/api/accounting/general-ledger?schemeId={fixture.SchemeId}&schemeClassId={fixture.ClassId}");
        var ledger = await ReadResponseAsync<GeneralLedgerDto>(ledgerResponse);
        ledger.Entries.Should().Contain(entry => entry.OriginatingEventType == "IncomeReceipt" && entry.OriginatingEventId == eventId);
    }

    [Fact]
    public async Task NavReconciliation_ComparesAccountingNavToPublishedNavAndWritesAudit()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var fixture = await CreateAccountingFixtureAsync(client);
        var cash = fixture.Account("1000");
        var payable = fixture.Account("2000");
        await CreateAndApproveManualJournalAsync(client, fixture, cash.Id, payable.Id, 300m, "nav-recon");

        var response = await client.GetAsync($"/api/accounting/nav-reconciliation?schemeId={fixture.SchemeId}&schemeClassId={fixture.ClassId}&valuationDate={PostingDate:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reconciliation = await ReadResponseAsync<AccountingNavReconciliationDto>(response);
        reconciliation.AccountingNav.Should().Be(0m);
        reconciliation.Status.Should().Be(AccountingNavReconciliationStatus.PendingPublishedNav.ToString());

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var audit = await dbContext.AuditLogs.FirstOrDefaultAsync(log => log.EntityId == reconciliation.Id.ToString() && log.Action == "AccountingNavReconciliationGenerated");
        audit.Should().NotBeNull();
    }

    private async Task<JournalDto> CreateAndApproveManualJournalAsync(HttpClient client, AccountingFixture fixture, Guid debitAccountId, Guid creditAccountId, decimal amount, string suffix)
    {
        await AuthenticateAsBootstrapAdminAsync(client);
        var journal = await CreateBalancedManualJournalAsync(client, fixture.SchemeId, fixture.ClassId, debitAccountId, creditAccountId, amount);
        var submit = await client.PostAsJsonAsync($"/api/accounting/journals/{journal.Id}/submit", new JournalWorkflowActionRequest($"Submit {suffix}."));
        submit.StatusCode.Should().Be(HttpStatusCode.OK);
        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail($"accounting-{suffix}-approver"), $"Accounting {suffix} Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approve = await client.PostAsJsonAsync($"/api/accounting/journals/{journal.Id}/approve", new JournalWorkflowActionRequest($"Approve {suffix}."));
        approve.StatusCode.Should().Be(HttpStatusCode.OK);
        return await ReadResponseAsync<JournalDto>(approve);
    }

    private static async Task<JournalDto> CreateBalancedManualJournalAsync(HttpClient client, Guid schemeId, Guid classId, Guid debitAccountId, Guid creditAccountId, decimal amount)
    {
        var response = await client.PostAsJsonAsync("/api/accounting/journals/manual", new CreateManualJournalRequest(
            schemeId,
            classId,
            PostingDate,
            "KES",
            "Balanced manual journal",
            false,
            null,
            [
                new CreateJournalLineRequest(debitAccountId, "Debit line", amount, 0m),
                new CreateJournalLineRequest(creditAccountId, "Credit line", 0m, amount)
            ]));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await ReadResponseAsync<JournalDto>(response);
    }

    private async Task<AccountingFixture> CreateAccountingFixtureAsync(HttpClient client)
    {
        var scheme = await CreateActiveSchemeFixtureAsync();
        var response = await client.PostAsJsonAsync("/api/accounting/chart-of-accounts", ChartRequest(scheme.SchemeId, scheme.ClassId));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var chart = await ReadResponseAsync<ChartOfAccountsDto>(response);
        return new AccountingFixture(scheme.SchemeId, scheme.ClassId, chart.InitialPeriod.Id, chart.Accounts);
    }

    private static CreateChartOfAccountsRequest ChartRequest(Guid schemeId, Guid classId)
    {
        return new CreateChartOfAccountsRequest(
            schemeId,
            classId,
            $"COA{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            "Main chart of accounts",
            "KES",
            "FY2026",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 12, 31),
            [
                new AccountDefinitionRequest("1000", "Cash and bank", AccountType.Asset.ToString(), true),
                new AccountDefinitionRequest("1100", "Investments", AccountType.Asset.ToString(), true),
                new AccountDefinitionRequest("2000", "Accounts payable", AccountType.Liability.ToString(), true),
                new AccountDefinitionRequest("3000", "Unit holder capital", AccountType.Equity.ToString(), true),
                new AccountDefinitionRequest("4000", "Investment income", AccountType.Income.ToString(), false),
                new AccountDefinitionRequest("5000", "Fund expenses", AccountType.Expense.ToString(), false)
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
        var passwordHasher = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.IPasswordHasher<Cis.Domain.Identity.User>>();
        var role = await dbContext.Roles.SingleAsync(role => role.Name == roleName);
        var now = DateTime.UtcNow;
        var user = Cis.Domain.Identity.User.Create(email, displayName, "pending", now);
        user.SetPasswordHash(passwordHasher.HashPassword(user, password), requirePasswordChange: false);
        user.AssignRole(role.Id, "test-fixture", "test-fixture", now);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return (user.Id, email, password);
    }

    private async Task<(Guid SchemeId, Guid ClassId)> CreateActiveSchemeFixtureAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;
        var code = $"ACC{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        var scheme = Cis.Domain.Schemes.Scheme.Create(code, $"Accounting Scheme {code}", "Unit Trust", "KES", "fixture-maker", now);
        var schemeClass = scheme.AddClass("A", "Class A", "KES", SchemeFrequency.Daily, SchemeFrequency.Daily, new TimeOnly(14, 0), 1000m, 500m, 0, 1);
        scheme.AddConfiguration("ForwardPricing", "Accrual");
        scheme.AddFeeSchedule(schemeClass.Id, "ManagementFee", "AUM", 1m, null, Cis.Domain.Common.BusinessDate.From(new DateOnly(2026, 1, 1)), null, []);
        scheme.AddApprovedInstrumentRule("TreasuryBill", 365, 20m, 25m, 80m);
        scheme.AddBankAccount("Victory Bank", $"0300{Random.Shared.Next(100000, 999999)}", "Collection", "KES", "VICBKENA");
        scheme.Submit("fixture-maker", now);
        scheme.Check("fixture-checker", now.AddMinutes(1));
        scheme.Approve("fixture-approver", now.AddMinutes(2));
        dbContext.Schemes.Add(scheme);
        await dbContext.SaveChangesAsync();
        return (scheme.Id, schemeClass.Id);
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response)
    {
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        apiResponse.Should().NotBeNull();
        return apiResponse!.Data;
    }

    private static string UniqueEmail(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}@test.local";
    }

    private sealed record AccountingFixture(Guid SchemeId, Guid ClassId, Guid PeriodId, IReadOnlyCollection<AccountDto> Accounts)
    {
        public AccountDto Account(string code)
        {
            return Accounts.Single(account => account.Code == code);
        }
    }
}
