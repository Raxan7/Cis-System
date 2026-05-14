using Cis.Domain.Common;

namespace Cis.Domain.Identity;

public sealed class Role : AggregateRoot
{
    private readonly List<RolePermission> _permissions = [];

    private Role()
    {
    }

    private Role(Guid id, string name, string description, bool isPrivileged)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Role id cannot be empty.", nameof(id));
        }

        Id = id;
        Name = Required(name, nameof(name), 100);
        NormalizedName = Name.ToUpperInvariant();
        Description = Required(description, nameof(description), 500);
        IsPrivileged = isPrivileged;
    }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public bool IsPrivileged { get; private set; }

    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    public static Role Create(Guid id, string name, string description, bool isPrivileged)
    {
        return new Role(id, name, description, isPrivileged);
    }

    public void GrantPermission(Guid permissionId)
    {
        if (_permissions.Any(permission => permission.PermissionId == permissionId))
        {
            return;
        }

        _permissions.Add(RolePermission.Create(Id, permissionId));
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
