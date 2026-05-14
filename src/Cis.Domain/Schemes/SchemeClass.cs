using Cis.Domain.Common;

namespace Cis.Domain.Schemes;

public sealed class SchemeClass : Entity
{
    private SchemeClass()
    {
    }

    private SchemeClass(
        Guid schemeId,
        string code,
        string name,
        string currency,
        SchemeFrequency valuationFrequency,
        SchemeFrequency dealingFrequency,
        TimeOnly cutOffTime,
        decimal minimumContribution,
        decimal minimumBalance,
        int lockInDays,
        int noticePeriodDays)
    {
        SchemeId = schemeId == Guid.Empty ? throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId)) : schemeId;
        Code = SchemeValidation.Required(code, nameof(code), 30).ToUpperInvariant();
        Name = SchemeValidation.Required(name, nameof(name), 200);
        Currency = SchemeValidation.Currency(currency, nameof(currency));
        ValuationFrequency = valuationFrequency;
        DealingFrequency = dealingFrequency;
        CutOffTime = cutOffTime;
        MinimumContribution = SchemeValidation.NonNegative(minimumContribution, nameof(minimumContribution));
        MinimumBalance = SchemeValidation.NonNegative(minimumBalance, nameof(minimumBalance));
        LockInDays = lockInDays < 0 ? throw new ArgumentException("Lock-in days cannot be negative.", nameof(lockInDays)) : lockInDays;
        NoticePeriodDays = noticePeriodDays < 0 ? throw new ArgumentException("Notice period days cannot be negative.", nameof(noticePeriodDays)) : noticePeriodDays;
        IsActive = true;
    }

    public Guid SchemeId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Currency { get; private set; } = string.Empty;

    public SchemeFrequency ValuationFrequency { get; private set; }

    public SchemeFrequency DealingFrequency { get; private set; }

    public TimeOnly CutOffTime { get; private set; }

    public decimal MinimumContribution { get; private set; }

    public decimal MinimumBalance { get; private set; }

    public int LockInDays { get; private set; }

    public int NoticePeriodDays { get; private set; }

    public bool IsActive { get; private set; }

    public Scheme? Scheme { get; private set; }

    public static SchemeClass Create(
        Guid schemeId,
        string code,
        string name,
        string currency,
        SchemeFrequency valuationFrequency,
        SchemeFrequency dealingFrequency,
        TimeOnly cutOffTime,
        decimal minimumContribution,
        decimal minimumBalance,
        int lockInDays,
        int noticePeriodDays)
    {
        return new SchemeClass(
            schemeId,
            code,
            name,
            currency,
            valuationFrequency,
            dealingFrequency,
            cutOffTime,
            minimumContribution,
            minimumBalance,
            lockInDays,
            noticePeriodDays);
    }
}
