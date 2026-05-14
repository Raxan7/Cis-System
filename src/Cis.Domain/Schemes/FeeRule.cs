using Cis.Domain.Common;

namespace Cis.Domain.Schemes;

public sealed class FeeRule : Entity
{
    private FeeRule()
    {
    }

    private FeeRule(Guid feeScheduleId, decimal? fromAmount, decimal? toAmount, decimal? rate, decimal? fixedAmount)
    {
        FeeScheduleId = feeScheduleId == Guid.Empty ? throw new ArgumentException("Fee schedule id cannot be empty.", nameof(feeScheduleId)) : feeScheduleId;
        FromAmount = fromAmount.HasValue ? SchemeValidation.NonNegative(fromAmount.Value, nameof(fromAmount)) : null;
        ToAmount = toAmount.HasValue ? SchemeValidation.NonNegative(toAmount.Value, nameof(toAmount)) : null;
        if (FromAmount.HasValue && ToAmount.HasValue && ToAmount.Value <= FromAmount.Value)
        {
            throw new ArgumentException("Tier upper amount must be greater than lower amount.", nameof(toAmount));
        }

        Rate = rate.HasValue ? SchemeValidation.Percentage(rate.Value, nameof(rate)) : null;
        FixedAmount = fixedAmount.HasValue ? SchemeValidation.NonNegative(fixedAmount.Value, nameof(fixedAmount)) : null;
        if (!Rate.HasValue && !FixedAmount.HasValue)
        {
            throw new ArgumentException("A tier must define a rate or fixed amount.", nameof(rate));
        }
    }

    public Guid FeeScheduleId { get; private set; }

    public decimal? FromAmount { get; private set; }

    public decimal? ToAmount { get; private set; }

    public decimal? Rate { get; private set; }

    public decimal? FixedAmount { get; private set; }

    public FeeSchedule? FeeSchedule { get; private set; }

    public static FeeRule Create(Guid feeScheduleId, decimal? fromAmount, decimal? toAmount, decimal? rate, decimal? fixedAmount)
    {
        return new FeeRule(feeScheduleId, fromAmount, toAmount, rate, fixedAmount);
    }

    public bool AmountRangeOverlaps(FeeRule other)
    {
        var thisStart = FromAmount ?? decimal.MinValue;
        var thisEnd = ToAmount ?? decimal.MaxValue;
        var otherStart = other.FromAmount ?? decimal.MinValue;
        var otherEnd = other.ToAmount ?? decimal.MaxValue;
        return thisStart < otherEnd && otherStart < thisEnd;
    }
}
