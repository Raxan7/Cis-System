namespace Cis.Domain.Common;

public sealed class BusinessDate : ValueObject, IComparable<BusinessDate>
{
    private BusinessDate()
    {
    }

    private BusinessDate(DateOnly value)
    {
        Value = value;
    }

    public DateOnly Value { get; private init; }

    public static BusinessDate From(DateOnly value)
    {
        return new BusinessDate(value);
    }

    public static BusinessDate From(DateTime value)
    {
        return new BusinessDate(DateOnly.FromDateTime(value));
    }

    public int CompareTo(BusinessDate? other)
    {
        return other is null ? 1 : Value.CompareTo(other.Value);
    }

    public override string ToString()
    {
        return Value.ToString("yyyy-MM-dd");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
