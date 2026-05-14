using Cis.Domain.Archive;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class ImmutableArchiveRecordConfiguration : IEntityTypeConfiguration<ImmutableArchiveRecord>
{
    public void Configure(EntityTypeBuilder<ImmutableArchiveRecord> builder)
    {
        builder.ToTable("immutable_archive_records", "archive");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.Module)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(record => record.EntityType)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(record => record.EntityId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(record => record.PayloadJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(record => record.PayloadHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(record => record.ArchivedByUserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(record => record.Reason)
            .HasMaxLength(1000)
            .IsRequired();

        builder.HasOne(record => record.RetentionPolicy)
            .WithMany()
            .HasForeignKey(record => record.RetentionPolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(record => new { record.Module, record.ArchivedAtUtc });
        builder.HasIndex(record => new { record.EntityType, record.EntityId });
        builder.HasIndex(record => record.PayloadHash).IsUnique();
        builder.HasIndex(record => record.WorkflowId);
        builder.HasIndex(record => record.RetentionPolicyId);
    }
}
