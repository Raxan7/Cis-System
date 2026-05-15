using Cis.Domain.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class ReportDefinitionConfiguration : IEntityTypeConfiguration<ReportDefinition>
{
    public void Configure(EntityTypeBuilder<ReportDefinition> builder)
    {
        builder.ToTable("report_definitions", "reports");
        builder.HasKey(report => report.Id);
        builder.Property(report => report.Code).HasMaxLength(50).IsRequired();
        builder.Property(report => report.Name).HasMaxLength(200).IsRequired();
        builder.Property(report => report.Category).HasMaxLength(100).IsRequired();
        builder.Property(report => report.Frequency).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(report => report.PrimaryUsers).HasMaxLength(500).IsRequired();
        builder.Property(report => report.RequiredPermission).HasMaxLength(200).IsRequired();
        builder.Property(report => report.CreatedByUserId).HasMaxLength(200).IsRequired();
        builder.HasMany(report => report.Owners).WithOne().HasForeignKey(owner => owner.ReportDefinitionId).OnDelete(DeleteBehavior.Restrict);
        builder.Metadata.FindNavigation(nameof(ReportDefinition.Owners))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(report => report.Code).IsUnique();
        builder.HasIndex(report => new { report.Category, report.Frequency });
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class ReportOwnerMatrixConfiguration : IEntityTypeConfiguration<ReportOwnerMatrix>
{
    public void Configure(EntityTypeBuilder<ReportOwnerMatrix> builder)
    {
        builder.ToTable("report_owner_matrix", "reports");
        builder.HasKey(owner => owner.Id);
        builder.Property(owner => owner.OwnerRole).HasMaxLength(100).IsRequired();
        builder.Property(owner => owner.Responsibility).HasMaxLength(500).IsRequired();
        builder.Property(owner => owner.CreatedByUserId).HasMaxLength(200).IsRequired();
        builder.HasIndex(owner => new { owner.ReportDefinitionId, owner.OwnerRole }).IsUnique();
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class ReportScheduleConfiguration : IEntityTypeConfiguration<ReportSchedule>
{
    public void Configure(EntityTypeBuilder<ReportSchedule> builder)
    {
        builder.ToTable("report_schedules", "reports");
        builder.HasKey(schedule => schedule.Id);
        builder.Property(schedule => schedule.Frequency).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(schedule => schedule.CronExpression).HasMaxLength(100).IsRequired();
        builder.Property(schedule => schedule.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(schedule => schedule.CreatedByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<ReportDefinition>().WithMany().HasForeignKey(schedule => schedule.ReportDefinitionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(schedule => new { schedule.ReportDefinitionId, schedule.Status });
        builder.HasIndex(schedule => schedule.NextRunAtUtc);
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class ReportRunConfiguration : IEntityTypeConfiguration<ReportRun>
{
    public void Configure(EntityTypeBuilder<ReportRun> builder)
    {
        builder.ToTable("report_runs", "reports");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.ReportCode).HasMaxLength(50).IsRequired();
        builder.Property(run => run.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(run => run.GeneratedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(run => run.ApprovedByUserId).HasMaxLength(200);
        builder.Property(run => run.PublishedByUserId).HasMaxLength(200);
        builder.HasOne<ReportDefinition>().WithMany().HasForeignKey(run => run.ReportDefinitionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.Parameters).WithOne().HasForeignKey(parameter => parameter.ReportRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.Outputs).WithOne().HasForeignKey(output => output.ReportRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.Approvals).WithOne().HasForeignKey(approval => approval.ReportRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.Distributions).WithOne().HasForeignKey(distribution => distribution.ReportRunId).OnDelete(DeleteBehavior.Restrict);
        builder.Metadata.FindNavigation(nameof(ReportRun.Parameters))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(ReportRun.Outputs))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(ReportRun.Approvals))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(ReportRun.Distributions))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(run => new { run.ReportCode, run.BusinessDate, run.VersionNumber });
        builder.HasIndex(run => new { run.Status, run.GeneratedAtUtc });
        builder.HasIndex(run => new { run.ReportDefinitionId, run.Status, run.GeneratedAtUtc });
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class ReportParameterConfiguration : IEntityTypeConfiguration<ReportParameter>
{
    public void Configure(EntityTypeBuilder<ReportParameter> builder)
    {
        builder.ToTable("report_parameters", "reports");
        builder.HasKey(parameter => parameter.Id);
        builder.Property(parameter => parameter.Name).HasMaxLength(100).IsRequired();
        builder.Property(parameter => parameter.Value).HasMaxLength(1000).IsRequired();
        builder.HasIndex(parameter => new { parameter.ReportRunId, parameter.Name }).IsUnique();
    }
}

internal sealed class ReportOutputConfiguration : IEntityTypeConfiguration<ReportOutput>
{
    public void Configure(EntityTypeBuilder<ReportOutput> builder)
    {
        builder.ToTable("report_outputs", "reports");
        builder.HasKey(output => output.Id);
        builder.Property(output => output.Format).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(output => output.StorageReference).HasMaxLength(500).IsRequired();
        builder.Property(output => output.ContentHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(output => new { output.ReportRunId, output.Format }).IsUnique();
    }
}

internal sealed class ReportApprovalConfiguration : IEntityTypeConfiguration<ReportApproval>
{
    public void Configure(EntityTypeBuilder<ReportApproval> builder)
    {
        builder.ToTable("report_approvals", "reports");
        builder.HasKey(approval => approval.Id);
        builder.Property(approval => approval.Decision).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(approval => approval.DecidedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(approval => approval.Comment).HasMaxLength(1000);
        builder.HasIndex(approval => new { approval.ReportRunId, approval.Decision });
    }
}

internal sealed class ReportDistributionConfiguration : IEntityTypeConfiguration<ReportDistribution>
{
    public void Configure(EntityTypeBuilder<ReportDistribution> builder)
    {
        builder.ToTable("report_distributions", "reports");
        builder.HasKey(distribution => distribution.Id);
        builder.Property(distribution => distribution.Channel).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(distribution => distribution.Recipient).HasMaxLength(300).IsRequired();
        builder.Property(distribution => distribution.DistributedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(distribution => distribution.Comment).HasMaxLength(1000);
        builder.HasIndex(distribution => new { distribution.ReportRunId, distribution.Channel, distribution.DistributedAtUtc });
    }
}

internal sealed class ReportVersionArchiveConfiguration : IEntityTypeConfiguration<ReportVersionArchive>
{
    public void Configure(EntityTypeBuilder<ReportVersionArchive> builder)
    {
        builder.ToTable("report_version_archives", "reports");
        builder.HasKey(archive => archive.Id);
        builder.Property(archive => archive.ArchivePayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(archive => archive.PayloadHash).HasMaxLength(128).IsRequired();
        builder.Property(archive => archive.ArchivedByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<ReportRun>().WithMany().HasForeignKey(archive => archive.ReportRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(archive => new { archive.ReportRunId, archive.VersionNumber }).IsUnique();
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class ReportBundleConfiguration : IEntityTypeConfiguration<ReportBundle>
{
    public void Configure(EntityTypeBuilder<ReportBundle> builder)
    {
        builder.ToTable("report_bundles", "reports");
        builder.HasKey(bundle => bundle.Id);
        builder.Property(bundle => bundle.BundleCode).HasMaxLength(50).IsRequired();
        builder.Property(bundle => bundle.Name).HasMaxLength(200).IsRequired();
        builder.Property(bundle => bundle.ReportRunIdsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(bundle => bundle.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(bundle => bundle.CreatedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(bundle => bundle.PublishedByUserId).HasMaxLength(200);
        builder.HasIndex(bundle => bundle.BundleCode).IsUnique();
        builder.HasIndex(bundle => new { bundle.Status, bundle.BusinessDate });
        StatutoryLimitConfiguration.Audit(builder);
    }
}
