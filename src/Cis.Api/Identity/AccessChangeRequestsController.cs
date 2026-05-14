using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.Identity;

[ApiController]
[Route("api/access-change-requests")]
public sealed class AccessChangeRequestsController : ControllerBase
{
    private readonly IIdentityService _identityService;

    public AccessChangeRequestsController(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    [HttpPost]
    [RequirePermission(Permissions.Identity.AccessChangeRequest)]
    [ProducesResponseType(typeof(ApiResponse<AccessChangeRequestDto>), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<ApiResponse<AccessChangeRequestDto>>> Create(
        [FromBody] CreateAccessChangeRequest request,
        CancellationToken cancellationToken)
    {
        var accessChangeRequest = await _identityService.CreateAccessChangeRequestAsync(request, cancellationToken);
        return Accepted(ApiResponse<AccessChangeRequestDto>.Success(accessChangeRequest, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/approve")]
    [RequirePermission(Permissions.Identity.AccessChangeApprove)]
    [ProducesResponseType(typeof(ApiResponse<AccessChangeRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AccessChangeRequestDto>>> Approve(
        Guid id,
        [FromBody] AccessChangeDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var accessChangeRequest = await _identityService.ApproveAccessChangeRequestAsync(id, request, cancellationToken);
        return Ok(ApiResponse<AccessChangeRequestDto>.Success(accessChangeRequest, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/reject")]
    [RequirePermission(Permissions.Identity.AccessChangeApprove)]
    [ProducesResponseType(typeof(ApiResponse<AccessChangeRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AccessChangeRequestDto>>> Reject(
        Guid id,
        [FromBody] AccessChangeDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var accessChangeRequest = await _identityService.RejectAccessChangeRequestAsync(id, request, cancellationToken);
        return Ok(ApiResponse<AccessChangeRequestDto>.Success(accessChangeRequest, HttpContext.TraceIdentifier));
    }
}
