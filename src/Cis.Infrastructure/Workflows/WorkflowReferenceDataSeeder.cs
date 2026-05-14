using Cis.Domain.Archive;
using Cis.Domain.Workflows;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.Workflows;

internal sealed class WorkflowReferenceDataSeeder
{
    private readonly CisDbContext _dbContext;

    public WorkflowReferenceDataSeeder(CisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var definition in ApprovalPolicies())
        {
            var exists = await _dbContext.ApprovalPolicies
                .AnyAsync(policy => policy.WorkflowType == definition.WorkflowType && policy.IsActive, cancellationToken);
            if (!exists)
            {
                _dbContext.ApprovalPolicies.Add(ApprovalPolicy.Create(
                    definition.WorkflowType,
                    definition.Name,
                    definition.Module,
                    requiresChecker: true,
                    requiresApprover: true,
                    now));
            }
        }

        foreach (var definition in RetentionPolicies())
        {
            var exists = await _dbContext.RetentionPolicies
                .AnyAsync(policy => policy.Module == definition.Module && policy.Name == definition.Name, cancellationToken);
            if (!exists)
            {
                _dbContext.RetentionPolicies.Add(RetentionPolicy.Create(
                    definition.Name,
                    definition.Module,
                    definition.RetentionDays,
                    definition.LegalHoldEnabled,
                    now));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyCollection<ApprovalPolicyDefinition> ApprovalPolicies()
    {
        return
        [
            new(WorkflowType.SchemeClassSetupOrAmendment, "Scheme/class setup or amendment approval", "Schemes"),
            new(WorkflowType.InvestorApprovalRiskClassificationChange, "Investor approval and risk classification approval", "Investors"),
            new(WorkflowType.ManualFeeWaiverPricingOverride, "Manual fee waiver and pricing override approval", "FeesTaxDistribution"),
            new(WorkflowType.BackdatedPostingCorrectionJournal, "Backdated posting and correction journal approval", "Accounting"),
            new(WorkflowType.LargeExceptionalRedemption, "Large or exceptional redemption approval", "Dealing"),
            new(WorkflowType.NavPreparationApprovalPublication, "NAV preparation approval and publication", "NAV"),
            new(WorkflowType.UserAccessChange, "User access change approval", "Identity"),
            new(WorkflowType.ReportPublicationRegulatorPackIssuance, "Report publication and regulator pack issuance", "Reports"),
            new(WorkflowType.PortalDigitalServiceRequest, "Investor portal digital service request approval", "Portal")
        ];
    }

    private static IReadOnlyCollection<RetentionPolicyDefinition> RetentionPolicies()
    {
        return
        [
            new("Default regulated archive retention", "Archive", 3650, true),
            new("Audit retention", "Audit", 3650, true),
            new("Workflow retention", "Workflow", 3650, true),
            new("Identity retention", "Identity", 3650, true),
            new("Reports retention", "Reports", 3650, true),
            new("Portal activity retention", "Portal", 3650, true)
        ];
    }

    private sealed record ApprovalPolicyDefinition(WorkflowType WorkflowType, string Name, string Module);

    private sealed record RetentionPolicyDefinition(string Name, string Module, int RetentionDays, bool LegalHoldEnabled);
}
