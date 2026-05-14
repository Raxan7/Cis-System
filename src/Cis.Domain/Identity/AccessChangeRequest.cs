using Cis.Domain.Common;

namespace Cis.Domain.Identity;

public sealed class AccessChangeRequest : AuditableAggregateRoot
{
    private readonly List<AccessChangeRequestRole> _roles = [];

    private AccessChangeRequest()
    {
    }

    private AccessChangeRequest(
        string requestNumber,
        Guid targetUserId,
        AccessChangeType changeType,
        string requestedByUserId,
        DateTime requestedAtUtc,
        string reason)
    {
        if (targetUserId == Guid.Empty)
        {
            throw new ArgumentException("Target user id cannot be empty.", nameof(targetUserId));
        }

        EnsureUtc(requestedAtUtc, nameof(requestedAtUtc));

        RequestNumber = Required(requestNumber, nameof(requestNumber), 40);
        TargetUserId = targetUserId;
        ChangeType = changeType;
        RequestedByUserId = Required(requestedByUserId, nameof(requestedByUserId), 200);
        RequestedAtUtc = requestedAtUtc;
        Reason = Required(reason, nameof(reason), 1000);
        Status = AccessChangeStatus.Pending;
    }

    public string RequestNumber { get; private set; } = string.Empty;

    public Guid TargetUserId { get; private set; }

    public AccessChangeType ChangeType { get; private set; }

    public AccessChangeStatus Status { get; private set; }

    public string RequestedByUserId { get; private set; } = string.Empty;

    public DateTime RequestedAtUtc { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public string? DecisionByUserId { get; private set; }

    public DateTime? DecisionAtUtc { get; private set; }

    public string? DecisionReason { get; private set; }

    public User? TargetUser { get; private set; }

    public IReadOnlyCollection<AccessChangeRequestRole> Roles => _roles.AsReadOnly();

    public static AccessChangeRequest CreateRoleAssignment(
        string requestNumber,
        Guid targetUserId,
        IEnumerable<Guid> roleIds,
        string requestedByUserId,
        DateTime requestedAtUtc,
        string reason)
    {
        var request = new AccessChangeRequest(requestNumber, targetUserId, AccessChangeType.RoleAssignment, requestedByUserId, requestedAtUtc, reason);
        foreach (var roleId in roleIds.Distinct())
        {
            request.AddRole(roleId);
        }

        if (request._roles.Count == 0)
        {
            throw new ArgumentException("At least one role is required for role assignment requests.", nameof(roleIds));
        }

        return request;
    }

    public static AccessChangeRequest CreateUserDeactivation(
        string requestNumber,
        Guid targetUserId,
        string requestedByUserId,
        DateTime requestedAtUtc,
        string reason)
    {
        return new AccessChangeRequest(requestNumber, targetUserId, AccessChangeType.UserDeactivation, requestedByUserId, requestedAtUtc, reason);
    }

    public void Approve(string approvedByUserId, DateTime approvedAtUtc, string decisionReason)
    {
        EnsurePending();
        EnsureUtc(approvedAtUtc, nameof(approvedAtUtc));
        var approver = Required(approvedByUserId, nameof(approvedByUserId), 200);
        if (string.Equals(RequestedByUserId, approver, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The request initiator cannot approve the same controlled workflow.");
        }

        Status = AccessChangeStatus.Approved;
        DecisionByUserId = approver;
        DecisionAtUtc = approvedAtUtc;
        DecisionReason = Required(decisionReason, nameof(decisionReason), 1000);
    }

    public void Reject(string rejectedByUserId, DateTime rejectedAtUtc, string decisionReason)
    {
        EnsurePending();
        EnsureUtc(rejectedAtUtc, nameof(rejectedAtUtc));
        var rejector = Required(rejectedByUserId, nameof(rejectedByUserId), 200);
        if (string.Equals(RequestedByUserId, rejector, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The request initiator cannot reject the same controlled workflow.");
        }

        Status = AccessChangeStatus.Rejected;
        DecisionByUserId = rejector;
        DecisionAtUtc = rejectedAtUtc;
        DecisionReason = Required(decisionReason, nameof(decisionReason), 1000);
    }

    private void AddRole(Guid roleId)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("Role id cannot be empty.", nameof(roleId));
        }

        if (_roles.Any(role => role.RoleId == roleId))
        {
            return;
        }

        _roles.Add(AccessChangeRequestRole.Create(Id, roleId));
    }

    private void EnsurePending()
    {
        if (Status != AccessChangeStatus.Pending)
        {
            throw new InvalidOperationException("Only pending access change requests can be decided.");
        }
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
