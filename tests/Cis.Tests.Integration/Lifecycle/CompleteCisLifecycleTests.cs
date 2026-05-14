using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Accounting;
using Cis.Contracts.Cash;
using Cis.Contracts.ComplianceRisk;
using Cis.Contracts.CustodyReconciliation;
using Cis.Contracts.Dealing;
using Cis.Contracts.FeesTaxDistribution;
using Cis.Contracts.Identity;
using Cis.Contracts.Integrations;
using Cis.Contracts.Investors;
using Cis.Contracts.NAV;
using Cis.Contracts.Portfolio;
using Cis.Contracts.Reports;
using Cis.Contracts.Schemes;
using Cis.Contracts.UnitRegister;
using Cis.Domain.Accounting;
using Cis.Domain.Cash;
using Cis.Domain.ComplianceRisk;
using Cis.Domain.Common;
using Cis.Domain.CustodyReconciliation;
using Cis.Domain.Dealing;
using Cis.Domain.FeesTaxDistribution;
using Cis.Domain.Identity;
using Cis.Domain.Investors;
using Cis.Domain.NAV;
using Cis.Domain.Reports;
using Cis.Domain.Schemes;
using Cis.Domain.UnitRegister;
using Cis.Infrastructure.Persistence;
using Cis.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Tests.Integration.Lifecycle;

[Collection(IntegrationTestCollection.Name)]
public sealed class CompleteCisLifecycleTests
{
    private static readonly DateOnly LifecycleDate = new(2026, 5, 12);
    private readonly CisApiFactory _factory;

    public CompleteCisLifecycleTests(CisApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Scenario1_SchemeSetup_CompletesMakerCheckerApprovalAndAudits()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);

        var scheme = await CreateSchemeThroughApiAsync(client, UniqueCode("E2ESCH"));
        scheme = await AddFullSchemeActivationDataAsync(client, scheme);
        var checker = await CreateSystemAdminDirectlyAsync(UniqueEmail("e2e-scheme-checker"), "E2E Scheme Checker", "Checker123!");
        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("e2e-scheme-approver"), "E2E Scheme Approver", "Approver123!");

        var submit = await client.PostAsJsonAsync($"/api/schemes/{scheme.Id}/submit", new SchemeWorkflowActionRequest("Submit E2E scheme."));
        submit.StatusCode.Should().Be(HttpStatusCode.OK);
        await AuthenticateAsync(client, checker.Email, checker.Password);
        var check = await client.PostAsJsonAsync($"/api/schemes/{scheme.Id}/check", new SchemeWorkflowActionRequest("Checked E2E scheme."));
        check.StatusCode.Should().Be(HttpStatusCode.OK);
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approve = await client.PostAsJsonAsync($"/api/schemes/{scheme.Id}/approve", new SchemeWorkflowActionRequest("Approved E2E scheme."));

        var approveBody = await approve.Content.ReadAsStringAsync();
        approve.StatusCode.Should().Be(HttpStatusCode.OK, approveBody);
        var active = await ReadResponseAsync<SchemeDto>(approve);
        active.Status.Should().Be("Active");
        active.VersionHistory.Should().NotBeEmpty();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var actions = await db.AuditLogs.Where(log => log.Module == "Schemes" && log.EntityId == scheme.Id.ToString()).Select(log => log.Action).ToListAsync();
        actions.Should().Contain(["SchemeCreated", "SchemeClassAdded", "FeeScheduleAdded", "SchemeSubmitted", "SchemeChecked", "SchemeApproved"]);
        (await db.SchemeVersionHistory.AnyAsync(history => history.SchemeId == scheme.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task Scenario2_InvestorOnboarding_CompletesKycAmlApprovalAndAudits()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);

        var investor = await CreateInvestorThroughApiAsync(client, UniqueEmail("e2e-investor"), UniqueIdentity());
        investor = await AddRequiredKycDocumentsAsync(client, investor);

        var duplicateResponse = await client.PostAsJsonAsync("/api/investors", IndividualInvestorRequest(UniqueEmail("e2e-duplicate"), investor.IndividualProfiles.Single().IdentityNumber));
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var duplicate = await ReadResponseAsync<InvestorDto>(duplicateResponse);
        duplicate.DuplicateDetectionResults.Should().NotBeEmpty();

        var aml = await client.PostAsJsonAsync($"/api/investors/{investor.Id}/aml-screening", new StartAmlScreeningRequest("ManualStub", null, []));
        aml.StatusCode.Should().Be(HttpStatusCode.OK);
        var submit = await client.PostAsync($"/api/investors/{investor.Id}/submit-kyc", null);
        submit.StatusCode.Should().Be(HttpStatusCode.OK);

        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("e2e-investor-approver"), "E2E Investor Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approve = await client.PostAsJsonAsync($"/api/investors/{investor.Id}/approve", new InvestorWorkflowDecisionRequest("Approved E2E investor."));
        var approveBody = await approve.Content.ReadAsStringAsync();
        approve.StatusCode.Should().Be(HttpStatusCode.OK, approveBody);
        var approved = await ReadResponseAsync<InvestorDto>(approve);
        approved.Status.Should().Be("Approved");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var actions = await db.AuditLogs.Where(log => log.EntityId == investor.Id.ToString()).Select(log => log.Action).ToListAsync();
        actions.Should().Contain(["InvestorCreated", "KycDocumentUploaded", "AmlScreeningPerformed", "KycSubmitted", "InvestorApproved"]);
    }

    [Fact]
    public async Task Scenario3_SubscriptionToUnitAllocation_ClearsCashAllocatesUnitsPostsJournalAndAudits()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateApprovedInvestorFixtureAsync("subscription");
        var scheme = await CreateActiveSchemeFixtureAsync("SUB");

        var create = await client.PostAsJsonAsync("/api/dealing/subscriptions", SubscriptionRequest(investor.Id, scheme.SchemeId, scheme.ClassId, fundsCleared: false, approvedNav: true));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var instruction = await ReadResponseAsync<DealingInstructionDto>(create);
        var submit = await client.PostAsJsonAsync($"/api/dealing/instructions/{instruction.Id}/submit", new DealingInstructionActionRequest("Submit subscription pending cash."));
        submit.StatusCode.Should().Be(HttpStatusCode.OK);
        instruction = await ReadResponseAsync<DealingInstructionDto>(submit);
        instruction.Status.Should().Be("PendingFunds");

        var csv = BankCsv(instruction.InstructionNumber, investor.Id, scheme.SchemeId, scheme.ClassId, 10_000m, "Credit");
        var import = await client.PostAsJsonAsync("/api/cash/bank-statements/import", new ImportBankStatementRequest(scheme.BankAccountId, LifecycleDate, 2, "subscription-cash.csv", csv));
        var importBody = await import.Content.ReadAsStringAsync();
        import.StatusCode.Should().Be(HttpStatusCode.Created, importBody);
        var imported = await ReadResponseAsync<BankStatementImportDto>(import);
        imported.MatchedLineCount.Should().Be(1);

        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("e2e-sub-approver"), "E2E Subscription Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approve = await client.PostAsJsonAsync($"/api/dealing/instructions/{instruction.Id}/approve", new DealingInstructionActionRequest("Approve cash-cleared subscription."));
        approve.StatusCode.Should().Be(HttpStatusCode.OK);
        var allocated = await ReadResponseAsync<DealingInstructionDto>(approve);
        allocated.Status.Should().Be("Allocated");
        allocated.Subscriptions.Single().AllocatedUnits.Should().Be(100m);
        allocated.Subscriptions.Single().ConfirmationNumber.Should().NotBeNullOrWhiteSpace();

        var units = await CreateApprovedUnitAdjustmentAsync(client, investor.Id, scheme.SchemeId, scheme.ClassId, 100m, "Post allocated subscription units.");
        var chart = await CreateAccountingFixtureAsync(client, scheme);
        await CreateAutomatedJournalAsync(client, chart, AutomatedJournalType.Subscription.ToString(), "DealingInstruction", allocated.Id.ToString(), 10_000m, "Subscription allocation journal.");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await db.UnitHoldings.AnyAsync(holding => holding.InvestorId == investor.Id && holding.Units == 100m)).Should().BeTrue();
        (await db.CashBookEntries.AnyAsync(entry => entry.SourceType == CashBookEntrySourceType.BankStatement && entry.SourceEntityId == imported.Lines.Single().Id)).Should().BeTrue();
        (await db.Journals.AnyAsync(journal => journal.OriginatingEventType == "DealingInstruction" && journal.OriginatingEventId == allocated.Id.ToString())).Should().BeTrue();
        var actions = await db.AuditLogs.Select(log => log.Action).ToListAsync();
        actions.Should().Contain(["CashBankStatementImported", "DealingInstructionApproved", "UnitAdjustmentApproved", "UnitMovementPosted", "AutomatedJournalPosted"]);
        units.Status.Should().Be("Approved");
    }

    [Fact]
    public async Task Scenario4_RedemptionPayout_SettlesPaymentReducesUnitsPostsJournalAndAudits()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var investor = await CreateApprovedInvestorFixtureAsync("redemption");
        var scheme = await CreateActiveSchemeFixtureAsync("RED");
        await CreateApprovedUnitAdjustmentAsync(client, investor.Id, scheme.SchemeId, scheme.ClassId, 500m, "Opening holding.");

        var create = await client.PostAsJsonAsync("/api/dealing/redemptions", RedemptionRequest(investor.Id, scheme.SchemeId, scheme.ClassId, 50m, 500m));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var instruction = await ReadResponseAsync<DealingInstructionDto>(create);
        instruction.Redemptions.Single().NetPayoutAmount.Should().BeGreaterThan(0m);

        var submit = await client.PostAsJsonAsync($"/api/dealing/instructions/{instruction.Id}/submit", new DealingInstructionActionRequest("Submit redemption."));
        submit.StatusCode.Should().Be(HttpStatusCode.OK);
        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("e2e-red-approver"), "E2E Redemption Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approve = await client.PostAsJsonAsync($"/api/dealing/instructions/{instruction.Id}/approve", new DealingInstructionActionRequest("Approve redemption."));
        approve.StatusCode.Should().Be(HttpStatusCode.OK);
        var settled = await ReadResponseAsync<DealingInstructionDto>(approve);
        settled.Status.Should().Be("Settled");
        settled.Redemptions.Single().RedemptionAdviceNumber.Should().NotBeNullOrWhiteSpace();

        await AuthenticateAsBootstrapAdminAsync(client);
        var payout = settled.Redemptions.Single().NetPayoutAmount;
        var paymentResponse = await client.PostAsJsonAsync("/api/cash/payments", new CreatePaymentInstructionRequest(investor.Id, scheme.SchemeId, scheme.ClassId, scheme.BankAccountId, payout, "KES", settled.Redemptions.Single().RedemptionAdviceNumber!, "Redemption", settled.Id));
        paymentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var payment = await ReadResponseAsync<PaymentInstructionDto>(paymentResponse);
        var payoutConfirmationCsv = BankCsv(payment.Reference, investor.Id, scheme.SchemeId, scheme.ClassId, payout, "Debit");
        var completed = await client.PostAsJsonAsync("/api/cash/bank-statements/import", new ImportBankStatementRequest(scheme.BankAccountId, LifecycleDate, 2, "redemption-payout.csv", payoutConfirmationCsv));
        var completedBody = await completed.Content.ReadAsStringAsync();
        completed.StatusCode.Should().Be(HttpStatusCode.Created, completedBody);
        var payoutImport = await ReadResponseAsync<BankStatementImportDto>(completed);
        payoutImport.MatchedLineCount.Should().Be(1);

        using (var verificationScope = _factory.Services.CreateScope())
        {
            var verificationDb = verificationScope.ServiceProvider.GetRequiredService<CisDbContext>();
            payment = await verificationDb.PaymentInstructions
                .AsNoTracking()
                .Include(item => item.StatusEvents)
                .Where(item => item.Id == payment.Id)
                .Select(item => new PaymentInstructionDto(
                    item.Id,
                    item.InvestorId,
                    item.SchemeId,
                    item.SchemeClassId,
                    item.SchemeBankAccountId,
                    item.Amount,
                    item.Currency,
                    item.Reference,
                    item.PaymentType.ToString(),
                    item.Status.ToString(),
                    item.RelatedDealingInstructionId,
                    item.RequestedByUserId,
                    item.RequestedAtUtc,
                    item.IdempotencyKey,
                    item.ExternalReference,
                    item.FailedReason,
                    item.StatusEvents
                        .OrderBy(statusEvent => statusEvent.OccurredAtUtc)
                        .Select(statusEvent => new PaymentStatusEventDto(
                            statusEvent.Id,
                            statusEvent.Status.ToString(),
                            statusEvent.EventType.ToString(),
                            statusEvent.OccurredAtUtc,
                            statusEvent.Reason,
                            statusEvent.ExternalReference,
                            statusEvent.ChangedByUserId))
                        .ToList()))
                .SingleAsync();
        }

        payment.Status.Should().Be("Completed");

        await CreateApprovedUnitAdjustmentAsync(client, investor.Id, scheme.SchemeId, scheme.ClassId, -50m, "Post redemption unit reduction.");
        var chart = await CreateAccountingFixtureAsync(client, scheme);
        await CreateAutomatedJournalAsync(client, chart, AutomatedJournalType.Redemption.ToString(), "DealingInstruction", settled.Id.ToString(), payout, "Redemption payout journal.");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var holding = await db.UnitHoldings.SingleAsync(holding => holding.InvestorId == investor.Id && holding.SchemeId == scheme.SchemeId);
        holding.Units.Should().Be(450m);
        (await db.PaymentInstructions.AnyAsync(item => item.Id == payment.Id && item.Status == PaymentInstructionStatus.Completed)).Should().BeTrue();
        (await db.Journals.AnyAsync(journal => journal.AutomatedJournalType == AutomatedJournalType.Redemption && journal.OriginatingEventId == settled.Id.ToString())).Should().BeTrue();
        var actions = await db.AuditLogs.Select(log => log.Action).ToListAsync();
        actions.Should().Contain(["DealingInstructionApproved", "CashPaymentInstructionCreated", "CashPaymentStatusUpdated", "UnitMovementPosted", "AutomatedJournalPosted"]);
    }

    [Fact]
    public async Task Scenario5_NavCycle_ValuesPortfolioPublishesNavAndArchivesImmutableVersion()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var scheme = await CreateActiveSchemeFixtureAsync("NAVC");
        var instrument = await CreateInstrumentViaApiAsync(client, "NAV-E2E-001", "E2E Treasury Bill", "TreasuryBill", "KES");

        var placement = await client.PostAsJsonAsync("/api/portfolio/placements", new CreatePlacementRequest(scheme.SchemeId, scheme.ClassId, instrument.Id, null, null, 100_000m, "KES", LifecycleDate, LifecycleDate.AddDays(91), 7m, 500m));
        placement.StatusCode.Should().Be(HttpStatusCode.Created);
        var placementDto = await ReadResponseAsync<PlacementDto>(placement);
        var submitPlacement = await client.PostAsJsonAsync($"/api/portfolio/placements/{placementDto.Id}/submit", new PlacementSubmitRequest());
        submitPlacement.StatusCode.Should().Be(HttpStatusCode.OK);
        var placementApprover = await CreateSystemAdminDirectlyAsync(UniqueEmail("e2e-placement-approver"), "E2E Placement Approver", "Approver123!");
        await AuthenticateAsync(client, placementApprover.Email, placementApprover.Password);
        var approvePlacement = await client.PostAsJsonAsync($"/api/portfolio/placements/{placementDto.Id}/approve", new PlacementApproveRequest(null));
        approvePlacement.StatusCode.Should().Be(HttpStatusCode.OK);

        await AuthenticateAsBootstrapAdminAsync(client);
        var pricing = await PostAsJsonWithIdempotencyAsync(client, "/api/integrations/pricing/upload", new PricingUploadRequest(scheme.SchemeId, scheme.ClassId, "TreasuryBill", "Bloomberg", "Reuters", true, 5, 5m, LifecycleDate, "prices.csv", "InstrumentCode,Price\nNAV-E2E-001,101.5"), $"pricing-{Guid.NewGuid():N}");
        pricing.StatusCode.Should().Be(HttpStatusCode.Created);

        var run = await CreateApprovedNavRunAsync(client, scheme, instrument.Id, 101.5m);
        var publish = await client.PostAsJsonAsync($"/api/nav/valuation-runs/{run.Id}/publish", new NavWorkflowActionRequest("Publish E2E NAV."));
        publish.StatusCode.Should().Be(HttpStatusCode.OK);
        var publication = await ReadResponseAsync<NavPublicationDto>(publish);
        publication.PublishedNav.Should().BeGreaterThan(0m);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await db.StalePriceExceptions.AnyAsync(exception => exception.ValuationRunId == run.Id && exception.Status == PriceExceptionStatus.Open)).Should().BeFalse();
        (await db.NavVersionArchives.AnyAsync(archive => archive.NavPublicationId == publication.Id)).Should().BeTrue();
        var archive = await db.NavVersionArchives.SingleAsync(archive => archive.NavPublicationId == publication.Id);
        db.NavVersionArchives.Remove(archive);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Scenario6_Reconciliation_GeneratesAssignsResolvesBreaksAndAudits()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var scheme = await CreateActiveSchemeFixtureAsync("REC");
        var custodian = await client.PostAsJsonAsync("/api/custody/custodians", new CreateCustodianRequest($"E2ECUST-{Guid.NewGuid():N}"[..30], "E2E Custodian", null, [new CreateCustodianAccountRequest(scheme.SchemeId, scheme.ClassId, "CASH-001", "Main account", "KES")]));
        custodian.StatusCode.Should().Be(HttpStatusCode.Created);
        var custodianDto = await ReadResponseAsync<CustodianDto>(custodian);

        var holdings = await PostAsJsonWithIdempotencyAsync(client, "/api/custody/statements/holdings/import", new ImportCustodianHoldingsRequest(custodianDto.Id, null, LifecycleDate, "holdings.csv", [new ImportHoldingLineRequest(null, scheme.SchemeId, scheme.ClassId, null, "TBILL-REC", "Treasury Bill", 100m, 1_000m, "KES", "SET-H-001", true)]), $"cust-hold-{Guid.NewGuid():N}");
        holdings.StatusCode.Should().Be(HttpStatusCode.Created);
        var holdingsImport = await ReadResponseAsync<CustodianStatementImportDto>(holdings);
        var cash = await PostAsJsonWithIdempotencyAsync(client, "/api/custody/statements/cash/import", new ImportCustodianCashRequest(custodianDto.Id, null, LifecycleDate, "cash.csv", [new ImportCashLineRequest(null, scheme.SchemeId, "CASH-001", "KES", LifecycleDate, 1_000m, "SET-C-001", true)]), $"cust-cash-{Guid.NewGuid():N}");
        cash.StatusCode.Should().Be(HttpStatusCode.Created);
        var cashImport = await ReadResponseAsync<CustodianStatementImportDto>(cash);

        var runResponse = await client.PostAsJsonAsync("/api/custody/reconciliation-runs", new CreateCustodyReconciliationRunRequest(custodianDto.Id, holdingsImport.Id, cashImport.Id, LifecycleDate, [new InternalHoldingSnapshotRequest(scheme.SchemeId, scheme.ClassId, "TBILL-REC", 95m, 900m, "KES")], [new InternalCashSnapshotRequest(scheme.SchemeId, "CASH-001", "KES", 900m)]));
        runResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var run = await ReadResponseAsync<CustodyReconciliationRunDto>(runResponse);
        run.Breaks.Should().NotBeEmpty();

        var targetBreak = run.Breaks.First();
        var assign = await client.PostAsJsonAsync($"/api/custody/reconciliation-breaks/{targetBreak.Id}/assign", new AssignCustodyBreakRequest("custody-owner", "Investigate E2E break."));
        assign.StatusCode.Should().Be(HttpStatusCode.OK);
        var resolve = await client.PostAsJsonAsync($"/api/custody/reconciliation-breaks/{targetBreak.Id}/resolve", new ResolveCustodyBreakRequest("evidence/reconciliation/e2e.pdf", "Evidence received and break resolved."));
        resolve.StatusCode.Should().Be(HttpStatusCode.OK);
        var resolved = await ReadResponseAsync<CustodyReconciliationBreakDto>(resolve);
        resolved.Status.Should().Be(ReconciliationBreakStatus.Resolved.ToString());
        resolved.ResolutionEvidenceReference.Should().Be("evidence/reconciliation/e2e.pdf");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await db.BreakAgings.AnyAsync(age => age.HoldingBreakId == targetBreak.Id || age.CashBreakId == targetBreak.Id)).Should().BeTrue();
        var actions = await db.AuditLogs.Where(log => log.Module == "CustodyReconciliation").Select(log => log.Action).ToListAsync();
        actions.Should().Contain(["CustodyReconciliationRunCreated", "CustodyBreakGenerated", "CustodyBreakAssigned", "CustodyBreakResolved"]);
    }

    [Fact]
    public async Task Scenario7_ComplianceLiquidity_RunChecksCloseBreachAndProduceRiskSourceData()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);

        var runResponse = await client.PostAsJsonAsync("/api/compliance/limit-runs", LimitRunRequest("e2e"));
        runResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var run = await ReadResponseAsync<LimitCheckRunDto>(runResponse);
        run.Breaches.Should().NotBeEmpty();
        var breach = run.Breaches.First();

        var assign = await client.PostAsJsonAsync($"/api/compliance/breaches/{breach.Id}/assign", new AssignBreachRequest("risk-owner", "Assign E2E breach."));
        assign.StatusCode.Should().Be(HttpStatusCode.OK);
        var remediate = await client.PostAsJsonAsync($"/api/compliance/breaches/{breach.Id}/remediate", new RemediateBreachRequest("Reduce exposure.", "evidence/compliance/remediation.pdf"));
        remediate.StatusCode.Should().Be(HttpStatusCode.OK);
        var closer = await CreateSystemAdminDirectlyAsync(UniqueEmail("e2e-breach-closer"), "E2E Breach Closer", "Approver123!");
        await AuthenticateAsync(client, closer.Email, closer.Password);
        var close = await client.PostAsJsonAsync($"/api/compliance/breaches/{breach.Id}/close", new CloseBreachRequest("evidence/compliance/closure.pdf"));
        close.StatusCode.Should().Be(HttpStatusCode.OK);

        await AuthenticateAsBootstrapAdminAsync(client);
        var liquidity = await client.PostAsJsonAsync("/api/risk/liquidity-coverage-runs", new CreateLiquidityCoverageRunRequest(null, null, LifecycleDate, "RISK-1.0", 500m, 250m, null));
        liquidity.StatusCode.Should().Be(HttpStatusCode.Created);
        var scenario = await client.PostAsJsonAsync("/api/risk/stress-scenarios", new CreateStressScenarioRequest(null, $"E2E Stress {Guid.NewGuid():N}", 800m, "{}"));
        scenario.StatusCode.Should().Be(HttpStatusCode.Created);
        var scenarioDto = await ReadResponseAsync<RedemptionStressScenarioDto>(scenario);
        var stress = await client.PostAsJsonAsync("/api/risk/stress-test-runs", new CreateStressTestRunRequest(scenarioDto.Id, LifecycleDate, "RISK-1.0", 400m, null));
        stress.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await db.LimitBreaches.AnyAsync(item => item.Id == breach.Id && item.Status == BreachStatus.Closed)).Should().BeTrue();
        (await db.RelatedPartyExposures.AnyAsync()).Should().BeTrue();
        (await db.CounterpartyLimitUsages.AnyAsync()).Should().BeTrue();
        (await db.LiquidityCoverageRuns.AnyAsync()).Should().BeTrue();
        (await db.RedemptionStressTestRuns.AnyAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task Scenario8_Distribution_ApprovesCoveragePublishesReinvestmentTaxUnitsAndJournals()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var scheme = await CreateActiveSchemeFixtureAsync("DST", includeDistributionFees: true);
        var investor = await CreateApprovedInvestorFixtureAsync("distribution");
        await CreateTaxRulesAsync(client);

        var declarationResponse = await client.PostAsJsonAsync("/api/distributions/declarations", new CreateDistributionDeclarationRequest(scheme.SchemeId, scheme.ClassId, LifecycleDate, LifecycleDate, LifecycleDate.AddDays(5), "DIST-1.0", 1_000m, 200m, 0m, 0m, 100m, 50m, 25m, 25m, 1_000m, 5_000m, false, null));
        declarationResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var declaration = await ReadResponseAsync<DistributionDeclarationDto>(declarationResponse);
        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("e2e-dist-approver"), "E2E Distribution Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approveDeclaration = await client.PostAsJsonAsync($"/api/distributions/declarations/{declaration.Id}/approve", new ApproveDistributionDeclarationRequest("Approved distribution.", false));
        approveDeclaration.StatusCode.Should().Be(HttpStatusCode.OK);

        await AuthenticateAsBootstrapAdminAsync(client);
        var runResponse = await client.PostAsJsonAsync("/api/distributions/runs", new CreateDistributionRunRequest(declaration.Id, 1.25m, [new CreateInvestorDistributionRequest(investor.Id, 100m, "Reinvest", 1_000m, 1_100m, 25m, 50m, "KE", "DISTRIBUTION")]));
        runResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var run = await ReadResponseAsync<DistributionRunDto>(runResponse);
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var publish = await client.PostAsJsonAsync($"/api/distributions/runs/{run.Id}/publish", new PublishDistributionRunRequest("Publish distribution."));
        publish.StatusCode.Should().Be(HttpStatusCode.OK);
        var published = await ReadResponseAsync<DistributionRunDto>(publish);

        await AuthenticateAsBootstrapAdminAsync(client);
        var chart = await CreateAccountingFixtureAsync(client, scheme);
        await CreateAutomatedJournalAsync(client, chart, AutomatedJournalType.Distribution.ToString(), "DistributionRun", published.Id.ToString(), published.TotalNetDistribution, "Distribution journal.");
        var investorDistributions = await client.GetAsync($"/api/distributions/investor/{investor.Id}");
        investorDistributions.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await db.TaxCalculations.AnyAsync(calculation => calculation.InvestorDistributionId == run.InvestorDistributions.Single().Id)).Should().BeTrue();
        (await db.UnitLedgerEntries.AnyAsync(entry => entry.SourceType == UnitMovementSourceType.ReinvestmentInstruction && entry.InvestorId == investor.Id)).Should().BeTrue();
        (await db.Journals.AnyAsync(journal => journal.AutomatedJournalType == AutomatedJournalType.Distribution && journal.OriginatingEventId == published.Id.ToString())).Should().BeTrue();
    }

    [Fact]
    public async Task Scenario9_RegulatoryPack_GeneratesApprovesPublishesBundleArchivesAndRestrictsDownload()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsBootstrapAdminAsync(client);
        var reportCodes = new[] { "REG-01", "REG-04", "REG-05", "REG-06", "REG-07" };
        var publishedRuns = new List<ReportRunDto>();

        foreach (var code in reportCodes)
        {
            var runResponse = await client.PostAsJsonAsync($"/api/reports/{code}/run", ReportRunRequest());
            var runBody = await runResponse.Content.ReadAsStringAsync();
            runResponse.StatusCode.Should().Be(HttpStatusCode.Created, $"{code}: {runBody}");
            var run = await ReadResponseAsync<ReportRunDto>(runResponse);
            var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail($"e2e-{code.ToLowerInvariant()}-approver"), $"E2E {code} Approver", "Approver123!");
            await AuthenticateAsync(client, approver.Email, approver.Password);
            var approve = await client.PostAsJsonAsync($"/api/reports/runs/{run.Id}/approve", new ReportActionRequest($"Approve {code}."));
            approve.StatusCode.Should().Be(HttpStatusCode.OK);
            var publish = await client.PostAsJsonAsync($"/api/reports/runs/{run.Id}/publish", new ReportActionRequest($"Publish {code}."));
            publish.StatusCode.Should().Be(HttpStatusCode.OK);
            publishedRuns.Add(await ReadResponseAsync<ReportRunDto>(publish));
            await AuthenticateAsBootstrapAdminAsync(client);
        }

        var bundleResponse = await client.PostAsJsonAsync("/api/reports/bundles", new CreateReportBundleRequest($"REG-BND-{Guid.NewGuid():N}"[..24], "Regulatory E2E pack", LifecycleDate, publishedRuns.Select(run => run.Id).ToArray()));
        bundleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var bundle = await ReadResponseAsync<ReportBundleDto>(bundleResponse);
        var publishBundle = await client.PostAsync($"/api/reports/bundles/{bundle.Id}/publish", null);
        publishBundle.StatusCode.Should().Be(HttpStatusCode.OK);

        var regulator = await CreateSystemAdminDirectlyAsync(UniqueEmail("e2e-regulator"), "E2E Regulator", "Approver123!");
        await AuthenticateAsync(client, regulator.Email, regulator.Password);
        var download = await client.GetAsync($"/api/reports/runs/{publishedRuns.First().Id}/download");
        download.StatusCode.Should().Be(HttpStatusCode.OK);

        var portalUser = await CreateUserWithRoleDirectlyAsync(UniqueEmail("e2e-reg-portal"), "E2E Portal User", "Portal123!", RoleNames.InvestorPortalUser);
        await AuthenticateAsync(client, portalUser.Email, portalUser.Password);
        var forbidden = await client.GetAsync($"/api/reports/runs/{publishedRuns.First().Id}/download");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        (await db.ReportVersionArchives.CountAsync(archive => publishedRuns.Select(run => run.Id).Contains(archive.ReportRunId))).Should().Be(reportCodes.Length);
        (await db.AuditLogs.AnyAsync(log => log.Module == "Reports" && log.Action == "ReportBundlePublished")).Should().BeTrue();
    }

    private async Task<SchemeDto> CreateSchemeThroughApiAsync(HttpClient client, string code)
    {
        var response = await client.PostAsJsonAsync("/api/schemes", new CreateSchemeRequest(code, $"Victory {code} Fund", "Unit Trust", "KES"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await ReadResponseAsync<SchemeDto>(response);
    }

    private async Task<SchemeDto> AddFullSchemeActivationDataAsync(HttpClient client, SchemeDto scheme)
    {
        var classResponse = await client.PostAsJsonAsync($"/api/schemes/{scheme.Id}/classes", new AddSchemeClassRequest("A", "Class A", "KES", "Daily", "Daily", new TimeOnly(14, 0), 1000m, 500m, 0, 1));
        classResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        scheme = await ReadResponseAsync<SchemeDto>(classResponse);
        var classId = scheme.Classes.Single().Id;

        foreach (var request in new (string Url, object Body)[]
        {
            ($"/api/schemes/{scheme.Id}/configuration", new AddSchemeConfigurationRequest("ForwardPricing", "Accrual")),
            ($"/api/schemes/{scheme.Id}/risk-profiles", new AddSchemeRiskProfileRequest("Moderate", 20m)),
            ($"/api/schemes/{scheme.Id}/liquidity-thresholds", new AddLiquidityThresholdRequest(20m, 15m, 10m)),
            ($"/api/schemes/{scheme.Id}/fee-schedules", new AddFeeScheduleRequest(classId, "ManagementFee", "AUM", 1m, null, new DateOnly(2026, 1, 1), null, null)),
            ($"/api/schemes/{scheme.Id}/approved-instruments", new AddApprovedInstrumentRuleRequest("TreasuryBill", 365, 20m, 25m, 80m)),
            ($"/api/schemes/{scheme.Id}/bank-accounts", new AddSchemeBankAccountRequest("Victory Bank", $"0100{Random.Shared.Next(100000, 999999)}", $"{scheme.Name} Collection", "KES", "VICBKENA")),
            ($"/api/schemes/{scheme.Id}/custodian-mappings", new AddSchemeCustodianMappingRequest("Victory Custody", $"CUST-{Guid.NewGuid():N}"[..20], $"SETT-{Guid.NewGuid():N}"[..20])),
            ($"/api/schemes/{scheme.Id}/distribution-rules", new AddDistributionRuleRequest("Monthly", true, 25)),
            ($"/api/schemes/{scheme.Id}/template-mappings", new AddTemplateMappingRequest("Statement", $"STMT-{Guid.NewGuid():N}"[..16]))
        })
        {
            var response = await client.PostAsJsonAsync(request.Url, request.Body);
            var body = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK, body);
            scheme = await ReadResponseAsync<SchemeDto>(response);
        }

        return scheme;
    }

    private async Task<InvestorDto> CreateInvestorThroughApiAsync(HttpClient client, string email, string identityNumber)
    {
        var response = await client.PostAsJsonAsync("/api/investors", IndividualInvestorRequest(email, identityNumber));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await ReadResponseAsync<InvestorDto>(response);
    }

    private async Task<InvestorDto> AddRequiredKycDocumentsAsync(HttpClient client, InvestorDto investor)
    {
        foreach (var type in new[] { "NationalId", "TaxCertificate", "ProofOfAddress" })
        {
            var response = await client.PostAsJsonAsync($"/api/investors/{investor.Id}/documents", new AddKycDocumentRequest(type, $"{type}.pdf", "application/pdf", 2048, $"documents/{investor.Id}/{type}.pdf", new DateOnly(2025, 1, 1), new DateOnly(2030, 12, 31)));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            investor = await ReadResponseAsync<InvestorDto>(response);
        }

        return investor;
    }

    private static CreateInvestorRequest IndividualInvestorRequest(string email, string identityNumber)
    {
        return new CreateInvestorRequest("Individual", $"Investor {Guid.NewGuid():N}"[..20], email, $"+2547{Random.Shared.Next(10000000, 99999999)}", "Low", "Standard onboarding.", identityNumber, "Jane", "Investor", new DateOnly(1990, 1, 1), "Kenyan", null, null, null, null, null, null, null, $"TAX-{Guid.NewGuid():N}"[..20], "Kenya", "1 Victory Way", "Standard", "Single Signatory", LifecycleDate, null);
    }

    private async Task<ApprovedInvestorFixture> CreateApprovedInvestorFixtureAsync(string prefix)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;
        var investor = Investor.Create($"INV-{prefix}-{Guid.NewGuid():N}"[..30], InvestorType.Individual, $"Approved {prefix} Investor", UniqueEmail($"e2e-{prefix}"), $"+2547{Random.Shared.Next(10000000, 99999999)}", "fixture-maker", now);
        investor.AttachIndividualProfile("Jane", prefix, UniqueIdentity(), new DateOnly(1990, 1, 1), "Kenyan");
        investor.AddDefaultKycRequirements(["NationalId", "TaxCertificate", "ProofOfAddress"]);
        investor.SetTaxProfile($"TAX-{Guid.NewGuid():N}"[..20], "Kenya");
        investor.AssignRiskClassification(InvestorRiskCategory.Low, "Fixture risk.", "fixture-maker", now);
        foreach (var documentType in new[] { "NationalId", "TaxCertificate", "ProofOfAddress" })
        {
            investor.AddDocument(documentType, $"{documentType}.pdf", "application/pdf", 2000, $"fixture/{Guid.NewGuid():N}.pdf", BusinessDate.From(new DateOnly(2026, 1, 1)), BusinessDate.From(new DateOnly(2030, 1, 1)), "fixture-maker", now, BusinessDate.From(LifecycleDate));
        }
        investor.AddAmlScreeningCase("Fixture", $"AML-{Guid.NewGuid():N}"[..20], [], now);
        investor.SubmitKyc("fixture-maker", now);
        investor.Approve("fixture-approver", now.AddMinutes(1), BusinessDate.From(LifecycleDate), "Fixture approved.");
        db.Investors.Add(investor);
        await db.SaveChangesAsync();
        return new ApprovedInvestorFixture(investor.Id, investor.InvestorNumber);
    }

    private async Task<ActiveSchemeFixture> CreateActiveSchemeFixtureAsync(string prefix, bool includeDistributionFees = false)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var now = DateTime.UtcNow;
        var code = $"{prefix}{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        var scheme = Scheme.Create(code, $"{prefix} Scheme {code}", "Unit Trust", "KES", "fixture-maker", now);
        var schemeClass = scheme.AddClass("A", "Class A", "KES", SchemeFrequency.Daily, SchemeFrequency.Daily, new TimeOnly(14, 0), 1000m, 500m, 0, 1);
        scheme.AddConfiguration("ForwardPricing", "Accrual");
        scheme.AddFeeSchedule(schemeClass.Id, "ManagementFee", includeDistributionFees ? "AverageNAV" : "AUM", includeDistributionFees ? 2m : 1m, null, BusinessDate.From(new DateOnly(2026, 1, 1)), null, []);
        if (includeDistributionFees)
        {
            scheme.AddFeeSchedule(schemeClass.Id, "CustodyFee", "AUM", 0.4m, null, BusinessDate.From(new DateOnly(2026, 1, 1)), null, []);
            scheme.AddFeeSchedule(schemeClass.Id, "TrusteeFee", "AUM", 0.2m, null, BusinessDate.From(new DateOnly(2026, 1, 1)), null, []);
            scheme.AddFeeSchedule(schemeClass.Id, "AdminFee", "AUM", 0.3m, null, BusinessDate.From(new DateOnly(2026, 1, 1)), null, []);
        }
        scheme.AddApprovedInstrumentRule("TreasuryBill", 365, 20m, 25m, 80m);
        scheme.AddBankAccount("Victory Bank", $"0100{Random.Shared.Next(100000, 999999)}", "Collection", "KES", "VICBKENA");
        scheme.Submit("fixture-maker", now);
        scheme.Check("fixture-checker", now.AddMinutes(1));
        scheme.Approve("fixture-approver", now.AddMinutes(2));
        db.Schemes.Add(scheme);
        await db.SaveChangesAsync();
        return new ActiveSchemeFixture(scheme.Id, schemeClass.Id, scheme.BankAccounts.Single().Id);
    }

    private static CreateSubscriptionRequest SubscriptionRequest(Guid investorId, Guid schemeId, Guid classId, bool fundsCleared, bool approvedNav)
    {
        return new CreateSubscriptionRequest(investorId, schemeId, classId, "Branch", LifecycleDate, DateTime.SpecifyKind(new DateTime(2026, 5, 12, 10, 0, 0), DateTimeKind.Utc), "Amount", 10_000m, null, "KES", fundsCleared, approvedNav, 100m, LifecycleDate);
    }

    private static CreateRedemptionRequest RedemptionRequest(Guid investorId, Guid schemeId, Guid classId, decimal units, decimal availableUnits)
    {
        return new CreateRedemptionRequest(investorId, schemeId, classId, "Operations", LifecycleDate, DateTime.SpecifyKind(new DateTime(2026, 5, 12, 10, 0, 0), DateTimeKind.Utc), "Units", null, units, false, availableUnits, 0m, 5m, 0, 1, true, 100m, LifecycleDate, 1m, 5m, "KES");
    }

    private async Task<UnitAdjustmentDto> CreateApprovedUnitAdjustmentAsync(HttpClient client, Guid investorId, Guid schemeId, Guid classId, decimal units, string reason)
    {
        await AuthenticateAsBootstrapAdminAsync(client);
        var create = await client.PostAsJsonAsync("/api/unit-register/adjustments", new CreateUnitAdjustmentRequest(investorId, schemeId, classId, units, LifecycleDate, $"ADJ-{Guid.NewGuid():N}"[..30], 6, reason));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var adjustment = await ReadResponseAsync<UnitAdjustmentDto>(create);
        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("e2e-unit-approver"), "E2E Unit Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approve = await client.PostAsJsonAsync($"/api/unit-register/adjustments/{adjustment.Id}/approve", new ApproveUnitAdjustmentRequest($"Approved: {reason}"));
        approve.StatusCode.Should().Be(HttpStatusCode.OK);
        return await ReadResponseAsync<UnitAdjustmentDto>(approve);
    }

    private async Task<AccountingFixture> CreateAccountingFixtureAsync(HttpClient client, ActiveSchemeFixture scheme)
    {
        await AuthenticateAsBootstrapAdminAsync(client);
        var response = await client.PostAsJsonAsync("/api/accounting/chart-of-accounts", new CreateChartOfAccountsRequest(scheme.SchemeId, scheme.ClassId, $"COA{Guid.NewGuid():N}"[..12].ToUpperInvariant(), "Lifecycle chart", "KES", "FY2026", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), [new AccountDefinitionRequest("1000", "Cash and bank", AccountType.Asset.ToString(), true), new AccountDefinitionRequest("1100", "Investments", AccountType.Asset.ToString(), true), new AccountDefinitionRequest("2000", "Accounts payable", AccountType.Liability.ToString(), true), new AccountDefinitionRequest("3000", "Unit holder capital", AccountType.Equity.ToString(), true), new AccountDefinitionRequest("4000", "Investment income", AccountType.Income.ToString(), false), new AccountDefinitionRequest("5000", "Fund expenses", AccountType.Expense.ToString(), false)]));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var chart = await ReadResponseAsync<ChartOfAccountsDto>(response);
        return new AccountingFixture(scheme.SchemeId, scheme.ClassId, chart.Accounts);
    }

    private static async Task<JournalDto> CreateAutomatedJournalAsync(HttpClient client, AccountingFixture fixture, string type, string eventType, string eventId, decimal amount, string description)
    {
        var debit = type == AutomatedJournalType.Redemption.ToString() ? fixture.Account("3000") : fixture.Account("1000");
        var credit = type == AutomatedJournalType.Redemption.ToString() ? fixture.Account("1000") : fixture.Account("3000");
        if (type == AutomatedJournalType.Distribution.ToString())
        {
            debit = fixture.Account("3000");
            credit = fixture.Account("1000");
        }

        var response = await client.PostAsJsonAsync("/api/accounting/journals/automated", new CreateAutomatedJournalRequest(fixture.SchemeId, fixture.ClassId, LifecycleDate, "KES", type, description, eventType, eventId, [new CreateJournalLineRequest(debit.Id, "Debit", amount, 0m), new CreateJournalLineRequest(credit.Id, "Credit", 0m, amount)]));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await ReadResponseAsync<JournalDto>(response);
    }

    private async Task<InstrumentDto> CreateInstrumentViaApiAsync(HttpClient client, string isin, string name, string type, string currency)
    {
        var response = await client.PostAsJsonAsync("/api/portfolio/instruments", new CreateInstrumentRequest(isin, name, type, currency, null, null, 7m, null));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await ReadResponseAsync<InstrumentDto>(response);
    }

    private async Task<ValuationRunDto> CreateApprovedNavRunAsync(HttpClient client, ActiveSchemeFixture scheme, Guid instrumentId, decimal price)
    {
        await AuthenticateAsBootstrapAdminAsync(client);
        var create = await client.PostAsJsonAsync("/api/nav/valuation-runs", NavRunRequest(scheme.SchemeId, scheme.ClassId, instrumentId, price));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var run = await ReadResponseAsync<ValuationRunDto>(create);
        var calculate = await client.PostAsync($"/api/nav/valuation-runs/{run.Id}/calculate", null);
        calculate.StatusCode.Should().Be(HttpStatusCode.OK);
        var submit = await client.PostAsJsonAsync($"/api/nav/valuation-runs/{run.Id}/submit", new NavWorkflowActionRequest("Submit E2E NAV."));
        submit.StatusCode.Should().Be(HttpStatusCode.OK);
        var checker = await CreateSystemAdminDirectlyAsync(UniqueEmail("e2e-nav-checker"), "E2E NAV Checker", "Checker123!");
        await AuthenticateAsync(client, checker.Email, checker.Password);
        var check = await client.PostAsJsonAsync($"/api/nav/valuation-runs/{run.Id}/check", new NavWorkflowActionRequest("Check E2E NAV."));
        check.StatusCode.Should().Be(HttpStatusCode.OK);
        var approver = await CreateSystemAdminDirectlyAsync(UniqueEmail("e2e-nav-approver"), "E2E NAV Approver", "Approver123!");
        await AuthenticateAsync(client, approver.Email, approver.Password);
        var approve = await client.PostAsJsonAsync($"/api/nav/valuation-runs/{run.Id}/approve", new NavWorkflowActionRequest("Approve E2E NAV."));
        approve.StatusCode.Should().Be(HttpStatusCode.OK);
        return await ReadResponseAsync<ValuationRunDto>(approve);
    }

    private static CreateValuationRunRequest NavRunRequest(Guid schemeId, Guid classId, Guid instrumentId, decimal price)
    {
        return new CreateValuationRunRequest(schemeId, classId, LifecycleDate, "NAV-FORMULA-V1", DayCountBasis.Actual365.ToString(), 4, 5, 5m, false, 10_000m, 500m, 100m, 2_000m, 300m, 100_000m, 1_000m, 500m, 0m, 1_000m, 10m, 5m, 100m, 1m, 2m, 1m, 50m, 1m, 50m, 1m, 1_000m, 1m, 5m, 2m, [new InstrumentValuationInputRequest(instrumentId, "TreasuryBill", 1_000m, price, LifecycleDate, 100_000m, 7.3m, 30, 98_000m, 100_000m, 90, null, false, price - 1m, "Bloomberg")]);
    }

    private static CreateLimitCheckRunRequest LimitRunRequest(string suffix)
    {
        return new CreateLimitCheckRunRequest(null, null, LifecycleDate, "RISK-1.0", [new NavSourceRequest(Guid.NewGuid(), Guid.NewGuid(), 1_000m), new NavSourceRequest(Guid.NewGuid(), Guid.NewGuid(), 2_000m)], 500m, 50m, 200m, 25m, [1_000m, 1_100m, 1_200m], 22m, 25m, 12m, [new LimitRuleRequest("Statutory", "Counterparty", $"CP-{suffix}", 100m, 120m, "Counterparty exposure exceeds limit."), new LimitRuleRequest("InternalPolicy", "Issuer", $"ISS-{suffix}", 200m, 350m, "Issuer exposure exceeds limit.")], [new RelatedPartyExposureRequest($"Related {suffix}", 300m, $"related/{suffix}.csv")], [new CounterpartyUsageRequest($"Counterparty {suffix}", 120m, 100m, $"counterparty/{suffix}.csv")]);
    }

    private async Task CreateTaxRulesAsync(HttpClient client)
    {
        foreach (var request in new[] { new CreateTaxRuleRequest("KE", "FUND", "VAT", 16m, new DateOnly(2026, 1, 1), null), new CreateTaxRuleRequest("KE", "FUND", "WHT", 5m, new DateOnly(2026, 1, 1), null), new CreateTaxRuleRequest("KE", "DISTRIBUTION", "WHT", 10m, new DateOnly(2026, 1, 1), null) })
        {
            var response = await client.PostAsJsonAsync("/api/taxes/rules", request);
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Conflict);
        }
    }

    private static RunReportRequest ReportRunRequest()
    {
        return new RunReportRequest(LifecycleDate, [new ReportParameterRequest("LifecycleDate", LifecycleDate.ToString("yyyy-MM-dd"))], [new ReportOutputRequest(ReportOutputFormat.PDF.ToString())]);
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

    private async Task<(Guid Id, string Email, string Password)> CreateSystemAdminDirectlyAsync(string email, string displayName, string password)
    {
        return await CreateUserWithRoleDirectlyAsync(email, displayName, password, RoleNames.SystemAdmin);
    }

    private async Task<(Guid Id, string Email, string Password)> CreateUserWithRoleDirectlyAsync(string email, string displayName, string password, string roleName)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CisDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var role = await db.Roles.SingleAsync(role => role.Name == roleName);
        var now = DateTime.UtcNow;
        var user = User.Create(email, displayName, "pending", now);
        user.SetPasswordHash(passwordHasher.HashPassword(user, password), requirePasswordChange: false);
        user.AssignRole(role.Id, "test-fixture", "test-fixture", now);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (user.Id, email, password);
    }

    private static async Task<HttpResponseMessage> PostAsJsonWithIdempotencyAsync<T>(HttpClient client, string uri, T request, string idempotencyKey)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, uri) { Content = JsonContent.Create(request) };
        message.Headers.Add(StandardHeaders.IdempotencyKey, idempotencyKey);
        return await client.SendAsync(message);
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response)
    {
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
        apiResponse.Should().NotBeNull();
        return apiResponse!.Data;
    }

    private static string BankCsv(string reference, Guid investorId, Guid schemeId, Guid classId, decimal amount, string direction)
    {
        return $"LineNumber,TransactionDate,Reference,Description,Amount,Direction,InvestorId,SchemeId,SchemeClassId\n1,{LifecycleDate:yyyy-MM-dd},{reference},Lifecycle cash,{amount},{direction},{investorId},{schemeId},{classId}";
    }

    private static string UniqueCode(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..12].ToUpperInvariant();

    private static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@victoryfs.local";

    private static string UniqueIdentity() => $"ID-{Guid.NewGuid():N}"[..20];

    private sealed record ApprovedInvestorFixture(Guid Id, string InvestorNumber);

    private sealed record ActiveSchemeFixture(Guid SchemeId, Guid ClassId, Guid BankAccountId);

    private sealed record AccountingFixture(Guid SchemeId, Guid ClassId, IReadOnlyCollection<AccountDto> Accounts)
    {
        public AccountDto Account(string code) => Accounts.Single(account => account.Code == code);
    }
}

