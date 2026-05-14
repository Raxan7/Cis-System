using Cis.Domain.Common;

namespace Cis.Domain.UnitRegister;

public sealed class UnitLedgerEntry : AuditableAggregateRoot
{
    private UnitLedgerEntry()
    {
    }

    private UnitLedgerEntry(
        UnitMovementType movementType,
        Guid investorId,
        Guid schemeId,
        Guid schemeClassId,
        BusinessDate valuationDate,
        decimal units,
        UnitMovementSource source,
        string transactionReference,
        int unitPrecision,
        string postedByUserId,
        DateTime postedAtUtc,
        string? narrative,
        Guid? adjustmentId)
    {
        ArgumentNullException.ThrowIfNull(valuationDate);
        ArgumentNullException.ThrowIfNull(source);
        if (!source.IsApproved)
        {
            throw new InvalidOperationException("Unit movement source must be approved before a ledger entry can be posted.");
        }

        if (source.InvestorId != investorId || source.SchemeId != schemeId || source.SchemeClassId != schemeClassId)
        {
            throw new InvalidOperationException("Unit movement source does not match the ledger entry investor, scheme, and class.");
        }

        UnitPrecision = UnitRegisterValidation.Precision(unitPrecision, nameof(unitPrecision));
        MovementType = movementType;
        InvestorId = investorId;
        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        ValuationDate = valuationDate;
        Units = UnitRegisterValidation.Units(units, UnitPrecision, nameof(units), allowNegative: MovementAllowsNegativeUnits(movementType));
        SourceId = source.Id;
        SourceType = source.SourceType;
        SourceEntityId = source.SourceId;
        TransactionReference = UnitRegisterValidation.Required(transactionReference, nameof(transactionReference), 100).ToUpperInvariant();
        PostedByUserId = UnitRegisterValidation.Required(postedByUserId, nameof(postedByUserId), 200);
        PostedAtUtc = UnitRegisterValidation.EnsureUtc(postedAtUtc, nameof(postedAtUtc));
        Narrative = string.IsNullOrWhiteSpace(narrative) ? null : UnitRegisterValidation.Required(narrative, nameof(narrative), 1000);
        AdjustmentId = adjustmentId;
        MarkCreated(PostedByUserId, PostedAtUtc);
    }

    public UnitMovementType MovementType { get; private set; }

    public Guid InvestorId { get; private set; }

    public Guid SchemeId { get; private set; }

    public Guid SchemeClassId { get; private set; }

    public BusinessDate ValuationDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);

    public decimal Units { get; private set; }

    public Guid SourceId { get; private set; }

    public UnitMovementSourceType SourceType { get; private set; }

    public Guid SourceEntityId { get; private set; }

    public Guid? AdjustmentId { get; private set; }

    public string TransactionReference { get; private set; } = string.Empty;

    public int UnitPrecision { get; private set; }

    public string PostedByUserId { get; private set; } = string.Empty;

    public DateTime PostedAtUtc { get; private set; }

    public string? Narrative { get; private set; }

    public decimal BalanceUnits => MovementType is UnitMovementType.Liened or UnitMovementType.LienReleased ? 0m : Units;

    public decimal LienUnits => MovementType switch
    {
        UnitMovementType.Liened => Units,
        UnitMovementType.LienReleased => -Units,
        _ => 0m
    };

    public static UnitLedgerEntry Create(
        UnitMovementType movementType,
        Guid investorId,
        Guid schemeId,
        Guid schemeClassId,
        BusinessDate valuationDate,
        decimal units,
        UnitMovementSource source,
        string transactionReference,
        int unitPrecision,
        string postedByUserId,
        DateTime postedAtUtc,
        string? narrative = null,
        Guid? adjustmentId = null)
    {
        return new UnitLedgerEntry(movementType, investorId, schemeId, schemeClassId, valuationDate, units, source, transactionReference, unitPrecision, postedByUserId, postedAtUtc, narrative, adjustmentId);
    }

    private static bool MovementAllowsNegativeUnits(UnitMovementType movementType)
    {
        return movementType is UnitMovementType.Redeemed
            or UnitMovementType.SwitchedOut
            or UnitMovementType.TransferredOut
            or UnitMovementType.Adjustment
            or UnitMovementType.Cancelled;
    }
}
