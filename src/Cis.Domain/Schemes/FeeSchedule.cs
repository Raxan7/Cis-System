using Cis.Domain.Common;

namespace Cis.Domain.Schemes;

public sealed class FeeSchedule : Entity
{
    private readonly List<FeeRule> _rules = [];

    private FeeSchedule()
    {
    }

    private FeeSchedule(
        Guid schemeId,
        Guid? schemeClassId,
        string feeType,
        string calculationBasis,
        decimal? rate,
        decimal? fixedAmount,
        BusinessDate effectiveFrom,
        BusinessDate? effectiveTo)
    {
        SchemeId = schemeId == Guid.Empty ? throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId)) : schemeId;
        SchemeClassId = schemeClassId;
        FeeType = SchemeValidation.Required(feeType, nameof(feeType), 100);
        CalculationBasis = SchemeValidation.Required(calculationBasis, nameof(calculationBasis), 100);
        Rate = rate.HasValue ? SchemeValidation.Percentage(rate.Value, nameof(rate)) : null;
        FixedAmount = fixedAmount.HasValue ? SchemeValidation.NonNegative(fixedAmount.Value, nameof(fixedAmount)) : null;
        EffectiveFrom = effectiveFrom ?? throw new ArgumentNullException(nameof(effectiveFrom));
        EffectiveTo = effectiveTo;
        if (effectiveTo is not null && effectiveTo.CompareTo(effectiveFrom) <= 0)
        {
            throw new ArgumentException("Effective-to date must be after effective-from date.", nameof(effectiveTo));
        }

        Status = FeeScheduleStatus.Draft;
    }

    public Guid SchemeId { get; private set; }

    public Guid? SchemeClassId { get; private set; }

    public string FeeType { get; private set; } = string.Empty;

    public string CalculationBasis { get; private set; } = string.Empty;

    public decimal? Rate { get; private set; }

    public decimal? FixedAmount { get; private set; }

    public BusinessDate EffectiveFrom { get; private set; } = BusinessDate.From(DateOnly.MinValue);

    public BusinessDate? EffectiveTo { get; private set; }

    public FeeScheduleStatus Status { get; private set; }

    public IReadOnlyCollection<FeeRule> Rules => _rules.AsReadOnly();

    public Scheme? Scheme { get; private set; }

    public SchemeClass? SchemeClass { get; private set; }

    public static FeeSchedule Create(
        Guid schemeId,
        Guid? schemeClassId,
        string feeType,
        string calculationBasis,
        decimal? rate,
        decimal? fixedAmount,
        BusinessDate effectiveFrom,
        BusinessDate? effectiveTo)
    {
        return new FeeSchedule(schemeId, schemeClassId, feeType, calculationBasis, rate, fixedAmount, effectiveFrom, effectiveTo);
    }

    public void AddRule(decimal? fromAmount, decimal? toAmount, decimal? rate, decimal? fixedAmount)
    {
        var rule = FeeRule.Create(Id, fromAmount, toAmount, rate, fixedAmount);
        if (_rules.Any(existing => existing.AmountRangeOverlaps(rule)))
        {
            throw new InvalidOperationException("Fee tier ranges cannot overlap.");
        }

        _rules.Add(rule);
    }

    public bool IsTiered => _rules.Count > 0;

    public void Activate()
    {
        Status = FeeScheduleStatus.Active;
    }

    public bool EffectiveDatesOverlap(FeeSchedule other)
    {
        var thisEnd = EffectiveTo?.Value ?? DateOnly.MaxValue;
        var otherEnd = other.EffectiveTo?.Value ?? DateOnly.MaxValue;
        return EffectiveFrom.Value < otherEnd && other.EffectiveFrom.Value < thisEnd;
    }
}
