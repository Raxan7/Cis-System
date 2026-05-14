using Cis.Domain.FeesTaxDistribution;
using Cis.Domain.Investors;
using Cis.Domain.Schemes;
using Cis.Domain.UnitRegister;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class FeeAccrualRunConfiguration : IEntityTypeConfiguration<FeeAccrualRun>
{
    public void Configure(EntityTypeBuilder<FeeAccrualRun> builder)
    {
        builder.ToTable("fee_accrual_runs", "fees_tax_distribution");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.RunNumber).HasMaxLength(40).IsRequired();
        builder.Property(run => run.FormulaVersion).HasMaxLength(50).IsRequired();
        builder.Property(run => run.DayCountBasis).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(run => run.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(run => run.OwnerUserId).HasMaxLength(200).IsRequired();
        builder.Property(run => run.ApprovedByUserId).HasMaxLength(200);
        ConfigureAudit(builder);

        builder.HasOne<Scheme>().WithMany().HasForeignKey(run => run.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(run => run.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.Calculations).WithOne().HasForeignKey(calculation => calculation.FeeAccrualRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.TaxCalculations).WithOne().HasForeignKey(calculation => calculation.FeeAccrualRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.VatCalculations).WithOne().HasForeignKey(calculation => calculation.FeeAccrualRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.WithholdingTaxCalculations).WithOne().HasForeignKey(calculation => calculation.FeeAccrualRunId).OnDelete(DeleteBehavior.Restrict);
        builder.Metadata.FindNavigation(nameof(FeeAccrualRun.Calculations))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(FeeAccrualRun.TaxCalculations))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(FeeAccrualRun.VatCalculations))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(FeeAccrualRun.WithholdingTaxCalculations))?.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(run => run.RunNumber).IsUnique();
        builder.HasIndex(run => new { run.SchemeId, run.SchemeClassId, run.PeriodStart, run.PeriodEnd }).IsUnique();
        builder.HasIndex(run => run.Status);
    }

    private static void ConfigureAudit(EntityTypeBuilder<FeeAccrualRun> builder)
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

internal sealed class FeeCalculationConfiguration : IEntityTypeConfiguration<FeeCalculation>
{
    public void Configure(EntityTypeBuilder<FeeCalculation> builder)
    {
        builder.ToTable("fee_calculations", "fees_tax_distribution");
        builder.HasKey(calculation => calculation.Id);
        builder.Property(calculation => calculation.FeeType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(calculation => calculation.FormulaCode).HasMaxLength(30).IsRequired();
        builder.Property(calculation => calculation.FormulaVersion).HasMaxLength(50).IsRequired();
        builder.Property(calculation => calculation.InputsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(calculation => calculation.OutputJson).HasColumnType("jsonb").IsRequired();
        builder.Property(calculation => calculation.OwnerUserId).HasMaxLength(200).IsRequired();
        builder.Property(calculation => calculation.ApprovedByUserId).HasMaxLength(200);
        builder.HasIndex(calculation => new { calculation.FeeAccrualRunId, calculation.FeeType, calculation.FormulaCode }).IsUnique();
    }
}

internal sealed class FeeWaiverRequestConfiguration : IEntityTypeConfiguration<FeeWaiverRequest>
{
    public void Configure(EntityTypeBuilder<FeeWaiverRequest> builder)
    {
        builder.ToTable("fee_waiver_requests", "fees_tax_distribution");
        builder.HasKey(waiver => waiver.Id);
        builder.Property(waiver => waiver.FeeType).HasMaxLength(100).IsRequired();
        builder.Property(waiver => waiver.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(waiver => waiver.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(waiver => waiver.RequestedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(waiver => waiver.ApprovedByUserId).HasMaxLength(200);
        builder.Property(waiver => waiver.DecisionComment).HasMaxLength(1000);
        ConfigureAudit(builder);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(waiver => waiver.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(waiver => waiver.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Investor>().WithMany().HasForeignKey(waiver => waiver.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(waiver => new { waiver.SchemeId, waiver.SchemeClassId, waiver.FeeType, waiver.EffectiveFrom, waiver.InvestorId });
        builder.HasIndex(waiver => waiver.Status);
    }

    private static void ConfigureAudit(EntityTypeBuilder<FeeWaiverRequest> builder)
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

internal sealed class TaxRuleConfiguration : IEntityTypeConfiguration<TaxRule>
{
    public void Configure(EntityTypeBuilder<TaxRule> builder)
    {
        builder.ToTable("tax_rules", "fees_tax_distribution");
        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.Jurisdiction).HasMaxLength(100).IsRequired();
        builder.Property(rule => rule.Category).HasMaxLength(100).IsRequired();
        builder.Property(rule => rule.TaxType).HasMaxLength(100).IsRequired();
        builder.Property(rule => rule.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(rule => rule.CreatedByUserId).HasMaxLength(200).IsRequired();
        ConfigureAudit(builder);
        builder.HasIndex(rule => new { rule.Jurisdiction, rule.Category, rule.TaxType, rule.EffectiveFrom }).IsUnique();
        builder.HasIndex(rule => new { rule.Jurisdiction, rule.Category, rule.TaxType, rule.Status });
    }

    private static void ConfigureAudit(EntityTypeBuilder<TaxRule> builder)
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

internal sealed class TaxCalculationConfiguration : IEntityTypeConfiguration<TaxCalculation>
{
    public void Configure(EntityTypeBuilder<TaxCalculation> builder)
    {
        builder.ToTable("tax_calculations", "fees_tax_distribution");
        builder.HasKey(calculation => calculation.Id);
        builder.Property(calculation => calculation.CalculationType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(calculation => calculation.FormulaCode).HasMaxLength(30).IsRequired();
        builder.Property(calculation => calculation.FormulaVersion).HasMaxLength(50).IsRequired();
        builder.Property(calculation => calculation.InputsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(calculation => calculation.OutputJson).HasColumnType("jsonb").IsRequired();
        builder.Property(calculation => calculation.OwnerUserId).HasMaxLength(200).IsRequired();
        builder.Property(calculation => calculation.ApproverUserId).HasMaxLength(200);
        builder.HasOne<TaxRule>().WithMany().HasForeignKey(calculation => calculation.TaxRuleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InvestorDistribution>().WithMany().HasForeignKey(calculation => calculation.InvestorDistributionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(calculation => calculation.FeeAccrualRunId);
        builder.HasIndex(calculation => calculation.InvestorDistributionId);
    }
}

internal sealed class VatCalculationConfiguration : IEntityTypeConfiguration<VatCalculation>
{
    public void Configure(EntityTypeBuilder<VatCalculation> builder)
    {
        builder.ToTable("vat_calculations", "fees_tax_distribution");
        builder.HasKey(calculation => calculation.Id);
        builder.Property(calculation => calculation.FormulaVersion).HasMaxLength(50).IsRequired();
        builder.Property(calculation => calculation.OwnerUserId).HasMaxLength(200).IsRequired();
        builder.Property(calculation => calculation.ApproverUserId).HasMaxLength(200);
        builder.HasOne<FeeCalculation>().WithMany().HasForeignKey(calculation => calculation.FeeCalculationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TaxRule>().WithMany().HasForeignKey(calculation => calculation.TaxRuleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(calculation => calculation.FeeAccrualRunId);
    }
}

internal sealed class WithholdingTaxCalculationConfiguration : IEntityTypeConfiguration<WithholdingTaxCalculation>
{
    public void Configure(EntityTypeBuilder<WithholdingTaxCalculation> builder)
    {
        builder.ToTable("withholding_tax_calculations", "fees_tax_distribution");
        builder.HasKey(calculation => calculation.Id);
        builder.Property(calculation => calculation.FormulaVersion).HasMaxLength(50).IsRequired();
        builder.Property(calculation => calculation.OwnerUserId).HasMaxLength(200).IsRequired();
        builder.Property(calculation => calculation.ApproverUserId).HasMaxLength(200);
        builder.HasOne<FeeCalculation>().WithMany().HasForeignKey(calculation => calculation.FeeCalculationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TaxRule>().WithMany().HasForeignKey(calculation => calculation.TaxRuleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(calculation => calculation.FeeAccrualRunId);
    }
}

internal sealed class DistributionDeclarationConfiguration : IEntityTypeConfiguration<DistributionDeclaration>
{
    public void Configure(EntityTypeBuilder<DistributionDeclaration> builder)
    {
        builder.ToTable("distribution_declarations", "fees_tax_distribution");
        builder.HasKey(declaration => declaration.Id);
        builder.Property(declaration => declaration.FormulaVersion).HasMaxLength(50).IsRequired();
        builder.Property(declaration => declaration.CoverageOverrideReason).HasMaxLength(1000);
        builder.Property(declaration => declaration.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(declaration => declaration.CreatedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(declaration => declaration.ApprovedByUserId).HasMaxLength(200);
        builder.Property(declaration => declaration.ApprovalComment).HasMaxLength(1000);
        ConfigureAudit(builder);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(declaration => declaration.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(declaration => declaration.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(declaration => new { declaration.SchemeId, declaration.SchemeClassId, declaration.RecordDate }).IsUnique();
        builder.HasIndex(declaration => declaration.Status);
    }

    private static void ConfigureAudit(EntityTypeBuilder<DistributionDeclaration> builder)
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

internal sealed class DistributionRunConfiguration : IEntityTypeConfiguration<DistributionRun>
{
    public void Configure(EntityTypeBuilder<DistributionRun> builder)
    {
        builder.ToTable("distribution_runs", "fees_tax_distribution");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.RunNumber).HasMaxLength(40).IsRequired();
        builder.Property(run => run.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(run => run.CreatedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(run => run.PublishedByUserId).HasMaxLength(200);
        ConfigureAudit(builder);
        builder.HasOne<DistributionDeclaration>().WithMany().HasForeignKey(run => run.DeclarationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(run => run.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(run => run.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.InvestorDistributions).WithOne().HasForeignKey(distribution => distribution.DistributionRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.ReinvestmentInstructions).WithOne().HasForeignKey(instruction => instruction.DistributionRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.ReinvestmentUnitAllocations).WithOne().HasForeignKey(allocation => allocation.DistributionRunId).OnDelete(DeleteBehavior.Restrict);
        builder.Metadata.FindNavigation(nameof(DistributionRun.InvestorDistributions))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(DistributionRun.ReinvestmentInstructions))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(DistributionRun.ReinvestmentUnitAllocations))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(run => run.RunNumber).IsUnique();
        builder.HasIndex(run => run.DeclarationId).IsUnique();
        builder.HasIndex(run => run.Status);
    }

    private static void ConfigureAudit(EntityTypeBuilder<DistributionRun> builder)
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

internal sealed class InvestorDistributionConfiguration : IEntityTypeConfiguration<InvestorDistribution>
{
    public void Configure(EntityTypeBuilder<InvestorDistribution> builder)
    {
        builder.ToTable("investor_distributions", "fees_tax_distribution");
        builder.HasKey(distribution => distribution.Id);
        builder.Property(distribution => distribution.Method).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(distribution => distribution.FormulaVersion).HasMaxLength(50).IsRequired();
        builder.Property(distribution => distribution.OwnerUserId).HasMaxLength(200).IsRequired();
        builder.Property(distribution => distribution.ApproverUserId).HasMaxLength(200);
        builder.HasOne<Investor>().WithMany().HasForeignKey(distribution => distribution.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TaxRule>().WithMany().HasForeignKey(distribution => distribution.TaxRuleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(distribution => new { distribution.DistributionRunId, distribution.InvestorId }).IsUnique();
        builder.HasIndex(distribution => distribution.InvestorId);
    }
}

internal sealed class ReinvestmentInstructionConfiguration : IEntityTypeConfiguration<ReinvestmentInstruction>
{
    public void Configure(EntityTypeBuilder<ReinvestmentInstruction> builder)
    {
        builder.ToTable("reinvestment_instructions", "fees_tax_distribution");
        builder.HasKey(instruction => instruction.Id);
        builder.Property(instruction => instruction.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(instruction => instruction.OwnerUserId).HasMaxLength(200).IsRequired();
        builder.Property(instruction => instruction.ApprovedByUserId).HasMaxLength(200);
        builder.HasOne<Investor>().WithMany().HasForeignKey(instruction => instruction.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(instruction => new { instruction.DistributionRunId, instruction.InvestorId }).IsUnique();
    }
}

internal sealed class ReinvestmentUnitAllocationConfiguration : IEntityTypeConfiguration<ReinvestmentUnitAllocation>
{
    public void Configure(EntityTypeBuilder<ReinvestmentUnitAllocation> builder)
    {
        builder.ToTable("reinvestment_unit_allocations", "fees_tax_distribution");
        builder.HasKey(allocation => allocation.Id);
        builder.Property(allocation => allocation.SourceReference).HasMaxLength(100).IsRequired();
        builder.Property(allocation => allocation.FormulaVersion).HasMaxLength(50).IsRequired();
        builder.HasOne<Investor>().WithMany().HasForeignKey(allocation => allocation.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InvestorDistribution>().WithMany().HasForeignKey(allocation => allocation.InvestorDistributionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ReinvestmentInstruction>().WithMany().HasForeignKey(allocation => allocation.ReinvestmentInstructionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitLedgerEntry>().WithMany().HasForeignKey(allocation => allocation.UnitLedgerEntryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(allocation => allocation.SourceReference).IsUnique();
        builder.HasIndex(allocation => allocation.UnitLedgerEntryId).IsUnique().HasFilter("unit_ledger_entry_id IS NOT NULL");
    }
}
