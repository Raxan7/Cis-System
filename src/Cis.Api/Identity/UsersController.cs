using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.Identity;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IIdentityService _identityService;

    public UsersController(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    [HttpPost]
    [RequirePermission(Permissions.Identity.UsersCreate)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _identityService.CreateUserAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, ApiResponse<UserDto>.Success(user, HttpContext.TraceIdentifier));
    }

    [HttpGet]
    [RequirePermission(Permissions.Identity.UsersRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<UserDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<UserDto>>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _identityService.GetUsersAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<UserDto>>.Success(users, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.Identity.UsersRead)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _identityService.GetUserByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<UserDto>.Success(user, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/roles")]
    [RequirePermission(Permissions.Identity.AccessChangeRequest)]
    [ProducesResponseType(typeof(ApiResponse<AccessChangeRequestDto>), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<ApiResponse<AccessChangeRequestDto>>> RequestRoleAssignment(
        Guid id,
        [FromBody] AssignRolesRequest request,
        CancellationToken cancellationToken)
    {
        var accessChangeRequest = await _identityService.RequestRoleAssignmentAsync(id, request, cancellationToken);
        return Accepted(ApiResponse<AccessChangeRequestDto>.Success(accessChangeRequest, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/deactivate")]
    [RequirePermission(Permissions.Identity.UsersDeactivateRequest)]
    [ProducesResponseType(typeof(ApiResponse<AccessChangeRequestDto>), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<ApiResponse<AccessChangeRequestDto>>> RequestDeactivation(
        Guid id,
        [FromBody] DeactivateUserRequest request,
        CancellationToken cancellationToken)
    {
        var accessChangeRequest = await _identityService.RequestUserDeactivationAsync(id, request, cancellationToken);
        return Accepted(ApiResponse<AccessChangeRequestDto>.Success(accessChangeRequest, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/password-reset")]
    [RequirePermission(Permissions.Identity.UsersPasswordReset)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] PasswordResetRequest request, CancellationToken cancellationToken)
    {
        await _identityService.ResetPasswordAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/mfa")]
    [RequirePermission(Permissions.Identity.UsersMfaManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateMfa(Guid id, [FromBody] MfaChangeRequest request, CancellationToken cancellationToken)
    {
        await _identityService.UpdateMfaAsync(id, request, cancellationToken);
        return NoContent();
    }
}
