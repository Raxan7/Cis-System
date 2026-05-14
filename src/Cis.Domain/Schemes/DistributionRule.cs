using Cis.Domain.Common;

namespace Cis.Domain.Schemes;

public sealed class DistributionRule : Entity
{
    private DistributionRule()
    {
    }

    private DistributionRule(Guid schemeId, SchemeFrequency distributionFrequency, bool reinvestmentAllowed, int paymentDay)
    {
        SchemeId = schemeId == Guid.Empty ? throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId)) : schemeId;
        DistributionFrequency = distributionFrequency;
        ReinvestmentAllowed = reinvestmentAllowed;
        PaymentDay = paymentDay is < 1 or > 31 ? throw new ArgumentException("Payment day must be between 1 and 31.", nameof(paymentDay)) : paymentDay;
        IsActive = true;
    }

    public Guid SchemeId { get; private set; }

    public SchemeFrequency DistributionFrequency { get; private set; }

    public bool ReinvestmentAllowed { get; private set; }

    public int PaymentDay { get; private set; }

    public bool IsActive { get; private set; }

    public Scheme? Scheme { get; private set; }

    public static DistributionRule Create(Guid schemeId, SchemeFrequency distributionFrequency, bool reinvestmentAllowed, int paymentDay)
    {
        return new DistributionRule(schemeId, distributionFrequency, reinvestmentAllowed, paymentDay);
    }
}
