using Cis.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class WorkflowActionConfiguration : IEntityTypeConfiguration<WorkflowAction>
{
    public void Configure(EntityTypeBuilder<WorkflowAction> builder)
    {
        builder.ToTable("workflow_actions", "workflow");

        builder.HasKey(action => action.Id);

        builder.Property(action => action.ActionType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(action => action.ActorUserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(action => action.Comment)
            .HasMaxLength(1000);

        builder.HasOne(action => action.WorkflowInstance)
            .WithMany(workflow => workflow.Actions)
            .HasForeignKey(action => action.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(action => new { action.WorkflowInstanceId, action.OccurredAtUtc });
        builder.HasIndex(action => new { action.ActionType, action.ActorUserId });
    }
}
