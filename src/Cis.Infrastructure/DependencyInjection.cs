using Cis.Application.Common.Interfaces;
using Cis.Infrastructure.Accounting;
using Cis.Infrastructure.Archive;
using Cis.Infrastructure.Audit;
using Cis.Infrastructure.Cash;
using Cis.Infrastructure.Cases;
using Cis.Infrastructure.Common;
using Cis.Infrastructure.ComplianceRisk;
using Cis.Infrastructure.CustodyReconciliation;
using Cis.Infrastructure.DataQuality;
using Cis.Infrastructure.Dealing;
using Cis.Infrastructure.FeesTaxDistribution;
using Cis.Infrastructure.Identity;
using Cis.Infrastructure.Investors;
using Cis.Infrastructure.Integrations;
using Cis.Infrastructure.NAV;
using Cis.Infrastructure.Operations;
using Cis.Infrastructure.Persistence;
using Cis.Infrastructure.Portal;
using Cis.Infrastructure.Reports;
using Cis.Infrastructure.Schemes;
using Cis.Infrastructure.Portfolio;
using Cis.Infrastructure.Security;
using Cis.Infrastructure.UnitRegister;
using Cis.Infrastructure.Workflows;
using Cis.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cis.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CisDb")
            ?? throw new InvalidOperationException("Connection string 'CisDb' is not configured.");
        var databaseCommandTimeoutSeconds = configuration.GetValue("Database:CommandTimeoutSeconds", 120);

        services.AddHttpContextAccessor();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<PasswordPolicyOptions>(configuration.GetSection(PasswordPolicyOptions.SectionName));
        services.Configure<BootstrapAdminOptions>(configuration.GetSection(BootstrapAdminOptions.SectionName));
        services.Configure<ApiCorsOptions>(configuration.GetSection(ApiCorsOptions.SectionName));
        services.Configure<SecurityRateLimitingOptions>(configuration.GetSection(SecurityRateLimitingOptions.SectionName));
        services.Configure<FileUploadSecurityOptions>(configuration.GetSection(FileUploadSecurityOptions.SectionName));
        services.Configure<MfaEnforcementOptions>(configuration.GetSection(MfaEnforcementOptions.SectionName));
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddScoped<IAuditWriter>(provider => provider.GetRequiredService<IAuditLogWriter>());
        services.AddScoped<IAuditQueryService, AuditLogWriter>();
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<IApprovalPolicyService, ApprovalPolicyService>();
        services.AddScoped<IImmutableArchiveService, ImmutableArchiveService>();
        services.AddScoped<IRetentionPolicyService, RetentionPolicyService>();
        services.AddScoped<ISchemeService, SchemeService>();
        services.AddScoped<IInvestorService, InvestorService>();
        services.AddScoped<IDealingService, DealingService>();
        services.AddScoped<IUnitRegisterService, UnitRegisterService>();
        services.AddScoped<ICashService, CashService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<INavService, NavService>();
        services.AddScoped<IAccountingService, AccountingService>();
        services.AddScoped<IFeesTaxDistributionService, FeesTaxDistributionService>();
        services.AddScoped<IComplianceRiskService, ComplianceRiskService>();
        services.AddScoped<ICustodyReconciliationService, CustodyReconciliationService>();
        services.AddScoped<IPortalService, PortalService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IIntegrationService, IntegrationService>();
        services.AddScoped<ICaseManagementService, CaseManagementService>();
        services.AddScoped<IDataQualityService, DataQualityService>();
        services.AddScoped<IOperationsService, OperationsService>();
        services.AddScoped<IBankStatementProvider>(provider => provider.GetRequiredService<IIntegrationService>());
        services.AddScoped<IPaymentStatusProvider>(provider => provider.GetRequiredService<IIntegrationService>());
        services.AddScoped<IMobileMoneyCallbackHandler>(provider => provider.GetRequiredService<IIntegrationService>());
        services.AddScoped<ICustodianStatementProvider>(provider => provider.GetRequiredService<IIntegrationService>());
        services.AddScoped<IPricingSourceProvider>(provider => provider.GetRequiredService<IIntegrationService>());
        services.AddScoped<IEmailSender>(provider => provider.GetRequiredService<IIntegrationService>());
        services.AddScoped<IErpExporter>(provider => provider.GetRequiredService<IIntegrationService>());
        services.AddScoped<ISmsSender, IntegrationService>();
        services.AddSingleton<IDocumentStorage, LocalDocumentStorage>();
        services.AddScoped<IKycQueryService>(provider => provider.GetRequiredService<IInvestorService>() as InvestorService ?? throw new InvalidOperationException("Investor service registration is invalid."));
        services.AddScoped<IAmlQueryService>(provider => provider.GetRequiredService<IInvestorService>() as InvestorService ?? throw new InvalidOperationException("Investor service registration is invalid."));
        services.AddScoped<IAmlScreeningProvider, ManualAmlScreeningProvider>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordPolicyValidator, PasswordPolicyValidator>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IFileSecurityValidator, FileSecurityValidator>();
        services.AddSingleton<IMalwareScanner, StubMalwareScanner>();
        services.AddScoped<IdentityReferenceDataSeeder>();
        services.AddScoped<WorkflowReferenceDataSeeder>();
        services.AddScoped<ReportsReferenceDataSeeder>();
        services.AddScoped<CaseReferenceDataSeeder>();
        services.AddScoped<DataQualityReferenceDataSeeder>();
        services.AddScoped<OperationsReferenceDataSeeder>();

        services.AddDbContext<CisDbContext>(options =>
        {
            options.UseNpgsql(
                    connectionString,
                    npgsql =>
                    {
                        npgsql.MigrationsAssembly(typeof(CisDbContext).Assembly.FullName);
                        npgsql.MigrationsHistoryTable("__ef_migrations_history", "infra");
                        npgsql.CommandTimeout(databaseCommandTimeoutSeconds);
                    })
                .UseSnakeCaseNamingConvention();
        });

        return services;
    }
}
