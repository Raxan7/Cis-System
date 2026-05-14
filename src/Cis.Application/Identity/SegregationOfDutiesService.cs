using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;

namespace Cis.Application.Identity;

internal sealed class SegregationOfDutiesService : ISegregationOfDutiesService
{
    public void EnsureCanDecide(string workflowName, string initiatedByUserId, string decisionByUserId)
    {
        if (string.Equals(initiatedByUserId, decisionByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["approver"] =
                [
                    $"Segregation of duties violation: the initiator cannot decide the same {workflowName} workflow."
                ]
            });
        }
    }
}
