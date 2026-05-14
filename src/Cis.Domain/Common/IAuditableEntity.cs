namespace Cis.Domain.Common;

public interface IAuditableEntity
{
    AuditMetadata Audit { get; }

    void MarkCreated(string? createdBy, DateTime createdAtUtc);

    void MarkModified(string? modifiedBy, DateTime modifiedAtUtc);
}
