using Cis.Domain.Common;

namespace Cis.Domain.Schemes;

public sealed class ApprovedInstrumentRule : Entity
{
    private ApprovedInstrumentRule()
    {
    }

    private ApprovedInstrumentRule(
        Guid schemeId,
        string instrumentType,
        int tenorLimitDays,
        decimal issuerLimit,
        decimal counterpartyLimit,
        decimal assetClassLimit)
    {
        SchemeId = schemeId == Guid.Empty ? throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId)) : schemeId;
        InstrumentType = SchemeValidation.Required(instrumentType, nameof(instrumentType), 100);
        TenorLimitDays = tenorLimitDays < 0 ? throw new ArgumentException("Tenor limit cannot be negative.", nameof(tenorLimitDays)) : tenorLimitDays;
        IssuerLimit = SchemeValidation.Percentage(issuerLimit, nameof(issuerLimit));
        CounterpartyLimit = SchemeValidation.Percentage(counterpartyLimit, nameof(counterpartyLimit));
        AssetClassLimit = SchemeValidation.Percentage(assetClassLimit, nameof(assetClassLimit));
        Status = RuleStatus.Draft;
    }

    public Guid SchemeId { get; private set; }

    public string InstrumentType { get; private set; } = string.Empty;

    public int TenorLimitDays { get; private set; }

    public decimal IssuerLimit { get; private set; }

    public decimal CounterpartyLimit { get; private set; }

    public decimal AssetClassLimit { get; private set; }

    public RuleStatus Status { get; private set; }

    public Scheme? Scheme { get; private set; }

    public static ApprovedInstrumentRule Create(
        Guid schemeId,
        string instrumentType,
        int tenorLimitDays,
        decimal issuerLimit,
        decimal counterpartyLimit,
        decimal assetClassLimit)
    {
        return new ApprovedInstrumentRule(schemeId, instrumentType, tenorLimitDays, issuerLimit, counterpartyLimit, assetClassLimit);
    }

    public void Activate()
    {
        Status = RuleStatus.Active;
    }
}
