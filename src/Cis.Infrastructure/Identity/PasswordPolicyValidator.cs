using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace Cis.Infrastructure.Identity;

internal sealed class PasswordPolicyValidator : IPasswordPolicyValidator
{
    private readonly PasswordPolicyOptions _options;

    public PasswordPolicyValidator(IOptions<PasswordPolicyOptions> options)
    {
        _options = options.Value;
    }

    public void Validate(string password, string? email = null)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password) || password.Length < _options.RequiredLength)
        {
            errors.Add($"Password must be at least {_options.RequiredLength} characters long.");
        }

        if (_options.RequireDigit && !password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one digit.");
        }

        if (_options.RequireUppercase && !password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase character.");
        }

        if (_options.RequireLowercase && !password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase character.");
        }

        if (_options.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
        {
            errors.Add("Password must contain at least one non-alphanumeric character.");
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var localPart = email.Split('@')[0];
            if (localPart.Length >= 4 && password.Contains(localPart, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Password must not contain the email username.");
            }
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["password"] = errors.ToArray()
            });
        }
    }
}
