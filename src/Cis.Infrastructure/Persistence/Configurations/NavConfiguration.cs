using Cis.Domain.NAV;
using Cis.Domain.Portfolio;
using Cis.Domain.Schemes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class ValuationRunConfiguration : IEntityTypeConfiguration<ValuationRun>
{
    public void Configure(EntityTypeBuilder<ValuationRun> builder)
    {
        builder.ToTable("valuation_runs", "nav");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.RunNumber).HasMaxLength(40).IsRequired();
        builder.Property(run => run.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(run => run.FormulaVersion).HasMaxLength(50).IsRequired();
        builder.Property(run => run.DayCountBasis).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(run => run.PreparedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(run => run.SubmittedByUserId).HasMaxLength(200);
        builder.Property(run => run.CheckedByUserId).HasMaxLength(200);
        builder.Property(run => run.ApprovedByUserId).HasMaxLength(200);
        builder.Property(run => run.PublishedByUserId).HasMaxLength(200);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(run => run.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(run => run.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(run => run.RunNumber).IsUnique();
        builder.HasIndex(run => new { run.SchemeId, run.SchemeClassId, run.ValuationDate });
        builder.HasIndex(run => run.Status);

        builder.HasMany(run => run.Inputs).WithOne().HasForeignKey(input => input.ValuationRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.Sources).WithOne().HasForeignKey(source => source.ValuationRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.InstrumentValuations).WithOne().HasForeignKey(valuation => valuation.ValuationRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.StalePriceExceptions).WithOne().HasForeignKey(exception => exception.ValuationRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.PricingVarianceExceptions).WithOne().HasForeignKey(exception => exception.ValuationRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.Calculations).WithOne().HasForeignKey(calculation => calculation.ValuationRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.NavPerUnits).WithOne().HasForeignKey(navPerUnit => navPerUnit.ValuationRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.Approvals).WithOne().HasForeignKey(approval => approval.ValuationRunId).OnDelete(DeleteBehavior.Restrict);
        builder.Metadata.FindNavigation(nameof(ValuationRun.Inputs))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(ValuationRun.Sources))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(ValuationRun.InstrumentValuations))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(ValuationRun.StalePriceExceptions))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(ValuationRun.PricingVarianceExceptions))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(ValuationRun.Calculations))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(ValuationRun.NavPerUnits))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(ValuationRun.Approvals))?.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsOne(run => run.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class ValuationInputConfiguration : IEntityTypeConfiguration<ValuationInput>
{
    public void Configure(EntityTypeBuilder<ValuationInput> builder)
    {
        builder.ToTable("valuation_inputs", "nav");
        builder.HasKey(input => input.Id);
        builder.Property(input => input.InputType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(input => input.Description).HasMaxLength(200).IsRequired();
        builder.Property(input => input.FormulaCode).HasMaxLength(30).IsRequired();
        builder.Property(input => input.SourceReference).HasMaxLength(200);
        builder.HasIndex(input => new { input.ValuationRunId, input.InputType });
        builder.HasIndex(input => input.FormulaCode);
    }
}

internal sealed class ValuationSourceConfiguration : IEntityTypeConfiguration<ValuationSource>
{
    public void Configure(EntityTypeBuilder<ValuationSource> builder)
    {
        builder.ToTable("valuation_sources", "nav");
        builder.HasKey(source => source.Id);
        builder.Property(source => source.SourceType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(source => source.ProviderName).HasMaxLength(100).IsRequired();
        builder.Property(source => source.SourceReference).HasMaxLength(200).IsRequired();
        builder.HasIndex(source => new { source.ValuationRunId, source.SourceType });
        builder.HasIndex(source => source.ReceivedAtUtc);
    }
}

internal sealed class PriceSourceHierarchyConfiguration : IEntityTypeConfiguration<PriceSourceHierarchy>
{
    public void Configure(EntityTypeBuilder<PriceSourceHierarchy> builder)
    {
        builder.ToTable("price_source_hierarchies", "nav");
        builder.HasKey(source => source.Id);
        builder.Property(source => source.InstrumentType).HasMaxLength(100).IsRequired();
        builder.Property(source => source.PrimarySource).HasMaxLength(100).IsRequired();
        builder.Property(source => source.SecondarySource).HasMaxLength(100).IsRequired();
        builder.HasOne<Scheme>().WithMany().HasForeignKey(source => source.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(source => source.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(source => new { source.SchemeId, source.SchemeClassId, source.InstrumentType, source.IsActive }).IsUnique();
        builder.OwnsOne(source => source.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class InstrumentValuationConfiguration : IEntityTypeConfiguration<InstrumentValuation>
{
    public void Configure(EntityTypeBuilder<InstrumentValuation> builder)
    {
        builder.ToTable("instrument_valuations", "nav");
        builder.HasKey(valuation => valuation.Id);
        builder.Property(valuation => valuation.InstrumentType).HasMaxLength(100).IsRequired();
        builder.Property(valuation => valuation.PriceSource).HasMaxLength(100).IsRequired();
        builder.Property(valuation => valuation.FormulaCode).HasMaxLength(30).IsRequired();
        builder.HasOne<Instrument>().WithMany().HasForeignKey(valuation => valuation.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ManualValuationOverride>().WithMany().HasForeignKey(valuation => valuation.ManualValuationOverrideId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(valuation => new { valuation.ValuationRunId, valuation.InstrumentId }).IsUnique();
        builder.HasIndex(valuation => new { valuation.IsPriceMissing, valuation.IsPriceStale });
    }
}

internal sealed class StalePriceExceptionConfiguration : IEntityTypeConfiguration<StalePriceException>
{
    public void Configure(EntityTypeBuilder<StalePriceException> builder)
    {
        builder.ToTable("stale_price_exceptions", "nav");
        builder.HasKey(exception => exception.Id);
        builder.Property(exception => exception.Message).HasMaxLength(500).IsRequired();
        builder.Property(exception => exception.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne<Instrument>().WithMany().HasForeignKey(exception => exception.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(exception => new { exception.ValuationRunId, exception.Status });
        builder.HasIndex(exception => exception.InstrumentId);
    }
}

internal sealed class PricingVarianceExceptionConfiguration : IEntityTypeConfiguration<PricingVarianceException>
{
    public void Configure(EntityTypeBuilder<PricingVarianceException> builder)
    {
        builder.ToTable("pricing_variance_exceptions", "nav");
        builder.HasKey(exception => exception.Id);
        builder.Property(exception => exception.Message).HasMaxLength(500).IsRequired();
        builder.Property(exception => exception.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne<Instrument>().WithMany().HasForeignKey(exception => exception.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(exception => new { exception.ValuationRunId, exception.Status });
        builder.HasIndex(exception => exception.InstrumentId);
    }
}

internal sealed class ManualValuationOverrideConfiguration : IEntityTypeConfiguration<ManualValuationOverride>
{
    public void Configure(EntityTypeBuilder<ManualValuationOverride> builder)
    {
        builder.ToTable("manual_valuation_overrides", "nav");
        builder.HasKey(manualOverride => manualOverride.Id);
        builder.Property(manualOverride => manualOverride.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(manualOverride => manualOverride.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(manualOverride => manualOverride.RequestedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(manualOverride => manualOverride.ApprovedByUserId).HasMaxLength(200);
        builder.Property(manualOverride => manualOverride.ApprovalComment).HasMaxLength(1000);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(manualOverride => manualOverride.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(manualOverride => manualOverride.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Instrument>().WithMany().HasForeignKey(manualOverride => manualOverride.InstrumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(manualOverride => new { manualOverride.SchemeId, manualOverride.SchemeClassId, manualOverride.ValuationDate, manualOverride.InstrumentId, manualOverride.Status });
        builder.HasIndex(manualOverride => manualOverride.RequestedByUserId);
        builder.OwnsOne(manualOverride => manualOverride.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class NavCalculationConfiguration : IEntityTypeConfiguration<NavCalculation>
{
    public void Configure(EntityTypeBuilder<NavCalculation> builder)
    {
        builder.ToTable("nav_calculations", "nav");
        builder.HasKey(calculation => calculation.Id);
        builder.Property(calculation => calculation.FormulaVersion).HasMaxLength(50).IsRequired();
        builder.Property(calculation => calculation.CalculatedByUserId).HasMaxLength(200).IsRequired();
        builder.HasIndex(calculation => new { calculation.ValuationRunId, calculation.CalculatedAtUtc });
    }
}

internal sealed class NavPerUnitConfiguration : IEntityTypeConfiguration<NavPerUnit>
{
    public void Configure(EntityTypeBuilder<NavPerUnit> builder)
    {
        builder.ToTable("nav_per_units", "nav");
        builder.HasKey(navPerUnit => navPerUnit.Id);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(navPerUnit => navPerUnit.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(navPerUnit => new { navPerUnit.ValuationRunId, navPerUnit.SchemeClassId });
    }
}

internal sealed class NavApprovalConfiguration : IEntityTypeConfiguration<NavApproval>
{
    public void Configure(EntityTypeBuilder<NavApproval> builder)
    {
        builder.ToTable("nav_approvals", "nav");
        builder.HasKey(approval => approval.Id);
        builder.Property(approval => approval.Step).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(approval => approval.ActorUserId).HasMaxLength(200).IsRequired();
        builder.Property(approval => approval.Comment).HasMaxLength(1000);
        builder.HasIndex(approval => new { approval.ValuationRunId, approval.Step });
        builder.HasIndex(approval => approval.ActorUserId);
    }
}

internal sealed class NavPublicationConfiguration : IEntityTypeConfiguration<NavPublication>
{
    public void Configure(EntityTypeBuilder<NavPublication> builder)
    {
        builder.ToTable("nav_publications", "nav");
        builder.HasKey(publication => publication.Id);
        builder.Property(publication => publication.FormulaVersion).HasMaxLength(50).IsRequired();
        builder.Property(publication => publication.PublishedByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<ValuationRun>().WithMany().HasForeignKey(publication => publication.ValuationRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(publication => publication.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(publication => publication.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(publication => new { publication.SchemeId, publication.SchemeClassId, publication.ValuationDate, publication.VersionNumber }).IsUnique();
        builder.HasIndex(publication => publication.PublishedAtUtc);
        builder.OwnsOne(publication => publication.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class NavVersionArchiveConfiguration : IEntityTypeConfiguration<NavVersionArchive>
{
    public void Configure(EntityTypeBuilder<NavVersionArchive> builder)
    {
        builder.ToTable("nav_version_archives", "nav");
        builder.HasKey(archive => archive.Id);
        builder.Property(archive => archive.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(archive => archive.PayloadHash).HasMaxLength(128).IsRequired();
        builder.HasOne<ValuationRun>().WithMany().HasForeignKey(archive => archive.ValuationRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<NavPublication>().WithMany().HasForeignKey(archive => archive.NavPublicationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(archive => new { archive.NavPublicationId, archive.VersionNumber }).IsUnique();
        builder.HasIndex(archive => archive.PayloadHash).IsUnique();
        builder.OwnsOne(archive => archive.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class NavRestatementConfiguration : IEntityTypeConfiguration<NavRestatement>
{
    public void Configure(EntityTypeBuilder<NavRestatement> builder)
    {
        builder.ToTable("nav_restatements", "nav");
        builder.HasKey(restatement => restatement.Id);
        builder.Property(restatement => restatement.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(restatement => restatement.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne<NavPublication>().WithMany().HasForeignKey(restatement => restatement.OriginalPublicationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<NavPublication>().WithMany().HasForeignKey(restatement => restatement.CorrectedPublicationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ValuationRun>().WithMany().HasForeignKey(restatement => restatement.CorrectedValuationRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(restatement => restatement.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(restatement => restatement.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(restatement => new { restatement.SchemeId, restatement.SchemeClassId, restatement.ValuationDate, restatement.CorrectedVersionNumber }).IsUnique();
        builder.OwnsOne(restatement => restatement.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}
