using Cis.Contracts;
using Cis.Contracts.Identity;

namespace Cis.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<AuthTokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthTokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);

    Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<UserDto>> GetUsersAsync(PaginationRequest pagination, CancellationToken cancellationToken = default);

    Task<UserDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AccessChangeRequestDto> RequestRoleAssignmentAsync(Guid userId, AssignRolesRequest request, CancellationToken cancellationToken = default);

    Task<AccessChangeRequestDto> RequestUserDeactivationAsync(Guid userId, DeactivateUserRequest request, CancellationToken cancellationToken = default);

    Task<AccessChangeRequestDto> CreateAccessChangeRequestAsync(CreateAccessChangeRequest request, CancellationToken cancellationToken = default);

    Task<AccessChangeRequestDto> ApproveAccessChangeRequestAsync(Guid id, AccessChangeDecisionRequest request, CancellationToken cancellationToken = default);

    Task<AccessChangeRequestDto> RejectAccessChangeRequestAsync(Guid id, AccessChangeDecisionRequest request, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(Guid userId, PasswordResetRequest request, CancellationToken cancellationToken = default);

    Task UpdateMfaAsync(Guid userId, MfaChangeRequest request, CancellationToken cancellationToken = default);
}
