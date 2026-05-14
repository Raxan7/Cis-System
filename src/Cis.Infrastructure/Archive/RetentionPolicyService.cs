using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.Archive;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.Archive;

internal sealed class RetentionPolicyService : IRetentionPolicyService
{
    private readonly CisDbContext _dbContext;

    public RetentionPolicyService(CisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RetentionPolicyDto> GetDefaultAsync(string module, CancellationToken cancellationToken = default)
    {
        var policy = await _dbContext.RetentionPolicies
            .AsNoTracking()
            .Where(candidate => candidate.IsActive && candidate.Module == module)
            .OrderByDescending(candidate => candidate.RetentionDays)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Retention policy was not found.");

        return new RetentionPolicyDto(
            policy.Id,
            policy.Name,
            policy.Module,
            policy.RetentionDays,
            policy.LegalHoldEnabled,
            policy.IsActive);
    }
}
