using Cis.Domain.Dealing;
using Cis.Domain.Investors;
using Cis.Domain.Schemes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class DealingInstructionConfiguration : IEntityTypeConfiguration<DealingInstruction>
{
    public void Configure(EntityTypeBuilder<DealingInstruction> builder)
    {
        builder.ToTable("dealing_instructions", "dealing");
        builder.HasKey(instruction => instruction.Id);
        builder.Property(instruction => instruction.InstructionNumber).HasMaxLength(100).IsRequired();
        builder.Property(instruction => instruction.InstructionType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(instruction => instruction.Channel).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(instruction => instruction.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(instruction => instruction.SubmittedByUserId).HasMaxLength(200);
        builder.Property(instruction => instruction.ApprovedByUserId).HasMaxLength(200);
        builder.Property(instruction => instruction.RejectedByUserId).HasMaxLength(200);
        builder.Property(instruction => instruction.CancelledByUserId).HasMaxLength(200);
        builder.Property(instruction => instruction.DecisionComment).HasMaxLength(1000);

        builder.OwnsOne(instruction => instruction.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });

        builder.HasOne<Investor>().WithMany().HasForeignKey(instruction => instruction.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(instruction => instruction.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(instruction => instruction.SchemeClassId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(instruction => instruction.InstructionNumber).IsUnique();
        builder.HasIndex(instruction => instruction.Status);
        builder.HasIndex(instruction => new { instruction.InvestorId, instruction.SchemeId, instruction.SchemeClassId });
        builder.HasIndex(instruction => instruction.BusinessDate);

        builder.Metadata.FindNavigation(nameof(DealingInstruction.SubscriptionInstructions))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(DealingInstruction.RedemptionInstructions))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(DealingInstruction.SwitchInstructions))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(DealingInstruction.TransferInstructions))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(DealingInstruction.ValidationResults))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(DealingInstruction.CutOffBreaches))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(DealingInstruction.StatusHistory))?.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class SubscriptionInstructionConfiguration : IEntityTypeConfiguration<SubscriptionInstruction>
{
    public void Configure(EntityTypeBuilder<SubscriptionInstruction> builder)
    {
        builder.ToTable("subscription_instructions", "dealing");
        builder.HasKey(subscription => subscription.Id);
        builder.Property(subscription => subscription.Mode).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(subscription => subscription.Currency).HasMaxLength(3).IsRequired();
        builder.Property(subscription => subscription.ConfirmationNumber).HasMaxLength(100);
        builder.HasOne(subscription => subscription.DealingInstruction).WithMany(instruction => instruction.SubscriptionInstructions).HasForeignKey(subscription => subscription.DealingInstructionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(subscription => subscription.DealingInstructionId).IsUnique();
        builder.HasIndex(subscription => new { subscription.FundsCleared, subscription.ApprovedNavAvailable });
    }
}

internal sealed class RedemptionInstructionConfiguration : IEntityTypeConfiguration<RedemptionInstruction>
{
    public void Configure(EntityTypeBuilder<RedemptionInstruction> builder)
    {
        builder.ToTable("redemption_instructions", "dealing");
        builder.HasKey(redemption => redemption.Id);
        builder.Property(redemption => redemption.Mode).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(redemption => redemption.RedemptionAdviceNumber).HasMaxLength(100);
        builder.HasOne(redemption => redemption.DealingInstruction).WithMany(instruction => instruction.RedemptionInstructions).HasForeignKey(redemption => redemption.DealingInstructionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(redemption => redemption.DealingInstructionId).IsUnique();
        builder.HasIndex(redemption => redemption.RequiresApprovalThreshold);
        builder.HasIndex(redemption => redemption.PayoutAuthorized);
    }
}

internal sealed class SwitchInstructionConfiguration : IEntityTypeConfiguration<SwitchInstruction>
{
    public void Configure(EntityTypeBuilder<SwitchInstruction> builder)
    {
        builder.ToTable("switch_instructions", "dealing");
        builder.HasKey(switchInstruction => switchInstruction.Id);
        builder.Property(switchInstruction => switchInstruction.Mode).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(switchInstruction => switchInstruction.OwnershipHistoryJson).HasColumnType("jsonb").IsRequired();
        builder.HasOne(switchInstruction => switchInstruction.DealingInstruction).WithMany(instruction => instruction.SwitchInstructions).HasForeignKey(switchInstruction => switchInstruction.DealingInstructionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(switchInstruction => switchInstruction.TargetSchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(switchInstruction => switchInstruction.TargetSchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(switchInstruction => switchInstruction.DealingInstructionId).IsUnique();
    }
}

internal sealed class TransferInstructionConfiguration : IEntityTypeConfiguration<TransferInstruction>
{
    public void Configure(EntityTypeBuilder<TransferInstruction> builder)
    {
        builder.ToTable("transfer_instructions", "dealing");
        builder.HasKey(transfer => transfer.Id);
        builder.Property(transfer => transfer.OwnershipHistoryJson).HasColumnType("jsonb").IsRequired();
        builder.HasOne(transfer => transfer.DealingInstruction).WithMany(instruction => instruction.TransferInstructions).HasForeignKey(transfer => transfer.DealingInstructionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Investor>().WithMany().HasForeignKey(transfer => transfer.ToInvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(transfer => transfer.DealingInstructionId).IsUnique();
        builder.HasIndex(transfer => transfer.ToInvestorId);
    }
}

internal sealed class LienConfiguration : IEntityTypeConfiguration<Lien>
{
    public void Configure(EntityTypeBuilder<Lien> builder)
    {
        builder.ToTable("liens", "dealing");
        builder.HasKey(lien => lien.Id);
        builder.Property(lien => lien.Currency).HasMaxLength(3).IsRequired();
        builder.Property(lien => lien.DocumentationReference).HasMaxLength(500).IsRequired();
        builder.Property(lien => lien.ApproverEvidenceReference).HasMaxLength(500).IsRequired();
        builder.Property(lien => lien.ReleaseEvidenceReference).HasMaxLength(500);
        builder.Property(lien => lien.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(lien => lien.PlacedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(lien => lien.ReleasedByUserId).HasMaxLength(200);
        builder.OwnsOne(lien => lien.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
        builder.HasOne<Investor>().WithMany().HasForeignKey(lien => lien.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(lien => lien.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(lien => lien.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(lien => new { lien.InvestorId, lien.SchemeId, lien.SchemeClassId, lien.Status });
    }
}

internal sealed class RecurringContributionPlanConfiguration : IEntityTypeConfiguration<RecurringContributionPlan>
{
    public void Configure(EntityTypeBuilder<RecurringContributionPlan> builder)
    {
        builder.ToTable("recurring_contribution_plans", "dealing");
        builder.HasKey(plan => plan.Id);
        builder.Property(plan => plan.Frequency).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(plan => plan.Currency).HasMaxLength(3).IsRequired();
        builder.Property(plan => plan.Channel).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(plan => plan.CollectionMethod).HasMaxLength(100).IsRequired();
        builder.Property(plan => plan.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.OwnsOne(plan => plan.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
        builder.HasOne<Investor>().WithMany().HasForeignKey(plan => plan.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(plan => plan.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(plan => plan.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(plan => new { plan.InvestorId, plan.Status });
        builder.HasIndex(plan => plan.EffectiveFrom);
    }
}

internal sealed class DealingBatchConfiguration : IEntityTypeConfiguration<DealingBatch>
{
    public void Configure(EntityTypeBuilder<DealingBatch> builder)
    {
        builder.ToTable("dealing_batches", "dealing");
        builder.HasKey(batch => batch.Id);
        builder.Property(batch => batch.BatchNumber).HasMaxLength(100).IsRequired();
        builder.Property(batch => batch.Channel).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(batch => batch.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.OwnsOne(batch => batch.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
        builder.HasIndex(batch => batch.BatchNumber).IsUnique();
        builder.HasIndex(batch => new { batch.BusinessDate, batch.Channel });
    }
}

internal sealed class DealingValidationResultConfiguration : IEntityTypeConfiguration<DealingValidationResult>
{
    public void Configure(EntityTypeBuilder<DealingValidationResult> builder)
    {
        builder.ToTable("dealing_validation_results", "dealing");
        builder.HasKey(result => result.Id);
        builder.Property(result => result.RuleCode).HasMaxLength(100).IsRequired();
        builder.Property(result => result.Message).HasMaxLength(1000).IsRequired();
        builder.Property(result => result.Severity).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne(result => result.DealingInstruction).WithMany(instruction => instruction.ValidationResults).HasForeignKey(result => result.DealingInstructionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(result => new { result.DealingInstructionId, result.RuleCode });
        builder.HasIndex(result => new { result.Passed, result.Severity });
    }
}

internal sealed class CutOffBreachConfiguration : IEntityTypeConfiguration<CutOffBreach>
{
    public void Configure(EntityTypeBuilder<CutOffBreach> builder)
    {
        builder.ToTable("cut_off_breaches", "dealing");
        builder.HasKey(breach => breach.Id);
        builder.Property(breach => breach.CutOffTime).HasColumnType("time without time zone").IsRequired();
        builder.Property(breach => breach.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(breach => breach.ApprovedByUserId).HasMaxLength(200);
        builder.HasOne(breach => breach.DealingInstruction).WithMany(instruction => instruction.CutOffBreaches).HasForeignKey(breach => breach.DealingInstructionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(breach => breach.RequiresApproval);
        builder.HasIndex(breach => breach.ReceivedAtUtc);
    }
}

internal sealed class ApprovalThresholdConfiguration : IEntityTypeConfiguration<ApprovalThreshold>
{
    public void Configure(EntityTypeBuilder<ApprovalThreshold> builder)
    {
        builder.ToTable("approval_thresholds", "dealing");
        builder.HasKey(threshold => threshold.Id);
        builder.Property(threshold => threshold.InstructionType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(threshold => threshold.Currency).HasMaxLength(3).IsRequired();
        builder.HasIndex(threshold => new { threshold.InstructionType, threshold.Currency, threshold.IsActive });
    }
}

internal sealed class InstructionStatusHistoryConfiguration : IEntityTypeConfiguration<InstructionStatusHistory>
{
    public void Configure(EntityTypeBuilder<InstructionStatusHistory> builder)
    {
        builder.ToTable("instruction_status_history", "dealing");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(history => history.ChangedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(history => history.Comment).HasMaxLength(1000);
        builder.HasOne(history => history.DealingInstruction).WithMany(instruction => instruction.StatusHistory).HasForeignKey(history => history.DealingInstructionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(history => new { history.DealingInstructionId, history.ChangedAtUtc });
    }
}
