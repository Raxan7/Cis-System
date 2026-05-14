using Cis.Domain.Operations;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.Operations;

internal sealed class OperationsReferenceDataSeeder
{
    private readonly CisDbContext _dbContext;

    public OperationsReferenceDataSeeder(CisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await SeedRtoRpoAsync(OperationalEnvironment.Development, 240, 1440, 1440, now, cancellationToken);
        await SeedRtoRpoAsync(OperationalEnvironment.Test, 240, 1440, 1440, now, cancellationToken);
        await SeedRtoRpoAsync(OperationalEnvironment.UAT, 120, 240, 240, now, cancellationToken);
        await SeedRtoRpoAsync(OperationalEnvironment.Production, 60, 15, 15, now, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRtoRpoAsync(OperationalEnvironment environment, int rtoMinutes, int rpoMinutes, int backupFrequencyMinutes, DateTime now, CancellationToken cancellationToken)
    {
        if (await _dbContext.RtoRpoConfigurations.AnyAsync(configuration => configuration.Environment == environment && configuration.SystemName == "Victory CIS" && configuration.IsActive, cancellationToken))
        {
            return;
        }

        _dbContext.RtoRpoConfigurations.Add(RtoRpoConfiguration.Create(environment, "Victory CIS", rtoMinutes, rpoMinutes, backupFrequencyMinutes, now, "seed"));
    }
}
