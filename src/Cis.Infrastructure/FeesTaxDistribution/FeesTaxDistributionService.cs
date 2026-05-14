using System.Text.Json;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.FeesTaxDistribution;
using Cis.Domain.Audit;
using Cis.Domain.Common;
using Cis.Domain.FeesTaxDistribution;
using Cis.Domain.Investors;
using Cis.Domain.Schemes;
using Cis.Domain.UnitRegister;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.FeesTaxDistribution;

internal sealed class FeesTaxDistributionService : IFeesTaxDistributionService
{
    private const string ModuleName = "FeesTaxDistribution";

    private readonly CisDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public FeesTaxDistributionService(
        CisDbContext dbContext,
        IAuditWriter auditWriter,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<FeeAccrualRunDto> CreateFeeAccrualRunAsync(CreateFeeAccrualRunRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        await EnsureActiveSchemeClassAsync(request.SchemeId, request.SchemeClassId, cancellationToken);

        var existingRuns = await _dbContext.FeeAccrualRuns
            .AsNoTracking()
            .Where(run => run.SchemeId == request.SchemeId && run.SchemeClassId == request.SchemeClassId)
            .ToListAsync(cancellationToken);
        if (existingRuns.Any(run => run.PeriodStart.Value == request.PeriodStart && run.PeriodEnd.Value == request.PeriodEnd))
        {
            throw new ConflictException("A fee accrual run already exists for this scheme class and period.");
        }

        var dayCountBasis = ParseEnum<FeeDayCountBasis>(request.DayCountBasis, "dayCountBasis");
        var run = CreateValidated(() => FeeAccrualRun.Create(
            request.SchemeId,
            request.SchemeClassId,
            BusinessDate.From(request.PeriodStart),
            BusinessDate.From(request.PeriodEnd),
            request.FormulaVersion,
            dayCountBasis,
            actor,
            now));

        var schedules = await LoadApplicableFeeSchedulesAsync(request.SchemeId, request.SchemeClassId, request.PeriodEnd, cancellationToken);
        var approvedWaivers = await _dbContext.FeeWaiverRequests
            .AsNoTracking()
            .Where(waiver => waiver.SchemeId == request.SchemeId && waiver.SchemeClassId == request.SchemeClassId && waiver.Status == FeeWaiverStatus.Approved)
            .ToListAsync(cancellationToken);

        AddFeeCalculation(run, schedules, approvedWaivers, FeeCalculationType.Management, "ManagementFee", "D-FML-014", request.ApplicableFeeBase, request.AnnualManagementFeeRate, request.PeriodDays, null, FeesTaxDistributionFormulaEngine.DailyManagementFee(request.ApplicableFeeBase, ResolveRate(schedules, "ManagementFee", request.AnnualManagementFeeRate), dayCountBasis) * request.PeriodDays, request, actor);
        AddFeeCalculation(run, schedules, approvedWaivers, FeeCalculationType.Management, "ManagementFee", "D-FML-015", request.AverageNav, request.AnnualManagementFeeRate, request.PeriodDays, null, FeesTaxDistributionFormulaEngine.PeriodManagementFeeFromAverageNav(request.AverageNav, ResolveRate(schedules, "ManagementFee", request.AnnualManagementFeeRate), request.PeriodDays, dayCountBasis), request, actor);
        AddFeeCalculation(run, schedules, approvedWaivers, FeeCalculationType.Custody, "CustodyFee", "D-FML-016", request.ApplicableFeeBase, request.AnnualCustodyFeeRate, request.PeriodDays, null, FeesTaxDistributionFormulaEngine.DailyCustodyFee(request.ApplicableFeeBase, ResolveRate(schedules, "CustodyFee", request.AnnualCustodyFeeRate), dayCountBasis) * request.PeriodDays, request, actor);
        AddFeeCalculation(run, schedules, approvedWaivers, FeeCalculationType.Trustee, "TrusteeFee", "D-FML-017", request.ApplicableFeeBase, request.AnnualTrusteeFeeRate, request.PeriodDays, null, FeesTaxDistributionFormulaEngine.DailyTrusteeFee(request.ApplicableFeeBase, ResolveRate(schedules, "TrusteeFee", request.AnnualTrusteeFeeRate), dayCountBasis) * request.PeriodDays, request, actor);

        var adminSchedule = ResolveSchedule(schedules, "AdminFee");
        var adminRate = adminSchedule?.Rate ?? request.AdminFeeRate;
        var adminFixed = adminSchedule?.FixedAmount ?? request.FixedAdminFee;
        AddFeeCalculation(run, schedules, approvedWaivers, FeeCalculationType.Admin, "AdminFee", "D-FML-018", request.ApplicableFeeBase, adminRate, request.PeriodDays, adminFixed, FeesTaxDistributionFormulaEngine.AdminFee(request.ApplicableFeeBase, adminRate, request.PeriodDays, dayCountBasis, adminFixed), request, actor);

        var entrySchedule = ResolveSchedule(schedules, "EntryFee");
        AddFeeCalculation(run, schedules, approvedWaivers, FeeCalculationType.Entry, "EntryFee", "D-FML-019", request.GrossContribution, entrySchedule?.Rate ?? request.EntryFeeRate, null, entrySchedule?.FixedAmount ?? request.FixedEntryFee, FeesTaxDistributionFormulaEngine.EntryFee(request.GrossContribution, entrySchedule?.Rate ?? request.EntryFeeRate, entrySchedule?.FixedAmount ?? request.FixedEntryFee, ToTierInputs(entrySchedule)), request, actor);

        var exitSchedule = ResolveSchedule(schedules, "ExitFee");
        AddFeeCalculation(run, schedules, approvedWaivers, FeeCalculationType.Exit, "ExitFee", "D-FML-020", request.GrossRedemptionValue, exitSchedule?.Rate ?? request.ExitFeeRate, null, exitSchedule?.FixedAmount ?? request.FixedExitFee, FeesTaxDistributionFormulaEngine.ExitFee(request.GrossRedemptionValue, exitSchedule?.Rate ?? request.ExitFeeRate, exitSchedule?.FixedAmount ?? request.FixedExitFee, ToTierInputs(exitSchedule)), request, actor);

        var switchSchedule = ResolveSchedule(schedules, "SwitchFee");
        AddFeeCalculation(run, schedules, approvedWaivers, FeeCalculationType.Switch, "SwitchFee", "D-FML-021", request.SwitchOutValue, switchSchedule?.Rate ?? request.SwitchFeeRate, null, switchSchedule?.FixedAmount ?? request.FixedSwitchFee, FeesTaxDistributionFormulaEngine.SwitchFee(request.SwitchOutValue, switchSchedule?.Rate ?? request.SwitchFeeRate, switchSchedule?.FixedAmount ?? request.FixedSwitchFee), request, actor);

        AddFeeCalculation(run, schedules, approvedWaivers, FeeCalculationType.Performance, "PerformanceFee", "D-FML-022", request.PerformanceBase, request.PerformanceFeeRate, null, null, FeesTaxDistributionFormulaEngine.PerformanceFee(request.PerformanceBase, request.HurdleBase, request.HighWaterMarkBase, ResolveRate(schedules, "PerformanceFee", request.PerformanceFeeRate)), request, actor);

        var netFees = FeesTaxDistributionFormulaEngine.Round(run.TotalFeeAmount - run.TotalWaiverAmount);
        var vatRule = await FindTaxRuleAsync(request.Jurisdiction, request.TaxCategory, "VAT", request.PeriodEnd, cancellationToken);
        var vatRate = vatRule?.Rate ?? 0m;
        var vat = FeesTaxDistributionFormulaEngine.Vat(netFees, vatRate);
        var vatCalculation = VatCalculation.Create(run.Id, null, vatRule?.Id, netFees, vatRate, vat, request.FormulaVersion, actor, null);
        var vatTaxCalculation = TaxCalculation.Create(run.Id, null, vatRule?.Id, TaxCalculationType.Vat, "D-FML-023", request.FormulaVersion, netFees, vatRate, vat, Snapshot(new { TaxableFeeAmount = netFees, VatRate = vatRate }), Snapshot(new { Vat = vat }), actor, null);
        run.AddVatCalculation(vatCalculation);
        run.AddTaxCalculation(vatTaxCalculation);

        var whtRule = await FindTaxRuleAsync(request.Jurisdiction, request.TaxCategory, "WHT", request.PeriodEnd, cancellationToken)
            ?? await FindTaxRuleAsync(request.Jurisdiction, request.TaxCategory, "WithholdingTax", request.PeriodEnd, cancellationToken);
        var whtRate = whtRule?.Rate ?? 0m;
        var wht = FeesTaxDistributionFormulaEngine.WithholdingTax(netFees, whtRate);
        var withholdingCalculation = WithholdingTaxCalculation.Create(run.Id, null, whtRule?.Id, netFees, whtRate, wht, request.FormulaVersion, actor, null);
        var whtTaxCalculation = TaxCalculation.Create(run.Id, null, whtRule?.Id, TaxCalculationType.WithholdingTax, "D-FML-024", request.FormulaVersion, netFees, whtRate, wht, Snapshot(new { TaxablePaymentBase = netFees, WithholdingTaxRate = whtRate }), Snapshot(new { WithholdingTax = wht }), actor, null);
        run.AddWithholdingTaxCalculation(withholdingCalculation);
        run.AddTaxCalculation(whtTaxCalculation);

        var ter = request.AverageNav > 0m
            ? FeesTaxDistributionFormulaEngine.TotalExpenseRatio(netFees + vat, request.AverageNav)
            : throw Validation("averageNav", "Average NAV must be greater than zero for TER calculation.");
        run.SetTotalExpenseRatio(ter);

        _dbContext.FeeAccrualRuns.Add(run);
        await SaveHandlingValidationAsync(cancellationToken);

        var dto = MapFeeAccrualRun(run);
        await WriteAuditAsync(AuditEventType.Created, "FeeAccrualRunCreated", "FeeAccrualRun", run.Id.ToString(), null, Snapshot(dto), "Fee accrual run calculated and created.", cancellationToken);
        return dto;
    }

    public async Task<FeeAccrualRunDto> GetFeeAccrualRunAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var run = await LoadFeeAccrualRunAsync(id, asTracking: false, cancellationToken);
        return MapFeeAccrualRun(run);
    }

    public async Task<FeeWaiverRequestDto> CreateFeeWaiverAsync(CreateFeeWaiverRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        await EnsureActiveSchemeClassAsync(request.SchemeId, request.SchemeClassId, cancellationToken);
        if (request.InvestorId.HasValue)
        {
            await EnsureInvestorExistsAsync(request.InvestorId.Value, cancellationToken);
        }

        var waiver = CreateValidated(() => FeeWaiverRequest.Create(
            request.SchemeId,
            request.SchemeClassId,
            request.InvestorId,
            request.FeeType,
            BusinessDate.From(request.EffectiveFrom),
            request.EffectiveTo.HasValue ? BusinessDate.From(request.EffectiveTo.Value) : null,
            request.WaiverRate,
            request.WaiverAmount,
            request.Reason,
            actor,
            now));

        _dbContext.FeeWaiverRequests.Add(waiver);
        await SaveHandlingValidationAsync(cancellationToken);

        var dto = MapFeeWaiver(waiver);
        await WriteAuditAsync(AuditEventType.Created, "FeeWaiverRequested", "FeeWaiverRequest", waiver.Id.ToString(), null, Snapshot(dto), "Fee waiver submitted for approval.", cancellationToken);
        return dto;
    }

    public async Task<FeeWaiverRequestDto> ApproveFeeWaiverAsync(Guid id, ApproveFeeWaiverRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var waiver = await _dbContext.FeeWaiverRequests.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException("Fee waiver request was not found.");
        var before = Snapshot(MapFeeWaiver(waiver));

        try
        {
            waiver.Approve(actor, now, request.Comment);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("feeWaiver", exception.Message);
        }

        await SaveHandlingValidationAsync(cancellationToken);
        var dto = MapFeeWaiver(waiver);
        await WriteAuditAsync(AuditEventType.Approved, "FeeWaiverApproved", "FeeWaiverRequest", waiver.Id.ToString(), before, Snapshot(dto), request.Comment ?? "Fee waiver approved.", cancellationToken);
        return dto;
    }

    public async Task<TaxRuleDto> CreateTaxRuleAsync(CreateTaxRuleRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var rule = CreateValidated(() => TaxRule.Create(
            request.Jurisdiction,
            request.Category,
            request.TaxType,
            request.Rate,
            BusinessDate.From(request.EffectiveFrom),
            request.EffectiveTo.HasValue ? BusinessDate.From(request.EffectiveTo.Value) : null,
            actor,
            now));

        _dbContext.TaxRules.Add(rule);
        await SaveHandlingValidationAsync(cancellationToken);

        var dto = MapTaxRule(rule);
        await WriteAuditAsync(AuditEventType.Created, "TaxRuleCreated", "TaxRule", rule.Id.ToString(), null, Snapshot(dto), "Tax rule created.", cancellationToken);
        return dto;
    }

    public async Task<DistributionDeclarationDto> CreateDistributionDeclarationAsync(CreateDistributionDeclarationRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        await EnsureActiveSchemeClassAsync(request.SchemeId, request.SchemeClassId, cancellationToken);

        var declaration = CreateValidated(() => DistributionDeclaration.Create(
            request.SchemeId,
            request.SchemeClassId,
            BusinessDate.From(request.DeclarationDate),
            BusinessDate.From(request.RecordDate),
            BusinessDate.From(request.PaymentDate),
            request.FormulaVersion,
            request.InvestmentIncome,
            request.OtherIncome,
            request.RealizedGainsLosses,
            request.PriorPeriodAdjustments,
            request.FundExpenses,
            request.Fees,
            request.Taxes,
            request.ReserveTransfers,
            request.EligibleUnits,
            request.AvailableCash,
            request.CoverageOverrideRequested,
            request.CoverageOverrideReason,
            actor,
            now));

        _dbContext.DistributionDeclarations.Add(declaration);
        await SaveHandlingValidationAsync(cancellationToken);

        var dto = MapDeclaration(declaration);
        await WriteAuditAsync(AuditEventType.Created, "DistributionDeclarationCreated", "DistributionDeclaration", declaration.Id.ToString(), null, Snapshot(dto), "Distribution declaration created.", cancellationToken);
        return dto;
    }

    public async Task<DistributionDeclarationDto> ApproveDistributionDeclarationAsync(Guid id, ApproveDistributionDeclarationRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var declaration = await _dbContext.DistributionDeclarations.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException("Distribution declaration was not found.");
        var before = Snapshot(MapDeclaration(declaration));

        try
        {
            declaration.Approve(actor, now, request.ApproveCoverageOverride, request.Comment);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("distributionDeclaration", exception.Message);
        }

        await SaveHandlingValidationAsync(cancellationToken);
        var dto = MapDeclaration(declaration);
        await WriteAuditAsync(AuditEventType.Approved, "DistributionDeclarationApproved", "DistributionDeclaration", declaration.Id.ToString(), before, Snapshot(dto), request.Comment ?? "Distribution declaration approved.", cancellationToken);
        return dto;
    }

    public async Task<DistributionRunDto> CreateDistributionRunAsync(CreateDistributionRunRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        if (request.Investors.Count == 0)
        {
            throw Validation("investors", "At least one investor distribution is required.");
        }

        var declaration = await _dbContext.DistributionDeclarations.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == request.DeclarationId, cancellationToken)
            ?? throw new NotFoundException("Distribution declaration was not found.");
        if (declaration.Status != DistributionDeclarationStatus.Approved)
        {
            throw Validation("declarationId", "Distribution run requires an approved declaration.");
        }

        var run = DistributionRun.Create(declaration.Id, declaration.SchemeId, declaration.SchemeClassId, declaration.PaymentDate, actor, now);
        foreach (var investorRequest in request.Investors)
        {
            await EnsureApprovedInvestorAsync(investorRequest.InvestorId, cancellationToken);
            var method = ParseEnum<DistributionMethod>(investorRequest.DistributionMethod, "distributionMethod");
            var jurisdiction = investorRequest.Jurisdiction ?? "KE";
            var taxCategory = investorRequest.TaxCategory ?? "DISTRIBUTION";
            var taxRule = await FindTaxRuleAsync(jurisdiction, taxCategory, "WHT", declaration.RecordDate.Value, cancellationToken)
                ?? await FindTaxRuleAsync(jurisdiction, taxCategory, "WithholdingTax", declaration.RecordDate.Value, cancellationToken);
            var taxRate = taxRule?.Rate ?? 0m;
            var gross = FeesTaxDistributionFormulaEngine.InvestorGrossDistribution(investorRequest.EligibleUnits, declaration.DistributionPerUnit);
            var tax = FeesTaxDistributionFormulaEngine.WithholdingTax(gross, taxRate);
            var net = FeesTaxDistributionFormulaEngine.InvestorNetDistribution(gross, tax);
            var investorReturn = FeesTaxDistributionFormulaEngine.InvestorReturnForPeriod(investorRequest.ClosingValue, investorRequest.OpeningValue, investorRequest.CashDistributions, investorRequest.NetContributions);

            var investorDistribution = InvestorDistribution.Create(
                run.Id,
                investorRequest.InvestorId,
                investorRequest.EligibleUnits,
                declaration.DistributionPerUnit,
                gross,
                tax,
                net,
                method,
                investorRequest.OpeningValue,
                investorRequest.ClosingValue,
                investorRequest.CashDistributions,
                investorRequest.NetContributions,
                investorReturn,
                taxRule?.Id,
                declaration.FormulaVersion,
                actor,
                declaration.ApprovedByUserId);
            run.AddInvestorDistribution(investorDistribution);

            var taxCalculation = TaxCalculation.Create(null, investorDistribution.Id, taxRule?.Id, TaxCalculationType.InvestorDistributionTax, "D-FML-031", declaration.FormulaVersion, gross, taxRate, tax, Snapshot(new { GrossDistribution = gross, WhtRate = taxRate }), Snapshot(new { InvestorTax = tax }), actor, declaration.ApprovedByUserId);
            _dbContext.TaxCalculations.Add(taxCalculation);

            if (method == DistributionMethod.Reinvest)
            {
                var reinvestmentUnits = FeesTaxDistributionFormulaEngine.ReinvestmentUnits(net, request.ReinvestmentPrice);
                var instruction = ReinvestmentInstruction.Create(run.Id, investorRequest.InvestorId, request.ReinvestmentPrice, actor);
                var allocation = ReinvestmentUnitAllocation.Create(run.Id, investorDistribution.Id, instruction.Id, investorRequest.InvestorId, net, request.ReinvestmentPrice, reinvestmentUnits, $"DIST-{instruction.Id:N}", declaration.FormulaVersion);
                run.AddReinvestmentInstruction(instruction);
                run.AddReinvestmentUnitAllocation(allocation);
            }
        }

        _dbContext.DistributionRuns.Add(run);
        await SaveHandlingValidationAsync(cancellationToken);

        var dto = MapDistributionRun(run);
        await WriteAuditAsync(AuditEventType.Created, "DistributionRunCreated", "DistributionRun", run.Id.ToString(), null, Snapshot(dto), "Distribution run created.", cancellationToken);
        return dto;
    }

    public async Task<DistributionRunDto> PublishDistributionRunAsync(Guid id, PublishDistributionRunRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var run = await LoadDistributionRunAsync(id, asTracking: true, cancellationToken);
        var declaration = await _dbContext.DistributionDeclarations.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == run.DeclarationId, cancellationToken)
            ?? throw new NotFoundException("Distribution declaration was not found.");
        if (declaration.Status != DistributionDeclarationStatus.Approved)
        {
            throw Validation("distributionRun", "Distribution run cannot be published before declaration approval.");
        }

        var before = Snapshot(MapDistributionRun(run));
        try
        {
            run.Publish(actor, now);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("distributionRun", exception.Message);
        }

        await PostReinvestmentUnitMovementsAsync(run, actor, now, cancellationToken);
        await SaveHandlingValidationAsync(cancellationToken);

        var dto = MapDistributionRun(run);
        await WriteAuditAsync(AuditEventType.Published, "DistributionRunPublished", "DistributionRun", run.Id.ToString(), before, Snapshot(dto), request.Comment ?? "Distribution run published and reinvestment units posted.", cancellationToken);
        return dto;
    }

    public async Task<IReadOnlyCollection<InvestorDistributionDto>> GetInvestorDistributionsAsync(Guid investorId, CancellationToken cancellationToken = default)
    {
        await EnsureInvestorExistsAsync(investorId, cancellationToken);
        var distributions = await _dbContext.InvestorDistributions
            .AsNoTracking()
            .Where(distribution => distribution.InvestorId == investorId)
            .OrderBy(distribution => distribution.DistributionRunId)
            .ToListAsync(cancellationToken);

        return distributions.Select(MapInvestorDistribution).ToArray();
    }

    private void AddFeeCalculation(
        FeeAccrualRun run,
        IReadOnlyCollection<FeeSchedule> schedules,
        IReadOnlyCollection<FeeWaiverRequest> approvedWaivers,
        FeeCalculationType feeCalculationType,
        string scheduleFeeType,
        string formulaCode,
        decimal feeBase,
        decimal? requestedRate,
        int? periodDays,
        decimal? requestedFixedAmount,
        decimal grossAmount,
        CreateFeeAccrualRunRequest request,
        string actor)
    {
        var schedule = ResolveSchedule(schedules, scheduleFeeType);
        var rate = schedule?.Rate ?? requestedRate;
        var fixedAmount = schedule?.FixedAmount ?? requestedFixedAmount;
        var roundedGross = FeesTaxDistributionFormulaEngine.Round(grossAmount);
        var waiverAmount = CalculateApprovedWaiverAmount(approvedWaivers, scheduleFeeType, request.PeriodEnd, roundedGross);
        var inputsJson = Snapshot(new
        {
            FeeBase = feeBase,
            Rate = rate,
            FixedAmount = fixedAmount,
            PeriodDays = periodDays,
            ScheduleId = schedule?.Id,
            Formula = formulaCode
        });
        var outputJson = Snapshot(new { GrossAmount = roundedGross, WaiverAmount = waiverAmount, NetAmount = roundedGross - waiverAmount });
        run.AddCalculation(FeeCalculation.Create(run.Id, feeCalculationType, formulaCode, request.FormulaVersion, feeBase, rate, periodDays, fixedAmount, roundedGross, waiverAmount, inputsJson, outputJson, actor));
    }

    private static decimal CalculateApprovedWaiverAmount(IReadOnlyCollection<FeeWaiverRequest> waivers, string feeType, DateOnly periodEnd, decimal grossAmount)
    {
        var waiverAmount = waivers
            .Where(waiver => waiver.AppliesTo(feeType, periodEnd, investorId: null))
            .Sum(waiver => (waiver.WaiverAmount ?? 0m) + grossAmount * ((waiver.WaiverRate ?? 0m) / 100m));
        return FeesTaxDistributionFormulaEngine.Round(Math.Min(grossAmount, waiverAmount));
    }

    private async Task PostReinvestmentUnitMovementsAsync(DistributionRun run, string actor, DateTime now, CancellationToken cancellationToken)
    {
        var allocations = run.ReinvestmentUnitAllocations.Where(allocation => allocation.UnitLedgerEntryId is null).ToList();
        if (allocations.Count == 0)
        {
            return;
        }

        var holdings = await _dbContext.UnitHoldings
            .Where(holding => holding.SchemeId == run.SchemeId && holding.SchemeClassId == run.SchemeClassId)
            .ToListAsync(cancellationToken);
        var snapshots = await _dbContext.UnitRegisterSnapshots
            .Where(snapshot => snapshot.SchemeId == run.SchemeId && snapshot.SchemeClassId == run.SchemeClassId)
            .ToListAsync(cancellationToken);
        const int unitPrecision = 6;

        foreach (var allocation in allocations)
        {
            var instruction = run.ReinvestmentInstructions.Single(candidate => candidate.Id == allocation.ReinvestmentInstructionId);
            var source = UnitMovementSource.Create(
                UnitMovementSourceType.ReinvestmentInstruction,
                instruction.Id,
                allocation.InvestorId,
                run.SchemeId,
                run.SchemeClassId,
                allocation.SourceReference,
                isApproved: true,
                actor,
                now);
            var entry = UnitLedgerEntry.Create(
                UnitMovementType.Issued,
                allocation.InvestorId,
                run.SchemeId,
                run.SchemeClassId,
                run.RunDate,
                allocation.UnitsAllocated,
                source,
                allocation.SourceReference,
                unitPrecision,
                actor,
                now,
                "Distribution reinvestment unit allocation.");

            _dbContext.UnitMovementSources.Add(source);
            _dbContext.UnitLedgerEntries.Add(entry);

            var holding = holdings.SingleOrDefault(candidate => candidate.InvestorId == allocation.InvestorId);
            if (holding is null)
            {
                holding = UnitHolding.Create(allocation.InvestorId, run.SchemeId, run.SchemeClassId, unitPrecision, actor, now);
                holdings.Add(holding);
                _dbContext.UnitHoldings.Add(holding);
            }

            holding.Apply(entry);

            var position = await _dbContext.InvestorPositions.SingleOrDefaultAsync(candidate =>
                candidate.InvestorId == allocation.InvestorId
                && candidate.SchemeId == run.SchemeId
                && candidate.SchemeClassId == run.SchemeClassId, cancellationToken);
            if (position is null)
            {
                position = InvestorPosition.Create(allocation.InvestorId, run.SchemeId, run.SchemeClassId, unitPrecision, actor, now);
                _dbContext.InvestorPositions.Add(position);
            }

            position.RefreshFrom(holding);

            _dbContext.HistoricalHoldingViews.Add(HistoricalHoldingView.Create(
                allocation.InvestorId,
                run.SchemeId,
                run.SchemeClassId,
                run.RunDate,
                allocation.SourceReference,
                holding.Units,
                holding.LienedUnits,
                unitPrecision,
                entry.Id,
                actor,
                now));

            allocation.LinkUnitLedgerEntry(entry.Id, now);
            instruction.MarkAllocated(actor, now);

            var snapshot = snapshots.SingleOrDefault(candidate => candidate.SnapshotDate == run.RunDate);
            if (snapshot is null)
            {
                snapshot = UnitRegisterSnapshot.Create(run.SchemeId, run.SchemeClassId, run.RunDate, actor, now);
                snapshots.Add(snapshot);
                _dbContext.UnitRegisterSnapshots.Add(snapshot);
            }

            snapshot.Refresh(
                holdings.Sum(candidate => candidate.Units),
                holdings.Sum(candidate => candidate.LienedUnits),
                holdings.Count(candidate => candidate.Units != 0m || candidate.LienedUnits != 0m),
                allocation.SourceReference);

            await WriteAuditAsync(AuditEventType.Created, "ReinvestmentUnitMovementPosted", "UnitLedgerEntry", entry.Id.ToString(), null, Snapshot(new { entry.Id, allocation.InvestorId, allocation.UnitsAllocated, allocation.SourceReference }), "Reinvestment unit movement posted from approved distribution run.", cancellationToken);
        }
    }

    private async Task<FeeAccrualRun> LoadFeeAccrualRunAsync(Guid id, bool asTracking, CancellationToken cancellationToken)
    {
        var query = _dbContext.FeeAccrualRuns
            .Include(run => run.Calculations)
            .Include(run => run.VatCalculations)
            .Include(run => run.WithholdingTaxCalculations)
            .AsSplitQuery();
        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException("Fee accrual run was not found.");
    }

    private async Task<DistributionRun> LoadDistributionRunAsync(Guid id, bool asTracking, CancellationToken cancellationToken)
    {
        var query = _dbContext.DistributionRuns
            .Include(run => run.InvestorDistributions)
            .Include(run => run.ReinvestmentInstructions)
            .Include(run => run.ReinvestmentUnitAllocations)
            .AsSplitQuery();
        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException("Distribution run was not found.");
    }

    private async Task<IReadOnlyCollection<FeeSchedule>> LoadApplicableFeeSchedulesAsync(Guid schemeId, Guid schemeClassId, DateOnly effectiveDate, CancellationToken cancellationToken)
    {
        var schedules = await _dbContext.FeeSchedules
            .AsNoTracking()
            .Include(schedule => schedule.Rules)
            .Where(schedule => schedule.SchemeId == schemeId
                && (schedule.SchemeClassId == null || schedule.SchemeClassId == schemeClassId)
                && schedule.Status == FeeScheduleStatus.Active)
            .ToListAsync(cancellationToken);

        return schedules
            .Where(schedule => schedule.EffectiveFrom.Value <= effectiveDate && (schedule.EffectiveTo is null || effectiveDate <= schedule.EffectiveTo.Value))
            .ToArray();
    }

    private static FeeSchedule? ResolveSchedule(IReadOnlyCollection<FeeSchedule> schedules, string feeType)
    {
        return schedules
            .Where(schedule => string.Equals(schedule.FeeType, feeType, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(schedule => schedule.SchemeClassId.HasValue)
            .ThenByDescending(schedule => schedule.EffectiveFrom.Value)
            .FirstOrDefault();
    }

    private static decimal ResolveRate(IReadOnlyCollection<FeeSchedule> schedules, string feeType, decimal fallbackRate)
    {
        return ResolveSchedule(schedules, feeType)?.Rate ?? fallbackRate;
    }

    private static IReadOnlyCollection<FeeTierInput>? ToTierInputs(FeeSchedule? schedule)
    {
        return schedule?.Rules.Count > 0
            ? schedule.Rules.Select(rule => new FeeTierInput(rule.FromAmount, rule.ToAmount, rule.Rate, rule.FixedAmount)).ToArray()
            : null;
    }

    private async Task<TaxRule?> FindTaxRuleAsync(string jurisdiction, string category, string taxType, DateOnly effectiveDate, CancellationToken cancellationToken)
    {
        var normalizedJurisdiction = jurisdiction.Trim().ToUpperInvariant();
        var normalizedCategory = category.Trim().ToUpperInvariant();
        var normalizedTaxType = taxType.Trim().ToUpperInvariant();
        var rules = await _dbContext.TaxRules
            .AsNoTracking()
            .Where(rule => rule.Jurisdiction == normalizedJurisdiction
                && rule.Category == normalizedCategory
                && rule.TaxType == normalizedTaxType
                && rule.Status == TaxRuleStatus.Active)
            .ToListAsync(cancellationToken);

        return rules
            .Where(rule => rule.IsEffective(effectiveDate))
            .OrderByDescending(rule => rule.EffectiveFrom.Value)
            .FirstOrDefault();
    }

    private async Task EnsureActiveSchemeClassAsync(Guid schemeId, Guid schemeClassId, CancellationToken cancellationToken)
    {
        var scheme = await _dbContext.Schemes.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == schemeId, cancellationToken)
            ?? throw new NotFoundException("Scheme was not found.");
        if (scheme.Status != SchemeStatus.Active)
        {
            throw Validation("schemeId", "Fees and distributions require an active scheme.");
        }

        var schemeClass = await _dbContext.SchemeClasses.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == schemeClassId && candidate.SchemeId == schemeId, cancellationToken)
            ?? throw new NotFoundException("Scheme class was not found.");
        if (!schemeClass.IsActive)
        {
            throw Validation("schemeClassId", "Fees and distributions require an active scheme class.");
        }
    }

    private async Task EnsureInvestorExistsAsync(Guid investorId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Investors.AnyAsync(investor => investor.Id == investorId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("Investor was not found.");
        }
    }

    private async Task EnsureApprovedInvestorAsync(Guid investorId, CancellationToken cancellationToken)
    {
        var investor = await _dbContext.Investors.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == investorId, cancellationToken)
            ?? throw new NotFoundException("Investor was not found.");
        if (investor.Status != InvestorStatus.Approved)
        {
            throw Validation("investorId", "Distribution runs cannot be created for an unapproved investor.");
        }
    }

    private static FeeAccrualRunDto MapFeeAccrualRun(FeeAccrualRun run)
    {
        return new FeeAccrualRunDto(
            run.Id,
            run.SchemeId,
            run.SchemeClassId,
            run.PeriodStart.Value,
            run.PeriodEnd.Value,
            run.RunNumber,
            run.FormulaVersion,
            run.DayCountBasis.ToString(),
            run.Status.ToString(),
            run.OwnerUserId,
            run.CalculatedAtUtc,
            run.ApprovedByUserId,
            run.ApprovedAtUtc,
            run.TotalFeeAmount,
            run.TotalWaiverAmount,
            run.TotalVatAmount,
            run.TotalWithholdingTaxAmount,
            run.NetPayableAmount,
            run.TotalExpenseRatio,
            run.Calculations.OrderBy(calculation => calculation.FormulaCode).Select(MapFeeCalculation).ToArray(),
            run.VatCalculations.Select(MapVatCalculation).ToArray(),
            run.WithholdingTaxCalculations.Select(MapWithholdingTaxCalculation).ToArray());
    }

    private static FeeCalculationDto MapFeeCalculation(FeeCalculation calculation)
    {
        return new FeeCalculationDto(calculation.Id, calculation.FeeType.ToString(), calculation.FormulaCode, calculation.FormulaVersion, calculation.ApplicableFeeBase, calculation.AnnualRate, calculation.PeriodDays, calculation.FixedAmount, calculation.GrossAmount, calculation.WaiverAmount, calculation.NetAmount, calculation.OwnerUserId, calculation.ApprovedByUserId);
    }

    private static VatCalculationDto MapVatCalculation(VatCalculation calculation)
    {
        return new VatCalculationDto(calculation.Id, calculation.TaxableFeeAmount, calculation.VatRate, calculation.VatAmount, calculation.TaxRuleId, calculation.OwnerUserId, calculation.ApproverUserId);
    }

    private static WithholdingTaxCalculationDto MapWithholdingTaxCalculation(WithholdingTaxCalculation calculation)
    {
        return new WithholdingTaxCalculationDto(calculation.Id, calculation.TaxablePaymentBase, calculation.WithholdingTaxRate, calculation.WithholdingTaxAmount, calculation.TaxRuleId, calculation.OwnerUserId, calculation.ApproverUserId);
    }

    private static FeeWaiverRequestDto MapFeeWaiver(FeeWaiverRequest waiver)
    {
        return new FeeWaiverRequestDto(waiver.Id, waiver.SchemeId, waiver.SchemeClassId, waiver.InvestorId, waiver.FeeType, waiver.EffectiveFrom.Value, waiver.EffectiveTo?.Value, waiver.WaiverRate, waiver.WaiverAmount, waiver.Reason, waiver.Status.ToString(), waiver.RequestedByUserId, waiver.RequestedAtUtc, waiver.ApprovedByUserId, waiver.ApprovedAtUtc, waiver.DecisionComment);
    }

    private static TaxRuleDto MapTaxRule(TaxRule rule)
    {
        return new TaxRuleDto(rule.Id, rule.Jurisdiction, rule.Category, rule.TaxType, rule.Rate, rule.EffectiveFrom.Value, rule.EffectiveTo?.Value, rule.Status.ToString());
    }

    private static DistributionDeclarationDto MapDeclaration(DistributionDeclaration declaration)
    {
        return new DistributionDeclarationDto(declaration.Id, declaration.SchemeId, declaration.SchemeClassId, declaration.DeclarationDate.Value, declaration.RecordDate.Value, declaration.PaymentDate.Value, declaration.FormulaVersion, declaration.GrossDistributableIncome, declaration.NetDistributableIncome, declaration.EligibleUnits, declaration.DistributionPerUnit, declaration.AvailableCash, declaration.PlannedDistributionAmount, declaration.DistributionCoverage, declaration.CoverageOverrideRequested, declaration.CoverageOverrideApproved, declaration.Status.ToString(), declaration.CreatedByUserId, declaration.CreatedAtUtc, declaration.ApprovedByUserId, declaration.ApprovedAtUtc);
    }

    private static DistributionRunDto MapDistributionRun(DistributionRun run)
    {
        return new DistributionRunDto(run.Id, run.DeclarationId, run.SchemeId, run.SchemeClassId, run.RunDate.Value, run.RunNumber, run.Status.ToString(), run.CreatedByUserId, run.CreatedAtUtc, run.PublishedByUserId, run.PublishedAtUtc, run.TotalGrossDistribution, run.TotalTaxAmount, run.TotalNetDistribution, run.TotalReinvestedUnits, run.InvestorDistributions.Select(MapInvestorDistribution).ToArray(), run.ReinvestmentUnitAllocations.Select(MapReinvestmentAllocation).ToArray());
    }

    private static InvestorDistributionDto MapInvestorDistribution(InvestorDistribution distribution)
    {
        return new InvestorDistributionDto(distribution.Id, distribution.DistributionRunId, distribution.InvestorId, distribution.EligibleUnits, distribution.DistributionPerUnit, distribution.GrossDistribution, distribution.InvestorTaxAmount, distribution.NetDistribution, distribution.Method.ToString(), distribution.InvestorReturnForPeriod, distribution.TaxRuleId);
    }

    private static ReinvestmentUnitAllocationDto MapReinvestmentAllocation(ReinvestmentUnitAllocation allocation)
    {
        return new ReinvestmentUnitAllocationDto(allocation.Id, allocation.DistributionRunId, allocation.InvestorId, allocation.InvestorDistributionId, allocation.ReinvestmentInstructionId, allocation.UnitLedgerEntryId, allocation.NetDistribution, allocation.ReinvestmentPrice, allocation.UnitsAllocated, allocation.SourceReference, allocation.AllocatedAtUtc);
    }

    private string CurrentUserIdOrThrow()
    {
        return _currentUserContext.UserId ?? throw new UnauthorizedAccessException("Authenticated user id is required.");
    }

    private Task WriteAuditAsync(AuditEventType eventType, string action, string entityName, string entityId, string? beforeJson, string? afterJson, string reason, CancellationToken cancellationToken)
    {
        return _auditWriter.WriteAsync(new AuditLogEntry(
            ModuleName,
            action,
            entityName,
            entityId,
            EventType: eventType,
            Summary: reason,
            BeforeJson: beforeJson,
            AfterJson: afterJson,
            Reason: reason), cancellationToken);
    }

    private async Task SaveHandlingValidationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is not null)
        {
            throw new ConflictException("The fees, tax, or distribution change conflicts with an existing record.");
        }
    }

    private static T CreateValidated<T>(Func<T> factory)
    {
        try
        {
            return factory();
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("feesTaxDistribution", exception.Message);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "feesTaxDistribution", exception.Message);
        }
    }

    private static TEnum ParseEnum<TEnum>(string value, string fieldName)
        where TEnum : struct
    {
        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
        {
            throw Validation(fieldName, $"Unsupported {fieldName} value '{value}'.");
        }

        return parsed;
    }

    private static ValidationException Validation(string fieldName, string message)
    {
        return new ValidationException(new Dictionary<string, string[]>
        {
            [fieldName] = [message]
        });
    }

    private static string Snapshot<T>(T? value)
    {
        return value is null ? "{}" : JsonSerializer.Serialize(value);
    }
}
