namespace Cis.Domain.Common;

public abstract class AuditableAggregateRoot : AggregateRoot, IAuditableEntity
{
    protected AuditableAggregateRoot()
    {
    }

    public AuditMetadata Audit { get; private set; } = AuditMetadata.Create("system", DateTime.UnixEpoch);

    public void MarkCreated(string? createdBy, DateTime createdAtUtc)
    {
        Audit = AuditMetadata.Create(createdBy, createdAtUtc);
    }

    public void MarkModified(string? modifiedBy, DateTime modifiedAtUtc)
    {
        Audit = Audit.MarkModified(modifiedBy, modifiedAtUtc);
    }
}
