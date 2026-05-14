using Cis.Domain.Common;

namespace Cis.Domain.Schemes;

public sealed class SchemeCustodianMapping : Entity
{
    private SchemeCustodianMapping()
    {
    }

    private SchemeCustodianMapping(Guid schemeId, string custodianName, string custodyAccountReference, string settlementAccountReference)
    {
        SchemeId = schemeId == Guid.Empty ? throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId)) : schemeId;
        CustodianName = SchemeValidation.Required(custodianName, nameof(custodianName), 200);
        CustodyAccountReference = SchemeValidation.Required(custodyAccountReference, nameof(custodyAccountReference), 100);
        SettlementAccountReference = SchemeValidation.Required(settlementAccountReference, nameof(settlementAccountReference), 100);
        IsActive = true;
    }

    public Guid SchemeId { get; private set; }

    public string CustodianName { get; private set; } = string.Empty;

    public string CustodyAccountReference { get; private set; } = string.Empty;

    public string SettlementAccountReference { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public Scheme? Scheme { get; private set; }

    public static SchemeCustodianMapping Create(Guid schemeId, string custodianName, string custodyAccountReference, string settlementAccountReference)
    {
        return new SchemeCustodianMapping(schemeId, custodianName, custodyAccountReference, settlementAccountReference);
    }
}
