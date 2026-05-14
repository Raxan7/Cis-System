using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Cis.Infrastructure.Persistence;

public sealed class CisDbContextFactory : IDesignTimeDbContextFactory<CisDbContext>
{
    public CisDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__CisDb")
            ?? "Host=localhost;Port=5432;Database=cis;Username=cis;Password=cis_password";

        var optionsBuilder = new DbContextOptionsBuilder<CisDbContext>();
        optionsBuilder
            .UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(CisDbContext).Assembly.FullName);
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "infra");
                })
            .UseSnakeCaseNamingConvention();

        return new CisDbContext(optionsBuilder.Options);
    }
}
