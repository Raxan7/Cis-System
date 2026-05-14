namespace Cis.Domain.Common;

public sealed class AuditMetadata : ValueObject
{
    private AuditMetadata()
    {
    }

    private AuditMetadata(string createdBy, DateTime createdAtUtc, string? lastModifiedBy, DateTime? lastModifiedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(createdBy))
        {
            throw new ArgumentException("Created by is required.", nameof(createdBy));
        }

        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        if (lastModifiedAtUtc.HasValue)
        {
            EnsureUtc(lastModifiedAtUtc.Value, nameof(lastModifiedAtUtc));
        }

        CreatedBy = createdBy;
        CreatedAtUtc = createdAtUtc;
        LastModifiedBy = lastModifiedBy;
        LastModifiedAtUtc = lastModifiedAtUtc;
    }

    public string CreatedBy { get; private init; } = "system";

    public DateTime CreatedAtUtc { get; private init; } = DateTime.UnixEpoch;

    public string? LastModifiedBy { get; private init; }

    public DateTime? LastModifiedAtUtc { get; private init; }

    public static AuditMetadata Create(string? createdBy, DateTime createdAtUtc)
    {
        return new AuditMetadata(NormalizeActor(createdBy), createdAtUtc, null, null);
    }

    public AuditMetadata MarkModified(string? modifiedBy, DateTime modifiedAtUtc)
    {
        return new AuditMetadata(CreatedBy, CreatedAtUtc, NormalizeActor(modifiedBy), modifiedAtUtc);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CreatedBy;
        yield return CreatedAtUtc;
        yield return LastModifiedBy;
        yield return LastModifiedAtUtc;
    }

    private static string NormalizeActor(string? actor)
    {
        return string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim();
    }

    private static void EnsureUtc(DateTime timestamp, string parameterName)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }
}
