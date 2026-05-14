using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cis.Infrastructure.Operations;

public sealed class BackgroundJobsHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public BackgroundJobsHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var provider = _configuration["BackgroundJobs:Provider"];
        var dashboardEnabled = _configuration.GetValue("BackgroundJobs:DashboardEnabled", true);

        if (string.IsNullOrWhiteSpace(provider))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Background job provider is not configured."));
        }

        return Task.FromResult(HealthCheckResult.Healthy($"Background job provider '{provider}' configured; dashboard enabled={dashboardEnabled}."));
    }
}
