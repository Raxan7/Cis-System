using Cis.Domain.Archive;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class RetentionPolicyConfiguration : IEntityTypeConfiguration<RetentionPolicy>
{
    public void Configure(EntityTypeBuilder<RetentionPolicy> builder)
    {
        builder.ToTable("retention_policies", "archive");

        builder.HasKey(policy => policy.Id);

        builder.Property(policy => policy.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(policy => policy.Module)
            .HasMaxLength(100)
            .IsRequired();

        builder.OwnsOne(policy => policy.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });

        builder.HasIndex(policy => new { policy.Module, policy.Name }).IsUnique();
        builder.HasIndex(policy => policy.IsActive);
    }
}
