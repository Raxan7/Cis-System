using Cis.Domain.Workflows;
using FluentAssertions;

namespace Cis.Tests.Unit.Workflow;

public sealed class WorkflowInstanceTests
{
    [Fact]
    public void Check_WhenActorSubmittedWorkflow_ThrowsSegregationException()
    {
        var policyId = Guid.NewGuid();
        var workflow = WorkflowInstance.Create(
            WorkflowType.NavPreparationApprovalPublication,
            policyId,
            "NavRun",
            "nav-001",
            "NAV approval",
            "maker-001",
            new DateTime(2026, 5, 11, 10, 0, 0, DateTimeKind.Utc),
            requiresChecker: true,
            requiresApprover: true);
        workflow.Submit("maker-001", new DateTime(2026, 5, 11, 11, 0, 0, DateTimeKind.Utc), "Submitted.");

        var action = () => workflow.Check("maker-001", new DateTime(2026, 5, 11, 12, 0, 0, DateTimeKind.Utc), "Checked.");

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*same user*");
    }

    [Fact]
    public void Approve_WhenDifferentUsersCompletedPriorSteps_FinalizesWorkflow()
    {
        var policyId = Guid.NewGuid();
        var workflow = WorkflowInstance.Create(
            WorkflowType.NavPreparationApprovalPublication,
            policyId,
            "NavRun",
            "nav-002",
            "NAV approval",
            "maker-001",
            new DateTime(2026, 5, 11, 10, 0, 0, DateTimeKind.Utc),
            requiresChecker: true,
            requiresApprover: true);

        workflow.Submit("maker-001", new DateTime(2026, 5, 11, 11, 0, 0, DateTimeKind.Utc), "Submitted.");
        workflow.Check("checker-001", new DateTime(2026, 5, 11, 12, 0, 0, DateTimeKind.Utc), "Checked.");
        workflow.Approve("approver-001", new DateTime(2026, 5, 11, 13, 0, 0, DateTimeKind.Utc), "Approved.");

        workflow.Status.Should().Be(WorkflowStatus.Approved);
        workflow.Actions.Select(action => action.ActionType).Should().Contain(WorkflowActionType.Approved);
    }
}
