using Cis.Contracts;
using Cis.Contracts.Audit;

namespace Cis.Application.Common.Interfaces;

public interface IAuditQueryService
{
    Task<PagedResult<AuditLogDto>> GetAsync(PaginationRequest pagination, CancellationToken cancellationToken = default);

    Task<PagedResult<AuditLogDto>> GetForEntityAsync(string entityType, string entityId, PaginationRequest pagination, CancellationToken cancellationToken = default);
}
