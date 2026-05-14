using Cis.Domain.Common;

namespace Cis.Domain.Identity;

public sealed class UserRole : Entity
{
    private UserRole()
    {
    }

    private UserRole(Guid userId, Guid roleId, string requestedByUserId, string approvedByUserId, DateTime grantedAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("Role id cannot be empty.", nameof(roleId));
        }

        if (grantedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Granted timestamp must be UTC.", nameof(grantedAtUtc));
        }

        UserId = userId;
        RoleId = roleId;
        RequestedByUserId = Required(requestedByUserId, nameof(requestedByUserId), 200);
        ApprovedByUserId = Required(approvedByUserId, nameof(approvedByUserId), 200);
        GrantedAtUtc = grantedAtUtc;
    }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public string RequestedByUserId { get; private set; } = string.Empty;

    public string ApprovedByUserId { get; private set; } = string.Empty;

    public DateTime GrantedAtUtc { get; private set; }

    public User? User { get; private set; }

    public Role? Role { get; private set; }

    public static UserRole Create(Guid userId, Guid roleId, string requestedByUserId, string approvedByUserId, DateTime grantedAtUtc)
    {
        return new UserRole(userId, roleId, requestedByUserId, approvedByUserId, grantedAtUtc);
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
}
