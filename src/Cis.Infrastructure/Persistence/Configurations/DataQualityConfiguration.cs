using Cis.Domain.DataQuality;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class DataQualityRuleConfiguration : IEntityTypeConfiguration<DataQualityRule>
{
    public void Configure(EntityTypeBuilder<DataQualityRule> builder)
    {
        builder.ToTable("data_quality_rules", "data_quality");
        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.Id).ValueGeneratedNever();
        builder.Property(rule => rule.Code).HasConversion<string>().HasMaxLength(100).IsRequired();
        builder.Property(rule => rule.Name).HasMaxLength(200).IsRequired();
        builder.Property(rule => rule.Description).HasMaxLength(1000).IsRequired();
        builder.Property(rule => rule.Severity).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(rule => rule.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.HasIndex(rule => rule.Code).IsUnique();
        builder.OwnsOne(rule => rule.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class DataQualityCheckRunConfiguration : IEntityTypeConfiguration<DataQualityCheckRun>
{
    public void Configure(EntityTypeBuilder<DataQualityCheckRun> builder)
    {
        builder.ToTable("data_quality_check_runs", "data_quality");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.Id).ValueGeneratedNever();
        builder.Property(run => run.RunNumber).HasMaxLength(50).IsRequired();
        builder.Property(run => run.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(run => run.RequestedByUserId).HasMaxLength(200).IsRequired();
        builder.HasIndex(run => run.RunNumber).IsUnique();
        builder.HasIndex(run => new { run.Status, run.StartedAtUtc });
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<DataQualityCheckRun> builder)
    {
        builder.OwnsOne(run => run.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class DataQualityExceptionConfiguration : IEntityTypeConfiguration<DataQualityException>
{
    public void Configure(EntityTypeBuilder<DataQualityException> builder)
    {
        builder.ToTable("data_quality_exceptions", "data_quality");
        builder.HasKey(exception => exception.Id);
        builder.Property(exception => exception.Id).ValueGeneratedNever();
        builder.Property(exception => exception.RuleCode).HasConversion<string>().HasMaxLength(100).IsRequired();
        builder.Property(exception => exception.Severity).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(exception => exception.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(exception => exception.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(exception => exception.Message).HasMaxLength(1000).IsRequired();
        builder.Property(exception => exception.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(exception => exception.OwnerUserId).HasMaxLength(200);
        builder.Property(exception => exception.ResolvedByUserId).HasMaxLength(200);
        builder.Property(exception => exception.ResolutionEvidenceReference).HasMaxLength(500);
        builder.Property(exception => exception.ResolutionComment).HasMaxLength(1000);
        builder.HasOne<DataQualityCheckRun>().WithMany().HasForeignKey(exception => exception.CheckRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DataQualityRule>().WithMany().HasForeignKey(exception => exception.RuleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(exception => new { exception.RuleCode, exception.Status });
        builder.HasIndex(exception => new { exception.EntityType, exception.EntityId });
        builder.HasIndex(exception => exception.DueAtUtc);
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<DataQualityException> builder)
    {
        builder.OwnsOne(exception => exception.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class ExceptionQueueConfiguration : IEntityTypeConfiguration<ExceptionQueue>
{
    public void Configure(EntityTypeBuilder<ExceptionQueue> builder)
    {
        builder.ToTable("exception_queue", "data_quality");
        builder.HasKey(queue => queue.Id);
        builder.Property(queue => queue.Id).ValueGeneratedNever();
        builder.Property(queue => queue.Severity).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(queue => queue.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.HasOne<DataQualityException>().WithOne().HasForeignKey<ExceptionQueue>(queue => queue.DataQualityExceptionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(queue => new { queue.Status, queue.Severity, queue.QueuedAtUtc });
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<ExceptionQueue> builder)
    {
        builder.OwnsOne(queue => queue.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class ExceptionAssignmentConfiguration : IEntityTypeConfiguration<ExceptionAssignment>
{
    public void Configure(EntityTypeBuilder<ExceptionAssignment> builder)
    {
        builder.ToTable("exception_assignments", "data_quality");
        builder.HasKey(assignment => assignment.Id);
        builder.Property(assignment => assignment.Id).ValueGeneratedNever();
        builder.Property(assignment => assignment.OwnerUserId).HasMaxLength(200).IsRequired();
        builder.Property(assignment => assignment.AssignedByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<DataQualityException>().WithMany().HasForeignKey(assignment => assignment.DataQualityExceptionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(assignment => new { assignment.DataQualityExceptionId, assignment.AssignedAtUtc });
    }
}

internal sealed class DataQualityDashboardSnapshotConfiguration : IEntityTypeConfiguration<DataQualityDashboardSnapshot>
{
    public void Configure(EntityTypeBuilder<DataQualityDashboardSnapshot> builder)
    {
        builder.ToTable("data_quality_dashboard_snapshots", "data_quality");
        builder.HasKey(snapshot => snapshot.Id);
        builder.Property(snapshot => snapshot.Id).ValueGeneratedNever();
        builder.HasIndex(snapshot => snapshot.GeneratedAtUtc);
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<DataQualityDashboardSnapshot> builder)
    {
        builder.OwnsOne(snapshot => snapshot.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}
