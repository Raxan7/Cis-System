using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.Workflows;
using Cis.Domain.Workflows;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.Workflows;

internal sealed class ApprovalPolicyService : IApprovalPolicyService
{
    private readonly CisDbContext _dbContext;

    public ApprovalPolicyService(CisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApprovalPolicyDto> GetActivePolicyAsync(WorkflowType workflowType, CancellationToken cancellationToken = default)
    {
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
    }
}
