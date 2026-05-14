using Cis.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class ApprovalPolicyConfiguration : IEntityTypeConfiguration<ApprovalPolicy>
{
    public void Configure(EntityTypeBuilder<ApprovalPolicy> builder)
    {
        builder.ToTable("approval_policies", "workflow");

        builder.HasKey(policy => policy.Id);

        builder.Property(policy => policy.WorkflowType)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

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

        builder.HasIndex(policy => policy.WorkflowType)
            .IsUnique()
            .HasFilter("is_active = true");
        builder.HasIndex(policy => policy.Module);
    }
}
