using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Cis.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Cis.Tests.Integration.Support;

public sealed class CisApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string ExternalConnectionStringVariable = "CIS_TEST_CONNECTION_STRING";

    private readonly object _startLock = new();
    private PostgreSqlContainer? _postgres;

    private bool _started;
    private string? _connectionString;

    public string ConnectionString
    {
        get
        {
            var externalConnectionString = Environment.GetEnvironmentVariable(ExternalConnectionStringVariable);
            if (!string.IsNullOrWhiteSpace(externalConnectionString))
            {
                return externalConnectionString;
            }

            EnsureStarted();
            return _connectionString!;
        }
    }

    public Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ExternalConnectionStringVariable)))
        {
            EnsureStarted();
        }

        return Task.CompletedTask;
    }

    public new async Task DisposeAsync()
    {
        if (_started)
        {
            await _postgres!.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ExternalConnectionStringVariable)))
        {
            EnsureStarted();
        }

        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CisDb"] = ConnectionString,
                ["Database:InitializeOnStartup"] = "true",
                ["Database:ApplyMigrationsOnStartup"] = "true",
                ["Database:CommandTimeoutSeconds"] = "180",
                ["Identity:BootstrapAdmin:Email"] = "admin.tests@victoryfs.local",
                ["Identity:BootstrapAdmin:DisplayName"] = "Test System Administrator",
                ["Identity:BootstrapAdmin:Password"] = "ChangeMe123!",
                ["Security:RateLimiting:GlobalPermitLimit"] = "1000",
                ["Security:RateLimiting:GlobalWindowSeconds"] = "60",
                ["Security:RateLimiting:PublicAuthPermitLimit"] = "1000",
                ["Security:RateLimiting:PublicAuthWindowSeconds"] = "60",
                ["Security:RateLimiting:PublicEndpointPermitLimit"] = "1000",
                ["Security:RateLimiting:PublicEndpointWindowSeconds"] = "60",
                ["Security:Mfa:RequireForPrivilegedRoles"] = "false",
                ["Portal:SelfRegistration:FixedOtpCode"] = "000000",
                ["Portal:SelfRegistration:OtpLength"] = "6",
                ["Portal:SelfRegistration:OtpExpiryMinutes"] = "10",
                ["Portal:SelfRegistration:MaxFailedOtpAttempts"] = "5"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<CisDbContext>>();
            services.AddDbContext<CisDbContext>(options =>
            {
                options.UseNpgsql(
                        ConnectionString,
                        npgsql =>
                        {
                            npgsql.MigrationsAssembly(typeof(CisDbContext).Assembly.FullName);
                            npgsql.MigrationsHistoryTable("__ef_migrations_history", "infra");
                            npgsql.CommandTimeout(180);
                        })
                    .UseSnakeCaseNamingConvention();
            });
        });
    }

    private void EnsureStarted()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ExternalConnectionStringVariable)))
        {
            return;
        }

        if (_started)
        {
            return;
        }

        lock (_startLock)
        {
            if (_started)
            {
                return;
            }

            _postgres = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("cis_tests")
                .WithUsername("cis")
                .WithPassword("cis_password")
                .Build();
            _postgres.StartAsync().GetAwaiter().GetResult();
            _connectionString = _postgres.GetConnectionString();
            _started = true;
        }
    }
}
