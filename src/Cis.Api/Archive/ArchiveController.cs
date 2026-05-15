using Cis.Api.Security;
using Cis.Api.Common;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Archive;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.Archive;

[ApiController]
[Route("api/archive")]
public sealed class ArchiveController : ControllerBase
{
    private readonly IImmutableArchiveService _archiveService;

    public ArchiveController(IImmutableArchiveService archiveService)
    {
        _archiveService = archiveService;
    }

    [HttpGet]
    [RequirePermission(Permissions.Archive.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<ArchiveRecordDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ArchiveRecordDto>>>> Get(
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        var records = await _archiveService.GetAsync(cancellationToken);
        return this.OkPaged(records, pagination);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.Archive.Read)]
    [ProducesResponseType(typeof(ApiResponse<ArchiveRecordDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ArchiveRecordDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var record = await _archiveService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<ArchiveRecordDto>.Success(record, HttpContext.TraceIdentifier));
    }
}
