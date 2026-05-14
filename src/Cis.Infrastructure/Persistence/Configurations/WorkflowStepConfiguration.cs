using Cis.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>
{
    public void Configure(EntityTypeBuilder<WorkflowStep> builder)
    {
        builder.ToTable("workflow_steps", "workflow");

        builder.HasKey(step => step.Id);

        builder.Property(step => step.StepType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(step => step.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(step => step.CompletedByUserId)
            .HasMaxLength(200);

        builder.HasOne(step => step.WorkflowInstance)
            .WithMany(workflow => workflow.Steps)
            .HasForeignKey(step => step.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(step => new { step.WorkflowInstanceId, step.StepType }).IsUnique();
        builder.HasIndex(step => new { step.StepType, step.Status });
    }
}
