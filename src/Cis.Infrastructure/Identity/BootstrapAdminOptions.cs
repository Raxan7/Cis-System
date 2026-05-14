namespace Cis.Infrastructure.Identity;

public sealed class BootstrapAdminOptions
{
    public const string SectionName = "Identity:BootstrapAdmin";

    public string? Email { get; init; }

    public string? DisplayName { get; init; }

    public string? Password { get; init; }
}
