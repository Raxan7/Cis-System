using Cis.Domain.Cases;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.Cases;

internal sealed class CaseReferenceDataSeeder
{
    private readonly CisDbContext _dbContext;

    public CaseReferenceDataSeeder(CisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.CaseSlaPolicies.AnyAsync(cancellationToken))
        {
            return;
        }

        _dbContext.CaseSlaPolicies.AddRange(
            CaseSlaPolicy.Create("General", ServiceCasePriority.Low, 120),
            CaseSlaPolicy.Create("General", ServiceCasePriority.Medium, 72),
            CaseSlaPolicy.Create("General", ServiceCasePriority.High, 24),
            CaseSlaPolicy.Create("General", ServiceCasePriority.Critical, 8));

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
