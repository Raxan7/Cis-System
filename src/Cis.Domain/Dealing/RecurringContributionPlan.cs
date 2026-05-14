using Cis.Domain.Common;

namespace Cis.Domain.Dealing;

public sealed class RecurringContributionPlan : AuditableAggregateRoot
{
    private RecurringContributionPlan()
    {
    }

    private RecurringContributionPlan(
        Guid investorId,
        Guid schemeId,
        Guid schemeClassId,
        RecurringPlanFrequency frequency,
        decimal amount,
        string currency,
        DealingChannel channel,
        BusinessDate effectiveFrom,
        BusinessDate? effectiveTo,
        string collectionMethod,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        DealingValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        InvestorId = investorId;
        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        Frequency = frequency;
        Amount = DealingValidation.Positive(amount, nameof(amount));
        Currency = DealingValidation.Currency(currency, nameof(currency));
        Channel = channel;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        CollectionMethod = DealingValidation.Required(collectionMethod, nameof(collectionMethod), 100);
        Status = RecurringPlanStatus.Active;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid InvestorId { get; private set; }

    public Guid SchemeId { get; private set; }

    public Guid SchemeClassId { get; private set; }

    public RecurringPlanFrequency Frequency { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public DealingChannel Channel { get; private set; }

    public BusinessDate EffectiveFrom { get; private set; } = BusinessDate.From(DateOnly.MinValue);

    public BusinessDate? EffectiveTo { get; private set; }

    public string CollectionMethod { get; private set; } = string.Empty;

    public RecurringPlanStatus Status { get; private set; }

    public int MissedCollections { get; private set; }

    public int FailedDebits { get; private set; }

    public static RecurringContributionPlan Create(
        Guid investorId,
        Guid schemeId,
        Guid schemeClassId,
        RecurringPlanFrequency frequency,
        decimal amount,
        string currency,
        DealingChannel channel,
        BusinessDate effectiveFrom,
        BusinessDate? effectiveTo,
        string collectionMethod,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        return new RecurringContributionPlan(investorId, schemeId, schemeClassId, frequency, amount, currency, channel, effectiveFrom, effectiveTo, collectionMethod, createdByUserId, createdAtUtc);
    }

    public void Amend(
        RecurringPlanFrequency frequency,
        decimal amount,
        DealingChannel channel,
        BusinessDate effectiveFrom,
        BusinessDate? effectiveTo,
        string collectionMethod)
    {
        EnsureNotCancelled();
        Frequency = frequency;
        Amount = DealingValidation.Positive(amount, nameof(amount));
        Channel = channel;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        CollectionMethod = DealingValidation.Required(collectionMethod, nameof(collectionMethod), 100);
    }

    public void Pause()
    {
        EnsureNotCancelled();
        Status = RecurringPlanStatus.Paused;
    }

    public void Resume()
    {
        EnsureNotCancelled();
        Status = RecurringPlanStatus.Active;
    }

    public void Cancel()
    {
        Status = RecurringPlanStatus.Cancelled;
    }

    public void RegisterFailedDebit()
    {
        EnsureNotCancelled();
        FailedDebits += 1;
    }

    public void RegisterMissedCollection()
    {
        EnsureNotCancelled();
        MissedCollections += 1;
    }

    private void EnsureNotCancelled()
    {
        if (Status == RecurringPlanStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled recurring plans cannot be amended.");
        }
    }
}
