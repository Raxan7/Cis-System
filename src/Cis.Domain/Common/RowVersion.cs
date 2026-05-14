namespace Cis.Domain.Common;

public sealed class RowVersion : ValueObject
{
    private RowVersion()
    {
    }

    private RowVersion(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Row version cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; private init; }

    public static RowVersion New()
    {
        return new RowVersion(Guid.NewGuid());
    }

    public static RowVersion From(Guid value)
    {
        return new RowVersion(value);
    }

    public override string ToString()
    {
        return Value.ToString("N");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
