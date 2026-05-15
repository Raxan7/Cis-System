using Cis.Application.Common.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cis.Infrastructure.Operations;

public sealed class BackgroundJobsHealthCheck : IHealthCheck
{
    private readonly IBackgroundJobDispatcher _backgroundJobDispatcher;

    public BackgroundJobsHealthCheck(IBackgroundJobDispatcher backgroundJobDispatcher)
    {
        _backgroundJobDispatcher = backgroundJobDispatcher;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var snapshot = _backgroundJobDispatcher.GetSnapshot();
        if (snapshot.QueuedCount > 500)
        {
            return Task.FromResult(HealthCheckResult.Degraded($"Background job queue depth is elevated at {snapshot.QueuedCount}."));
        }

        return Task.FromResult(HealthCheckResult.Healthy(
            $"Background jobs healthy. queued={snapshot.QueuedCount}, active={snapshot.ActiveCount}, completed={snapshot.CompletedCount}, failed={snapshot.FailedCount}."));
    }
}
