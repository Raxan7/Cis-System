using Cis.Contracts.Workflows;
using Cis.Domain.Workflows;

namespace Cis.Application.Common.Interfaces;

public interface IApprovalPolicyService
{
    Task<ApprovalPolicyDto> GetActivePolicyAsync(WorkflowType workflowType, CancellationToken cancellationToken = default);
}
