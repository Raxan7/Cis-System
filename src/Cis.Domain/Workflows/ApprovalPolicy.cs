using Cis.Domain.Common;

namespace Cis.Domain.Workflows;

public sealed class ApprovalPolicy : AuditableAggregateRoot
{
    private ApprovalPolicy()
    {
    }

    private ApprovalPolicy(
        WorkflowType workflowType,
        string name,
        string module,
        bool requiresChecker,
        bool requiresApprover,
        bool isActive,
        DateTime createdAtUtc)
    {
        WorkflowType = workflowType;
        Name = Required(name, nameof(name), 200);
        Module = Required(module, nameof(module), 100);
        RequiresChecker = requiresChecker;
        RequiresApprover = requiresApprover;
        IsActive = isActive;
        MarkCreated("system", createdAtUtc);
    }

    public WorkflowType WorkflowType { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Module { get; private set; } = string.Empty;

    public bool RequiresChecker { get; private set; }

    public bool RequiresApprover { get; private set; }

    public bool IsActive { get; private set; }

    public static ApprovalPolicy Create(
        WorkflowType workflowType,
        string name,
        string module,
        bool requiresChecker,
        bool requiresApprover,
        DateTime createdAtUtc)
    {
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        if (!requiresChecker && !requiresApprover)
        {
            throw new ArgumentException("At least one checker or approver step is required.", nameof(requiresApprover));
        }

        return new ApprovalPolicy(workflowType, name, module, requiresChecker, requiresApprover, isActive: true, createdAtUtc);
    }

    public void Deactivate(DateTime deactivatedAtUtc)
    {
        EnsureUtc(deactivatedAtUtc, nameof(deactivatedAtUtc));
        IsActive = false;
    }

    private static string Required(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} is required.", parameterName);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"{parameterName} cannot exceed {maxLength} characters.", parameterName);
        }

        return trimmed;
    }

    private static void EnsureUtc(DateTime timestamp, string parameterName)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be UTC.", parameterName);
        }
    }
}
