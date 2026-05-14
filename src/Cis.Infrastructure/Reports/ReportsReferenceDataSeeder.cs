using Cis.Application.Common.Security;
using Cis.Domain.Reports;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.Reports;

internal sealed class ReportsReferenceDataSeeder
{
    private readonly CisDbContext _dbContext;

    public ReportsReferenceDataSeeder(CisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var definition in Definitions())
        {
            var exists = await _dbContext.ReportDefinitions.AnyAsync(report => report.Code == definition.Code, cancellationToken);
            if (exists)
            {
                continue;
            }

            var report = ReportDefinition.Create(definition.Code, definition.Name, definition.Category, definition.Frequency, definition.PrimaryUsers, definition.RequiredPermission, "system", now);
            foreach (var owner in definition.Owners)
            {
                report.AddOwner(owner.OwnerRole, owner.Responsibility, "system", now);
            }

            _dbContext.ReportDefinitions.Add(report);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyCollection<ReportDefinitionSeed> Definitions()
    {
        var legacyDefinitions = new List<ReportDefinitionSeed>
        {
            new("NAV-DAILY", "Daily NAV Report", "NAV", ReportFrequency.Daily, "Fund accountants; Executive management", Permissions.Reports.Read,
            [
                new("FundAccountantFinanceOfficer", "Generate and reconcile daily NAV reports"),
                new("ExecutiveManagement", "Approve and publish official NAV reports")
            ]),
            new("INVESTOR-STATEMENT", "Investor Statement", "Investor", ReportFrequency.Monthly, "Relationship managers; Investors", Permissions.Reports.Read,
            [
                new("RelationshipManager", "Review investor statement report content"),
                new("FundOperationsOfficer", "Generate investor statement extracts")
            ]),
            new("REG-PACK", "Regulator-Ready Pack", "Regulatory", ReportFrequency.Monthly, "Compliance; Regulators; Trustees", Permissions.Reports.RegulatorPack,
            [
                new("ComplianceRiskOfficer", "Approve regulator-ready pack content"),
                new("ExecutiveManagement", "Publish official regulator-ready pack")
            ])
        };

        legacyDefinitions.AddRange(ReportCatalogue.Entries.Select(entry => new ReportDefinitionSeed(
            entry.Code,
            entry.Name,
            entry.Category,
            entry.Frequency,
            entry.PrimaryUsers,
            entry.RequiredPermission,
            [
                new("ReportOwner", $"Owns {entry.Code} report definition and query output"),
                new("ReportReviewer", $"Reviews {entry.Code} report generation and CSV export")
            ])));

        return legacyDefinitions;
    }

    private sealed record ReportDefinitionSeed(string Code, string Name, string Category, ReportFrequency Frequency, string PrimaryUsers, string RequiredPermission, IReadOnlyCollection<OwnerSeed> Owners);

    private sealed record OwnerSeed(string OwnerRole, string Responsibility);
}
