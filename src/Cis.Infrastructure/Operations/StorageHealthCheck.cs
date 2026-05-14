using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cis.Infrastructure.Operations;

public sealed class StorageHealthCheck : IHealthCheck
{
    private readonly string _rootPath;

    public StorageHealthCheck(IConfiguration configuration)
    {
        _rootPath = configuration["Storage:LocalPath"]
            ?? configuration["Integrations:LocalStoragePath"]
            ?? Path.Combine(AppContext.BaseDirectory, "integration-storage");
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(_rootPath);
            var probePath = Path.Combine(_rootPath, $".health-{Guid.NewGuid():N}.tmp");
            await File.WriteAllTextAsync(probePath, DateTime.UtcNow.ToString("O"), cancellationToken);
            File.Delete(probePath);
            return HealthCheckResult.Healthy("Storage path is writable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Storage path is not writable.", exception);
        }
    }
}
