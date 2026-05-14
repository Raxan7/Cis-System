using Cis.Domain.Common;

namespace Cis.Domain.UnitRegister;

public sealed class HistoricalHoldingView : AuditableAggregateRoot
{
    private HistoricalHoldingView()
    {
    }

    private HistoricalHoldingView(
        Guid investorId,
        Guid schemeId,
        Guid schemeClassId,
        BusinessDate valuationDate,
        string transactionReference,
        decimal units,
        decimal lienedUnits,
        int unitPrecision,
        Guid unitLedgerEntryId,
        string recordedByUserId,
        DateTime recordedAtUtc)
    {
        InvestorId = investorId;
        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        ValuationDate = valuationDate;
        TransactionReference = UnitRegisterValidation.Required(transactionReference, nameof(transactionReference), 100).ToUpperInvariant();
        UnitPrecision = UnitRegisterValidation.Precision(unitPrecision, nameof(unitPrecision));
        Units = Math.Round(units, UnitPrecision, MidpointRounding.AwayFromZero);
        LienedUnits = Math.Round(lienedUnits, UnitPrecision, MidpointRounding.AwayFromZero);
        RedeemableUnits = Units - LienedUnits;
        UnitLedgerEntryId = unitLedgerEntryId;
        RecordedAtUtc = UnitRegisterValidation.EnsureUtc(recordedAtUtc, nameof(recordedAtUtc));
        MarkCreated(recordedByUserId, RecordedAtUtc);
    }

    public Guid InvestorId { get; private set; }

    public Guid SchemeId { get; private set; }

    public Guid SchemeClassId { get; private set; }

    public BusinessDate ValuationDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);

    public string TransactionReference { get; private set; } = string.Empty;

    public decimal Units { get; private set; }

    public decimal LienedUnits { get; private set; }

    public decimal RedeemableUnits { get; private set; }

    public int UnitPrecision { get; private set; }

    public Guid UnitLedgerEntryId { get; private set; }

    public DateTime RecordedAtUtc { get; private set; }

    public static HistoricalHoldingView Create(
        Guid investorId,
        Guid schemeId,
        Guid schemeClassId,
        BusinessDate valuationDate,
        string transactionReference,
        decimal units,
        decimal lienedUnits,
        int unitPrecision,
        Guid unitLedgerEntryId,
        string recordedByUserId,
        DateTime recordedAtUtc)
    {
        return new HistoricalHoldingView(investorId, schemeId, schemeClassId, valuationDate, transactionReference, units, lienedUnits, unitPrecision, unitLedgerEntryId, recordedByUserId, recordedAtUtc);
    }
}
