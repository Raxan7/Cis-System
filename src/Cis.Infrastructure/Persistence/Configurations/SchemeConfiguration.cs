using Cis.Domain.Schemes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class SchemeConfiguration : IEntityTypeConfiguration<Scheme>
{
    public void Configure(EntityTypeBuilder<Scheme> builder)
    {
        builder.ToTable("schemes", "schemes");

        builder.HasKey(scheme => scheme.Id);

        builder.Property(scheme => scheme.Code).HasMaxLength(30).IsRequired();
        builder.Property(scheme => scheme.Name).HasMaxLength(200).IsRequired();
        builder.Property(scheme => scheme.LegalType).HasMaxLength(100).IsRequired();
        builder.Property(scheme => scheme.BaseCurrency).HasMaxLength(3).IsRequired();
        builder.Property(scheme => scheme.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(scheme => scheme.SubmittedByUserId).HasMaxLength(200);
        builder.Property(scheme => scheme.CheckedByUserId).HasMaxLength(200);
        builder.Property(scheme => scheme.ApprovedByUserId).HasMaxLength(200);

        builder.OwnsOne(scheme => scheme.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });

        builder.HasIndex(scheme => scheme.Code).IsUnique();
        builder.HasIndex(scheme => scheme.Status);
        builder.HasIndex(scheme => scheme.BaseCurrency);

        builder.Metadata.FindNavigation(nameof(Scheme.Classes))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Scheme.Configurations))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Scheme.FeeSchedules))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Scheme.EligibilityRules))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Scheme.ApprovedInstrumentRules))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Scheme.RiskProfiles))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Scheme.LiquidityThresholds))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Scheme.BankAccounts))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Scheme.CustodianMappings))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Scheme.DistributionRules))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Scheme.TemplateMappings))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(Scheme.VersionHistory))?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class SchemeClassConfiguration : IEntityTypeConfiguration<SchemeClass>
{
    public void Configure(EntityTypeBuilder<SchemeClass> builder)
    {
        builder.ToTable("scheme_classes", "schemes");
        builder.HasKey(schemeClass => schemeClass.Id);
        builder.Property(schemeClass => schemeClass.Code).HasMaxLength(30).IsRequired();
        builder.Property(schemeClass => schemeClass.Name).HasMaxLength(200).IsRequired();
        builder.Property(schemeClass => schemeClass.Currency).HasMaxLength(3).IsRequired();
        builder.Property(schemeClass => schemeClass.ValuationFrequency).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(schemeClass => schemeClass.DealingFrequency).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(schemeClass => schemeClass.CutOffTime).HasColumnType("time without time zone").IsRequired();
        builder.HasOne(schemeClass => schemeClass.Scheme).WithMany(scheme => scheme.Classes).HasForeignKey(schemeClass => schemeClass.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(schemeClass => new { schemeClass.SchemeId, schemeClass.Code }).IsUnique();
        builder.HasIndex(schemeClass => new { schemeClass.Currency, schemeClass.IsActive });
    }
}

internal sealed class SchemeConfigurationEntityConfiguration : IEntityTypeConfiguration<Cis.Domain.Schemes.SchemeConfiguration>
{
    public void Configure(EntityTypeBuilder<Cis.Domain.Schemes.SchemeConfiguration> builder)
    {
        builder.ToTable("scheme_configurations", "schemes");
        builder.HasKey(configuration => configuration.Id);
        builder.Property(configuration => configuration.NavPricingBasis).HasMaxLength(100).IsRequired();
        builder.Property(configuration => configuration.IncomeRecognitionBasis).HasMaxLength(100).IsRequired();
        builder.HasOne(configuration => configuration.Scheme).WithMany(scheme => scheme.Configurations).HasForeignKey(configuration => configuration.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(configuration => configuration.SchemeId);
    }
}

internal sealed class FeeScheduleConfiguration : IEntityTypeConfiguration<FeeSchedule>
{
    public void Configure(EntityTypeBuilder<FeeSchedule> builder)
    {
        builder.ToTable("fee_schedules", "schemes");
        builder.HasKey(fee => fee.Id);
        builder.Property(fee => fee.FeeType).HasMaxLength(100).IsRequired();
        builder.Property(fee => fee.CalculationBasis).HasMaxLength(100).IsRequired();
        builder.Property(fee => fee.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne(fee => fee.Scheme).WithMany(scheme => scheme.FeeSchedules).HasForeignKey(fee => fee.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(fee => fee.SchemeClass).WithMany().HasForeignKey(fee => fee.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(fee => new { fee.SchemeId, fee.SchemeClassId, fee.FeeType, fee.EffectiveFrom });
        builder.HasIndex(fee => fee.Status);
        builder.Metadata.FindNavigation(nameof(FeeSchedule.Rules))?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class FeeRuleConfiguration : IEntityTypeConfiguration<FeeRule>
{
    public void Configure(EntityTypeBuilder<FeeRule> builder)
    {
        builder.ToTable("fee_rules", "schemes");
        builder.HasKey(rule => rule.Id);
        builder.HasOne(rule => rule.FeeSchedule).WithMany(fee => fee.Rules).HasForeignKey(rule => rule.FeeScheduleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(rule => rule.FeeScheduleId);
    }
}

internal sealed class SchemeEligibilityRuleConfiguration : IEntityTypeConfiguration<SchemeEligibilityRule>
{
    public void Configure(EntityTypeBuilder<SchemeEligibilityRule> builder)
    {
        builder.ToTable("scheme_eligibility_rules", "schemes");
        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.RuleType).HasMaxLength(100).IsRequired();
        builder.Property(rule => rule.Description).HasMaxLength(500).IsRequired();
        builder.Property(rule => rule.RuleExpressionJson).HasColumnType("jsonb").IsRequired();
        builder.Property(rule => rule.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne(rule => rule.Scheme).WithMany(scheme => scheme.EligibilityRules).HasForeignKey(rule => rule.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(rule => new { rule.SchemeId, rule.RuleType, rule.Status });
    }
}

internal sealed class ApprovedInstrumentRuleConfiguration : IEntityTypeConfiguration<ApprovedInstrumentRule>
{
    public void Configure(EntityTypeBuilder<ApprovedInstrumentRule> builder)
    {
        builder.ToTable("approved_instrument_rules", "schemes");
        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.InstrumentType).HasMaxLength(100).IsRequired();
        builder.Property(rule => rule.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne(rule => rule.Scheme).WithMany(scheme => scheme.ApprovedInstrumentRules).HasForeignKey(rule => rule.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(rule => new { rule.SchemeId, rule.InstrumentType, rule.Status });
    }
}

internal sealed class SchemeRiskProfileConfiguration : IEntityTypeConfiguration<SchemeRiskProfile>
{
    public void Configure(EntityTypeBuilder<SchemeRiskProfile> builder)
    {
        builder.ToTable("scheme_risk_profiles", "schemes");
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.RiskRating).HasMaxLength(100).IsRequired();
        builder.HasOne(profile => profile.Scheme).WithMany(scheme => scheme.RiskProfiles).HasForeignKey(profile => profile.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(profile => profile.SchemeId);
    }
}

internal sealed class LiquidityThresholdConfiguration : IEntityTypeConfiguration<LiquidityThreshold>
{
    public void Configure(EntityTypeBuilder<LiquidityThreshold> builder)
    {
        builder.ToTable("liquidity_thresholds", "schemes");
        builder.HasKey(threshold => threshold.Id);
        builder.HasOne(threshold => threshold.Scheme).WithMany(scheme => scheme.LiquidityThresholds).HasForeignKey(threshold => threshold.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(threshold => threshold.SchemeId);
    }
}

internal sealed class SchemeBankAccountConfiguration : IEntityTypeConfiguration<SchemeBankAccount>
{
    public void Configure(EntityTypeBuilder<SchemeBankAccount> builder)
    {
        builder.ToTable("scheme_bank_accounts", "schemes");
        builder.HasKey(account => account.Id);
        builder.Property(account => account.BankName).HasMaxLength(200).IsRequired();
        builder.Property(account => account.AccountNumber).HasMaxLength(100).IsRequired();
        builder.Property(account => account.AccountName).HasMaxLength(200).IsRequired();
        builder.Property(account => account.Currency).HasMaxLength(3).IsRequired();
        builder.Property(account => account.SwiftCode).HasMaxLength(20);
        builder.HasOne(account => account.Scheme).WithMany(scheme => scheme.BankAccounts).HasForeignKey(account => account.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(account => new { account.SchemeId, account.AccountNumber, account.Currency }).IsUnique();
        builder.HasIndex(account => new { account.Currency, account.IsActive });
    }
}

internal sealed class SchemeCustodianMappingConfiguration : IEntityTypeConfiguration<SchemeCustodianMapping>
{
    public void Configure(EntityTypeBuilder<SchemeCustodianMapping> builder)
    {
        builder.ToTable("scheme_custodian_mappings", "schemes");
        builder.HasKey(mapping => mapping.Id);
        builder.Property(mapping => mapping.CustodianName).HasMaxLength(200).IsRequired();
        builder.Property(mapping => mapping.CustodyAccountReference).HasMaxLength(100).IsRequired();
        builder.Property(mapping => mapping.SettlementAccountReference).HasMaxLength(100).IsRequired();
        builder.HasOne(mapping => mapping.Scheme).WithMany(scheme => scheme.CustodianMappings).HasForeignKey(mapping => mapping.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(mapping => new { mapping.SchemeId, mapping.CustodianName, mapping.CustodyAccountReference }).IsUnique();
    }
}

internal sealed class DistributionRuleConfiguration : IEntityTypeConfiguration<DistributionRule>
{
    public void Configure(EntityTypeBuilder<DistributionRule> builder)
    {
        builder.ToTable("distribution_rules", "schemes");
        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.DistributionFrequency).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne(rule => rule.Scheme).WithMany(scheme => scheme.DistributionRules).HasForeignKey(rule => rule.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(rule => new { rule.SchemeId, rule.IsActive });
    }
}

internal sealed class TemplateMappingConfiguration : IEntityTypeConfiguration<TemplateMapping>
{
    public void Configure(EntityTypeBuilder<TemplateMapping> builder)
    {
        builder.ToTable("template_mappings", "schemes");
        builder.HasKey(mapping => mapping.Id);
        builder.Property(mapping => mapping.TemplateType).HasMaxLength(100).IsRequired();
        builder.Property(mapping => mapping.TemplateCode).HasMaxLength(100).IsRequired();
        builder.HasOne(mapping => mapping.Scheme).WithMany(scheme => scheme.TemplateMappings).HasForeignKey(mapping => mapping.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(mapping => new { mapping.SchemeId, mapping.TemplateType }).IsUnique();
    }
}

internal sealed class SchemeVersionHistoryConfiguration : IEntityTypeConfiguration<SchemeVersionHistory>
{
    public void Configure(EntityTypeBuilder<SchemeVersionHistory> builder)
    {
        builder.ToTable("scheme_version_history", "schemes");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.ChangeType).HasMaxLength(100).IsRequired();
        builder.Property(history => history.ChangedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(history => history.BeforeJson).HasColumnType("jsonb");
        builder.Property(history => history.AfterJson).HasColumnType("jsonb").IsRequired();
        builder.HasOne(history => history.Scheme).WithMany(scheme => scheme.VersionHistory).HasForeignKey(history => history.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(history => new { history.SchemeId, history.VersionNumber });
        builder.HasIndex(history => history.ChangedAtUtc);
    }
}
