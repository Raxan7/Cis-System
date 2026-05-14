using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.DataQuality;
using Cis.Contracts.Identity;
using Cis.Domain.Accounting;
using Cis.Domain.Cash;
using Cis.Domain.Common;
using Cis.Domain.CustodyReconciliation;
using Cis.Domain.DataQuality;
using Cis.Domain.Dealing;
using Cis.Domain.Identity;
using Cis.Domain.Investors;
using Cis.Domain.NAV;
using Cis.Domain.Portfolio;
using Cis.Domain.Reports;
using Cis.Domain.Schemes;
using Cis.Domain.UnitRegister;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.DataQuality;

[Collection(IntegrationTestCollection.Name)]
public sealed class DataQualityModuleTests
{
    private readonly CisApiFactory _factory;

    public DataQualityModuleTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DataQualityEndpoint_WithoutToken_ReturnsUnauthorizedProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/data-quality/exceptions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task DataQualityEndpoint_WithRoleWithoutPermission_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var portalUser = await CreateUserWithRoleDirectlyAsync(UniqueEmail("dq-portal"), "DQ Portal", "Portal123!", RoleNames.InvestorPortalUser);
        await AuthenticateAsync(client, portalUser.Email, portalUser.Password);

        var response = await client.GetAsync("/api/data-quality/exceptions");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CheckRun_GeneratesExceptionsForAllConfiguredRules_AndDashboardSummarizesOpenOverdue()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        await SeedRuleSourceDataAsync();

        var runResponse = await client.PostAsync("/api/data-quality/check-runs", null);
        var runBody = await runResponse.Content.ReadAsStringAsync();
        runResponse.StatusCode.Should().Be(HttpStatusCode.Created, runBody);
        var run = await ReadResponseAsync<DataQualityCheckRunDto>(runResponse);
        run.Status.Should().Be(DataQualityRunStatus.Completed.ToString());
        run.RulesEvaluated.Should().BeGreaterThanOrEqualTo(11);
        run.ExceptionsGenerated.Should().BeGreaterThanOrEqualTo(11);

        var exceptionsResponse = await client.GetAsync("/api/data-quality/exceptions");
        exceptionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var exceptions = await ReadResponseAsync<IReadOnlyCollection<DataQualityExceptionDto>>(exceptionsResponse);
        var ruleCodes = exceptions.Select(exception => exception.RuleCode).ToHashSet();
        ruleCodes.Should().Contain(Enum.GetNames<DataQualityRuleCode>());

        var exceptionToResolve = exceptions.First(exception => exception.Status == DataQualityExceptionStatus.Open.ToString());
        var assignResponse = await client.PostAsJsonAsync($"/api/data-quality/exceptions/{exceptionToResolve.Id}/assign", new AssignDataQualityExceptionRequest("dq-owner-001"));
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var assigned = await ReadResponseAsync<DataQualityExceptionDto>(assignResponse);
        assigned.Status.Should().Be(DataQualityExceptionStatus.Assigned.ToString());
        assigned.OwnerUserId.Should().Be("dq-owner-001");

        var invalidResolve = await client.PostAsJsonAsync($"/api/data-quality/exceptions/{exceptionToResolve.Id}/resolve", new ResolveDataQualityExceptionRequest(string.Empty, "No evidence."));
        invalidResolve.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var resolveResponse = await client.PostAsJsonAsync($"/api/data-quality/exceptions/{exceptionToResolve.Id}/resolve", new ResolveDataQualityExceptionRequest("evidence/data-quality/resolution.pdf", "Corrected and evidenced."));
        resolveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var resolved = await ReadResponseAsync<DataQualityExceptionDto>(resolveResponse);
        resolved.Status.Should().Be(DataQualityExceptionStatus.Resolved.ToString());
        resolved.ResolutionEvidenceReference.Should().Be("evidence/data-quality/resolution.pdf");

        var dashboardResponse = await client.GetAsync("/api/data-quality/dashboard");
        dashboardResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dashboard = await ReadResponseAsync<DataQualityDashboardDto>(dashboardResponse);
        dashboard.OpenExceptions.Should().BeGreaterThan(0);
        dashboard.OverdueExceptions.Should().BeGreaterThan(0);
        dashboard.ResolvedExceptions.Should().BeGreaterThan(0);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await dbContext.ExceptionAssignments.AnyAsync(item => item.DataQualityExceptionId == exceptionToResolve.Id)).Should().BeTrue();
        (await dbContext.ExceptionQueue.AnyAsync(item => item.DataQualityExceptionId == exceptionToResolve.Id && item.Status == DataQualityExceptionStatus.Resolved)).Should().BeTrue();
        var auditActions = await dbContext.AuditLogs.Where(log => log.Module == "DataQuality").Select(log => log.Action).ToListAsync();
        auditActions.Should().Contain(["DataQualityCheckRunCompleted", "DataQualityExceptionAssigned", "DataQualityExceptionResolved"]);
    }

    [Fact]
    public async Task RuleCreation_ValidatesDuplicateRuleCodes_AndWritesAuditLog()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var code = $"Rule{Guid.NewGuid():N}"[..20];

        var invalid = await client.PostAsJsonAsync("/api/data-quality/rules", new CreateDataQualityRuleRequest("NotARule", "Bad rule", "Bad code", "High", 0));
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var first = await client.PostAsJsonAsync("/api/data-quality/rules", new CreateDataQualityRuleRequest(
            DataQualityRuleCode.MissingRequiredInvestorFields.ToString(),
            $"Duplicate probe {code}",
            "Should be rejected because the seeded rule code already exists.",
            DataQualitySeverity.High.ToString(),
            1));
        first.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await dbContext.DataQualityRules.AnyAsync(rule => rule.Code == DataQualityRuleCode.MissingRequiredInvestorFields)).Should().BeTrue();
    }

    private async Task SeedRuleSourceDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;

        var investorMissing = Investor.Create($"DQ-MISS-{Guid.NewGuid():N}"[..30], InvestorType.Individual, "Missing Profile Investor", UniqueEmail("dq-missing"), "254700000001", "fixture", now);
        var investorWithKyc = Investor.Create($"DQ-KYC-{Guid.NewGuid():N}"[..30], InvestorType.Individual, "Expired KYC Investor", UniqueEmail("dq-kyc"), "254700000002", "fixture", now);
        investorWithKyc.AttachIndividualProfile("Expired", "KYC", $"ID-{Guid.NewGuid():N}"[..20], new DateOnly(1980, 1, 1), "KE");
        investorWithKyc.AddDefaultKycRequirements(["NationalId"]);
        investorWithKyc.AddDocument("NationalId", "id.pdf", "application/pdf", 100, "kyc/id.pdf", null, BusinessDate.From(DateOnly.FromDateTime(now.AddDays(-1))), "fixture", now, BusinessDate.From(now));

        var investorDuplicateA = Investor.Create($"DQ-DUPA-{Guid.NewGuid():N}"[..30], InvestorType.Individual, "Duplicate A", UniqueEmail("dq-dupa"), "254700000003", "fixture", now);
        var investorDuplicateB = Investor.Create($"DQ-DUPB-{Guid.NewGuid():N}"[..30], InvestorType.Individual, "Duplicate B", UniqueEmail("dq-dupb"), "254700000004", "fixture", now);
        investorDuplicateA.AttachIndividualProfile("Duplicate", "A", $"DUP-{Guid.NewGuid():N}"[..20], new DateOnly(1981, 1, 1), "KE");
        investorDuplicateB.AttachIndividualProfile("Duplicate", "B", $"DUP-{Guid.NewGuid():N}"[..20], new DateOnly(1982, 1, 1), "KE");
        investorDuplicateA.AddDuplicateWarning("IdentityNumber", "DUP-ID", investorDuplicateB.Id, investorDuplicateB.InvestorNumber, now);

        var scheme = Scheme.Create($"DQ-SCH-{Guid.NewGuid():N}"[..30], "Data Quality Scheme", "Unit Trust", "KES", "fixture", now);
        var schemeClass = scheme.AddClass("A", "Class A", "KES", SchemeFrequency.Daily, SchemeFrequency.Daily, new TimeOnly(14, 0), 1000m, 500m, 0, 1);
        scheme.AddConfiguration("ForwardPricing", "Accrual");
        scheme.AddBankAccount("Victory Bank", $"0999{Random.Shared.Next(100000, 999999)}", "Collection", "KES", "VICBKENA");

        var bankImport = BankStatementImport.Create(Guid.NewGuid(), "dq-bank.csv", DateOnly.FromDateTime(now), 0, $"dq-{Guid.NewGuid():N}", Guid.NewGuid().ToString("N"), "fixture", now, []);
        var bankLine = BankStatementLine.Create(bankImport.Id, Guid.NewGuid(), 1, DateOnly.FromDateTime(now), $"DQ-REC-{Guid.NewGuid():N}"[..30], "Unmatched receipt", 1000m, BankStatementLineDirection.Credit, null, scheme.Id, schemeClass.Id);

        var holding = UnitHolding.Create(investorWithKyc.Id, scheme.Id, schemeClass.Id, 4, "fixture", now);
        SetPrivate(holding, "Units", -1m);

        var dealing = DealingInstruction.CreateSubscription($"DQ-SUB-{Guid.NewGuid():N}"[..30], investorWithKyc.Id, scheme.Id, schemeClass.Id, DealingChannel.Operations, BusinessDate.From(now), now, DealingInstructionMode.Amount, 1000m, null, "KES", true, true, 10m, BusinessDate.From(now), "fixture", now);

        var instrument = Instrument.Create($"DQ{Guid.NewGuid():N}"[..12], "DQ Instrument", InstrumentType.TreasuryBill, "KES", "fixture", now);
        var valuationRun = ValuationRun.Create(scheme.Id, schemeClass.Id, BusinessDate.From(now), "D-FML-001", DayCountBasis.Actual365, 4, 3, 5m, false, "fixture", now);
        var stalePrice = StalePriceException.Create(valuationRun.Id, instrument.Id, BusinessDate.From(DateOnly.FromDateTime(now.AddDays(-10))), BusinessDate.From(now), 3, false, "Price is stale.");

        var chart = ChartOfAccounts.Create(scheme.Id, $"DQCOA{Random.Shared.Next(1000, 9999)}", "DQ Chart", "KES", "fixture", now);
        var debit = chart.AddAccount($"DQA{Random.Shared.Next(1000, 9999)}", "Cash", AccountType.Asset, false);
        var credit = chart.AddAccount($"DQL{Random.Shared.Next(1000, 9999)}", "Payable", AccountType.Liability, false);
        var ledger = Ledger.Create(scheme.Id, schemeClass.Id, chart.Id, $"DQLDG{Random.Shared.Next(1000, 9999)}", "DQ Ledger", "KES", "fixture", now);
        var period = AccountingPeriod.Create(scheme.Id, "DQ Period", BusinessDate.From(new DateOnly(2026, 1, 1)), BusinessDate.From(new DateOnly(2026, 12, 31)), "fixture", now);
        var journal = Journal.CreateManual(scheme.Id, schemeClass.Id, ledger.Id, period.Id, BusinessDate.From(now), "KES", "Unbalanced DQ journal", false, false, null, "fixture", now);
        journal.AddLine(debit.Id, "Debit", 100m, 0m);
        journal.AddLine(credit.Id, "Credit", 0m, 90m);

        var custodian = Custodian.Create($"DQC{Random.Shared.Next(1000, 9999)}", "DQ Custodian", null, "fixture", now);
        var custodyRun = CustodyReconciliationRun.Create(custodian.Id, null, null, BusinessDate.From(now), "{}", "fixture", now);
        var holdingBreak = HoldingsReconciliationBreak.Create(custodyRun.Id, null, scheme.Id, schemeClass.Id, $"INS{Random.Shared.Next(1000, 9999)}", 10m, 9m, 100m, 90m, ReconciliationBreakType.QuantityMismatch, ReconciliationBreakSeverity.High, "fixture", now.AddDays(-10));
        var definition = await dbContext.ReportDefinitions.FirstAsync(cancellationToken: default);
        var reportRun = ReportRun.Create(definition.Id, definition.Code, BusinessDate.From(now), now, "fixture", now);
        SetPrivate(reportRun, "Status", ReportRunStatus.Published);
        SetPrivate(reportRun, "PublishedByUserId", "fixture");
        SetPrivate(reportRun, "PublishedAtUtc", now);

        var overlappingOne = FeeSchedule.Create(scheme.Id, schemeClass.Id, "Management", "NAV", 0.01m, null, BusinessDate.From(new DateOnly(2026, 1, 1)), BusinessDate.From(new DateOnly(2026, 6, 30)));
        var overlappingTwo = FeeSchedule.Create(scheme.Id, schemeClass.Id, "Management", "NAV", 0.02m, null, BusinessDate.From(new DateOnly(2026, 3, 1)), BusinessDate.From(new DateOnly(2026, 12, 31)));

        dbContext.Investors.AddRange(investorMissing, investorWithKyc, investorDuplicateA, investorDuplicateB);
        dbContext.Schemes.Add(scheme);
        dbContext.BankStatementImports.Add(bankImport);
        dbContext.BankStatementLines.Add(bankLine);
        dbContext.UnitHoldings.Add(holding);
        dbContext.DealingInstructions.Add(dealing);
        dbContext.Instruments.Add(instrument);
        dbContext.ValuationRuns.Add(valuationRun);
        dbContext.StalePriceExceptions.Add(stalePrice);
        dbContext.ChartOfAccounts.Add(chart);
        dbContext.Ledgers.Add(ledger);
        dbContext.AccountingPeriods.Add(period);
        dbContext.Journals.Add(journal);
        dbContext.Custodians.Add(custodian);
        dbContext.CustodyReconciliationRuns.Add(custodyRun);
        dbContext.HoldingsReconciliationBreaks.Add(holdingBreak);
        dbContext.ReportRuns.Add(reportRun);
        dbContext.FeeSchedules.AddRange(overlappingOne, overlappingTwo);
        await dbContext.SaveChangesAsync();
    }

    private static void SetPrivate<T>(object instance, string propertyName, T value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property {propertyName} was not found.");
        property.SetValue(instance, value);
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
