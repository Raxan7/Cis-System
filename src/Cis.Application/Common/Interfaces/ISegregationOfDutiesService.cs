namespace Cis.Application.Common.Interfaces;

public interface ISegregationOfDutiesService
{
    void EnsureCanDecide(string workflowName, string initiatedByUserId, string decisionByUserId);
}
