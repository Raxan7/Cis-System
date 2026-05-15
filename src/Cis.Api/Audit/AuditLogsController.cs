using Cis.Api.Common;
using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Audit;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.Audit;

[ApiController]
[Route("api/audit-logs")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditQueryService _auditQueryService;

    public AuditLogsController(IAuditQueryService auditQueryService)
    {
        _auditQueryService = auditQueryService;
    }

    [HttpGet]
    [RequirePermission(Permissions.Audit.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<AuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AuditLogDto>>>> Get([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var auditLogs = await _auditQueryService.GetAsync(pagination, cancellationToken);
        return this.OkPaged(auditLogs);
    }

    [HttpGet("entity/{entityType}/{entityId}")]
    [RequirePermission(Permissions.Audit.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<AuditLogDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AuditLogDto>>>> GetForEntity(
        string entityType,
        string entityId,
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        var auditLogs = await _auditQueryService.GetForEntityAsync(entityType, entityId, pagination, cancellationToken);
        return this.OkPaged(auditLogs);
    }
}
