using Cis.Contracts.Identity;
using Cis.Domain.Identity;

namespace Cis.Application.Common.Interfaces;

public interface IJwtTokenService
{
    Task<AuthTokenResponse> IssueTokensAsync(User user, CancellationToken cancellationToken = default);

    string HashRefreshToken(string refreshToken);
}
