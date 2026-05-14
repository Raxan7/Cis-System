using Cis.Domain.Common;

namespace Cis.Domain.UnitRegister;

public sealed class UnitMovementSource : AuditableAggregateRoot
{
    private UnitMovementSource()
    {
    }

    private UnitMovementSource(
        UnitMovementSourceType sourceType,
        Guid sourceId,
        Guid investorId,
        Guid schemeId,
        Guid schemeClassId,
        string reference,
        bool isApproved,
        string verifiedByUserId,
        DateTime verifiedAtUtc)
    {
        if (sourceId == Guid.Empty)
        {
            throw new ArgumentException("Source id is required.", nameof(sourceId));
        }

        SourceType = sourceType;
        SourceId = sourceId;
        InvestorId = investorId;
        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        Reference = UnitRegisterValidation.Required(reference, nameof(reference), 100).ToUpperInvariant();
        IsApproved = isApproved;
        VerifiedByUserId = UnitRegisterValidation.Required(verifiedByUserId, nameof(verifiedByUserId), 200);
        VerifiedAtUtc = UnitRegisterValidation.EnsureUtc(verifiedAtUtc, nameof(verifiedAtUtc));
        MarkCreated(VerifiedByUserId, VerifiedAtUtc);
    }

    public UnitMovementSourceType SourceType { get; private set; }

    public Guid SourceId { get; private set; }

    public Guid InvestorId { get; private set; }

    public Guid SchemeId { get; private set; }

    public Guid SchemeClassId { get; private set; }

    public string Reference { get; private set; } = string.Empty;

    public bool IsApproved { get; private set; }

    public string VerifiedByUserId { get; private set; } = string.Empty;

    public DateTime VerifiedAtUtc { get; private set; }

    public static UnitMovementSource Create(
        UnitMovementSourceType sourceType,
        Guid sourceId,
        Guid investorId,
        Guid schemeId,
        Guid schemeClassId,
        string reference,
        bool isApproved,
        string verifiedByUserId,
        DateTime verifiedAtUtc)
    {
        return new UnitMovementSource(sourceType, sourceId, investorId, schemeId, schemeClassId, reference, isApproved, verifiedByUserId, verifiedAtUtc);
    }
}
