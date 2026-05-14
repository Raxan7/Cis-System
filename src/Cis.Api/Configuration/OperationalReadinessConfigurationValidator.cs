using Cis.Infrastructure.Identity;
using Cis.Infrastructure.Security;

namespace Cis.Api.Configuration;

public static class OperationalReadinessConfigurationValidator
{
    private static readonly string[] RequiredSections =
    [
        "ConnectionStrings:CisDb",
        "Jwt:Issuer",
        "Jwt:Audience",
        "Jwt:SigningKey",
        "OperationalReadiness:Environment",
        "OperationalReadiness:RtoMinutes",
        "OperationalReadiness:RpoMinutes",
        "OperationalReadiness:BackupDirectory",
        "Storage:LocalPath",
        "BackgroundJobs:Provider",
        "BackgroundJobs:DashboardAdminRole",
        "Security:FileUploads:MaxSizeBytes"
    ];

    public static void Validate(IConfiguration configuration, string environmentName)
    {
        var errors = new List<string>();

        foreach (var key in RequiredSections)
        {
            if (string.IsNullOrWhiteSpace(configuration[key]))
            {
                errors.Add($"{key} is required.");
            }
        }

        var rto = configuration.GetValue<int?>("OperationalReadiness:RtoMinutes");
        if (rto is null or <= 0)
        {
            errors.Add("OperationalReadiness:RtoMinutes must be greater than zero.");
        }

        var rpo = configuration.GetValue<int?>("OperationalReadiness:RpoMinutes");
        if (rpo is null or <= 0)
        {
            errors.Add("OperationalReadiness:RpoMinutes must be greater than zero.");
        }

        if (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase))
        {
            ValidateProductionSecrets(configuration, errors);
        }

        var uploadMaxSize = configuration.GetValue<long?>($"{FileUploadSecurityOptions.SectionName}:MaxSizeBytes");
        if (uploadMaxSize is null or <= 0)
        {
            errors.Add("Security:FileUploads:MaxSizeBytes must be greater than zero.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Application configuration is invalid for {environmentName}: {string.Join(" ", errors)}");
        }
    }

    private static void ValidateProductionSecrets(IConfiguration configuration, ICollection<string> errors)
    {
        var connectionString = configuration.GetConnectionString("CisDb");
        if (ContainsAny(connectionString, "localhost", "127.0.0.1", "cis_password", "Password=postgres", "Password=cis_password", "__REQUIRED"))
        {
            errors.Add("Production ConnectionStrings:CisDb must not use local/default database credentials.");
        }

        var signingKey = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey)
            || signingKey.Length < 48
            || ContainsAny(signingKey, "development", "change-before-production", "ChangeMe", "__REQUIRED"))
        {
            errors.Add("Production Jwt:SigningKey must be a non-default secret with at least 48 characters.");
        }

        var bootstrapPassword = configuration[$"{BootstrapAdminOptions.SectionName}:Password"];
        if (ContainsAny(bootstrapPassword, "ChangeMe", "password", "__REQUIRED"))
        {
            errors.Add("Production bootstrap admin password must not be a default or placeholder secret.");
        }

        var backupDirectory = configuration["OperationalReadiness:BackupDirectory"];
        if (ContainsAny(backupDirectory, "__REQUIRED", "tmp", "temp"))
        {
            errors.Add("Production backup directory must be an explicit durable storage path.");
        }

        var allowedOrigins = configuration.GetSection($"{ApiCorsOptions.SectionName}:AllowedOrigins").Get<string[]>() ?? [];
        if (allowedOrigins.Length == 0 || allowedOrigins.Any(origin => ContainsAny(origin, "__REQUIRED", "http://localhost", "http://127.0.0.1")))
        {
            errors.Add("Production Security:Cors:AllowedOrigins must contain at least one explicit non-local origin.");
        }

        if (!configuration.GetValue($"{MfaEnforcementOptions.SectionName}:RequireForPrivilegedRoles", false))
        {
            errors.Add("Production Security:Mfa:RequireForPrivilegedRoles must be enabled.");
        }
    }

    private static bool ContainsAny(string? value, params string[] probes)
    {
        return !string.IsNullOrWhiteSpace(value)
            && probes.Any(probe => value.Contains(probe, StringComparison.OrdinalIgnoreCase));
    }
}
