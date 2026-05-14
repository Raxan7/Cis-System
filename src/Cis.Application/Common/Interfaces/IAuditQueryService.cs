using Cis.Contracts.Audit;

namespace Cis.Application.Common.Interfaces;

public interface IAuditQueryService
{
    Task<IReadOnlyCollection<AuditLogDto>> GetAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AuditLogDto>> GetForEntityAsync(string entityType, string entityId, CancellationToken cancellationToken = default);
}
