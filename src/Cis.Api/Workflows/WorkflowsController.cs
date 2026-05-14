using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Workflows;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.Workflows;

[ApiController]
[Route("api/workflows")]
public sealed class WorkflowsController : ControllerBase
{
    private readonly IWorkflowService _workflowService;

    public WorkflowsController(IWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    [HttpGet("pending")]
    [RequirePermission(Permissions.Workflow.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<WorkflowDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<WorkflowDto>>>> GetPending(CancellationToken cancellationToken)
    {
        var workflows = await _workflowService.GetPendingAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<WorkflowDto>>.Success(workflows, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/submit")]
    [RequirePermission(Permissions.Workflow.Submit)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<WorkflowDto>>> Submit(
        Guid id,
        [FromBody] WorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        var workflow = await _workflowService.SubmitAsync(id, request, cancellationToken);
        return Ok(ApiResponse<WorkflowDto>.Success(workflow, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/check")]
    [RequirePermission(Permissions.Workflow.Check)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<WorkflowDto>>> Check(
        Guid id,
        [FromBody] WorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        var workflow = await _workflowService.CheckAsync(id, request, cancellationToken);
        return Ok(ApiResponse<WorkflowDto>.Success(workflow, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/approve")]
    [RequirePermission(Permissions.Workflow.Approve)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<WorkflowDto>>> Approve(
        Guid id,
        [FromBody] WorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        var workflow = await _workflowService.ApproveAsync(id, request, cancellationToken);
        return Ok(ApiResponse<WorkflowDto>.Success(workflow, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/reject")]
    [RequirePermission(Permissions.Workflow.Reject)]
    [ProducesResponseType(typeof(ApiResponse<WorkflowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<WorkflowDto>>> Reject(
        Guid id,
        [FromBody] WorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        var workflow = await _workflowService.RejectAsync(id, request, cancellationToken);
        return Ok(ApiResponse<WorkflowDto>.Success(workflow, HttpContext.TraceIdentifier));
    }
}
