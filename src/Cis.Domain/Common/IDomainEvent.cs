namespace Cis.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}
