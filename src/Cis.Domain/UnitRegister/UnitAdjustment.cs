using Cis.Domain.Common;

namespace Cis.Domain.UnitRegister;

public sealed class UnitAdjustment : AuditableAggregateRoot
{
    private UnitAdjustment()
    {
    }

    private UnitAdjustment(
        Guid investorId,
        Guid schemeId,
        Guid schemeClassId,
        decimal units,
        BusinessDate valuationDate,
        string transactionReference,
        int unitPrecision,
        string reason,
        string? idempotencyKey,
        string requestedByUserId,
        DateTime requestedAtUtc)
    {
        UnitPrecision = UnitRegisterValidation.Precision(unitPrecision, nameof(unitPrecision));
        InvestorId = investorId;
        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        Units = UnitRegisterValidation.Units(units, UnitPrecision, nameof(units));
        ValuationDate = valuationDate;
        TransactionReference = UnitRegisterValidation.Required(transactionReference, nameof(transactionReference), 100).ToUpperInvariant();
        Reason = UnitRegisterValidation.Required(reason, nameof(reason), 1000);
        IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey)
            ? null
            : UnitRegisterValidation.Required(idempotencyKey, nameof(idempotencyKey), 200);
        RequestedByUserId = UnitRegisterValidation.Required(requestedByUserId, nameof(requestedByUserId), 200);
        RequestedAtUtc = UnitRegisterValidation.EnsureUtc(requestedAtUtc, nameof(requestedAtUtc));
        Status = UnitAdjustmentStatus.PendingApproval;
        MarkCreated(RequestedByUserId, RequestedAtUtc);
    }

    public Guid InvestorId { get; private set; }

    public Guid SchemeId { get; private set; }

    public Guid SchemeClassId { get; private set; }

    public decimal Units { get; private set; }

    public BusinessDate ValuationDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);

    public string TransactionReference { get; private set; } = string.Empty;

    public int UnitPrecision { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public string? IdempotencyKey { get; private set; }

    public UnitAdjustmentStatus Status { get; private set; }

    public string RequestedByUserId { get; private set; } = string.Empty;

    public DateTime RequestedAtUtc { get; private set; }

    public string? ApprovedByUserId { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    public string? DecisionComment { get; private set; }

    public static UnitAdjustment Create(
        Guid investorId,
        Guid schemeId,
        Guid schemeClassId,
        decimal units,
        BusinessDate valuationDate,
        string transactionReference,
        int unitPrecision,
        string reason,
        string? idempotencyKey,
        string requestedByUserId,
        DateTime requestedAtUtc)
    {
        return new UnitAdjustment(investorId, schemeId, schemeClassId, units, valuationDate, transactionReference, unitPrecision, reason, idempotencyKey, requestedByUserId, requestedAtUtc);
    }

    public void Approve(string approvedByUserId, DateTime approvedAtUtc, string? comment)
    {
        if (Status != UnitAdjustmentStatus.PendingApproval)
        {
            throw new InvalidOperationException("Only pending unit adjustments can be approved.");
        }

        var actor = UnitRegisterValidation.Required(approvedByUserId, nameof(approvedByUserId), 200);
        if (string.Equals(actor, RequestedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Segregation of duties violation: requester cannot approve the same unit adjustment.");
        }

        ApprovedByUserId = actor;
        ApprovedAtUtc = UnitRegisterValidation.EnsureUtc(approvedAtUtc, nameof(approvedAtUtc));
        DecisionComment = string.IsNullOrWhiteSpace(comment) ? null : UnitRegisterValidation.Required(comment, nameof(comment), 1000);
        Status = UnitAdjustmentStatus.Approved;
    }
}
