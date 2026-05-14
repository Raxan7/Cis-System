using Cis.Domain.Common;

namespace Cis.Domain.UnitRegister;

public sealed class InvestorPosition : AuditableAggregateRoot
{
    private InvestorPosition()
    {
    }

    private InvestorPosition(Guid investorId, Guid schemeId, Guid schemeClassId, int unitPrecision, string createdByUserId, DateTime createdAtUtc)
    {
        InvestorId = investorId;
        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        UnitPrecision = UnitRegisterValidation.Precision(unitPrecision, nameof(unitPrecision));
        MarkCreated(createdByUserId, UnitRegisterValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc)));
    }

    public Guid InvestorId { get; private set; }

    public Guid SchemeId { get; private set; }

    public Guid SchemeClassId { get; private set; }

    public decimal Units { get; private set; }

    public decimal LienedUnits { get; private set; }

    public decimal RedeemableUnits { get; private set; }

    public int UnitPrecision { get; private set; }

    public BusinessDate? AsOfDate { get; private set; }

    public string? LastTransactionReference { get; private set; }

    public static InvestorPosition Create(Guid investorId, Guid schemeId, Guid schemeClassId, int unitPrecision, string createdByUserId, DateTime createdAtUtc)
    {
        return new InvestorPosition(investorId, schemeId, schemeClassId, unitPrecision, createdByUserId, createdAtUtc);
    }

    public void RefreshFrom(UnitHolding holding)
    {
        if (holding.InvestorId != InvestorId || holding.SchemeId != SchemeId || holding.SchemeClassId != SchemeClassId)
        {
            throw new InvalidOperationException("Holding does not belong to this investor position.");
        }

        Units = holding.Units;
        LienedUnits = holding.LienedUnits;
        RedeemableUnits = holding.RedeemableUnits;
        UnitPrecision = holding.UnitPrecision;
        AsOfDate = holding.LastMovementDate;
        LastTransactionReference = holding.LastTransactionReference;
    }
}
