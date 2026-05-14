using Cis.Domain.Common;

namespace Cis.Domain.UnitRegister;

public sealed class UnitRegisterSnapshot : AuditableAggregateRoot
{
    private UnitRegisterSnapshot()
    {
    }

    private UnitRegisterSnapshot(Guid schemeId, Guid schemeClassId, BusinessDate snapshotDate, string createdByUserId, DateTime createdAtUtc)
    {
        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        SnapshotDate = snapshotDate;
        MarkCreated(createdByUserId, UnitRegisterValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc)));
    }

    public Guid SchemeId { get; private set; }

    public Guid SchemeClassId { get; private set; }

    public BusinessDate SnapshotDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);

    public decimal TotalUnits { get; private set; }

    public decimal LienedUnits { get; private set; }

    public decimal RedeemableUnits { get; private set; }

    public int HoldingCount { get; private set; }

    public string? LastTransactionReference { get; private set; }

    public static UnitRegisterSnapshot Create(Guid schemeId, Guid schemeClassId, BusinessDate snapshotDate, string createdByUserId, DateTime createdAtUtc)
    {
        return new UnitRegisterSnapshot(schemeId, schemeClassId, snapshotDate, createdByUserId, createdAtUtc);
    }

    public void Refresh(decimal totalUnits, decimal lienedUnits, int holdingCount, string transactionReference)
    {
        if (totalUnits < 0m || lienedUnits < 0m || lienedUnits > totalUnits)
        {
            throw new InvalidOperationException("Unit register snapshot totals are invalid.");
        }

        TotalUnits = totalUnits;
        LienedUnits = lienedUnits;
        RedeemableUnits = totalUnits - lienedUnits;
        HoldingCount = holdingCount;
        LastTransactionReference = UnitRegisterValidation.Required(transactionReference, nameof(transactionReference), 100).ToUpperInvariant();
    }
}
