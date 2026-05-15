using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.Workflows;
using Cis.Domain.Workflows;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Cis.Infrastructure.Workflows;

internal sealed class ApprovalPolicyService : IApprovalPolicyService
{
    private readonly CisDbContext _dbContext;
    private readonly IMemoryCache _memoryCache;

    public ApprovalPolicyService(CisDbContext dbContext, IMemoryCache memoryCache)
    {
        _dbContext = dbContext;
        _memoryCache = memoryCache;
    }

    public async Task<ApprovalPolicyDto> GetActivePolicyAsync(WorkflowType workflowType, CancellationToken cancellationToken = default)
    {
        return await _memoryCache.GetOrCreateAsync($"approval-policy:{workflowType}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            entry.SlidingExpiration = TimeSpan.FromMinutes(5);

            var policy = await _dbContext.ApprovalPolicies
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.WorkflowType == workflowType && candidate.IsActive, cancellationToken)
                ?? throw new NotFoundException("Approval policy was not found.");

            return new ApprovalPolicyDto(
                policy.Id,
                policy.WorkflowType.ToString(),
                policy.Name,
                policy.Module,
                policy.RequiresChecker,
                policy.RequiresApprover,
                policy.IsActive);
        }) ?? throw new NotFoundException("Approval policy was not found.");
    }
}
