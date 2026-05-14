using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.NAV;
using Cis.Domain.Audit;
using Cis.Domain.Common;
using Cis.Domain.NAV;
using Cis.Domain.Schemes;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.NAV;

internal sealed class NavService : INavService
{
    private const string ModuleName = "NAV";
    private readonly CisDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public NavService(
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

    public async Task<ValuationRunDto> CreateValuationRunAsync(CreateValuationRunRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var dayCountBasis = ParseEnum<DayCountBasis>(request.DayCountBasis, "dayCountBasis");
        var valuationDate = BusinessDate.From(request.ValuationDate);

        if (request.Instruments is null || request.Instruments.Count == 0)
        {
            throw Validation("instruments", "At least one instrument valuation input is required.");
        }

        await EnsureSchemeClassActiveAsync(request.SchemeId, request.SchemeClassId, cancellationToken);
        await EnsureInstrumentsExistAsync(request.Instruments.Select(instrument => instrument.InstrumentId).Distinct().ToArray(), cancellationToken);

        var run = ValuationRun.Create(
            request.SchemeId,
            request.SchemeClassId,
            valuationDate,
            request.FormulaVersion,
            dayCountBasis,
            request.ReportPrecision,
            request.PriceStaleAfterDays,
            request.PriceVarianceTolerancePercent,
            request.AllowsAmortizedCost,
            actor,
            now);

        AddConfiguredFormulaInputs(run, request);

        var approvedOverrides = await GetApprovedOverridesAsync(request, cancellationToken);
        var appliedOverrideIds = new List<Guid>();

        foreach (var instrument in request.Instruments)
        {
            await EnsurePriceSourceHierarchyAsync(request, instrument.InstrumentType, actor, now, cancellationToken);
            var valuation = BuildInstrumentValuation(run, instrument, approvedOverrides, out var stalePriceException, out var varianceException, out var appliedOverrideId);
            run.AddInstrumentValuation(valuation);
            run.AddInput(ValuationInput.Create(run.Id, ValuationInputType.InvestmentValue, $"Investment value for {instrument.InstrumentId}", valuation.InvestmentValue, valuation.Quantity, valuation.MarketPrice, "D-FML-002", instrument.PriceSource));
            run.AddInput(ValuationInput.Create(run.Id, ValuationInputType.AccruedIncome, $"Accrued income for {instrument.InstrumentId}", valuation.AccruedIncome, null, null, "D-FML-003", instrument.PriceSource));
            run.AddSource(ValuationSource.Create(
                run.Id,
                valuation.OverrideApplied ? ValuationSourceType.ManualOverride : ValuationSourceType.MarketPrice,
                valuation.OverrideApplied ? "ApprovedManualOverride" : instrument.PriceSource,
                valuation.OverrideApplied ? valuation.ManualValuationOverrideId!.Value.ToString() : instrument.InstrumentId.ToString(),
                now,
                valuation.OverrideApplied || !valuation.IsPriceMissing,
                valuation.OverrideApplied));

            if (stalePriceException is not null)
            {
                run.AddStalePriceException(stalePriceException);
            }

            if (varianceException is not null)
            {
                run.AddPricingVarianceException(varianceException);
            }

            if (appliedOverrideId.HasValue)
            {
                appliedOverrideIds.Add(appliedOverrideId.Value);
            }
        }

        _dbContext.ValuationRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = MapRun(run);
        await WriteAuditAsync(AuditEventType.Created, "ValuationRunCreated", "ValuationRun", run.Id.ToString(), null, Snapshot(dto), "Valuation run created.", cancellationToken);

        foreach (var overrideId in appliedOverrideIds.Distinct())
        {
            await WriteAuditAsync(AuditEventType.OverrideApplied, "ManualValuationOverrideApplied", "ManualValuationOverride", overrideId.ToString(), null, Snapshot(dto), $"Approved manual override {overrideId} applied to valuation run {run.Id}.", cancellationToken);
        }

        return dto;
    }

    public async Task<ValuationRunDto> CalculateValuationRunAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var run = await LoadRunForUpdateAsync(id, cancellationToken);

        try
        {
            var output = BuildFormulaOutput(run);
            run.Calculate(output, actor, now);
            MarkLatestCalculationArtifactsAdded(run);
        }
        catch (ArgumentException ex)
        {
            throw Validation("valuationRun", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            throw Validation("valuationRun", ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        var dto = MapRun(run);
        await WriteAuditAsync(AuditEventType.Updated, "ValuationRunCalculated", "ValuationRun", run.Id.ToString(), null, Snapshot(dto), "NAV formulas calculated.", cancellationToken);
        return dto;
    }

    public async Task<ValuationRunDto> SubmitValuationRunAsync(Guid id, NavWorkflowActionRequest request, CancellationToken cancellationToken = default)
    {
        var run = await LoadRunForUpdateAsync(id, cancellationToken);
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;

        try
        {
            run.Submit(actor, now, request.Comment);
            MarkLatestApprovalAdded(run);
        }
        catch (InvalidOperationException ex)
        {
            throw Validation("valuationRun", ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        var dto = MapRun(run);
        await WriteAuditAsync(AuditEventType.Submitted, "ValuationRunSubmitted", "ValuationRun", run.Id.ToString(), null, Snapshot(dto), request.Comment ?? "Valuation run submitted.", cancellationToken);
        return dto;
    }

    public async Task<ValuationRunDto> CheckValuationRunAsync(Guid id, NavWorkflowActionRequest request, CancellationToken cancellationToken = default)
    {
        var run = await LoadRunForUpdateAsync(id, cancellationToken);
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;

        try
        {
            run.Check(actor, now, request.Comment);
            MarkLatestApprovalAdded(run);
        }
        catch (InvalidOperationException ex)
        {
            throw Validation("valuationRun", ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        var dto = MapRun(run);
        await WriteAuditAsync(AuditEventType.Checked, "ValuationRunChecked", "ValuationRun", run.Id.ToString(), null, Snapshot(dto), request.Comment ?? "Valuation run checked.", cancellationToken);
        return dto;
    }

    public async Task<ValuationRunDto> ApproveValuationRunAsync(Guid id, NavWorkflowActionRequest request, CancellationToken cancellationToken = default)
    {
        var run = await LoadRunForUpdateAsync(id, cancellationToken);
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;

        try
        {
            run.Approve(actor, now, request.Comment);
            MarkLatestApprovalAdded(run);
        }
        catch (InvalidOperationException ex)
        {
            throw Validation("valuationRun", ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        var dto = MapRun(run);
        await WriteAuditAsync(AuditEventType.Approved, "ValuationRunApproved", "ValuationRun", run.Id.ToString(), null, Snapshot(dto), request.Comment ?? "Valuation run approved.", cancellationToken);
        return dto;
    }

    public async Task<NavPublicationDto> PublishValuationRunAsync(Guid id, NavWorkflowActionRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var run = await LoadRunForUpdateAsync(id, cancellationToken);

        var calculation = LatestCalculation(run) ?? throw Validation("valuationRun", "Valuation run must be calculated before publication.");
        var navPerUnit = LatestNavPerUnit(run) ?? throw Validation("valuationRun", "Valuation run must have NAV per unit before publication.");
        var versionNumber = await NextVersionNumberAsync(run.SchemeId, run.SchemeClassId, run.ValuationDate, cancellationToken);

        try
        {
            run.Publish(actor, now, versionNumber, request.Comment);
            MarkLatestApprovalAdded(run);
        }
        catch (InvalidOperationException ex)
        {
            throw Validation("valuationRun", ex.Message);
        }

        var publication = NavPublication.Create(
            run.Id,
            run.SchemeId,
            run.SchemeClassId,
            run.ValuationDate,
            versionNumber,
            calculation.NetAssetValue,
            navPerUnit.UnitPrice,
            navPerUnit.ReportedUnitPrice,
            run.FormulaVersion,
            actor,
            now);

        var publicationDto = MapPublication(publication);
        var payloadJson = Snapshot(new
        {
            Publication = publicationDto,
            Calculation = MapCalculation(calculation),
            NavPerUnit = MapNavPerUnit(navPerUnit),
            run.StalePriceExceptions,
            run.PricingVarianceExceptions
        });
        var archive = NavVersionArchive.Create(run.Id, publication.Id, versionNumber, payloadJson, Sha256(payloadJson), actor, now);

        _dbContext.NavPublications.Add(publication);
        _dbContext.NavVersionArchives.Add(archive);
        await _dbContext.SaveChangesAsync(cancellationToken);

        publicationDto = MapPublication(publication);
        await WriteAuditAsync(AuditEventType.Published, "NavPublished", "NavPublication", publication.Id.ToString(), null, Snapshot(publicationDto), request.Comment ?? "NAV published.", cancellationToken);
        return publicationDto;
    }

    public async Task<ManualValuationOverrideDto> CreateManualOverrideAsync(CreateManualValuationOverrideRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        await EnsureSchemeClassActiveAsync(request.SchemeId, request.SchemeClassId, cancellationToken);
        await EnsureInstrumentsExistAsync([request.InstrumentId], cancellationToken);

        var manualOverride = ManualValuationOverride.Create(
            request.SchemeId,
            request.SchemeClassId,
            BusinessDate.From(request.ValuationDate),
            request.InstrumentId,
            request.OverridePrice,
            request.OverrideValue,
            request.Reason,
            actor,
            now);

        _dbContext.ManualValuationOverrides.Add(manualOverride);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = MapOverride(manualOverride);
        await WriteAuditAsync(AuditEventType.Created, "ManualValuationOverrideCreated", "ManualValuationOverride", manualOverride.Id.ToString(), null, Snapshot(dto), request.Reason, cancellationToken);
        return dto;
    }

    public async Task<ManualValuationOverrideDto> ApproveManualOverrideAsync(Guid id, ApproveManualValuationOverrideRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var manualOverride = await _dbContext.ManualValuationOverrides
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Manual valuation override not found.");

        try
        {
            manualOverride.Approve(actor, now, request.Comment);
        }
        catch (InvalidOperationException ex)
        {
            throw Validation("manualOverride", ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        var dto = MapOverride(manualOverride);
        await WriteAuditAsync(AuditEventType.Approved, "ManualValuationOverrideApproved", "ManualValuationOverride", manualOverride.Id.ToString(), null, Snapshot(dto), request.Comment ?? "Manual valuation override approved.", cancellationToken);
        return dto;
    }

    public async Task<NavRestatementDto> CreateRestatementAsync(CreateNavRestatementRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var original = await _dbContext.NavPublications
            .AsNoTracking()
            .FirstOrDefaultAsync(publication => publication.Id == request.OriginalPublicationId, cancellationToken)
            ?? throw new NotFoundException("Original NAV publication not found.");

        var correctedRun = await LoadRunForUpdateAsync(request.CorrectedValuationRunId, cancellationToken);
        if (correctedRun.Status != ValuationRunStatus.Approved)
        {
            throw Validation("correctedValuationRunId", "Corrected valuation run must be approved before restatement.");
        }

        if (correctedRun.SchemeId != original.SchemeId
            || correctedRun.SchemeClassId != original.SchemeClassId
            || correctedRun.ValuationDate.Value != original.ValuationDate.Value)
        {
            throw Validation("correctedValuationRunId", "Corrected valuation run must match the original publication scheme, class, and valuation date.");
        }

        var calculation = LatestCalculation(correctedRun) ?? throw Validation("correctedValuationRunId", "Corrected valuation run must have a calculation.");
        var navPerUnit = LatestNavPerUnit(correctedRun) ?? throw Validation("correctedValuationRunId", "Corrected valuation run must have NAV per unit.");
        var versionNumber = await NextVersionNumberAsync(original.SchemeId, original.SchemeClassId, original.ValuationDate, cancellationToken);
        correctedRun.Publish(actor, now, versionNumber, $"Restatement: {request.Reason}");
        MarkLatestApprovalAdded(correctedRun);

        var correctedPublication = NavPublication.Create(
            correctedRun.Id,
            correctedRun.SchemeId,
            correctedRun.SchemeClassId,
            correctedRun.ValuationDate,
            versionNumber,
            calculation.NetAssetValue,
            navPerUnit.UnitPrice,
            navPerUnit.ReportedUnitPrice,
            correctedRun.FormulaVersion,
            actor,
            now);

        var payloadJson = Snapshot(new
        {
            Publication = MapPublication(correctedPublication),
            Calculation = MapCalculation(calculation),
            NavPerUnit = MapNavPerUnit(navPerUnit),
            RestatementReason = request.Reason
        });
        var archive = NavVersionArchive.Create(correctedRun.Id, correctedPublication.Id, versionNumber, payloadJson, Sha256(payloadJson), actor, now);
        var restatement = NavRestatement.Create(
            original.Id,
            correctedRun.Id,
            correctedPublication.Id,
            correctedRun.SchemeId,
            correctedRun.SchemeClassId,
            correctedRun.ValuationDate,
            versionNumber,
            request.Reason,
            actor,
            now);

        _dbContext.NavPublications.Add(correctedPublication);
        _dbContext.NavVersionArchives.Add(archive);
        _dbContext.NavRestatements.Add(restatement);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = MapRestatement(restatement);
        await WriteAuditAsync(AuditEventType.Published, "NavRestatementPublished", "NavRestatement", restatement.Id.ToString(), null, Snapshot(dto), request.Reason, cancellationToken);
        return dto;
    }

    public async Task<IReadOnlyCollection<NavPublicationDto>> GetHistoryAsync(
        Guid? schemeId,
        Guid? schemeClassId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.NavPublications.AsNoTracking().AsQueryable();
        if (schemeId.HasValue)
        {
            query = query.Where(publication => publication.SchemeId == schemeId.Value);
        }

        if (schemeClassId.HasValue)
        {
            query = query.Where(publication => publication.SchemeClassId == schemeClassId.Value);
        }

        var publications = await query.ToListAsync(cancellationToken);
        publications = publications
            .Where(publication => !fromDate.HasValue || publication.ValuationDate.Value >= fromDate.Value)
            .Where(publication => !toDate.HasValue || publication.ValuationDate.Value <= toDate.Value)
            .OrderByDescending(publication => publication.ValuationDate.Value)
            .ThenByDescending(publication => publication.VersionNumber)
            .ToList();

        return publications.Select(MapPublication).ToList();
    }

    public async Task<NavReconstructionDto> ReconstructAsync(Guid schemeId, DateOnly valuationDate, CancellationToken cancellationToken = default)
    {
        var businessDate = BusinessDate.From(valuationDate);
        var publications = await _dbContext.NavPublications
            .AsNoTracking()
            .Where(publication => publication.SchemeId == schemeId)
            .ToListAsync(cancellationToken);

        var latestPublications = publications
            .Where(publication => publication.ValuationDate.Value == businessDate.Value)
            .GroupBy(publication => publication.SchemeClassId)
            .Select(group => group.OrderByDescending(publication => publication.VersionNumber).First())
            .OrderBy(publication => publication.SchemeClassId)
            .Select(MapPublication)
            .ToList();

        return new NavReconstructionDto(schemeId, valuationDate, latestPublications, _dateTimeProvider.UtcNow);
    }

    private InstrumentValuation BuildInstrumentValuation(
        ValuationRun run,
        InstrumentValuationInputRequest request,
        IReadOnlyDictionary<Guid, ManualValuationOverride> approvedOverrides,
        out StalePriceException? stalePriceException,
        out PricingVarianceException? varianceException,
        out Guid? appliedOverrideId)
    {
        stalePriceException = null;
        varianceException = null;
        appliedOverrideId = null;

        if (request.PriceDate.HasValue && request.PriceDate.Value > run.ValuationDate.Value)
        {
            throw Validation("priceDate", "Price date cannot be after valuation date.");
        }

        if (request.UseAmortizedCost && !run.AllowsAmortizedCost)
        {
            throw Validation("useAmortizedCost", "Amortized cost valuation is not permitted for this valuation run.");
        }

        var approvedOverride = approvedOverrides.TryGetValue(request.InstrumentId, out var foundOverride) ? foundOverride : null;
        var accruedIncome = NavFormulaEngine.AccruedIncome(request.Principal, request.AnnualRate, request.AccrualDays, run.DayCountBasis);
        var dailyAccretion = request.PurchaseCost.HasValue && request.MaturityValue.HasValue && request.TotalDaysToMaturity.HasValue
            ? NavFormulaEngine.DailyAccretion(request.MaturityValue.Value, request.PurchaseCost.Value, request.TotalDaysToMaturity.Value)
            : 0m;

        var currentPrice = approvedOverride?.OverridePrice ?? request.MarketPrice;
        var priceDate = approvedOverride is not null
            ? run.ValuationDate
            : request.PriceDate.HasValue
                ? BusinessDate.From(request.PriceDate.Value)
                : null;

        var isMissingPrice = !currentPrice.HasValue && !(request.UseAmortizedCost && request.AmortizedCost.HasValue);
        var isStale = false;
        if (currentPrice.HasValue && priceDate is null)
        {
            isStale = true;
        }
        else if (currentPrice.HasValue && priceDate is not null)
        {
            var ageDays = run.ValuationDate.Value.DayNumber - priceDate.Value.DayNumber;
            isStale = ageDays > run.PriceStaleAfterDays;
        }

        var investmentValue = approvedOverride is not null
            ? (approvedOverride.OverrideValue > 0m
                ? approvedOverride.OverrideValue
                : NavFormulaEngine.InvestmentValue(request.Quantity, approvedOverride.OverridePrice, accruedIncome, false, null))
            : request.UseAmortizedCost && request.AmortizedCost.HasValue
                ? NavFormulaEngine.InvestmentValue(request.Quantity, 0m, accruedIncome, true, request.AmortizedCost.Value)
                : currentPrice.HasValue
                    ? NavFormulaEngine.InvestmentValue(request.Quantity, currentPrice.Value, accruedIncome, false, null)
                    : 0m;

        if (isMissingPrice || isStale)
        {
            stalePriceException = StalePriceException.Create(
                run.Id,
                request.InstrumentId,
                priceDate,
                run.ValuationDate,
                run.PriceStaleAfterDays,
                isMissingPrice,
                isMissingPrice ? "Market price is missing and no approved override is available." : "Market price is stale against the configured price hierarchy.");
        }

        if (currentPrice.HasValue && request.PriorPrice.HasValue && request.PriorPrice.Value > 0m)
        {
            var variancePercent = NavFormulaEngine.PriceVariancePercentage(currentPrice.Value, request.PriorPrice.Value);
            if (variancePercent > run.PriceVarianceTolerancePercent)
            {
                varianceException = PricingVarianceException.Create(
                    run.Id,
                    request.InstrumentId,
                    currentPrice.Value,
                    request.PriorPrice.Value,
                    variancePercent,
                    run.PriceVarianceTolerancePercent,
                    "Pricing variance exceeds configured tolerance.");
            }
        }

        if (approvedOverride is not null)
        {
            appliedOverrideId = approvedOverride.Id;
        }

        return InstrumentValuation.Create(
            run.Id,
            request.InstrumentId,
            request.InstrumentType,
            request.Quantity,
            currentPrice,
            accruedIncome,
            dailyAccretion,
            request.AmortizedCost,
            request.UseAmortizedCost && approvedOverride is null,
            investmentValue,
            priceDate,
            approvedOverride is not null ? "ApprovedManualOverride" : request.PriceSource,
            isMissingPrice,
            isStale,
            approvedOverride is not null,
            approvedOverride?.Id,
            "D-FML-002");
    }

    private NavFormulaOutput BuildFormulaOutput(ValuationRun run)
    {
        var investmentValue = NavFormulaEngine.Round(run.InstrumentValuations.Sum(valuation => valuation.InvestmentValue));
        var instrumentAccruedIncome = NavFormulaEngine.Round(run.InstrumentValuations.Sum(valuation => valuation.AccruedIncome));
        var dailyAccretion = NavFormulaEngine.Round(run.InstrumentValuations.Sum(valuation => valuation.DailyAccretion));
        var cashAndBank = InputAmount(run, ValuationInputType.CashAndBank);
        var otherReceivables = InputAmount(run, ValuationInputType.OtherReceivable);
        var prepayments = InputAmount(run, ValuationInputType.Prepayment);
        var totalLiabilities = InputAmount(run, ValuationInputType.Liability);
        var accruedExpenses = InputAmount(run, ValuationInputType.AccruedExpense);
        var grossAssetValue = NavFormulaEngine.GrossAssetValue(investmentValue, cashAndBank, instrumentAccruedIncome, otherReceivables, prepayments);
        var nav = NavFormulaEngine.NetAssetValue(grossAssetValue, totalLiabilities, accruedExpenses);
        var openingUnits = InputAmount(run, ValuationInputType.OpeningUnits);
        var unitsIssued = InputAmount(run, ValuationInputType.UnitsIssued);
        var unitsRedeemed = InputAmount(run, ValuationInputType.UnitsRedeemed);
        var approvedUnitAdjustments = InputAmount(run, ValuationInputType.ApprovedUnitAdjustment);
        var closingUnits = NavFormulaEngine.ClosingUnits(openingUnits, unitsIssued, unitsRedeemed, approvedUnitAdjustments);
        var unitPrice = NavFormulaEngine.UnitPrice(nav, closingUnits);

        var netSubscriptionAmount = NavFormulaEngine.NetSubscriptionAmount(
            InputAmount(run, ValuationInputType.GrossContribution),
            InputAmount(run, ValuationInputType.EntryFee),
            InputAmount(run, ValuationInputType.SubscriptionTaxCharge));
        var unitsAllocated = netSubscriptionAmount == 0m ? 0m : NavFormulaEngine.UnitsAllocated(netSubscriptionAmount, unitPrice);

        var redemptionPrice = PositiveOrFallback(InputAmount(run, ValuationInputType.RedemptionPrice), unitPrice);
        var grossRedemptionValue = NavFormulaEngine.GrossRedemptionValue(InputAmount(run, ValuationInputType.RedemptionUnits), redemptionPrice);
        var netRedemptionPayable = NavFormulaEngine.NetRedemptionPayable(
            grossRedemptionValue,
            InputAmount(run, ValuationInputType.ExitFee),
            InputAmount(run, ValuationInputType.RedemptionTaxCharge));

        var sourceFundPrice = PositiveOrFallback(InputAmount(run, ValuationInputType.SourceFundPrice), unitPrice);
        var switchOutValue = NavFormulaEngine.SwitchOutValue(InputAmount(run, ValuationInputType.UnitsSwitchedOut), sourceFundPrice);
        var netSwitchAmount = InputAmount(run, ValuationInputType.NetSwitchAmount);
        var targetFundPrice = PositiveOrFallback(InputAmount(run, ValuationInputType.TargetFundPrice), unitPrice);
        var switchInUnits = netSwitchAmount == 0m ? 0m : NavFormulaEngine.SwitchInUnits(netSwitchAmount, targetFundPrice);

        var applicablePrice = PositiveOrFallback(InputAmount(run, ValuationInputType.ApplicablePrice), unitPrice);
        var redeemableAmount = NavFormulaEngine.RedeemableAmount(
            InputAmount(run, ValuationInputType.AvailableUnits),
            applicablePrice,
            InputAmount(run, ValuationInputType.EstimatedExitCharge),
            InputAmount(run, ValuationInputType.ApplicableTax));

        return new NavFormulaOutput(
            investmentValue,
            instrumentAccruedIncome,
            dailyAccretion,
            cashAndBank,
            otherReceivables,
            prepayments,
            grossAssetValue,
            totalLiabilities,
            accruedExpenses,
            nav,
            openingUnits,
            unitsIssued,
            unitsRedeemed,
            approvedUnitAdjustments,
            closingUnits,
            unitPrice,
            netSubscriptionAmount,
            unitsAllocated,
            grossRedemptionValue,
            netRedemptionPayable,
            switchOutValue,
            switchInUnits,
            redeemableAmount);
    }

    private static decimal PositiveOrFallback(decimal value, decimal fallback)
    {
        return value > 0m ? value : fallback;
    }

    private static decimal InputAmount(ValuationRun run, ValuationInputType type)
    {
        return NavFormulaEngine.Round(run.Inputs.Where(input => input.InputType == type).Sum(input => input.Amount));
    }

    private static void AddConfiguredFormulaInputs(ValuationRun run, CreateValuationRunRequest request)
    {
        AddInput(run, ValuationInputType.CashAndBank, "Cash and bank", request.CashAndBank, "D-FML-001");
        AddInput(run, ValuationInputType.OtherReceivable, "Other receivables", request.OtherReceivables, "D-FML-001");
        AddInput(run, ValuationInputType.Prepayment, "Prepayments", request.Prepayments, "D-FML-001");
        AddInput(run, ValuationInputType.Liability, "Total liabilities", request.TotalLiabilities, "D-FML-005");
        AddInput(run, ValuationInputType.AccruedExpense, "Accrued expenses", request.AccruedExpenses, "D-FML-005");
        AddInput(run, ValuationInputType.OpeningUnits, "Opening units", request.OpeningUnits, "D-FML-006");
        AddInput(run, ValuationInputType.UnitsIssued, "Units issued", request.UnitsIssued, "D-FML-006");
        AddInput(run, ValuationInputType.UnitsRedeemed, "Units redeemed", request.UnitsRedeemed, "D-FML-006");
        AddInput(run, ValuationInputType.ApprovedUnitAdjustment, "Approved unit adjustments", request.ApprovedUnitAdjustments, "D-FML-006");
        AddInput(run, ValuationInputType.GrossContribution, "Gross contribution", request.GrossContribution, "D-FML-008");
        AddInput(run, ValuationInputType.EntryFee, "Entry fee", request.EntryFee, "D-FML-008");
        AddInput(run, ValuationInputType.SubscriptionTaxCharge, "Subscription tax and charges", request.SubscriptionTaxCharges, "D-FML-008");
        AddInput(run, ValuationInputType.RedemptionUnits, "Units redeemed for pricing", request.RedemptionUnits, "D-FML-010");
        AddInput(run, ValuationInputType.RedemptionPrice, "Applicable redemption price", request.RedemptionPrice, "D-FML-010");
        AddInput(run, ValuationInputType.ExitFee, "Exit fee", request.ExitFee, "D-FML-011");
        AddInput(run, ValuationInputType.RedemptionTaxCharge, "Redemption tax and charges", request.RedemptionTaxCharges, "D-FML-011");
        AddInput(run, ValuationInputType.UnitsSwitchedOut, "Units switched out", request.UnitsSwitchedOut, "D-FML-012");
        AddInput(run, ValuationInputType.SourceFundPrice, "Source fund price", request.SourceFundPrice, "D-FML-012");
        AddInput(run, ValuationInputType.NetSwitchAmount, "Net switch amount", request.NetSwitchAmount, "D-FML-012");
        AddInput(run, ValuationInputType.TargetFundPrice, "Target fund price", request.TargetFundPrice, "D-FML-012");
        AddInput(run, ValuationInputType.AvailableUnits, "Available units", request.AvailableUnits, "D-FML-013");
        AddInput(run, ValuationInputType.ApplicablePrice, "Applicable price", request.ApplicablePrice, "D-FML-013");
        AddInput(run, ValuationInputType.EstimatedExitCharge, "Estimated exit charges", request.EstimatedExitCharges, "D-FML-013");
        AddInput(run, ValuationInputType.ApplicableTax, "Applicable tax", request.ApplicableTax, "D-FML-013");
    }

    private static void AddInput(ValuationRun run, ValuationInputType type, string description, decimal amount, string formulaCode)
    {
        run.AddInput(ValuationInput.Create(run.Id, type, description, amount, null, null, formulaCode, "ApiInput"));
    }

    private async Task<IReadOnlyDictionary<Guid, ManualValuationOverride>> GetApprovedOverridesAsync(CreateValuationRunRequest request, CancellationToken cancellationToken)
    {
        var instrumentIds = request.Instruments.Select(instrument => instrument.InstrumentId).Distinct().ToArray();
        var approved = await _dbContext.ManualValuationOverrides
            .AsNoTracking()
            .Where(item => item.SchemeId == request.SchemeId
                && item.SchemeClassId == request.SchemeClassId
                && item.Status == ManualValuationOverrideStatus.Approved
                && instrumentIds.Contains(item.InstrumentId))
            .ToListAsync(cancellationToken);

        return approved
            .Where(item => item.ValuationDate.Value == request.ValuationDate)
            .GroupBy(item => item.InstrumentId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.ApprovedAtUtc).First());
    }

    private async Task EnsurePriceSourceHierarchyAsync(
        CreateValuationRunRequest request,
        string instrumentType,
        string actor,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var pendingExists = _dbContext.PriceSourceHierarchies.Local.Any(item => item.SchemeId == request.SchemeId
            && item.SchemeClassId == request.SchemeClassId
            && string.Equals(item.InstrumentType, instrumentType, StringComparison.OrdinalIgnoreCase)
            && item.IsActive);
        if (pendingExists)
        {
            return;
        }

        var exists = await _dbContext.PriceSourceHierarchies
            .AsNoTracking()
            .AnyAsync(item => item.SchemeId == request.SchemeId
                && item.SchemeClassId == request.SchemeClassId
                && item.InstrumentType == instrumentType
                && item.IsActive,
                cancellationToken);

        if (exists)
        {
            return;
        }

        _dbContext.PriceSourceHierarchies.Add(PriceSourceHierarchy.Create(
            request.SchemeId,
            request.SchemeClassId,
            instrumentType,
            "ExternalPriceProvider",
            "ManualApprovedOverride",
            true,
            request.PriceStaleAfterDays,
            request.PriceVarianceTolerancePercent,
            actor,
            now));
    }

    private async Task EnsureSchemeClassActiveAsync(Guid schemeId, Guid schemeClassId, CancellationToken cancellationToken)
    {
        var scheme = await _dbContext.Schemes
            .AsNoTracking()
            .Include(item => item.Classes)
            .FirstOrDefaultAsync(item => item.Id == schemeId, cancellationToken)
            ?? throw new NotFoundException("Scheme not found.");

        if (scheme.Status != SchemeStatus.Active)
        {
            throw Validation("schemeId", "Scheme must be active for NAV valuation.");
        }

        var schemeClass = scheme.Classes.FirstOrDefault(item => item.Id == schemeClassId);
        if (schemeClass is null || !schemeClass.IsActive)
        {
            throw new NotFoundException("Scheme class not found or inactive.");
        }
    }

    private async Task EnsureInstrumentsExistAsync(IReadOnlyCollection<Guid> instrumentIds, CancellationToken cancellationToken)
    {
        if (instrumentIds.Any(id => id == Guid.Empty))
        {
            throw Validation("instrumentId", "Instrument id cannot be empty.");
        }

        var existing = await _dbContext.Instruments
            .AsNoTracking()
            .Where(instrument => instrumentIds.Contains(instrument.Id))
            .Select(instrument => instrument.Id)
            .ToListAsync(cancellationToken);

        var missing = instrumentIds.Except(existing).ToList();
        if (missing.Count > 0)
        {
            throw new NotFoundException($"Instrument not found: {missing[0]}.");
        }
    }

    private async Task<ValuationRun> LoadRunForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.ValuationRuns
            .Include(run => run.Inputs)
            .Include(run => run.Sources)
            .Include(run => run.InstrumentValuations)
            .Include(run => run.StalePriceExceptions)
            .Include(run => run.PricingVarianceExceptions)
            .Include(run => run.Calculations)
            .Include(run => run.NavPerUnits)
            .Include(run => run.Approvals)
            .FirstOrDefaultAsync(run => run.Id == id, cancellationToken)
            ?? throw new NotFoundException("Valuation run not found.");
    }

    private void MarkLatestCalculationArtifactsAdded(ValuationRun run)
    {
        foreach (var entry in _dbContext.ChangeTracker.Entries<NavCalculation>()
            .Where(entry => entry.Entity.ValuationRunId == run.Id && entry.State == EntityState.Modified))
        {
            entry.State = EntityState.Added;
        }

        foreach (var entry in _dbContext.ChangeTracker.Entries<NavPerUnit>()
            .Where(entry => entry.Entity.ValuationRunId == run.Id && entry.State == EntityState.Modified))
        {
            entry.State = EntityState.Added;
        }
    }

    private void MarkLatestApprovalAdded(ValuationRun run)
    {
        foreach (var entry in _dbContext.ChangeTracker.Entries<NavApproval>()
            .Where(entry => entry.Entity.ValuationRunId == run.Id && entry.State == EntityState.Modified))
        {
            entry.State = EntityState.Added;
        }
    }

    private async Task<int> NextVersionNumberAsync(Guid schemeId, Guid schemeClassId, BusinessDate valuationDate, CancellationToken cancellationToken)
    {
        var publications = await _dbContext.NavPublications
            .AsNoTracking()
            .Where(publication => publication.SchemeId == schemeId && publication.SchemeClassId == schemeClassId)
            .ToListAsync(cancellationToken);

        var maxVersion = publications
            .Where(publication => publication.ValuationDate.Value == valuationDate.Value)
            .Select(publication => publication.VersionNumber)
            .DefaultIfEmpty(0)
            .Max();

        return maxVersion + 1;
    }

    private string CurrentUserIdOrThrow()
    {
        var userId = _currentUserContext.UserId;
        return string.IsNullOrWhiteSpace(userId) ? throw new UnauthorizedAccessException("User context is not available.") : userId;
    }

    private async Task WriteAuditAsync(
        AuditEventType eventType,
        string action,
        string entityName,
        string entityId,
        string? beforeJson,
        string? afterJson,
        string reason,
        CancellationToken cancellationToken)
    {
        await _auditWriter.WriteAsync(new AuditLogEntry(
            ModuleName,
            action,
            entityName,
            entityId,
            eventType,
            CurrentUserIdOrThrow(),
            _currentUserContext.DisplayName,
            _currentUserContext.Roles.Count == 0 ? null : string.Join(",", _currentUserContext.Roles),
            _currentUserContext.CorrelationId,
            _currentUserContext.IpAddress,
            _currentUserContext.UserAgent,
            reason,
            null,
            beforeJson,
            afterJson,
            reason,
            null,
            null),
            cancellationToken);
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

    private static ValidationException Validation(string field, string message)
    {
        return new ValidationException(new Dictionary<string, string[]>
        {
            [field] = [message]
        });
    }

    private static string Snapshot<T>(T? obj)
    {
        return obj is null ? "{}" : JsonSerializer.Serialize(obj);
    }

    private static string Sha256(string payload)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }

    private static NavCalculation? LatestCalculation(ValuationRun run)
    {
        return run.Calculations.OrderByDescending(calculation => calculation.CalculatedAtUtc).FirstOrDefault();
    }

    private static NavPerUnit? LatestNavPerUnit(ValuationRun run)
    {
        return run.NavPerUnits.OrderByDescending(item => item.Id).FirstOrDefault();
    }

    private static ValuationRunDto MapRun(ValuationRun run)
    {
        var latestCalculation = LatestCalculation(run);
        var latestNavPerUnit = LatestNavPerUnit(run);

        return new ValuationRunDto(
            run.Id,
            run.SchemeId,
            run.SchemeClassId,
            run.ValuationDate.Value,
            run.RunNumber,
            run.Status.ToString(),
            run.FormulaVersion,
            run.DayCountBasis.ToString(),
            run.InternalPrecision,
            run.ReportPrecision,
            run.PriceStaleAfterDays,
            run.PriceVarianceTolerancePercent,
            run.AllowsAmortizedCost,
            run.PreparedByUserId,
            run.PreparedAtUtc,
            run.SubmittedByUserId,
            run.SubmittedAtUtc,
            run.CheckedByUserId,
            run.CheckedAtUtc,
            run.ApprovedByUserId,
            run.ApprovedAtUtc,
            run.PublishedByUserId,
            run.PublishedAtUtc,
            run.PublishedVersion,
            run.InstrumentValuations.Select(MapInstrumentValuation).ToList(),
            run.StalePriceExceptions.Select(MapStalePriceException).ToList(),
            run.PricingVarianceExceptions.Select(MapPricingVarianceException).ToList(),
            latestCalculation is null ? null : MapCalculation(latestCalculation),
            latestNavPerUnit is null ? null : MapNavPerUnit(latestNavPerUnit),
            run.Approvals.OrderBy(approval => approval.OccurredAtUtc).Select(MapApproval).ToList());
    }

    private static InstrumentValuationDto MapInstrumentValuation(InstrumentValuation valuation)
    {
        return new InstrumentValuationDto(
            valuation.Id,
            valuation.InstrumentId,
            valuation.InstrumentType,
            valuation.Quantity,
            valuation.MarketPrice,
            valuation.AccruedIncome,
            valuation.DailyAccretion,
            valuation.AmortizedCost,
            valuation.UseAmortizedCost,
            valuation.InvestmentValue,
            valuation.PriceDate?.Value,
            valuation.PriceSource,
            valuation.IsPriceMissing,
            valuation.IsPriceStale,
            valuation.OverrideApplied,
            valuation.ManualValuationOverrideId,
            valuation.FormulaCode);
    }

    private static StalePriceExceptionDto MapStalePriceException(StalePriceException exception)
    {
        return new StalePriceExceptionDto(
            exception.Id,
            exception.InstrumentId,
            exception.PriceDate?.Value,
            exception.ValuationDate.Value,
            exception.MaxAgeDays,
            exception.MissingPrice,
            exception.Message,
            exception.Status.ToString());
    }

    private static PricingVarianceExceptionDto MapPricingVarianceException(PricingVarianceException exception)
    {
        return new PricingVarianceExceptionDto(
            exception.Id,
            exception.InstrumentId,
            exception.CurrentPrice,
            exception.PriorPrice,
            exception.VariancePercent,
            exception.TolerancePercent,
            exception.Message,
            exception.Status.ToString());
    }

    private static NavCalculationDto MapCalculation(NavCalculation calculation)
    {
        return new NavCalculationDto(
            calculation.Id,
            calculation.FormulaVersion,
            calculation.InvestmentValue,
            calculation.InstrumentAccruedIncome,
            calculation.DailyAccretion,
            calculation.CashAndBank,
            calculation.OtherReceivables,
            calculation.Prepayments,
            calculation.GrossAssetValue,
            calculation.TotalLiabilities,
            calculation.AccruedExpenses,
            calculation.NetAssetValue,
            calculation.NetSubscriptionAmount,
            calculation.UnitsAllocated,
            calculation.GrossRedemptionValue,
            calculation.NetRedemptionPayable,
            calculation.SwitchOutValue,
            calculation.SwitchInUnits,
            calculation.RedeemableAmount,
            calculation.CalculatedByUserId,
            calculation.CalculatedAtUtc);
    }

    private static NavPerUnitDto MapNavPerUnit(NavPerUnit navPerUnit)
    {
        return new NavPerUnitDto(
            navPerUnit.Id,
            navPerUnit.SchemeClassId,
            navPerUnit.OpeningUnits,
            navPerUnit.UnitsIssued,
            navPerUnit.UnitsRedeemed,
            navPerUnit.ApprovedUnitAdjustments,
            navPerUnit.ClosingUnits,
            navPerUnit.NavAmount,
            navPerUnit.UnitPrice,
            navPerUnit.ReportedUnitPrice);
    }

    private static NavApprovalDto MapApproval(NavApproval approval)
    {
        return new NavApprovalDto(approval.Id, approval.Step.ToString(), approval.ActorUserId, approval.OccurredAtUtc, approval.Comment);
    }

    private static ManualValuationOverrideDto MapOverride(ManualValuationOverride manualOverride)
    {
        return new ManualValuationOverrideDto(
            manualOverride.Id,
            manualOverride.SchemeId,
            manualOverride.SchemeClassId,
            manualOverride.ValuationDate.Value,
            manualOverride.InstrumentId,
            manualOverride.OverridePrice,
            manualOverride.OverrideValue,
            manualOverride.Reason,
            manualOverride.Status.ToString(),
            manualOverride.RequestedByUserId,
            manualOverride.RequestedAtUtc,
            manualOverride.ApprovedByUserId,
            manualOverride.ApprovedAtUtc,
            manualOverride.ApprovalComment);
    }

    private static NavPublicationDto MapPublication(NavPublication publication)
    {
        return new NavPublicationDto(
            publication.Id,
            publication.ValuationRunId,
            publication.SchemeId,
            publication.SchemeClassId,
            publication.ValuationDate.Value,
            publication.VersionNumber,
            publication.PublishedNav,
            publication.PublishedUnitPrice,
            publication.ReportedUnitPrice,
            publication.FormulaVersion,
            publication.PublishedByUserId,
            publication.PublishedAtUtc);
    }

    private static NavRestatementDto MapRestatement(NavRestatement restatement)
    {
        return new NavRestatementDto(
            restatement.Id,
            restatement.OriginalPublicationId,
            restatement.CorrectedValuationRunId,
            restatement.CorrectedPublicationId,
            restatement.SchemeId,
            restatement.SchemeClassId,
            restatement.ValuationDate.Value,
            restatement.CorrectedVersionNumber,
            restatement.Reason,
            restatement.Status.ToString());
    }
}
