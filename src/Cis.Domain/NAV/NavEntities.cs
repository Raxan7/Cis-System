using Cis.Domain.Common;

namespace Cis.Domain.NAV;

public sealed class ValuationRun : AuditableAggregateRoot
{
    private readonly List<ValuationInput> _inputs = [];
    private readonly List<ValuationSource> _sources = [];
    private readonly List<InstrumentValuation> _instrumentValuations = [];
    private readonly List<StalePriceException> _stalePriceExceptions = [];
    private readonly List<PricingVarianceException> _pricingVarianceExceptions = [];
    private readonly List<NavCalculation> _calculations = [];
    private readonly List<NavPerUnit> _navPerUnits = [];
    private readonly List<NavApproval> _approvals = [];

    private ValuationRun()
    {
    }

    private ValuationRun(
        Guid schemeId,
        Guid schemeClassId,
        BusinessDate valuationDate,
        string formulaVersion,
        DayCountBasis dayCountBasis,
        int reportPrecision,
        int priceStaleAfterDays,
        decimal priceVarianceTolerancePercent,
        bool allowsAmortizedCost,
        string preparedByUserId,
        DateTime preparedAtUtc)
    {
        NavValidation.EnsureUtc(preparedAtUtc, nameof(preparedAtUtc));
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        SchemeClassId = schemeClassId != Guid.Empty ? schemeClassId : throw new ArgumentException("Scheme class id cannot be empty.", nameof(schemeClassId));
        ValuationDate = valuationDate;
        RunNumber = $"NAV-{valuationDate.Value:yyyyMMdd}-{Guid.NewGuid():N}"[..40].ToUpperInvariant();
        Status = ValuationRunStatus.Draft;
        FormulaVersion = NavValidation.Required(formulaVersion, nameof(formulaVersion), 50);
        DayCountBasis = dayCountBasis;
        InternalPrecision = NavFormulaEngine.InternalPrecision;
        ReportPrecision = reportPrecision is >= 0 and <= 12 ? reportPrecision : throw new ArgumentException("Report precision must be between 0 and 12.", nameof(reportPrecision));
        PriceStaleAfterDays = priceStaleAfterDays is >= 0 and <= 3660 ? priceStaleAfterDays : throw new ArgumentException("Price stale days must be between 0 and 3660.", nameof(priceStaleAfterDays));
        PriceVarianceTolerancePercent = NavValidation.Percentage(priceVarianceTolerancePercent, nameof(priceVarianceTolerancePercent));
        AllowsAmortizedCost = allowsAmortizedCost;
        PreparedByUserId = NavValidation.Required(preparedByUserId, nameof(preparedByUserId), 200);
        PreparedAtUtc = preparedAtUtc;
        MarkCreated(preparedByUserId, preparedAtUtc);
    }

    public Guid SchemeId { get; private set; }
    public Guid SchemeClassId { get; private set; }
    public BusinessDate ValuationDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public string RunNumber { get; private set; } = string.Empty;
    public ValuationRunStatus Status { get; private set; }
    public string FormulaVersion { get; private set; } = string.Empty;
    public DayCountBasis DayCountBasis { get; private set; }
    public int InternalPrecision { get; private set; }
    public int ReportPrecision { get; private set; }
    public int PriceStaleAfterDays { get; private set; }
    public decimal PriceVarianceTolerancePercent { get; private set; }
    public bool AllowsAmortizedCost { get; private set; }
    public string PreparedByUserId { get; private set; } = string.Empty;
    public DateTime PreparedAtUtc { get; private set; }
    public string? SubmittedByUserId { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public string? CheckedByUserId { get; private set; }
    public DateTime? CheckedAtUtc { get; private set; }
    public string? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public string? PublishedByUserId { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public int? PublishedVersion { get; private set; }

    public IReadOnlyCollection<ValuationInput> Inputs => _inputs.AsReadOnly();
    public IReadOnlyCollection<ValuationSource> Sources => _sources.AsReadOnly();
    public IReadOnlyCollection<InstrumentValuation> InstrumentValuations => _instrumentValuations.AsReadOnly();
    public IReadOnlyCollection<StalePriceException> StalePriceExceptions => _stalePriceExceptions.AsReadOnly();
    public IReadOnlyCollection<PricingVarianceException> PricingVarianceExceptions => _pricingVarianceExceptions.AsReadOnly();
    public IReadOnlyCollection<NavCalculation> Calculations => _calculations.AsReadOnly();
    public IReadOnlyCollection<NavPerUnit> NavPerUnits => _navPerUnits.AsReadOnly();
    public IReadOnlyCollection<NavApproval> Approvals => _approvals.AsReadOnly();

    public static ValuationRun Create(
        Guid schemeId,
        Guid schemeClassId,
        BusinessDate valuationDate,
        string formulaVersion,
        DayCountBasis dayCountBasis,
        int reportPrecision,
        int priceStaleAfterDays,
        decimal priceVarianceTolerancePercent,
        bool allowsAmortizedCost,
        string preparedByUserId,
        DateTime preparedAtUtc)
    {
        return new ValuationRun(
            schemeId,
            schemeClassId,
            valuationDate,
            formulaVersion,
            dayCountBasis,
            reportPrecision,
            priceStaleAfterDays,
            priceVarianceTolerancePercent,
            allowsAmortizedCost,
            preparedByUserId,
            preparedAtUtc);
    }

    public void AddInput(ValuationInput input)
    {
        EnsureNotPublished();
        _inputs.Add(input ?? throw new ArgumentNullException(nameof(input)));
    }

    public void AddSource(ValuationSource source)
    {
        EnsureNotPublished();
        _sources.Add(source ?? throw new ArgumentNullException(nameof(source)));
    }

    public void AddInstrumentValuation(InstrumentValuation valuation)
    {
        EnsureNotPublished();
        _instrumentValuations.Add(valuation ?? throw new ArgumentNullException(nameof(valuation)));
    }

    public void AddStalePriceException(StalePriceException exception)
    {
        EnsureNotPublished();
        _stalePriceExceptions.Add(exception ?? throw new ArgumentNullException(nameof(exception)));
    }

    public void AddPricingVarianceException(PricingVarianceException exception)
    {
        EnsureNotPublished();
        _pricingVarianceExceptions.Add(exception ?? throw new ArgumentNullException(nameof(exception)));
    }

    public void Calculate(NavFormulaOutput output, string calculatedByUserId, DateTime calculatedAtUtc)
    {
        EnsureNotPublished();
        NavValidation.EnsureUtc(calculatedAtUtc, nameof(calculatedAtUtc));

        var actor = NavValidation.Required(calculatedByUserId, nameof(calculatedByUserId), 200);
        var calculation = NavCalculation.Create(Id, FormulaVersion, output, actor, calculatedAtUtc);
        var navPerUnit = NavPerUnit.Create(
            Id,
            SchemeClassId,
            output.OpeningUnits,
            output.UnitsIssued,
            output.UnitsRedeemed,
            output.ApprovedUnitAdjustments,
            output.ClosingUnits,
            output.NetAssetValue,
            output.UnitPrice,
            decimal.Round(output.UnitPrice, ReportPrecision, MidpointRounding.AwayFromZero));

        _calculations.Add(calculation);
        _navPerUnits.Add(navPerUnit);
        Status = ValuationRunStatus.Calculated;
    }

    public void Submit(string submittedByUserId, DateTime submittedAtUtc, string? comment)
    {
        NavValidation.EnsureUtc(submittedAtUtc, nameof(submittedAtUtc));
        if (Status != ValuationRunStatus.Calculated)
        {
            throw new InvalidOperationException("Only calculated valuation runs can be submitted.");
        }

        var actor = NavValidation.Required(submittedByUserId, nameof(submittedByUserId), 200);
        SubmittedByUserId = actor;
        SubmittedAtUtc = submittedAtUtc;
        CheckedByUserId = null;
        CheckedAtUtc = null;
        ApprovedByUserId = null;
        ApprovedAtUtc = null;
        _approvals.Add(NavApproval.Create(Id, NavApprovalStepType.Submitted, actor, submittedAtUtc, comment));
        Status = ValuationRunStatus.Submitted;
    }

    public void Check(string checkedByUserId, DateTime checkedAtUtc, string? comment)
    {
        NavValidation.EnsureUtc(checkedAtUtc, nameof(checkedAtUtc));
        if (Status != ValuationRunStatus.Submitted)
        {
            throw new InvalidOperationException("Only submitted valuation runs can be checked.");
        }

        var actor = NavValidation.Required(checkedByUserId, nameof(checkedByUserId), 200);
        if (string.Equals(actor, SubmittedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Segregation of duties violation: submitter cannot check the same NAV workflow.");
        }

        CheckedByUserId = actor;
        CheckedAtUtc = checkedAtUtc;
        _approvals.Add(NavApproval.Create(Id, NavApprovalStepType.Checked, actor, checkedAtUtc, comment));
        Status = ValuationRunStatus.Checked;
    }

    public void Approve(string approvedByUserId, DateTime approvedAtUtc, string? comment)
    {
        NavValidation.EnsureUtc(approvedAtUtc, nameof(approvedAtUtc));
        if (Status != ValuationRunStatus.Checked)
        {
            throw new InvalidOperationException("Only checked valuation runs can be approved.");
        }

        var actor = NavValidation.Required(approvedByUserId, nameof(approvedByUserId), 200);
        if (string.Equals(actor, SubmittedByUserId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(actor, CheckedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Segregation of duties violation: approver cannot be the submitter or checker.");
        }

        ApprovedByUserId = actor;
        ApprovedAtUtc = approvedAtUtc;
        _approvals.Add(NavApproval.Create(Id, NavApprovalStepType.Approved, actor, approvedAtUtc, comment));
        Status = ValuationRunStatus.Approved;
    }

    public void Publish(string publishedByUserId, DateTime publishedAtUtc, int versionNumber, string? comment)
    {
        NavValidation.EnsureUtc(publishedAtUtc, nameof(publishedAtUtc));
        if (Status != ValuationRunStatus.Approved)
        {
            throw new InvalidOperationException("NAV cannot be published without an approved valuation run.");
        }

        var actor = NavValidation.Required(publishedByUserId, nameof(publishedByUserId), 200);
        PublishedByUserId = actor;
        PublishedAtUtc = publishedAtUtc;
        PublishedVersion = NavValidation.Positive(versionNumber, nameof(versionNumber));
        _approvals.Add(NavApproval.Create(Id, NavApprovalStepType.Published, actor, publishedAtUtc, comment));
        Status = ValuationRunStatus.Published;
    }

    private void EnsureNotPublished()
    {
        if (Status == ValuationRunStatus.Published)
        {
            throw new InvalidOperationException("Published NAV valuation runs cannot be changed.");
        }
    }
}

public sealed class ValuationInput : Entity
{
    private ValuationInput()
    {
    }

    private ValuationInput(
        Guid valuationRunId,
        ValuationInputType inputType,
        string description,
        decimal amount,
        decimal? quantity,
        decimal? price,
        string formulaCode,
        string? sourceReference)
    {
        ValuationRunId = valuationRunId != Guid.Empty ? valuationRunId : throw new ArgumentException("Valuation run id cannot be empty.", nameof(valuationRunId));
        InputType = inputType;
        Description = NavValidation.Required(description, nameof(description), 200);
        Amount = amount;
        Quantity = quantity;
        Price = price;
        FormulaCode = NavValidation.Required(formulaCode, nameof(formulaCode), 30);
        SourceReference = NavValidation.Optional(sourceReference, 200);
    }

    public Guid ValuationRunId { get; private set; }
    public ValuationInputType InputType { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public decimal? Quantity { get; private set; }
    public decimal? Price { get; private set; }
    public string FormulaCode { get; private set; } = string.Empty;
    public string? SourceReference { get; private set; }

    public static ValuationInput Create(
        Guid valuationRunId,
        ValuationInputType inputType,
        string description,
        decimal amount,
        decimal? quantity,
        decimal? price,
        string formulaCode,
        string? sourceReference)
    {
        return new ValuationInput(valuationRunId, inputType, description, amount, quantity, price, formulaCode, sourceReference);
    }
}

public sealed class ValuationSource : Entity
{
    private ValuationSource()
    {
    }

    private ValuationSource(
        Guid valuationRunId,
        ValuationSourceType sourceType,
        string providerName,
        string sourceReference,
        DateTime receivedAtUtc,
        bool isApproved,
        bool isOverride)
    {
        NavValidation.EnsureUtc(receivedAtUtc, nameof(receivedAtUtc));
        ValuationRunId = valuationRunId != Guid.Empty ? valuationRunId : throw new ArgumentException("Valuation run id cannot be empty.", nameof(valuationRunId));
        SourceType = sourceType;
        ProviderName = NavValidation.Required(providerName, nameof(providerName), 100);
        SourceReference = NavValidation.Required(sourceReference, nameof(sourceReference), 200);
        ReceivedAtUtc = receivedAtUtc;
        IsApproved = isApproved;
        IsOverride = isOverride;
    }

    public Guid ValuationRunId { get; private set; }
    public ValuationSourceType SourceType { get; private set; }
    public string ProviderName { get; private set; } = string.Empty;
    public string SourceReference { get; private set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; private set; }
    public bool IsApproved { get; private set; }
    public bool IsOverride { get; private set; }

    public static ValuationSource Create(
        Guid valuationRunId,
        ValuationSourceType sourceType,
        string providerName,
        string sourceReference,
        DateTime receivedAtUtc,
        bool isApproved,
        bool isOverride)
    {
        return new ValuationSource(valuationRunId, sourceType, providerName, sourceReference, receivedAtUtc, isApproved, isOverride);
    }
}

public sealed class PriceSourceHierarchy : AuditableAggregateRoot
{
    private PriceSourceHierarchy()
    {
    }

    private PriceSourceHierarchy(
        Guid schemeId,
        Guid schemeClassId,
        string instrumentType,
        string primarySource,
        string secondarySource,
        bool manualFallbackAllowed,
        int maxPriceAgeDays,
        decimal varianceTolerancePercent,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        NavValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        SchemeClassId = schemeClassId != Guid.Empty ? schemeClassId : throw new ArgumentException("Scheme class id cannot be empty.", nameof(schemeClassId));
        InstrumentType = NavValidation.Required(instrumentType, nameof(instrumentType), 100);
        PrimarySource = NavValidation.Required(primarySource, nameof(primarySource), 100);
        SecondarySource = NavValidation.Required(secondarySource, nameof(secondarySource), 100);
        ManualFallbackAllowed = manualFallbackAllowed;
        MaxPriceAgeDays = maxPriceAgeDays is >= 0 and <= 3660 ? maxPriceAgeDays : throw new ArgumentException("Max price age days must be between 0 and 3660.", nameof(maxPriceAgeDays));
        VarianceTolerancePercent = NavValidation.Percentage(varianceTolerancePercent, nameof(varianceTolerancePercent));
        IsActive = true;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid SchemeId { get; private set; }
    public Guid SchemeClassId { get; private set; }
    public string InstrumentType { get; private set; } = string.Empty;
    public string PrimarySource { get; private set; } = string.Empty;
    public string SecondarySource { get; private set; } = string.Empty;
    public bool ManualFallbackAllowed { get; private set; }
    public int MaxPriceAgeDays { get; private set; }
    public decimal VarianceTolerancePercent { get; private set; }
    public bool IsActive { get; private set; }

    public static PriceSourceHierarchy Create(
        Guid schemeId,
        Guid schemeClassId,
        string instrumentType,
        string primarySource,
        string secondarySource,
        bool manualFallbackAllowed,
        int maxPriceAgeDays,
        decimal varianceTolerancePercent,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        return new PriceSourceHierarchy(
            schemeId,
            schemeClassId,
            instrumentType,
            primarySource,
            secondarySource,
            manualFallbackAllowed,
            maxPriceAgeDays,
            varianceTolerancePercent,
            createdByUserId,
            createdAtUtc);
    }
}

public sealed class InstrumentValuation : Entity
{
    private InstrumentValuation()
    {
    }

    private InstrumentValuation(
        Guid valuationRunId,
        Guid instrumentId,
        string instrumentType,
        decimal quantity,
        decimal? marketPrice,
        decimal accruedIncome,
        decimal dailyAccretion,
        decimal? amortizedCost,
        bool useAmortizedCost,
        decimal investmentValue,
        BusinessDate? priceDate,
        string priceSource,
        bool isPriceMissing,
        bool isPriceStale,
        bool overrideApplied,
        Guid? manualValuationOverrideId,
        string formulaCode)
    {
        ValuationRunId = valuationRunId != Guid.Empty ? valuationRunId : throw new ArgumentException("Valuation run id cannot be empty.", nameof(valuationRunId));
        InstrumentId = instrumentId != Guid.Empty ? instrumentId : throw new ArgumentException("Instrument id cannot be empty.", nameof(instrumentId));
        InstrumentType = NavValidation.Required(instrumentType, nameof(instrumentType), 100);
        Quantity = NavValidation.NonNegative(quantity, nameof(quantity));
        MarketPrice = marketPrice.HasValue ? NavValidation.NonNegative(marketPrice.Value, nameof(marketPrice)) : null;
        AccruedIncome = NavValidation.NonNegative(accruedIncome, nameof(accruedIncome));
        DailyAccretion = dailyAccretion;
        AmortizedCost = amortizedCost.HasValue ? NavValidation.NonNegative(amortizedCost.Value, nameof(amortizedCost)) : null;
        UseAmortizedCost = useAmortizedCost;
        InvestmentValue = NavValidation.NonNegative(investmentValue, nameof(investmentValue));
        PriceDate = priceDate;
        PriceSource = NavValidation.Required(priceSource, nameof(priceSource), 100);
        IsPriceMissing = isPriceMissing;
        IsPriceStale = isPriceStale;
        OverrideApplied = overrideApplied;
        ManualValuationOverrideId = manualValuationOverrideId;
        FormulaCode = NavValidation.Required(formulaCode, nameof(formulaCode), 30);
    }

    public Guid ValuationRunId { get; private set; }
    public Guid InstrumentId { get; private set; }
    public string InstrumentType { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal? MarketPrice { get; private set; }
    public decimal AccruedIncome { get; private set; }
    public decimal DailyAccretion { get; private set; }
    public decimal? AmortizedCost { get; private set; }
    public bool UseAmortizedCost { get; private set; }
    public decimal InvestmentValue { get; private set; }
    public BusinessDate? PriceDate { get; private set; }
    public string PriceSource { get; private set; } = string.Empty;
    public bool IsPriceMissing { get; private set; }
    public bool IsPriceStale { get; private set; }
    public bool OverrideApplied { get; private set; }
    public Guid? ManualValuationOverrideId { get; private set; }
    public string FormulaCode { get; private set; } = string.Empty;

    public static InstrumentValuation Create(
        Guid valuationRunId,
        Guid instrumentId,
        string instrumentType,
        decimal quantity,
        decimal? marketPrice,
        decimal accruedIncome,
        decimal dailyAccretion,
        decimal? amortizedCost,
        bool useAmortizedCost,
        decimal investmentValue,
        BusinessDate? priceDate,
        string priceSource,
        bool isPriceMissing,
        bool isPriceStale,
        bool overrideApplied,
        Guid? manualValuationOverrideId,
        string formulaCode)
    {
        return new InstrumentValuation(
            valuationRunId,
            instrumentId,
            instrumentType,
            quantity,
            marketPrice,
            accruedIncome,
            dailyAccretion,
            amortizedCost,
            useAmortizedCost,
            investmentValue,
            priceDate,
            priceSource,
            isPriceMissing,
            isPriceStale,
            overrideApplied,
            manualValuationOverrideId,
            formulaCode);
    }
}

public sealed class StalePriceException : Entity
{
    private StalePriceException()
    {
    }

    private StalePriceException(
        Guid valuationRunId,
        Guid instrumentId,
        BusinessDate? priceDate,
        BusinessDate valuationDate,
        int maxAgeDays,
        bool missingPrice,
        string message)
    {
        ValuationRunId = valuationRunId != Guid.Empty ? valuationRunId : throw new ArgumentException("Valuation run id cannot be empty.", nameof(valuationRunId));
        InstrumentId = instrumentId != Guid.Empty ? instrumentId : throw new ArgumentException("Instrument id cannot be empty.", nameof(instrumentId));
        PriceDate = priceDate;
        ValuationDate = valuationDate;
        MaxAgeDays = maxAgeDays;
        MissingPrice = missingPrice;
        Message = NavValidation.Required(message, nameof(message), 500);
        Status = PriceExceptionStatus.Open;
    }

    public Guid ValuationRunId { get; private set; }
    public Guid InstrumentId { get; private set; }
    public BusinessDate? PriceDate { get; private set; }
    public BusinessDate ValuationDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public int MaxAgeDays { get; private set; }
    public bool MissingPrice { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public PriceExceptionStatus Status { get; private set; }

    public static StalePriceException Create(
        Guid valuationRunId,
        Guid instrumentId,
        BusinessDate? priceDate,
        BusinessDate valuationDate,
        int maxAgeDays,
        bool missingPrice,
        string message)
    {
        return new StalePriceException(valuationRunId, instrumentId, priceDate, valuationDate, maxAgeDays, missingPrice, message);
    }
}

public sealed class PricingVarianceException : Entity
{
    private PricingVarianceException()
    {
    }

    private PricingVarianceException(
        Guid valuationRunId,
        Guid instrumentId,
        decimal currentPrice,
        decimal priorPrice,
        decimal variancePercent,
        decimal tolerancePercent,
        string message)
    {
        ValuationRunId = valuationRunId != Guid.Empty ? valuationRunId : throw new ArgumentException("Valuation run id cannot be empty.", nameof(valuationRunId));
        InstrumentId = instrumentId != Guid.Empty ? instrumentId : throw new ArgumentException("Instrument id cannot be empty.", nameof(instrumentId));
        CurrentPrice = NavValidation.NonNegative(currentPrice, nameof(currentPrice));
        PriorPrice = NavValidation.Positive(priorPrice, nameof(priorPrice));
        VariancePercent = NavValidation.NonNegative(variancePercent, nameof(variancePercent));
        TolerancePercent = NavValidation.Percentage(tolerancePercent, nameof(tolerancePercent));
        Message = NavValidation.Required(message, nameof(message), 500);
        Status = PriceExceptionStatus.Open;
    }

    public Guid ValuationRunId { get; private set; }
    public Guid InstrumentId { get; private set; }
    public decimal CurrentPrice { get; private set; }
    public decimal PriorPrice { get; private set; }
    public decimal VariancePercent { get; private set; }
    public decimal TolerancePercent { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public PriceExceptionStatus Status { get; private set; }

    public static PricingVarianceException Create(
        Guid valuationRunId,
        Guid instrumentId,
        decimal currentPrice,
        decimal priorPrice,
        decimal variancePercent,
        decimal tolerancePercent,
        string message)
    {
        return new PricingVarianceException(valuationRunId, instrumentId, currentPrice, priorPrice, variancePercent, tolerancePercent, message);
    }
}

public sealed class ManualValuationOverride : AuditableAggregateRoot
{
    private ManualValuationOverride()
    {
    }

    private ManualValuationOverride(
        Guid schemeId,
        Guid schemeClassId,
        BusinessDate valuationDate,
        Guid instrumentId,
        decimal overridePrice,
        decimal overrideValue,
        string reason,
        string requestedByUserId,
        DateTime requestedAtUtc)
    {
        NavValidation.EnsureUtc(requestedAtUtc, nameof(requestedAtUtc));
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        SchemeClassId = schemeClassId != Guid.Empty ? schemeClassId : throw new ArgumentException("Scheme class id cannot be empty.", nameof(schemeClassId));
        ValuationDate = valuationDate;
        InstrumentId = instrumentId != Guid.Empty ? instrumentId : throw new ArgumentException("Instrument id cannot be empty.", nameof(instrumentId));
        OverridePrice = NavValidation.NonNegative(overridePrice, nameof(overridePrice));
        OverrideValue = NavValidation.NonNegative(overrideValue, nameof(overrideValue));
        Reason = NavValidation.Required(reason, nameof(reason), 1000);
        RequestedByUserId = NavValidation.Required(requestedByUserId, nameof(requestedByUserId), 200);
        RequestedAtUtc = requestedAtUtc;
        Status = ManualValuationOverrideStatus.PendingApproval;
        MarkCreated(requestedByUserId, requestedAtUtc);
    }

    public Guid SchemeId { get; private set; }
    public Guid SchemeClassId { get; private set; }
    public BusinessDate ValuationDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public Guid InstrumentId { get; private set; }
    public decimal OverridePrice { get; private set; }
    public decimal OverrideValue { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public ManualValuationOverrideStatus Status { get; private set; }
    public string RequestedByUserId { get; private set; } = string.Empty;
    public DateTime RequestedAtUtc { get; private set; }
    public string? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public string? ApprovalComment { get; private set; }

    public static ManualValuationOverride Create(
        Guid schemeId,
        Guid schemeClassId,
        BusinessDate valuationDate,
        Guid instrumentId,
        decimal overridePrice,
        decimal overrideValue,
        string reason,
        string requestedByUserId,
        DateTime requestedAtUtc)
    {
        return new ManualValuationOverride(
            schemeId,
            schemeClassId,
            valuationDate,
            instrumentId,
            overridePrice,
            overrideValue,
            reason,
            requestedByUserId,
            requestedAtUtc);
    }

    public void Approve(string approvedByUserId, DateTime approvedAtUtc, string? comment)
    {
        NavValidation.EnsureUtc(approvedAtUtc, nameof(approvedAtUtc));
        if (Status != ManualValuationOverrideStatus.PendingApproval)
        {
            throw new InvalidOperationException("Only pending valuation overrides can be approved.");
        }

        var actor = NavValidation.Required(approvedByUserId, nameof(approvedByUserId), 200);
        if (string.Equals(actor, RequestedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Segregation of duties violation: requester cannot approve the same valuation override.");
        }

        ApprovedByUserId = actor;
        ApprovedAtUtc = approvedAtUtc;
        ApprovalComment = NavValidation.Optional(comment, 1000);
        Status = ManualValuationOverrideStatus.Approved;
    }
}

public sealed class NavCalculation : Entity
{
    private NavCalculation()
    {
    }

    private NavCalculation(Guid valuationRunId, string formulaVersion, NavFormulaOutput output, string calculatedByUserId, DateTime calculatedAtUtc)
    {
        NavValidation.EnsureUtc(calculatedAtUtc, nameof(calculatedAtUtc));
        ValuationRunId = valuationRunId != Guid.Empty ? valuationRunId : throw new ArgumentException("Valuation run id cannot be empty.", nameof(valuationRunId));
        FormulaVersion = NavValidation.Required(formulaVersion, nameof(formulaVersion), 50);
        InvestmentValue = output.InvestmentValue;
        InstrumentAccruedIncome = output.InstrumentAccruedIncome;
        DailyAccretion = output.DailyAccretion;
        CashAndBank = output.CashAndBank;
        OtherReceivables = output.OtherReceivables;
        Prepayments = output.Prepayments;
        GrossAssetValue = output.GrossAssetValue;
        TotalLiabilities = output.TotalLiabilities;
        AccruedExpenses = output.AccruedExpenses;
        NetAssetValue = output.NetAssetValue;
        NetSubscriptionAmount = output.NetSubscriptionAmount;
        UnitsAllocated = output.UnitsAllocated;
        GrossRedemptionValue = output.GrossRedemptionValue;
        NetRedemptionPayable = output.NetRedemptionPayable;
        SwitchOutValue = output.SwitchOutValue;
        SwitchInUnits = output.SwitchInUnits;
        RedeemableAmount = output.RedeemableAmount;
        CalculatedByUserId = NavValidation.Required(calculatedByUserId, nameof(calculatedByUserId), 200);
        CalculatedAtUtc = calculatedAtUtc;
    }

    public Guid ValuationRunId { get; private set; }
    public string FormulaVersion { get; private set; } = string.Empty;
    public decimal InvestmentValue { get; private set; }
    public decimal InstrumentAccruedIncome { get; private set; }
    public decimal DailyAccretion { get; private set; }
    public decimal CashAndBank { get; private set; }
    public decimal OtherReceivables { get; private set; }
    public decimal Prepayments { get; private set; }
    public decimal GrossAssetValue { get; private set; }
    public decimal TotalLiabilities { get; private set; }
    public decimal AccruedExpenses { get; private set; }
    public decimal NetAssetValue { get; private set; }
    public decimal NetSubscriptionAmount { get; private set; }
    public decimal UnitsAllocated { get; private set; }
    public decimal GrossRedemptionValue { get; private set; }
    public decimal NetRedemptionPayable { get; private set; }
    public decimal SwitchOutValue { get; private set; }
    public decimal SwitchInUnits { get; private set; }
    public decimal RedeemableAmount { get; private set; }
    public string CalculatedByUserId { get; private set; } = string.Empty;
    public DateTime CalculatedAtUtc { get; private set; }

    public static NavCalculation Create(Guid valuationRunId, string formulaVersion, NavFormulaOutput output, string calculatedByUserId, DateTime calculatedAtUtc)
    {
        return new NavCalculation(valuationRunId, formulaVersion, output, calculatedByUserId, calculatedAtUtc);
    }
}

public sealed class NavPerUnit : Entity
{
    private NavPerUnit()
    {
    }

    private NavPerUnit(
        Guid valuationRunId,
        Guid schemeClassId,
        decimal openingUnits,
        decimal unitsIssued,
        decimal unitsRedeemed,
        decimal approvedUnitAdjustments,
        decimal closingUnits,
        decimal navAmount,
        decimal unitPrice,
        decimal reportedUnitPrice)
    {
        ValuationRunId = valuationRunId != Guid.Empty ? valuationRunId : throw new ArgumentException("Valuation run id cannot be empty.", nameof(valuationRunId));
        SchemeClassId = schemeClassId != Guid.Empty ? schemeClassId : throw new ArgumentException("Scheme class id cannot be empty.", nameof(schemeClassId));
        OpeningUnits = NavValidation.NonNegative(openingUnits, nameof(openingUnits));
        UnitsIssued = NavValidation.NonNegative(unitsIssued, nameof(unitsIssued));
        UnitsRedeemed = NavValidation.NonNegative(unitsRedeemed, nameof(unitsRedeemed));
        ApprovedUnitAdjustments = approvedUnitAdjustments;
        ClosingUnits = NavValidation.Positive(closingUnits, nameof(closingUnits));
        NavAmount = navAmount;
        UnitPrice = NavValidation.NonNegative(unitPrice, nameof(unitPrice));
        ReportedUnitPrice = NavValidation.NonNegative(reportedUnitPrice, nameof(reportedUnitPrice));
    }

    public Guid ValuationRunId { get; private set; }
    public Guid SchemeClassId { get; private set; }
    public decimal OpeningUnits { get; private set; }
    public decimal UnitsIssued { get; private set; }
    public decimal UnitsRedeemed { get; private set; }
    public decimal ApprovedUnitAdjustments { get; private set; }
    public decimal ClosingUnits { get; private set; }
    public decimal NavAmount { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal ReportedUnitPrice { get; private set; }

    public static NavPerUnit Create(
        Guid valuationRunId,
        Guid schemeClassId,
        decimal openingUnits,
        decimal unitsIssued,
        decimal unitsRedeemed,
        decimal approvedUnitAdjustments,
        decimal closingUnits,
        decimal navAmount,
        decimal unitPrice,
        decimal reportedUnitPrice)
    {
        return new NavPerUnit(
            valuationRunId,
            schemeClassId,
            openingUnits,
            unitsIssued,
            unitsRedeemed,
            approvedUnitAdjustments,
            closingUnits,
            navAmount,
            unitPrice,
            reportedUnitPrice);
    }
}

public sealed class NavApproval : Entity
{
    private NavApproval()
    {
    }

    private NavApproval(Guid valuationRunId, NavApprovalStepType step, string actorUserId, DateTime occurredAtUtc, string? comment)
    {
        NavValidation.EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        ValuationRunId = valuationRunId != Guid.Empty ? valuationRunId : throw new ArgumentException("Valuation run id cannot be empty.", nameof(valuationRunId));
        Step = step;
        ActorUserId = NavValidation.Required(actorUserId, nameof(actorUserId), 200);
        OccurredAtUtc = occurredAtUtc;
        Comment = NavValidation.Optional(comment, 1000);
    }

    public Guid ValuationRunId { get; private set; }
    public NavApprovalStepType Step { get; private set; }
    public string ActorUserId { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }
    public string? Comment { get; private set; }

    public static NavApproval Create(Guid valuationRunId, NavApprovalStepType step, string actorUserId, DateTime occurredAtUtc, string? comment)
    {
        return new NavApproval(valuationRunId, step, actorUserId, occurredAtUtc, comment);
    }
}

public sealed class NavPublication : AuditableAggregateRoot
{
    private NavPublication()
    {
    }

    private NavPublication(
        Guid valuationRunId,
        Guid schemeId,
        Guid schemeClassId,
        BusinessDate valuationDate,
        int versionNumber,
        decimal publishedNav,
        decimal publishedUnitPrice,
        decimal reportedUnitPrice,
        string formulaVersion,
        string publishedByUserId,
        DateTime publishedAtUtc)
    {
        NavValidation.EnsureUtc(publishedAtUtc, nameof(publishedAtUtc));
        ValuationRunId = valuationRunId != Guid.Empty ? valuationRunId : throw new ArgumentException("Valuation run id cannot be empty.", nameof(valuationRunId));
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        SchemeClassId = schemeClassId != Guid.Empty ? schemeClassId : throw new ArgumentException("Scheme class id cannot be empty.", nameof(schemeClassId));
        ValuationDate = valuationDate;
        VersionNumber = NavValidation.Positive(versionNumber, nameof(versionNumber));
        PublishedNav = publishedNav;
        PublishedUnitPrice = NavValidation.NonNegative(publishedUnitPrice, nameof(publishedUnitPrice));
        ReportedUnitPrice = NavValidation.NonNegative(reportedUnitPrice, nameof(reportedUnitPrice));
        FormulaVersion = NavValidation.Required(formulaVersion, nameof(formulaVersion), 50);
        PublishedByUserId = NavValidation.Required(publishedByUserId, nameof(publishedByUserId), 200);
        PublishedAtUtc = publishedAtUtc;
        MarkCreated(publishedByUserId, publishedAtUtc);
    }

    public Guid ValuationRunId { get; private set; }
    public Guid SchemeId { get; private set; }
    public Guid SchemeClassId { get; private set; }
    public BusinessDate ValuationDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public int VersionNumber { get; private set; }
    public decimal PublishedNav { get; private set; }
    public decimal PublishedUnitPrice { get; private set; }
    public decimal ReportedUnitPrice { get; private set; }
    public string FormulaVersion { get; private set; } = string.Empty;
    public string PublishedByUserId { get; private set; } = string.Empty;
    public DateTime PublishedAtUtc { get; private set; }

    public static NavPublication Create(
        Guid valuationRunId,
        Guid schemeId,
        Guid schemeClassId,
        BusinessDate valuationDate,
        int versionNumber,
        decimal publishedNav,
        decimal publishedUnitPrice,
        decimal reportedUnitPrice,
        string formulaVersion,
        string publishedByUserId,
        DateTime publishedAtUtc)
    {
        return new NavPublication(
            valuationRunId,
            schemeId,
            schemeClassId,
            valuationDate,
            versionNumber,
            publishedNav,
            publishedUnitPrice,
            reportedUnitPrice,
            formulaVersion,
            publishedByUserId,
            publishedAtUtc);
    }
}

public sealed class NavVersionArchive : AuditableAggregateRoot
{
    private NavVersionArchive()
    {
    }

    private NavVersionArchive(
        Guid valuationRunId,
        Guid navPublicationId,
        int versionNumber,
        string payloadJson,
        string payloadHash,
        string archivedByUserId,
        DateTime archivedAtUtc)
    {
        NavValidation.EnsureUtc(archivedAtUtc, nameof(archivedAtUtc));
        ValuationRunId = valuationRunId != Guid.Empty ? valuationRunId : throw new ArgumentException("Valuation run id cannot be empty.", nameof(valuationRunId));
        NavPublicationId = navPublicationId != Guid.Empty ? navPublicationId : throw new ArgumentException("NAV publication id cannot be empty.", nameof(navPublicationId));
        VersionNumber = NavValidation.Positive(versionNumber, nameof(versionNumber));
        PayloadJson = NavValidation.Required(payloadJson, nameof(payloadJson));
        PayloadHash = NavValidation.Required(payloadHash, nameof(payloadHash), 128);
        ArchivedAtUtc = archivedAtUtc;
        MarkCreated(archivedByUserId, archivedAtUtc);
    }

    public Guid ValuationRunId { get; private set; }
    public Guid NavPublicationId { get; private set; }
    public int VersionNumber { get; private set; }
    public string PayloadJson { get; private set; } = string.Empty;
    public string PayloadHash { get; private set; } = string.Empty;
    public DateTime ArchivedAtUtc { get; private set; }

    public static NavVersionArchive Create(
        Guid valuationRunId,
        Guid navPublicationId,
        int versionNumber,
        string payloadJson,
        string payloadHash,
        string archivedByUserId,
        DateTime archivedAtUtc)
    {
        return new NavVersionArchive(valuationRunId, navPublicationId, versionNumber, payloadJson, payloadHash, archivedByUserId, archivedAtUtc);
    }
}

public sealed class NavRestatement : AuditableAggregateRoot
{
    private NavRestatement()
    {
    }

    private NavRestatement(
        Guid originalPublicationId,
        Guid correctedValuationRunId,
        Guid correctedPublicationId,
        Guid schemeId,
        Guid schemeClassId,
        BusinessDate valuationDate,
        int correctedVersionNumber,
        string reason,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        NavValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        OriginalPublicationId = originalPublicationId != Guid.Empty ? originalPublicationId : throw new ArgumentException("Original publication id cannot be empty.", nameof(originalPublicationId));
        CorrectedValuationRunId = correctedValuationRunId != Guid.Empty ? correctedValuationRunId : throw new ArgumentException("Corrected valuation run id cannot be empty.", nameof(correctedValuationRunId));
        CorrectedPublicationId = correctedPublicationId != Guid.Empty ? correctedPublicationId : throw new ArgumentException("Corrected publication id cannot be empty.", nameof(correctedPublicationId));
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        SchemeClassId = schemeClassId != Guid.Empty ? schemeClassId : throw new ArgumentException("Scheme class id cannot be empty.", nameof(schemeClassId));
        ValuationDate = valuationDate;
        CorrectedVersionNumber = NavValidation.Positive(correctedVersionNumber, nameof(correctedVersionNumber));
        Reason = NavValidation.Required(reason, nameof(reason), 1000);
        Status = NavRestatementStatus.Published;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid OriginalPublicationId { get; private set; }
    public Guid CorrectedValuationRunId { get; private set; }
    public Guid CorrectedPublicationId { get; private set; }
    public Guid SchemeId { get; private set; }
    public Guid SchemeClassId { get; private set; }
    public BusinessDate ValuationDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public int CorrectedVersionNumber { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public NavRestatementStatus Status { get; private set; }

    public static NavRestatement Create(
        Guid originalPublicationId,
        Guid correctedValuationRunId,
        Guid correctedPublicationId,
        Guid schemeId,
        Guid schemeClassId,
        BusinessDate valuationDate,
        int correctedVersionNumber,
        string reason,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        return new NavRestatement(
            originalPublicationId,
            correctedValuationRunId,
            correctedPublicationId,
            schemeId,
            schemeClassId,
            valuationDate,
            correctedVersionNumber,
            reason,
            createdByUserId,
            createdAtUtc);
    }
}
