using Cis.Contracts.Workflows;

namespace Cis.Application.Common.Interfaces;

public interface IWorkflowService
{
    Task<WorkflowDto> CreateAsync(CreateWorkflowRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WorkflowDto>> GetPendingAsync(CancellationToken cancellationToken = default);

    Task<WorkflowDto> SubmitAsync(Guid id, WorkflowActionRequest request, CancellationToken cancellationToken = default);

    Task<WorkflowDto> CheckAsync(Guid id, WorkflowActionRequest request, CancellationToken cancellationToken = default);

    Task<WorkflowDto> ApproveAsync(Guid id, WorkflowActionRequest request, CancellationToken cancellationToken = default);

    Task<WorkflowDto> RejectAsync(Guid id, WorkflowActionRequest request, CancellationToken cancellationToken = default);
}
