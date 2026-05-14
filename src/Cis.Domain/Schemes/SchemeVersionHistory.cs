using Cis.Domain.Common;

namespace Cis.Domain.Schemes;

public sealed class SchemeVersionHistory : Entity
{
    private SchemeVersionHistory()
    {
    }

    private SchemeVersionHistory(
        Guid schemeId,
        int versionNumber,
        string changeType,
        string changedByUserId,
        DateTime changedAtUtc,
        BusinessDate? effectiveDate,
        string? beforeJson,
        string afterJson)
    {
        SchemeId = schemeId == Guid.Empty ? throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId)) : schemeId;
        VersionNumber = versionNumber <= 0 ? throw new ArgumentException("Version number must be positive.", nameof(versionNumber)) : versionNumber;
        ChangeType = SchemeValidation.Required(changeType, nameof(changeType), 100);
        ChangedByUserId = SchemeValidation.Required(changedByUserId, nameof(changedByUserId), 200);
        SchemeValidation.EnsureUtc(changedAtUtc, nameof(changedAtUtc));
        ChangedAtUtc = changedAtUtc;
        EffectiveDate = effectiveDate;
        BeforeJson = beforeJson;
        AfterJson = SchemeValidation.Required(afterJson, nameof(afterJson), 4000);
    }

    public Guid SchemeId { get; private set; }

    public int VersionNumber { get; private set; }

    public string ChangeType { get; private set; } = string.Empty;

    public string ChangedByUserId { get; private set; } = string.Empty;

    public DateTime ChangedAtUtc { get; private set; }

    public BusinessDate? EffectiveDate { get; private set; }

    public string? BeforeJson { get; private set; }

    public string AfterJson { get; private set; } = string.Empty;

    public Scheme? Scheme { get; private set; }

    public static SchemeVersionHistory Create(
        Guid schemeId,
        int versionNumber,
        string changeType,
        string changedByUserId,
        DateTime changedAtUtc,
        BusinessDate? effectiveDate,
        string? beforeJson,
        string afterJson)
    {
        return new SchemeVersionHistory(schemeId, versionNumber, changeType, changedByUserId, changedAtUtc, effectiveDate, beforeJson, afterJson);
    }
}
