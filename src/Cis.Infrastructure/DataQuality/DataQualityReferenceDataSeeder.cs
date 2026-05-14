using Cis.Domain.DataQuality;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.DataQuality;

internal sealed class DataQualityReferenceDataSeeder
{
    private readonly CisDbContext _dbContext;

    public DataQualityReferenceDataSeeder(CisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _dbContext.DataQualityRules.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        _dbContext.DataQualityRules.AddRange(DefaultRules("system", now));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public static IReadOnlyCollection<DataQualityRule> DefaultRules(string createdByUserId, DateTime createdAtUtc)
    {
        return
        [
            DataQualityRule.Create(DataQualityRuleCode.MissingRequiredInvestorFields, "Missing required investor fields", "Investor record is missing mandatory profile or contact data.", DataQualitySeverity.High, 2, createdByUserId, createdAtUtc),
            DataQualityRule.Create(DataQualityRuleCode.ExpiredKycDocuments, "Expired KYC documents", "KYC document has expired or is marked expired.", DataQualitySeverity.High, 1, createdByUserId, createdAtUtc),
            DataQualityRule.Create(DataQualityRuleCode.DuplicateInvestorIdentifiers, "Duplicate investor identifiers", "Investor identifier duplicate warning exists.", DataQualitySeverity.High, 2, createdByUserId, createdAtUtc),
            DataQualityRule.Create(DataQualityRuleCode.UnmatchedBankReceipts, "Unmatched bank receipts", "Bank receipt remains unmatched or in suspense.", DataQualitySeverity.Medium, 1, createdByUserId, createdAtUtc),
            DataQualityRule.Create(DataQualityRuleCode.NegativeHoldings, "Negative holdings", "Unit holding has negative units or negative redeemable units.", DataQualitySeverity.Critical, 0, createdByUserId, createdAtUtc),
            DataQualityRule.Create(DataQualityRuleCode.UnapprovedNavUsedForDealing, "Unapproved NAV used for dealing", "Dealing instruction references missing or unpublished NAV.", DataQualitySeverity.Critical, 0, createdByUserId, createdAtUtc),
            DataQualityRule.Create(DataQualityRuleCode.StalePriceInputs, "Stale price inputs", "NAV valuation has missing or stale price input exception.", DataQualitySeverity.High, 0, createdByUserId, createdAtUtc),
            DataQualityRule.Create(DataQualityRuleCode.UnbalancedJournal, "Unbalanced journal", "Journal line debits and credits do not balance.", DataQualitySeverity.Critical, 0, createdByUserId, createdAtUtc),
            DataQualityRule.Create(DataQualityRuleCode.UnresolvedReconciliationBreakOlderThanThreshold, "Aged unresolved reconciliation break", "Reconciliation break remains unresolved beyond configured threshold.", DataQualitySeverity.High, 3, createdByUserId, createdAtUtc),
            DataQualityRule.Create(DataQualityRuleCode.ReportPublicationWithoutApproval, "Report publication without approval", "Report run is published without approval evidence.", DataQualitySeverity.Critical, 0, createdByUserId, createdAtUtc),
            DataQualityRule.Create(DataQualityRuleCode.ConfigurationEffectiveDateOverlap, "Configuration effective-date overlap", "Configuration has overlapping effective-date ranges.", DataQualitySeverity.High, 0, createdByUserId, createdAtUtc)
        ];
    }
}
