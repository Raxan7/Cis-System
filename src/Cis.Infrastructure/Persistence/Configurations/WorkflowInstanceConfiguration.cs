using Cis.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable("workflow_instances", "workflow");

        builder.HasKey(workflow => workflow.Id);

        builder.Property(workflow => workflow.WorkflowType)
            .HasConversion<string>()
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(workflow => workflow.EntityType)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(workflow => workflow.EntityId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(workflow => workflow.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(workflow => workflow.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(workflow => workflow.InitiatedByUserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(workflow => workflow.SubmittedByUserId)
            .HasMaxLength(200);

        builder.Property(workflow => workflow.CheckedByUserId)
            .HasMaxLength(200);

        builder.Property(workflow => workflow.ApprovedByUserId)
            .HasMaxLength(200);

        builder.Property(workflow => workflow.RejectedByUserId)
            .HasMaxLength(200);

        builder.Property(workflow => workflow.DecisionComment)
            .HasMaxLength(1000);

        builder.HasOne(workflow => workflow.ApprovalPolicy)
            .WithMany()
            .HasForeignKey(workflow => workflow.ApprovalPolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(workflow => workflow.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });

        builder.HasIndex(workflow => new { workflow.WorkflowType, workflow.Status });
        builder.HasIndex(workflow => new { workflow.EntityType, workflow.EntityId });
        builder.HasIndex(workflow => workflow.ApprovalPolicyId);

        builder.Metadata.FindNavigation(nameof(WorkflowInstance.Steps))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(WorkflowInstance.Actions))?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
