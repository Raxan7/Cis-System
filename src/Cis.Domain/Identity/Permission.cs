using Cis.Domain.Common;

namespace Cis.Domain.Identity;

public sealed class Permission : AggregateRoot
{
    private Permission()
    {
    }

    private Permission(Guid id, string code, string module, string description)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Permission id cannot be empty.", nameof(id));
        }

        Id = id;
        Code = Required(code, nameof(code), 150);
        Module = Required(module, nameof(module), 100);
        Description = Required(description, nameof(description), 500);
    }

    public string Code { get; private set; } = string.Empty;

    public string Module { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public static Permission Create(Guid id, string code, string module, string description)
    {
        return new Permission(id, code, module, description);
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
