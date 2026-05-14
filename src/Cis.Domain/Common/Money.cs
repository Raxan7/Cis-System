namespace Cis.Domain.Common;

public sealed class Money : ValueObject
{
    private Money()
    {
    }

    private Money(decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        if (normalizedCurrency.Length != 3 || normalizedCurrency.Any(c => c < 'A' || c > 'Z'))
        {
            throw new ArgumentException("Currency must be a three-letter ISO 4217 code.", nameof(currency));
        }

        Amount = amount;
        Currency = normalizedCurrency;
    }

    public decimal Amount { get; private init; }

    public string Currency { get; private init; } = string.Empty;

    public static Money From(decimal amount, string currency)
    {
        return new Money(amount, currency);
    }

    public static Money Zero(string currency)
    {
        return new Money(0m, currency);
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal multiplier)
    {
        return new Money(Amount * multiplier, Currency);
    }

    public Money Negate()
    {
        return new Money(-Amount, Currency);
    }

    public override string ToString()
    {
        return $"{Currency} {Amount}";
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException("Money values must have the same currency.");
        }
    }
}
