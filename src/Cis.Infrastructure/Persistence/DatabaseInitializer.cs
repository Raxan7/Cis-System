using Cis.Infrastructure.Identity;
using Cis.Infrastructure.Cases;
using Cis.Infrastructure.DataQuality;
using Cis.Infrastructure.Operations;
using Cis.Infrastructure.Reports;
using Cis.Infrastructure.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CisDbContext>();

        if (configuration.GetValue("Database:ApplyMigrationsOnStartup", false))
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        var seeder = scope.ServiceProvider.GetRequiredService<IdentityReferenceDataSeeder>();
        await seeder.SeedAsync(cancellationToken);

        var workflowSeeder = scope.ServiceProvider.GetRequiredService<WorkflowReferenceDataSeeder>();
        await workflowSeeder.SeedAsync(cancellationToken);

        var reportsSeeder = scope.ServiceProvider.GetRequiredService<ReportsReferenceDataSeeder>();
        await reportsSeeder.SeedAsync(cancellationToken);

        var caseSeeder = scope.ServiceProvider.GetRequiredService<CaseReferenceDataSeeder>();
        await caseSeeder.SeedAsync(cancellationToken);

        var dataQualitySeeder = scope.ServiceProvider.GetRequiredService<DataQualityReferenceDataSeeder>();
        await dataQualitySeeder.SeedAsync(cancellationToken);

        var operationsSeeder = scope.ServiceProvider.GetRequiredService<OperationsReferenceDataSeeder>();
        await operationsSeeder.SeedAsync(cancellationToken);
    }
}
