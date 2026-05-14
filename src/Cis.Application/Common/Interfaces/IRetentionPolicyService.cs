using Cis.Contracts.Archive;

namespace Cis.Application.Common.Interfaces;

public interface IRetentionPolicyService
{
    Task<RetentionPolicyDto> GetDefaultAsync(string module, CancellationToken cancellationToken = default);
}
