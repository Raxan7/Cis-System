using Cis.Domain.Common;

namespace Cis.Domain.UnitRegister;

public sealed class UnitHolding : AuditableAggregateRoot
{
    private UnitHolding()
    {
    }

    private UnitHolding(Guid investorId, Guid schemeId, Guid schemeClassId, int unitPrecision, string createdByUserId, DateTime createdAtUtc)
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

    public decimal RedeemableUnits => Units - LienedUnits;

    public int UnitPrecision { get; private set; }

    public BusinessDate? LastMovementDate { get; private set; }

    public string? LastTransactionReference { get; private set; }

    public static UnitHolding Create(Guid investorId, Guid schemeId, Guid schemeClassId, int unitPrecision, string createdByUserId, DateTime createdAtUtc)
    {
        return new UnitHolding(investorId, schemeId, schemeClassId, unitPrecision, createdByUserId, createdAtUtc);
    }

    public void Apply(UnitLedgerEntry entry)
    {
        if (entry.InvestorId != InvestorId || entry.SchemeId != SchemeId || entry.SchemeClassId != SchemeClassId)
        {
            throw new InvalidOperationException("Ledger entry does not belong to this holding.");
        }

        UnitPrecision = entry.UnitPrecision;
        Units = Math.Round(Units + entry.BalanceUnits, UnitPrecision, MidpointRounding.AwayFromZero);
        LienedUnits = Math.Round(LienedUnits + entry.LienUnits, UnitPrecision, MidpointRounding.AwayFromZero);
        if (Units < 0m)
        {
            throw new InvalidOperationException("Unit holding cannot become negative.");
        }

        if (LienedUnits < 0m)
        {
            throw new InvalidOperationException("Liened units cannot become negative.");
        }

        if (LienedUnits > Units)
        {
            throw new InvalidOperationException("Liened units cannot exceed total units.");
        }

        LastMovementDate = entry.ValuationDate;
        LastTransactionReference = entry.TransactionReference;
    }
}
