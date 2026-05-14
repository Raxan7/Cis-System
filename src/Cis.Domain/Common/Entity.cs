namespace Cis.Domain.Common;

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity()
    {
    }

    public Guid Id { get; protected set; } = Guid.NewGuid();

    public RowVersion RowVersion { get; protected set; } = RowVersion.New();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void RefreshRowVersion()
    {
        RowVersion = RowVersion.New();
    }

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
