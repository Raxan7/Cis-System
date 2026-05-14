namespace Cis.Domain.Common;

public sealed class Percentage : ValueObject
{
    private Percentage()
    {
    }

    private Percentage(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; private init; }

    public decimal Ratio => Value / 100m;

    public static Percentage FromPercent(decimal value)
    {
        return new Percentage(value);
    }

    public static Percentage FromRatio(decimal ratio)
    {
        return new Percentage(ratio * 100m);
    }

    public override string ToString()
    {
        return $"{Value}%";
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
