using Cis.Domain.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class RtoRpoConfigurationConfiguration : IEntityTypeConfiguration<RtoRpoConfiguration>
{
    public void Configure(EntityTypeBuilder<RtoRpoConfiguration> builder)
    {
        builder.ToTable("rto_rpo_configurations", "operations");
        builder.HasKey(configuration => configuration.Id);
        builder.Property(configuration => configuration.Id).ValueGeneratedNever();
        builder.Property(configuration => configuration.Environment).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(configuration => configuration.SystemName).HasMaxLength(120).IsRequired();
        builder.Property(configuration => configuration.RtoMinutes).IsRequired();
        builder.Property(configuration => configuration.RpoMinutes).IsRequired();
        builder.Property(configuration => configuration.BackupFrequencyMinutes).IsRequired();
        builder.HasIndex(configuration => new { configuration.Environment, configuration.SystemName, configuration.IsActive });
        builder.HasIndex(configuration => new { configuration.Environment, configuration.SystemName, configuration.EffectiveFromUtc }).IsUnique();
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<RtoRpoConfiguration> builder)
    {
        builder.OwnsOne(configuration => configuration.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class DRTestRecordConfiguration : IEntityTypeConfiguration<DRTestRecord>
{
    public void Configure(EntityTypeBuilder<DRTestRecord> builder)
    {
        builder.ToTable("dr_test_records", "operations");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).ValueGeneratedNever();
        builder.Property(record => record.TestName).HasMaxLength(200).IsRequired();
        builder.Property(record => record.Environment).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(record => record.Scenario).HasMaxLength(2000).IsRequired();
        builder.Property(record => record.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(record => record.RequestedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(record => record.EvidenceReference).HasMaxLength(500);
        builder.Property(record => record.Findings).HasMaxLength(2000);
        builder.HasIndex(record => new { record.Environment, record.Status, record.PlannedAtUtc });
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<DRTestRecord> builder)
    {
        builder.OwnsOne(record => record.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class BackupRunRecordConfiguration : IEntityTypeConfiguration<BackupRunRecord>
{
    public void Configure(EntityTypeBuilder<BackupRunRecord> builder)
    {
        builder.ToTable("backup_run_records", "operations");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).ValueGeneratedNever();
        builder.Property(record => record.Environment).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(record => record.BackupType).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(record => record.DatabaseName).HasMaxLength(120).IsRequired();
        builder.Property(record => record.StorageReference).HasMaxLength(500).IsRequired();
        builder.Property(record => record.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(record => record.Sha256).HasMaxLength(128);
        builder.Property(record => record.ErrorMessage).HasMaxLength(1000);
        builder.Property(record => record.InitiatedByUserId).HasMaxLength(200).IsRequired();
        builder.HasIndex(record => new { record.Environment, record.Status, record.StartedAtUtc });
        builder.HasIndex(record => new { record.DatabaseName, record.StartedAtUtc });
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<BackupRunRecord> builder)
    {
        builder.OwnsOne(record => record.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class RestoreTestRecordConfiguration : IEntityTypeConfiguration<RestoreTestRecord>
{
    public void Configure(EntityTypeBuilder<RestoreTestRecord> builder)
    {
        builder.ToTable("restore_test_records", "operations");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).ValueGeneratedNever();
        builder.Property(record => record.Environment).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(record => record.TargetDatabaseName).HasMaxLength(120).IsRequired();
        builder.Property(record => record.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(record => record.EvidenceReference).HasMaxLength(500);
        builder.Property(record => record.ValidationSummary).HasMaxLength(2000);
        builder.Property(record => record.InitiatedByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<BackupRunRecord>().WithMany().HasForeignKey(record => record.BackupRunRecordId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(record => new { record.Environment, record.Status, record.StartedAtUtc });
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<RestoreTestRecord> builder)
    {
        builder.OwnsOne(record => record.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}
