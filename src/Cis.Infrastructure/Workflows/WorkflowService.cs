using System.Text.Json;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.Workflows;
using Cis.Domain.Audit;
using Cis.Domain.Workflows;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.Workflows;

internal sealed class WorkflowService : IWorkflowService
{
    private const string ModuleName = "Workflow";

    private readonly CisDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public WorkflowService(
        CisDbContext dbContext,
        IAuditWriter auditWriter,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<WorkflowDto> CreateAsync(CreateWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        if (!Enum.TryParse<WorkflowType>(request.WorkflowType, ignoreCase: true, out var workflowType))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["workflowType"] = ["Unsupported workflow type."]
            });
        }

        var policy = await GetPolicyAsync(workflowType, cancellationToken);
        var now = _dateTimeProvider.UtcNow;
        var workflow = WorkflowInstance.Create(
            workflowType,
            policy.Id,
            request.EntityType,
            request.EntityId,
            request.Title,
            actor,
            now,
            policy.RequiresChecker,
            policy.RequiresApprover);

        _dbContext.WorkflowInstances.Add(workflow);
        await _dbContext.SaveChangesAsync(cancellationToken);
        var dto = await MapWorkflowAsync(workflow.Id, cancellationToken);
        await WriteWorkflowAuditAsync(AuditEventType.Created, "WorkflowCreated", dto, request.Reason ?? "Workflow created.", cancellationToken);

        return dto;
    }

    public async Task<IReadOnlyCollection<WorkflowDto>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var ids = await _dbContext.WorkflowInstances
            .AsNoTracking()
            .Where(workflow => workflow.Status == WorkflowStatus.Draft
                || workflow.Status == WorkflowStatus.Submitted
                || workflow.Status == WorkflowStatus.Checked)
            .OrderBy(workflow => workflow.Audit.CreatedAtUtc)
            .Select(workflow => workflow.Id)
            .ToListAsync(cancellationToken);

        var workflows = new List<WorkflowDto>();
        foreach (var id in ids)
        {
            workflows.Add(await MapWorkflowAsync(id, cancellationToken));
        }

        return workflows;
    }

    public Task<WorkflowDto> SubmitAsync(Guid id, WorkflowActionRequest request, CancellationToken cancellationToken = default)
    {
        return ActAsync(id, request, AuditEventType.Submitted, "WorkflowSubmitted", (workflow, actor, now) => workflow.Submit(actor, now, request.Comment), cancellationToken);
    }

    public Task<WorkflowDto> CheckAsync(Guid id, WorkflowActionRequest request, CancellationToken cancellationToken = default)
    {
        return ActAsync(id, request, AuditEventType.Checked, "WorkflowChecked", (workflow, actor, now) => workflow.Check(actor, now, request.Comment), cancellationToken);
    }

    public Task<WorkflowDto> ApproveAsync(Guid id, WorkflowActionRequest request, CancellationToken cancellationToken = default)
    {
        return ActAsync(id, request, AuditEventType.Approved, "WorkflowApproved", (workflow, actor, now) => workflow.Approve(actor, now, request.Comment), cancellationToken);
    }

    public async Task<WorkflowDto> RejectAsync(Guid id, WorkflowActionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Comment))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["comment"] = ["A rejection comment is required."]
            });
        }

        return await ActAsync(id, request, AuditEventType.Rejected, "WorkflowRejected", (workflow, actor, now) => workflow.Reject(actor, now, request.Comment), cancellationToken);
    }

    private async Task<WorkflowDto> ActAsync(
        Guid id,
        WorkflowActionRequest request,
        AuditEventType eventType,
        string action,
        Action<WorkflowInstance, string, DateTime> transition,
        CancellationToken cancellationToken)
    {
        var actor = CurrentUserIdOrThrow();
        var workflow = await GetWorkflowAggregateAsync(id, cancellationToken);
        try
        {
            transition(workflow, actor, _dateTimeProvider.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["workflow"] = [exception.Message]
            });
        }

        MarkNewWorkflowActionAsAdded(id, eventType);
        await _dbContext.SaveChangesAsync(cancellationToken);
        var dto = await MapWorkflowAsync(id, cancellationToken);
        await WriteWorkflowAuditAsync(eventType, action, dto, request.Comment, cancellationToken);

        return dto;
    }

    private async Task<ApprovalPolicy> GetPolicyAsync(WorkflowType workflowType, CancellationToken cancellationToken)
    {
        return await _dbContext.ApprovalPolicies
            .FirstOrDefaultAsync(policy => policy.WorkflowType == workflowType && policy.IsActive, cancellationToken)
            ?? throw new NotFoundException("Approval policy was not found.");
    }

    private async Task<WorkflowInstance> GetWorkflowAggregateAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.WorkflowInstances
            .Include(workflow => workflow.Steps)
            .FirstOrDefaultAsync(workflow => workflow.Id == id, cancellationToken)
            ?? throw new NotFoundException("Workflow was not found.");
    }

    private void MarkNewWorkflowActionAsAdded(Guid workflowId, AuditEventType eventType)
    {
        var actionType = eventType switch
        {
            AuditEventType.Submitted => WorkflowActionType.Submitted,
            AuditEventType.Checked => WorkflowActionType.Checked,
            AuditEventType.Approved => WorkflowActionType.Approved,
            AuditEventType.Rejected => WorkflowActionType.Rejected,
            _ => WorkflowActionType.Created
        };

        foreach (var entry in _dbContext.ChangeTracker.Entries<WorkflowAction>()
                     .Where(entry => entry.Entity.WorkflowInstanceId == workflowId
                         && entry.Entity.ActionType == actionType
                         && entry.State == EntityState.Modified))
        {
            entry.State = EntityState.Added;
        }
    }

    private async Task<WorkflowDto> MapWorkflowAsync(Guid id, CancellationToken cancellationToken)
    {
        var workflow = await _dbContext.WorkflowInstances
            .AsNoTracking()
            .Include(candidate => candidate.Steps)
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException("Workflow was not found.");

        return new WorkflowDto(
            workflow.Id,
            workflow.WorkflowType.ToString(),
            workflow.EntityType,
            workflow.EntityId,
            workflow.Title,
            workflow.Status.ToString(),
            workflow.InitiatedByUserId,
            workflow.SubmittedByUserId,
            workflow.SubmittedAtUtc,
            workflow.CheckedByUserId,
            workflow.CheckedAtUtc,
            workflow.ApprovedByUserId,
            workflow.ApprovedAtUtc,
            workflow.RejectedByUserId,
            workflow.RejectedAtUtc,
            workflow.DecisionComment,
            workflow.Steps
                .OrderBy(step => step.StepOrder)
                .Select(step => new WorkflowStepDto(
                    step.StepType.ToString(),
                    step.StepOrder,
                    step.Status.ToString(),
                    step.CompletedByUserId,
                    step.CompletedAtUtc))
                .ToArray());
    }

    private Task WriteWorkflowAuditAsync(
        AuditEventType eventType,
        string action,
        WorkflowDto workflow,
        string? reason,
        CancellationToken cancellationToken)
    {
        return _auditWriter.WriteAsync(new AuditLogEntry(
            ModuleName,
            action,
            "WorkflowInstance",
            workflow.Id.ToString(),
            EventType: eventType,
            Summary: $"{workflow.WorkflowType} workflow {workflow.Status}.",
            AfterJson: JsonSerializer.Serialize(workflow),
            Reason: reason,
            WorkflowId: workflow.Id), cancellationToken);
    }

    private string CurrentUserIdOrThrow()
    {
        return _currentUserContext.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user id is required.");
    }
}
