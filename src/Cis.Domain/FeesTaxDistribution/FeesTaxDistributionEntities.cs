using Cis.Domain.Common;

namespace Cis.Domain.FeesTaxDistribution;

public sealed class FeeAccrualRun : AuditableAggregateRoot
{
    private readonly List<FeeCalculation> _calculations = [];
    private readonly List<TaxCalculation> _taxCalculations = [];
    private readonly List<VatCalculation> _vatCalculations = [];
    private readonly List<WithholdingTaxCalculation> _withholdingTaxCalculations = [];

    private FeeAccrualRun()
    {
    }

    private FeeAccrualRun(
        Guid schemeId,
        Guid schemeClassId,
        BusinessDate periodStart,
        BusinessDate periodEnd,
        string formulaVersion,
        FeeDayCountBasis dayCountBasis,
        string ownerUserId,
        DateTime calculatedAtUtc)
    {
        if (schemeId == Guid.Empty)
        {
            throw new ArgumentException("Scheme id is required.", nameof(schemeId));
        }

        if (schemeClassId == Guid.Empty)
        {
            throw new ArgumentException("Scheme class id is required.", nameof(schemeClassId));
        }

        if (periodEnd.CompareTo(periodStart) < 0)
        {
            throw new ArgumentException("Period end cannot be before period start.", nameof(periodEnd));
        }

        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        RunNumber = $"FEE-{periodEnd.Value:yyyyMMdd}-{Guid.NewGuid():N}"[..40].ToUpperInvariant();
        FormulaVersion = FeesTaxDistributionValidation.Required(formulaVersion, nameof(formulaVersion), 50);
        DayCountBasis = dayCountBasis;
        InternalPrecision = FeesTaxDistributionFormulaEngine.InternalPrecision;
        OwnerUserId = FeesTaxDistributionValidation.Required(ownerUserId, nameof(ownerUserId), 200);
        CalculatedAtUtc = FeesTaxDistributionValidation.EnsureUtc(calculatedAtUtc, nameof(calculatedAtUtc));
        Status = FeeAccrualRunStatus.Calculated;
        MarkCreated(OwnerUserId, CalculatedAtUtc);
    }

    public Guid SchemeId { get; private set; }
    public Guid SchemeClassId { get; private set; }
    public BusinessDate PeriodStart { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public BusinessDate PeriodEnd { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public string RunNumber { get; private set; } = string.Empty;
    public string FormulaVersion { get; private set; } = string.Empty;
    public FeeDayCountBasis DayCountBasis { get; private set; }
    public int InternalPrecision { get; private set; }
    public FeeAccrualRunStatus Status { get; private set; }
    public string OwnerUserId { get; private set; } = string.Empty;
    public DateTime CalculatedAtUtc { get; private set; }
    public string? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public decimal TotalFeeAmount { get; private set; }
    public decimal TotalWaiverAmount { get; private set; }
    public decimal TotalVatAmount { get; private set; }
    public decimal TotalWithholdingTaxAmount { get; private set; }
    public decimal NetPayableAmount { get; private set; }
    public decimal TotalExpenseRatio { get; private set; }

    public IReadOnlyCollection<FeeCalculation> Calculations => _calculations.AsReadOnly();
    public IReadOnlyCollection<TaxCalculation> TaxCalculations => _taxCalculations.AsReadOnly();
    public IReadOnlyCollection<VatCalculation> VatCalculations => _vatCalculations.AsReadOnly();
    public IReadOnlyCollection<WithholdingTaxCalculation> WithholdingTaxCalculations => _withholdingTaxCalculations.AsReadOnly();

    public static FeeAccrualRun Create(
        Guid schemeId,
        Guid schemeClassId,
        BusinessDate periodStart,
        BusinessDate periodEnd,
        string formulaVersion,
        FeeDayCountBasis dayCountBasis,
        string ownerUserId,
        DateTime calculatedAtUtc)
    {
        return new FeeAccrualRun(schemeId, schemeClassId, periodStart, periodEnd, formulaVersion, dayCountBasis, ownerUserId, calculatedAtUtc);
    }

    public void AddCalculation(FeeCalculation calculation)
    {
        if (Status != FeeAccrualRunStatus.Calculated)
        {
            throw new InvalidOperationException("Approved fee accrual runs cannot be changed.");
        }

        _calculations.Add(calculation ?? throw new ArgumentNullException(nameof(calculation)));
        RecalculateTotals();
    }

    public void AddTaxCalculation(TaxCalculation calculation)
    {
        _taxCalculations.Add(calculation ?? throw new ArgumentNullException(nameof(calculation)));
    }

    public void AddVatCalculation(VatCalculation calculation)
    {
        _vatCalculations.Add(calculation ?? throw new ArgumentNullException(nameof(calculation)));
        RecalculateTotals();
    }

    public void AddWithholdingTaxCalculation(WithholdingTaxCalculation calculation)
    {
        _withholdingTaxCalculations.Add(calculation ?? throw new ArgumentNullException(nameof(calculation)));
        RecalculateTotals();
    }

    public void SetTotalExpenseRatio(decimal totalExpenseRatio)
    {
        TotalExpenseRatio = FeesTaxDistributionFormulaEngine.Round(totalExpenseRatio);
    }

    public void Approve(string approvedByUserId, DateTime approvedAtUtc)
    {
        if (Status != FeeAccrualRunStatus.Calculated)
        {
            throw new InvalidOperationException("Only calculated fee accrual runs can be approved.");
        }

        var actor = FeesTaxDistributionValidation.Required(approvedByUserId, nameof(approvedByUserId), 200);
        if (string.Equals(actor, OwnerUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Segregation of duties violation: fee accrual approver cannot be the owner.");
        }

        ApprovedByUserId = actor;
        ApprovedAtUtc = FeesTaxDistributionValidation.EnsureUtc(approvedAtUtc, nameof(approvedAtUtc));
        foreach (var calculation in _calculations)
        {
            calculation.MarkApproved(actor);
        }

        Status = FeeAccrualRunStatus.Approved;
    }

    private void RecalculateTotals()
    {
        var totalableCalculations = _calculations
            .Where(calculation => calculation.FormulaCode != "D-FML-014" || _calculations.All(other => other.FormulaCode != "D-FML-015"))
            .ToList();
        TotalFeeAmount = FeesTaxDistributionFormulaEngine.Round(totalableCalculations.Sum(calculation => calculation.GrossAmount));
        TotalWaiverAmount = FeesTaxDistributionFormulaEngine.Round(totalableCalculations.Sum(calculation => calculation.WaiverAmount));
        TotalVatAmount = FeesTaxDistributionFormulaEngine.Round(_vatCalculations.Sum(calculation => calculation.VatAmount));
        TotalWithholdingTaxAmount = FeesTaxDistributionFormulaEngine.Round(_withholdingTaxCalculations.Sum(calculation => calculation.WithholdingTaxAmount));
        NetPayableAmount = FeesTaxDistributionFormulaEngine.NetPayable(TotalFeeAmount - TotalWaiverAmount, TotalVatAmount, TotalWithholdingTaxAmount);
    }
}

public sealed class FeeCalculation : Entity
{
    private FeeCalculation()
    {
    }

    private FeeCalculation(
        Guid feeAccrualRunId,
        FeeCalculationType feeType,
        string formulaCode,
        string formulaVersion,
        decimal applicableFeeBase,
        decimal? annualRate,
        int? periodDays,
        decimal? fixedAmount,
        decimal grossAmount,
        decimal waiverAmount,
        string inputsJson,
        string outputJson,
        string ownerUserId)
    {
        FeeAccrualRunId = feeAccrualRunId;
        FeeType = feeType;
        FormulaCode = FeesTaxDistributionValidation.Required(formulaCode, nameof(formulaCode), 30);
        FormulaVersion = FeesTaxDistributionValidation.Required(formulaVersion, nameof(formulaVersion), 50);
        ApplicableFeeBase = FeesTaxDistributionValidation.NonNegative(applicableFeeBase, nameof(applicableFeeBase));
        AnnualRate = annualRate.HasValue ? FeesTaxDistributionValidation.Percentage(annualRate.Value, nameof(annualRate)) : null;
        PeriodDays = periodDays;
        FixedAmount = fixedAmount.HasValue ? FeesTaxDistributionValidation.NonNegative(fixedAmount.Value, nameof(fixedAmount)) : null;
        GrossAmount = FeesTaxDistributionValidation.NonNegative(grossAmount, nameof(grossAmount));
        WaiverAmount = FeesTaxDistributionValidation.NonNegative(waiverAmount, nameof(waiverAmount));
        NetAmount = FeesTaxDistributionFormulaEngine.Round(GrossAmount - WaiverAmount);
        InputsJson = FeesTaxDistributionValidation.Required(inputsJson, nameof(inputsJson), 8000);
        OutputJson = FeesTaxDistributionValidation.Required(outputJson, nameof(outputJson), 8000);
        OwnerUserId = FeesTaxDistributionValidation.Required(ownerUserId, nameof(ownerUserId), 200);
    }

    public Guid FeeAccrualRunId { get; private set; }
    public FeeCalculationType FeeType { get; private set; }
    public string FormulaCode { get; private set; } = string.Empty;
    public string FormulaVersion { get; private set; } = string.Empty;
    public decimal ApplicableFeeBase { get; private set; }
    public decimal? AnnualRate { get; private set; }
    public int? PeriodDays { get; private set; }
    public decimal? FixedAmount { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal WaiverAmount { get; private set; }
    public decimal NetAmount { get; private set; }
    public string InputsJson { get; private set; } = "{}";
    public string OutputJson { get; private set; } = "{}";
    public string OwnerUserId { get; private set; } = string.Empty;
    public string? ApprovedByUserId { get; private set; }

    public static FeeCalculation Create(
        Guid feeAccrualRunId,
        FeeCalculationType feeType,
        string formulaCode,
        string formulaVersion,
        decimal applicableFeeBase,
        decimal? annualRate,
        int? periodDays,
        decimal? fixedAmount,
        decimal grossAmount,
        decimal waiverAmount,
        string inputsJson,
        string outputJson,
        string ownerUserId)
    {
        return new FeeCalculation(feeAccrualRunId, feeType, formulaCode, formulaVersion, applicableFeeBase, annualRate, periodDays, fixedAmount, grossAmount, waiverAmount, inputsJson, outputJson, ownerUserId);
    }

    public void MarkApproved(string approvedByUserId)
    {
        ApprovedByUserId = FeesTaxDistributionValidation.Required(approvedByUserId, nameof(approvedByUserId), 200);
    }
}

public sealed class FeeWaiverRequest : AuditableAggregateRoot
{
    private FeeWaiverRequest()
    {
    }

    private FeeWaiverRequest(
        Guid schemeId,
        Guid schemeClassId,
        Guid? investorId,
        string feeType,
        BusinessDate effectiveFrom,
        BusinessDate? effectiveTo,
        decimal? waiverRate,
        decimal? waiverAmount,
        string reason,
        string requestedByUserId,
        DateTime requestedAtUtc)
    {
        if (schemeId == Guid.Empty)
        {
            throw new ArgumentException("Scheme id is required.", nameof(schemeId));
        }

        if (schemeClassId == Guid.Empty)
        {
            throw new ArgumentException("Scheme class id is required.", nameof(schemeClassId));
        }

        if (effectiveTo is not null && effectiveTo.CompareTo(effectiveFrom) < 0)
        {
            throw new ArgumentException("Effective-to date cannot be before effective-from date.", nameof(effectiveTo));
        }

        if (!waiverRate.HasValue && !waiverAmount.HasValue)
        {
            throw new ArgumentException("A waiver rate or waiver amount is required.", nameof(waiverRate));
        }

        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        InvestorId = investorId;
        FeeType = FeesTaxDistributionValidation.Required(feeType, nameof(feeType), 100);
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        WaiverRate = waiverRate.HasValue ? FeesTaxDistributionValidation.Percentage(waiverRate.Value, nameof(waiverRate)) : null;
        WaiverAmount = waiverAmount.HasValue ? FeesTaxDistributionValidation.NonNegative(waiverAmount.Value, nameof(waiverAmount)) : null;
        Reason = FeesTaxDistributionValidation.Required(reason, nameof(reason), 1000);
        Status = FeeWaiverStatus.PendingApproval;
        RequestedByUserId = FeesTaxDistributionValidation.Required(requestedByUserId, nameof(requestedByUserId), 200);
        RequestedAtUtc = FeesTaxDistributionValidation.EnsureUtc(requestedAtUtc, nameof(requestedAtUtc));
        MarkCreated(RequestedByUserId, RequestedAtUtc);
    }

    public Guid SchemeId { get; private set; }
    public Guid SchemeClassId { get; private set; }
    public Guid? InvestorId { get; private set; }
    public string FeeType { get; private set; } = string.Empty;
    public BusinessDate EffectiveFrom { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public BusinessDate? EffectiveTo { get; private set; }
    public decimal? WaiverRate { get; private set; }
    public decimal? WaiverAmount { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public FeeWaiverStatus Status { get; private set; }
    public string RequestedByUserId { get; private set; } = string.Empty;
    public DateTime RequestedAtUtc { get; private set; }
    public string? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public string? DecisionComment { get; private set; }

    public static FeeWaiverRequest Create(
        Guid schemeId,
        Guid schemeClassId,
        Guid? investorId,
        string feeType,
        BusinessDate effectiveFrom,
        BusinessDate? effectiveTo,
        decimal? waiverRate,
        decimal? waiverAmount,
        string reason,
        string requestedByUserId,
        DateTime requestedAtUtc)
    {
        return new FeeWaiverRequest(schemeId, schemeClassId, investorId, feeType, effectiveFrom, effectiveTo, waiverRate, waiverAmount, reason, requestedByUserId, requestedAtUtc);
    }

    public void Approve(string approvedByUserId, DateTime approvedAtUtc, string? comment)
    {
        if (Status != FeeWaiverStatus.PendingApproval)
        {
            throw new InvalidOperationException("Only pending fee waivers can be approved.");
        }

        var actor = FeesTaxDistributionValidation.Required(approvedByUserId, nameof(approvedByUserId), 200);
        if (string.Equals(actor, RequestedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Segregation of duties violation: requester cannot approve the same fee waiver.");
        }

        ApprovedByUserId = actor;
        ApprovedAtUtc = FeesTaxDistributionValidation.EnsureUtc(approvedAtUtc, nameof(approvedAtUtc));
        DecisionComment = string.IsNullOrWhiteSpace(comment) ? null : FeesTaxDistributionValidation.Required(comment, nameof(comment), 1000);
        Status = FeeWaiverStatus.Approved;
    }

    public bool AppliesTo(string feeType, DateOnly businessDate, Guid? investorId)
    {
        if (Status != FeeWaiverStatus.Approved)
        {
            return false;
        }

        if (!string.Equals(FeeType, feeType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (InvestorId.HasValue && InvestorId.Value != investorId)
        {
            return false;
        }

        return EffectiveFrom.Value <= businessDate && (EffectiveTo is null || businessDate <= EffectiveTo.Value);
    }
}

public sealed class TaxRule : AuditableAggregateRoot
{
    private TaxRule()
    {
    }

    private TaxRule(
        string jurisdiction,
        string category,
        string taxType,
        decimal rate,
        BusinessDate effectiveFrom,
        BusinessDate? effectiveTo,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        if (effectiveTo is not null && effectiveTo.CompareTo(effectiveFrom) < 0)
        {
            throw new ArgumentException("Effective-to date cannot be before effective-from date.", nameof(effectiveTo));
        }

        Jurisdiction = FeesTaxDistributionValidation.Required(jurisdiction, nameof(jurisdiction), 100).ToUpperInvariant();
        Category = FeesTaxDistributionValidation.Required(category, nameof(category), 100).ToUpperInvariant();
        TaxType = FeesTaxDistributionValidation.Required(taxType, nameof(taxType), 100).ToUpperInvariant();
        Rate = FeesTaxDistributionValidation.Percentage(rate, nameof(rate));
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Status = TaxRuleStatus.Active;
        CreatedByUserId = FeesTaxDistributionValidation.Required(createdByUserId, nameof(createdByUserId), 200);
        CreatedAtUtc = FeesTaxDistributionValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        MarkCreated(CreatedByUserId, CreatedAtUtc);
    }

    public string Jurisdiction { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string TaxType { get; private set; } = string.Empty;
    public decimal Rate { get; private set; }
    public BusinessDate EffectiveFrom { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public BusinessDate? EffectiveTo { get; private set; }
    public TaxRuleStatus Status { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static TaxRule Create(
        string jurisdiction,
        string category,
        string taxType,
        decimal rate,
        BusinessDate effectiveFrom,
        BusinessDate? effectiveTo,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        return new TaxRule(jurisdiction, category, taxType, rate, effectiveFrom, effectiveTo, createdByUserId, createdAtUtc);
    }

    public bool IsEffective(DateOnly date)
    {
        return Status == TaxRuleStatus.Active && EffectiveFrom.Value <= date && (EffectiveTo is null || date <= EffectiveTo.Value);
    }
}

public sealed class TaxCalculation : Entity
{
    private TaxCalculation()
    {
    }

    private TaxCalculation(
        Guid? feeAccrualRunId,
        Guid? investorDistributionId,
        Guid? taxRuleId,
        TaxCalculationType calculationType,
        string formulaCode,
        string formulaVersion,
        decimal taxableBase,
        decimal rate,
        decimal taxAmount,
        string inputsJson,
        string outputJson,
        string ownerUserId,
        string? approverUserId)
    {
        FeeAccrualRunId = feeAccrualRunId;
        InvestorDistributionId = investorDistributionId;
        TaxRuleId = taxRuleId;
        CalculationType = calculationType;
        FormulaCode = FeesTaxDistributionValidation.Required(formulaCode, nameof(formulaCode), 30);
        FormulaVersion = FeesTaxDistributionValidation.Required(formulaVersion, nameof(formulaVersion), 50);
        TaxableBase = FeesTaxDistributionValidation.NonNegative(taxableBase, nameof(taxableBase));
        Rate = FeesTaxDistributionValidation.Percentage(rate, nameof(rate));
        TaxAmount = FeesTaxDistributionValidation.NonNegative(taxAmount, nameof(taxAmount));
        InputsJson = FeesTaxDistributionValidation.Required(inputsJson, nameof(inputsJson), 8000);
        OutputJson = FeesTaxDistributionValidation.Required(outputJson, nameof(outputJson), 8000);
        OwnerUserId = FeesTaxDistributionValidation.Required(ownerUserId, nameof(ownerUserId), 200);
        ApproverUserId = string.IsNullOrWhiteSpace(approverUserId) ? null : FeesTaxDistributionValidation.Required(approverUserId, nameof(approverUserId), 200);
    }

    public Guid? FeeAccrualRunId { get; private set; }
    public Guid? InvestorDistributionId { get; private set; }
    public Guid? TaxRuleId { get; private set; }
    public TaxCalculationType CalculationType { get; private set; }
    public string FormulaCode { get; private set; } = string.Empty;
    public string FormulaVersion { get; private set; } = string.Empty;
    public decimal TaxableBase { get; private set; }
    public decimal Rate { get; private set; }
    public decimal TaxAmount { get; private set; }
    public string InputsJson { get; private set; } = "{}";
    public string OutputJson { get; private set; } = "{}";
    public string OwnerUserId { get; private set; } = string.Empty;
    public string? ApproverUserId { get; private set; }

    public static TaxCalculation Create(
        Guid? feeAccrualRunId,
        Guid? investorDistributionId,
        Guid? taxRuleId,
        TaxCalculationType calculationType,
        string formulaCode,
        string formulaVersion,
        decimal taxableBase,
        decimal rate,
        decimal taxAmount,
        string inputsJson,
        string outputJson,
        string ownerUserId,
        string? approverUserId)
    {
        return new TaxCalculation(feeAccrualRunId, investorDistributionId, taxRuleId, calculationType, formulaCode, formulaVersion, taxableBase, rate, taxAmount, inputsJson, outputJson, ownerUserId, approverUserId);
    }
}

public sealed class VatCalculation : Entity
{
    private VatCalculation()
    {
    }

    private VatCalculation(Guid feeAccrualRunId, Guid? feeCalculationId, Guid? taxRuleId, decimal taxableFeeAmount, decimal vatRate, decimal vatAmount, string formulaVersion, string ownerUserId, string? approverUserId)
    {
        FeeAccrualRunId = feeAccrualRunId;
        FeeCalculationId = feeCalculationId;
        TaxRuleId = taxRuleId;
        TaxableFeeAmount = FeesTaxDistributionValidation.NonNegative(taxableFeeAmount, nameof(taxableFeeAmount));
        VatRate = FeesTaxDistributionValidation.Percentage(vatRate, nameof(vatRate));
        VatAmount = FeesTaxDistributionValidation.NonNegative(vatAmount, nameof(vatAmount));
        FormulaVersion = FeesTaxDistributionValidation.Required(formulaVersion, nameof(formulaVersion), 50);
        OwnerUserId = FeesTaxDistributionValidation.Required(ownerUserId, nameof(ownerUserId), 200);
        ApproverUserId = string.IsNullOrWhiteSpace(approverUserId) ? null : FeesTaxDistributionValidation.Required(approverUserId, nameof(approverUserId), 200);
    }

    public Guid FeeAccrualRunId { get; private set; }
    public Guid? FeeCalculationId { get; private set; }
    public Guid? TaxRuleId { get; private set; }
    public decimal TaxableFeeAmount { get; private set; }
    public decimal VatRate { get; private set; }
    public decimal VatAmount { get; private set; }
    public string FormulaVersion { get; private set; } = string.Empty;
    public string OwnerUserId { get; private set; } = string.Empty;
    public string? ApproverUserId { get; private set; }

    public static VatCalculation Create(Guid feeAccrualRunId, Guid? feeCalculationId, Guid? taxRuleId, decimal taxableFeeAmount, decimal vatRate, decimal vatAmount, string formulaVersion, string ownerUserId, string? approverUserId)
    {
        return new VatCalculation(feeAccrualRunId, feeCalculationId, taxRuleId, taxableFeeAmount, vatRate, vatAmount, formulaVersion, ownerUserId, approverUserId);
    }
}

public sealed class WithholdingTaxCalculation : Entity
{
    private WithholdingTaxCalculation()
    {
    }

    private WithholdingTaxCalculation(Guid feeAccrualRunId, Guid? feeCalculationId, Guid? taxRuleId, decimal taxablePaymentBase, decimal withholdingTaxRate, decimal withholdingTaxAmount, string formulaVersion, string ownerUserId, string? approverUserId)
    {
        FeeAccrualRunId = feeAccrualRunId;
        FeeCalculationId = feeCalculationId;
        TaxRuleId = taxRuleId;
        TaxablePaymentBase = FeesTaxDistributionValidation.NonNegative(taxablePaymentBase, nameof(taxablePaymentBase));
        WithholdingTaxRate = FeesTaxDistributionValidation.Percentage(withholdingTaxRate, nameof(withholdingTaxRate));
        WithholdingTaxAmount = FeesTaxDistributionValidation.NonNegative(withholdingTaxAmount, nameof(withholdingTaxAmount));
        FormulaVersion = FeesTaxDistributionValidation.Required(formulaVersion, nameof(formulaVersion), 50);
        OwnerUserId = FeesTaxDistributionValidation.Required(ownerUserId, nameof(ownerUserId), 200);
        ApproverUserId = string.IsNullOrWhiteSpace(approverUserId) ? null : FeesTaxDistributionValidation.Required(approverUserId, nameof(approverUserId), 200);
    }

    public Guid FeeAccrualRunId { get; private set; }
    public Guid? FeeCalculationId { get; private set; }
    public Guid? TaxRuleId { get; private set; }
    public decimal TaxablePaymentBase { get; private set; }
    public decimal WithholdingTaxRate { get; private set; }
    public decimal WithholdingTaxAmount { get; private set; }
    public string FormulaVersion { get; private set; } = string.Empty;
    public string OwnerUserId { get; private set; } = string.Empty;
    public string? ApproverUserId { get; private set; }

    public static WithholdingTaxCalculation Create(Guid feeAccrualRunId, Guid? feeCalculationId, Guid? taxRuleId, decimal taxablePaymentBase, decimal withholdingTaxRate, decimal withholdingTaxAmount, string formulaVersion, string ownerUserId, string? approverUserId)
    {
        return new WithholdingTaxCalculation(feeAccrualRunId, feeCalculationId, taxRuleId, taxablePaymentBase, withholdingTaxRate, withholdingTaxAmount, formulaVersion, ownerUserId, approverUserId);
    }
}

public sealed class DistributionDeclaration : AuditableAggregateRoot
{
    private DistributionDeclaration()
    {
    }

    private DistributionDeclaration(
        Guid schemeId,
        Guid schemeClassId,
        BusinessDate declarationDate,
        BusinessDate recordDate,
        BusinessDate paymentDate,
        string formulaVersion,
        decimal investmentIncome,
        decimal otherIncome,
        decimal realizedGainsLosses,
        decimal priorPeriodAdjustments,
        decimal fundExpenses,
        decimal fees,
        decimal taxes,
        decimal reserveTransfers,
        decimal eligibleUnits,
        decimal availableCash,
        bool coverageOverrideRequested,
        string? coverageOverrideReason,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        if (schemeId == Guid.Empty)
        {
            throw new ArgumentException("Scheme id is required.", nameof(schemeId));
        }

        if (schemeClassId == Guid.Empty)
        {
            throw new ArgumentException("Scheme class id is required.", nameof(schemeClassId));
        }

        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        DeclarationDate = declarationDate;
        RecordDate = recordDate;
        PaymentDate = paymentDate;
        FormulaVersion = FeesTaxDistributionValidation.Required(formulaVersion, nameof(formulaVersion), 50);
        InvestmentIncome = investmentIncome;
        OtherIncome = otherIncome;
        RealizedGainsLosses = realizedGainsLosses;
        PriorPeriodAdjustments = priorPeriodAdjustments;
        FundExpenses = FeesTaxDistributionValidation.NonNegative(fundExpenses, nameof(fundExpenses));
        Fees = FeesTaxDistributionValidation.NonNegative(fees, nameof(fees));
        Taxes = FeesTaxDistributionValidation.NonNegative(taxes, nameof(taxes));
        ReserveTransfers = FeesTaxDistributionValidation.NonNegative(reserveTransfers, nameof(reserveTransfers));
        EligibleUnits = FeesTaxDistributionValidation.Positive(eligibleUnits, nameof(eligibleUnits));
        AvailableCash = FeesTaxDistributionValidation.NonNegative(availableCash, nameof(availableCash));
        CoverageOverrideRequested = coverageOverrideRequested;
        CoverageOverrideReason = string.IsNullOrWhiteSpace(coverageOverrideReason) ? null : FeesTaxDistributionValidation.Required(coverageOverrideReason, nameof(coverageOverrideReason), 1000);
        CreatedByUserId = FeesTaxDistributionValidation.Required(createdByUserId, nameof(createdByUserId), 200);
        CreatedAtUtc = FeesTaxDistributionValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        GrossDistributableIncome = FeesTaxDistributionFormulaEngine.GrossDistributableIncome(investmentIncome, otherIncome, realizedGainsLosses, priorPeriodAdjustments);
        NetDistributableIncome = FeesTaxDistributionFormulaEngine.NetDistributableIncome(GrossDistributableIncome, fundExpenses, fees, taxes, reserveTransfers);
        DistributionPerUnit = FeesTaxDistributionFormulaEngine.DistributionPerUnit(NetDistributableIncome, eligibleUnits);
        PlannedDistributionAmount = FeesTaxDistributionFormulaEngine.Round(NetDistributableIncome);
        DistributionCoverage = FeesTaxDistributionFormulaEngine.DistributionCoverage(availableCash, PlannedDistributionAmount);
        Status = DistributionDeclarationStatus.Draft;
        MarkCreated(CreatedByUserId, CreatedAtUtc);
    }

    public Guid SchemeId { get; private set; }
    public Guid SchemeClassId { get; private set; }
    public BusinessDate DeclarationDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public BusinessDate RecordDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public BusinessDate PaymentDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public string FormulaVersion { get; private set; } = string.Empty;
    public decimal InvestmentIncome { get; private set; }
    public decimal OtherIncome { get; private set; }
    public decimal RealizedGainsLosses { get; private set; }
    public decimal PriorPeriodAdjustments { get; private set; }
    public decimal FundExpenses { get; private set; }
    public decimal Fees { get; private set; }
    public decimal Taxes { get; private set; }
    public decimal ReserveTransfers { get; private set; }
    public decimal GrossDistributableIncome { get; private set; }
    public decimal NetDistributableIncome { get; private set; }
    public decimal EligibleUnits { get; private set; }
    public decimal DistributionPerUnit { get; private set; }
    public decimal AvailableCash { get; private set; }
    public decimal PlannedDistributionAmount { get; private set; }
    public decimal DistributionCoverage { get; private set; }
    public bool CoverageOverrideRequested { get; private set; }
    public string? CoverageOverrideReason { get; private set; }
    public bool CoverageOverrideApproved { get; private set; }
    public DistributionDeclarationStatus Status { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public string? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public string? ApprovalComment { get; private set; }

    public static DistributionDeclaration Create(
        Guid schemeId,
        Guid schemeClassId,
        BusinessDate declarationDate,
        BusinessDate recordDate,
        BusinessDate paymentDate,
        string formulaVersion,
        decimal investmentIncome,
        decimal otherIncome,
        decimal realizedGainsLosses,
        decimal priorPeriodAdjustments,
        decimal fundExpenses,
        decimal fees,
        decimal taxes,
        decimal reserveTransfers,
        decimal eligibleUnits,
        decimal availableCash,
        bool coverageOverrideRequested,
        string? coverageOverrideReason,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        return new DistributionDeclaration(schemeId, schemeClassId, declarationDate, recordDate, paymentDate, formulaVersion, investmentIncome, otherIncome, realizedGainsLosses, priorPeriodAdjustments, fundExpenses, fees, taxes, reserveTransfers, eligibleUnits, availableCash, coverageOverrideRequested, coverageOverrideReason, createdByUserId, createdAtUtc);
    }

    public void Approve(string approvedByUserId, DateTime approvedAtUtc, bool approveCoverageOverride, string? comment)
    {
        if (Status != DistributionDeclarationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft distribution declarations can be approved.");
        }

        var actor = FeesTaxDistributionValidation.Required(approvedByUserId, nameof(approvedByUserId), 200);
        if (string.Equals(actor, CreatedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Segregation of duties violation: declaration creator cannot approve the same distribution.");
        }

        if (DistributionCoverage < 1m && !(CoverageOverrideRequested && approveCoverageOverride))
        {
            throw new InvalidOperationException("Distribution coverage check failed and requires an approved override workflow.");
        }

        CoverageOverrideApproved = DistributionCoverage >= 1m ? false : approveCoverageOverride;
        ApprovedByUserId = actor;
        ApprovedAtUtc = FeesTaxDistributionValidation.EnsureUtc(approvedAtUtc, nameof(approvedAtUtc));
        ApprovalComment = string.IsNullOrWhiteSpace(comment) ? null : FeesTaxDistributionValidation.Required(comment, nameof(comment), 1000);
        Status = DistributionDeclarationStatus.Approved;
    }
}

public sealed class DistributionRun : AuditableAggregateRoot
{
    private readonly List<InvestorDistribution> _investorDistributions = [];
    private readonly List<ReinvestmentInstruction> _reinvestmentInstructions = [];
    private readonly List<ReinvestmentUnitAllocation> _reinvestmentUnitAllocations = [];

    private DistributionRun()
    {
    }

    private DistributionRun(Guid declarationId, Guid schemeId, Guid schemeClassId, BusinessDate runDate, string createdByUserId, DateTime createdAtUtc)
    {
        DeclarationId = declarationId != Guid.Empty ? declarationId : throw new ArgumentException("Declaration id is required.", nameof(declarationId));
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id is required.", nameof(schemeId));
        SchemeClassId = schemeClassId != Guid.Empty ? schemeClassId : throw new ArgumentException("Scheme class id is required.", nameof(schemeClassId));
        RunDate = runDate;
        RunNumber = $"DIST-{runDate.Value:yyyyMMdd}-{Guid.NewGuid():N}"[..40].ToUpperInvariant();
        Status = DistributionRunStatus.Draft;
        CreatedByUserId = FeesTaxDistributionValidation.Required(createdByUserId, nameof(createdByUserId), 200);
        CreatedAtUtc = FeesTaxDistributionValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        MarkCreated(CreatedByUserId, CreatedAtUtc);
    }

    public Guid DeclarationId { get; private set; }
    public Guid SchemeId { get; private set; }
    public Guid SchemeClassId { get; private set; }
    public BusinessDate RunDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public string RunNumber { get; private set; } = string.Empty;
    public DistributionRunStatus Status { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public string? PublishedByUserId { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }
    public decimal TotalGrossDistribution { get; private set; }
    public decimal TotalTaxAmount { get; private set; }
    public decimal TotalNetDistribution { get; private set; }
    public decimal TotalReinvestedUnits { get; private set; }

    public IReadOnlyCollection<InvestorDistribution> InvestorDistributions => _investorDistributions.AsReadOnly();
    public IReadOnlyCollection<ReinvestmentInstruction> ReinvestmentInstructions => _reinvestmentInstructions.AsReadOnly();
    public IReadOnlyCollection<ReinvestmentUnitAllocation> ReinvestmentUnitAllocations => _reinvestmentUnitAllocations.AsReadOnly();

    public static DistributionRun Create(Guid declarationId, Guid schemeId, Guid schemeClassId, BusinessDate runDate, string createdByUserId, DateTime createdAtUtc)
    {
        return new DistributionRun(declarationId, schemeId, schemeClassId, runDate, createdByUserId, createdAtUtc);
    }

    public void AddInvestorDistribution(InvestorDistribution investorDistribution)
    {
        EnsureDraft();
        _investorDistributions.Add(investorDistribution ?? throw new ArgumentNullException(nameof(investorDistribution)));
        RecalculateTotals();
    }

    public void AddReinvestmentInstruction(ReinvestmentInstruction instruction)
    {
        EnsureDraft();
        _reinvestmentInstructions.Add(instruction ?? throw new ArgumentNullException(nameof(instruction)));
    }

    public void AddReinvestmentUnitAllocation(ReinvestmentUnitAllocation allocation)
    {
        EnsureDraft();
        _reinvestmentUnitAllocations.Add(allocation ?? throw new ArgumentNullException(nameof(allocation)));
        RecalculateTotals();
    }

    public void Publish(string publishedByUserId, DateTime publishedAtUtc)
    {
        if (Status != DistributionRunStatus.Draft)
        {
            throw new InvalidOperationException("Only draft distribution runs can be published.");
        }

        var actor = FeesTaxDistributionValidation.Required(publishedByUserId, nameof(publishedByUserId), 200);
        if (string.Equals(actor, CreatedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Segregation of duties violation: distribution run creator cannot publish the same run.");
        }

        PublishedByUserId = actor;
        PublishedAtUtc = FeesTaxDistributionValidation.EnsureUtc(publishedAtUtc, nameof(publishedAtUtc));
        Status = DistributionRunStatus.Published;
    }

    private void EnsureDraft()
    {
        if (Status != DistributionRunStatus.Draft)
        {
            throw new InvalidOperationException("Published distribution runs cannot be changed.");
        }
    }

    private void RecalculateTotals()
    {
        TotalGrossDistribution = FeesTaxDistributionFormulaEngine.Round(_investorDistributions.Sum(distribution => distribution.GrossDistribution));
        TotalTaxAmount = FeesTaxDistributionFormulaEngine.Round(_investorDistributions.Sum(distribution => distribution.InvestorTaxAmount));
        TotalNetDistribution = FeesTaxDistributionFormulaEngine.Round(_investorDistributions.Sum(distribution => distribution.NetDistribution));
        TotalReinvestedUnits = FeesTaxDistributionFormulaEngine.Round(_reinvestmentUnitAllocations.Sum(allocation => allocation.UnitsAllocated));
    }
}

public sealed class InvestorDistribution : Entity
{
    private InvestorDistribution()
    {
    }

    private InvestorDistribution(
        Guid distributionRunId,
        Guid investorId,
        decimal eligibleUnits,
        decimal distributionPerUnit,
        decimal grossDistribution,
        decimal investorTaxAmount,
        decimal netDistribution,
        DistributionMethod method,
        decimal openingValue,
        decimal closingValue,
        decimal cashDistributions,
        decimal netContributions,
        decimal investorReturnForPeriod,
        Guid? taxRuleId,
        string formulaVersion,
        string ownerUserId,
        string? approverUserId)
    {
        DistributionRunId = distributionRunId;
        InvestorId = investorId != Guid.Empty ? investorId : throw new ArgumentException("Investor id is required.", nameof(investorId));
        EligibleUnits = FeesTaxDistributionValidation.NonNegative(eligibleUnits, nameof(eligibleUnits));
        DistributionPerUnit = distributionPerUnit;
        GrossDistribution = FeesTaxDistributionValidation.NonNegative(grossDistribution, nameof(grossDistribution));
        InvestorTaxAmount = FeesTaxDistributionValidation.NonNegative(investorTaxAmount, nameof(investorTaxAmount));
        NetDistribution = FeesTaxDistributionValidation.NonNegative(netDistribution, nameof(netDistribution));
        Method = method;
        OpeningValue = openingValue;
        ClosingValue = closingValue;
        CashDistributions = cashDistributions;
        NetContributions = netContributions;
        InvestorReturnForPeriod = investorReturnForPeriod;
        TaxRuleId = taxRuleId;
        FormulaVersion = FeesTaxDistributionValidation.Required(formulaVersion, nameof(formulaVersion), 50);
        OwnerUserId = FeesTaxDistributionValidation.Required(ownerUserId, nameof(ownerUserId), 200);
        ApproverUserId = string.IsNullOrWhiteSpace(approverUserId) ? null : FeesTaxDistributionValidation.Required(approverUserId, nameof(approverUserId), 200);
    }

    public Guid DistributionRunId { get; private set; }
    public Guid InvestorId { get; private set; }
    public decimal EligibleUnits { get; private set; }
    public decimal DistributionPerUnit { get; private set; }
    public decimal GrossDistribution { get; private set; }
    public decimal InvestorTaxAmount { get; private set; }
    public decimal NetDistribution { get; private set; }
    public DistributionMethod Method { get; private set; }
    public decimal OpeningValue { get; private set; }
    public decimal ClosingValue { get; private set; }
    public decimal CashDistributions { get; private set; }
    public decimal NetContributions { get; private set; }
    public decimal InvestorReturnForPeriod { get; private set; }
    public Guid? TaxRuleId { get; private set; }
    public string FormulaVersion { get; private set; } = string.Empty;
    public string OwnerUserId { get; private set; } = string.Empty;
    public string? ApproverUserId { get; private set; }

    public static InvestorDistribution Create(
        Guid distributionRunId,
        Guid investorId,
        decimal eligibleUnits,
        decimal distributionPerUnit,
        decimal grossDistribution,
        decimal investorTaxAmount,
        decimal netDistribution,
        DistributionMethod method,
        decimal openingValue,
        decimal closingValue,
        decimal cashDistributions,
        decimal netContributions,
        decimal investorReturnForPeriod,
        Guid? taxRuleId,
        string formulaVersion,
        string ownerUserId,
        string? approverUserId)
    {
        return new InvestorDistribution(distributionRunId, investorId, eligibleUnits, distributionPerUnit, grossDistribution, investorTaxAmount, netDistribution, method, openingValue, closingValue, cashDistributions, netContributions, investorReturnForPeriod, taxRuleId, formulaVersion, ownerUserId, approverUserId);
    }
}

public sealed class ReinvestmentInstruction : Entity
{
    private ReinvestmentInstruction()
    {
    }

    private ReinvestmentInstruction(Guid distributionRunId, Guid investorId, decimal reinvestmentPrice, string ownerUserId)
    {
        DistributionRunId = distributionRunId;
        InvestorId = investorId != Guid.Empty ? investorId : throw new ArgumentException("Investor id is required.", nameof(investorId));
        ReinvestmentPrice = FeesTaxDistributionValidation.Positive(reinvestmentPrice, nameof(reinvestmentPrice));
        Status = ReinvestmentInstructionStatus.PendingApproval;
        OwnerUserId = FeesTaxDistributionValidation.Required(ownerUserId, nameof(ownerUserId), 200);
    }

    public Guid DistributionRunId { get; private set; }
    public Guid InvestorId { get; private set; }
    public decimal ReinvestmentPrice { get; private set; }
    public ReinvestmentInstructionStatus Status { get; private set; }
    public string OwnerUserId { get; private set; } = string.Empty;
    public string? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }

    public static ReinvestmentInstruction Create(Guid distributionRunId, Guid investorId, decimal reinvestmentPrice, string ownerUserId)
    {
        return new ReinvestmentInstruction(distributionRunId, investorId, reinvestmentPrice, ownerUserId);
    }

    public void MarkAllocated(string approvedByUserId, DateTime approvedAtUtc)
    {
        ApprovedByUserId = FeesTaxDistributionValidation.Required(approvedByUserId, nameof(approvedByUserId), 200);
        ApprovedAtUtc = FeesTaxDistributionValidation.EnsureUtc(approvedAtUtc, nameof(approvedAtUtc));
        Status = ReinvestmentInstructionStatus.Allocated;
    }
}

public sealed class ReinvestmentUnitAllocation : Entity
{
    private ReinvestmentUnitAllocation()
    {
    }

    private ReinvestmentUnitAllocation(
        Guid distributionRunId,
        Guid investorDistributionId,
        Guid reinvestmentInstructionId,
        Guid investorId,
        decimal netDistribution,
        decimal reinvestmentPrice,
        decimal unitsAllocated,
        string sourceReference,
        string formulaVersion)
    {
        DistributionRunId = distributionRunId;
        InvestorDistributionId = investorDistributionId;
        ReinvestmentInstructionId = reinvestmentInstructionId;
        InvestorId = investorId != Guid.Empty ? investorId : throw new ArgumentException("Investor id is required.", nameof(investorId));
        NetDistribution = FeesTaxDistributionValidation.NonNegative(netDistribution, nameof(netDistribution));
        ReinvestmentPrice = FeesTaxDistributionValidation.Positive(reinvestmentPrice, nameof(reinvestmentPrice));
        UnitsAllocated = FeesTaxDistributionValidation.NonNegative(unitsAllocated, nameof(unitsAllocated));
        SourceReference = FeesTaxDistributionValidation.Required(sourceReference, nameof(sourceReference), 100).ToUpperInvariant();
        FormulaVersion = FeesTaxDistributionValidation.Required(formulaVersion, nameof(formulaVersion), 50);
    }

    public Guid DistributionRunId { get; private set; }
    public Guid InvestorDistributionId { get; private set; }
    public Guid ReinvestmentInstructionId { get; private set; }
    public Guid InvestorId { get; private set; }
    public Guid? UnitLedgerEntryId { get; private set; }
    public decimal NetDistribution { get; private set; }
    public decimal ReinvestmentPrice { get; private set; }
    public decimal UnitsAllocated { get; private set; }
    public string SourceReference { get; private set; } = string.Empty;
    public string FormulaVersion { get; private set; } = string.Empty;
    public DateTime? AllocatedAtUtc { get; private set; }

    public static ReinvestmentUnitAllocation Create(
        Guid distributionRunId,
        Guid investorDistributionId,
        Guid reinvestmentInstructionId,
        Guid investorId,
        decimal netDistribution,
        decimal reinvestmentPrice,
        decimal unitsAllocated,
        string sourceReference,
        string formulaVersion)
    {
        return new ReinvestmentUnitAllocation(distributionRunId, investorDistributionId, reinvestmentInstructionId, investorId, netDistribution, reinvestmentPrice, unitsAllocated, sourceReference, formulaVersion);
    }

    public void LinkUnitLedgerEntry(Guid unitLedgerEntryId, DateTime allocatedAtUtc)
    {
        if (UnitLedgerEntryId.HasValue)
        {
            throw new InvalidOperationException("Reinvestment allocation is already linked to a unit ledger entry.");
        }

        UnitLedgerEntryId = unitLedgerEntryId != Guid.Empty ? unitLedgerEntryId : throw new ArgumentException("Unit ledger entry id is required.", nameof(unitLedgerEntryId));
        AllocatedAtUtc = FeesTaxDistributionValidation.EnsureUtc(allocatedAtUtc, nameof(allocatedAtUtc));
    }
}
