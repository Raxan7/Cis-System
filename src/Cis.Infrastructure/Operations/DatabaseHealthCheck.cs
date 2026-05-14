using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cis.Infrastructure.Operations;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly CisDbContext _dbContext;

    public DatabaseHealthCheck(CisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? HealthCheckResult.Healthy("PostgreSQL connection succeeded.")
            : HealthCheckResult.Unhealthy("PostgreSQL connection failed.");
    }
}
