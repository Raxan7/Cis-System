using Cis.Domain.Common;
using Cis.Domain.ComplianceRisk;
using Cis.Domain.Schemes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class StatutoryLimitConfiguration : IEntityTypeConfiguration<StatutoryLimit>
{
    public void Configure(EntityTypeBuilder<StatutoryLimit> builder)
    {
        builder.ToTable("statutory_limits", "compliance_risk");
        builder.HasKey(limit => limit.Id);
        ConfigureLimit(builder);
        builder.HasIndex(limit => new { limit.SchemeId, limit.SchemeClassId, limit.LimitType, limit.Reference, limit.EffectiveDate }).IsUnique();
    }

    internal static void ConfigureLimit<T>(EntityTypeBuilder<T> builder)
        where T : AuditableAggregateRoot
    {
        builder.Property("LimitType").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property("Reference").HasMaxLength(200).IsRequired();
        builder.Property("CreatedByUserId").HasMaxLength(200).IsRequired();
        builder.HasOne<Scheme>().WithMany().HasForeignKey("SchemeId").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey("SchemeClassId").OnDelete(DeleteBehavior.Restrict);
        Audit(builder);
    }

    internal static void Audit<T>(EntityTypeBuilder<T> builder)
        where T : AuditableAggregateRoot
    {
        builder.OwnsOne(entity => entity.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class InternalPolicyLimitConfiguration : IEntityTypeConfiguration<InternalPolicyLimit>
{
    public void Configure(EntityTypeBuilder<InternalPolicyLimit> builder)
    {
        builder.ToTable("internal_policy_limits", "compliance_risk");
        builder.HasKey(limit => limit.Id);
        StatutoryLimitConfiguration.ConfigureLimit(builder);
        builder.HasIndex(limit => new { limit.SchemeId, limit.SchemeClassId, limit.LimitType, limit.Reference, limit.EffectiveDate }).IsUnique();
    }
}

internal sealed class LimitCheckRunConfiguration : IEntityTypeConfiguration<LimitCheckRun>
{
    public void Configure(EntityTypeBuilder<LimitCheckRun> builder)
    {
        builder.ToTable("limit_check_runs", "compliance_risk");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.RunNumber).HasMaxLength(40).IsRequired();
        builder.Property(run => run.FormulaVersion).HasMaxLength(50).IsRequired();
        builder.Property(run => run.SourceDataJson).HasColumnType("jsonb").IsRequired();
        builder.Property(run => run.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(run => run.RunByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<Scheme>().WithMany().HasForeignKey(run => run.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(run => run.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.Breaches).WithOne().HasForeignKey(breach => breach.LimitCheckRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.RelatedPartyExposures).WithOne().HasForeignKey(exposure => exposure.LimitCheckRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.CounterpartyLimitUsages).WithOne().HasForeignKey(usage => usage.LimitCheckRunId).OnDelete(DeleteBehavior.Restrict);
        builder.Metadata.FindNavigation(nameof(LimitCheckRun.Breaches))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(LimitCheckRun.RelatedPartyExposures))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(LimitCheckRun.CounterpartyLimitUsages))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(run => run.RunNumber).IsUnique();
        builder.HasIndex(run => new { run.SchemeId, run.SchemeClassId, run.BusinessDate });
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class LimitBreachConfiguration : IEntityTypeConfiguration<LimitBreach>
{
    public void Configure(EntityTypeBuilder<LimitBreach> builder)
    {
        builder.ToTable("limit_breaches", "compliance_risk");
        builder.HasKey(breach => breach.Id);
        builder.Property(breach => breach.RuleScope).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(breach => breach.LimitType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(breach => breach.Reference).HasMaxLength(200).IsRequired();
        builder.Property(breach => breach.Severity).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(breach => breach.Description).HasMaxLength(1000).IsRequired();
        builder.Property(breach => breach.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(breach => breach.OwnerUserId).HasMaxLength(200);
        builder.Property(breach => breach.AssignmentComment).HasMaxLength(1000);
        builder.Property(breach => breach.RemediationPlan).HasMaxLength(1000);
        builder.Property(breach => breach.ClosureEvidenceReference).HasMaxLength(500);
        builder.Property(breach => breach.CreatedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(breach => breach.RemediatedByUserId).HasMaxLength(200);
        builder.Property(breach => breach.ClosedByUserId).HasMaxLength(200);
        builder.HasOne<StatutoryLimit>().WithMany().HasForeignKey(breach => breach.StatutoryLimitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InternalPolicyLimit>().WithMany().HasForeignKey(breach => breach.InternalPolicyLimitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(breach => new { breach.Status, breach.Severity });
        builder.HasIndex(breach => new { breach.LimitCheckRunId, breach.RuleScope, breach.LimitType, breach.Reference }).IsUnique();
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class BreachExceptionRegisterConfiguration : IEntityTypeConfiguration<BreachExceptionRegister>
{
    public void Configure(EntityTypeBuilder<BreachExceptionRegister> builder)
    {
        builder.ToTable("breach_exception_register", "compliance_risk");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.ExceptionReason).HasMaxLength(1000).IsRequired();
        builder.Property(record => record.EvidenceReference).HasMaxLength(500).IsRequired();
        builder.Property(record => record.RegisteredByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<LimitBreach>().WithMany().HasForeignKey(record => record.LimitBreachId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(record => record.LimitBreachId);
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class RemediationActionConfiguration : IEntityTypeConfiguration<RemediationAction>
{
    public void Configure(EntityTypeBuilder<RemediationAction> builder)
    {
        builder.ToTable("remediation_actions", "compliance_risk");
        builder.HasKey(action => action.Id);
        builder.Property(action => action.ActionDescription).HasMaxLength(1000).IsRequired();
        builder.Property(action => action.EvidenceReference).HasMaxLength(500).IsRequired();
        builder.Property(action => action.OwnerUserId).HasMaxLength(200).IsRequired();
        builder.Property(action => action.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne<LimitBreach>().WithMany().HasForeignKey(action => action.LimitBreachId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(action => action.LimitBreachId);
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class LiquidityCoverageRunConfiguration : IEntityTypeConfiguration<LiquidityCoverageRun>
{
    public void Configure(EntityTypeBuilder<LiquidityCoverageRun> builder)
    {
        builder.ToTable("liquidity_coverage_runs", "compliance_risk");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.FormulaVersion).HasMaxLength(50).IsRequired();
        builder.Property(run => run.SourceDataJson).HasColumnType("jsonb").IsRequired();
        builder.Property(run => run.RunByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<Scheme>().WithMany().HasForeignKey(run => run.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(run => run.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(run => new { run.SchemeId, run.SchemeClassId, run.BusinessDate });
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class RedemptionStressScenarioConfiguration : IEntityTypeConfiguration<RedemptionStressScenario>
{
    public void Configure(EntityTypeBuilder<RedemptionStressScenario> builder)
    {
        builder.ToTable("redemption_stress_scenarios", "compliance_risk");
        builder.HasKey(scenario => scenario.Id);
        builder.Property(scenario => scenario.Name).HasMaxLength(200).IsRequired();
        builder.Property(scenario => scenario.AssumptionsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(scenario => scenario.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(scenario => scenario.CreatedByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<Scheme>().WithMany().HasForeignKey(scenario => scenario.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(scenario => new { scenario.SchemeId, scenario.Name }).IsUnique();
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class RedemptionStressTestRunConfiguration : IEntityTypeConfiguration<RedemptionStressTestRun>
{
    public void Configure(EntityTypeBuilder<RedemptionStressTestRun> builder)
    {
        builder.ToTable("redemption_stress_test_runs", "compliance_risk");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.FormulaVersion).HasMaxLength(50).IsRequired();
        builder.Property(run => run.SourceDataJson).HasColumnType("jsonb").IsRequired();
        builder.Property(run => run.RunByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<RedemptionStressScenario>().WithMany().HasForeignKey(run => run.ScenarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(run => run.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(run => new { run.ScenarioId, run.BusinessDate }).IsUnique();
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class LiquidationTimeAnalysisRunConfiguration : IEntityTypeConfiguration<LiquidationTimeAnalysisRun>
{
    public void Configure(EntityTypeBuilder<LiquidationTimeAnalysisRun> builder)
    {
        builder.ToTable("liquidation_time_analysis_runs", "compliance_risk");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.FormulaVersion).HasMaxLength(50).IsRequired();
        builder.Property(run => run.AssumptionsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(run => run.SourceDataJson).HasColumnType("jsonb").IsRequired();
        builder.Property(run => run.RunByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<Scheme>().WithMany().HasForeignKey(run => run.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(run => run.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(run => new { run.SchemeId, run.SchemeClassId, run.BusinessDate });
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class RelatedPartyExposureConfiguration : IEntityTypeConfiguration<RelatedPartyExposure>
{
    public void Configure(EntityTypeBuilder<RelatedPartyExposure> builder)
    {
        builder.ToTable("related_party_exposures", "compliance_risk");
        builder.HasKey(exposure => exposure.Id);
        builder.Property(exposure => exposure.RelatedPartyName).HasMaxLength(200).IsRequired();
        builder.Property(exposure => exposure.SourceReference).HasMaxLength(200).IsRequired();
        builder.HasIndex(exposure => new { exposure.LimitCheckRunId, exposure.RelatedPartyName }).IsUnique();
    }
}

internal sealed class CounterpartyLimitUsageConfiguration : IEntityTypeConfiguration<CounterpartyLimitUsage>
{
    public void Configure(EntityTypeBuilder<CounterpartyLimitUsage> builder)
    {
        builder.ToTable("counterparty_limit_usages", "compliance_risk");
        builder.HasKey(usage => usage.Id);
        builder.Property(usage => usage.CounterpartyName).HasMaxLength(200).IsRequired();
        builder.Property(usage => usage.SourceReference).HasMaxLength(200).IsRequired();
        builder.HasIndex(usage => new { usage.LimitCheckRunId, usage.CounterpartyName }).IsUnique();
    }
}

internal sealed class RiskDashboardSnapshotConfiguration : IEntityTypeConfiguration<RiskDashboardSnapshot>
{
    public void Configure(EntityTypeBuilder<RiskDashboardSnapshot> builder)
    {
        builder.ToTable("risk_dashboard_snapshots", "compliance_risk");
        builder.HasKey(snapshot => snapshot.Id);
        builder.Property(snapshot => snapshot.GeneratedByUserId).HasMaxLength(200).IsRequired();
        builder.HasIndex(snapshot => snapshot.GeneratedAtUtc);
        StatutoryLimitConfiguration.Audit(builder);
    }
}
