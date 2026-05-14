using Cis.Domain.Cases;
using Cis.Domain.Investors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class ServiceCaseConfiguration : IEntityTypeConfiguration<ServiceCase>
{
    public void Configure(EntityTypeBuilder<ServiceCase> builder)
    {
        builder.ToTable("service_cases", "cases");
        builder.HasKey(serviceCase => serviceCase.Id);
        builder.Property(serviceCase => serviceCase.Id).ValueGeneratedNever();
        builder.Property(serviceCase => serviceCase.CaseNumber).HasMaxLength(40).IsRequired();
        builder.Property(serviceCase => serviceCase.CaseType).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(serviceCase => serviceCase.Category).HasMaxLength(100).IsRequired();
        builder.Property(serviceCase => serviceCase.Subject).HasMaxLength(200).IsRequired();
        builder.Property(serviceCase => serviceCase.Description).HasMaxLength(4000).IsRequired();
        builder.Property(serviceCase => serviceCase.Priority).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(serviceCase => serviceCase.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(serviceCase => serviceCase.OwnerUserId).HasMaxLength(200);
        builder.Property(serviceCase => serviceCase.LoggedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(serviceCase => serviceCase.ResolvedByUserId).HasMaxLength(200);
        builder.Property(serviceCase => serviceCase.ResolutionSummary).HasMaxLength(2000);
        builder.Property(serviceCase => serviceCase.ResolutionEvidenceReference).HasMaxLength(1000);
        builder.HasIndex(serviceCase => serviceCase.CaseNumber).IsUnique();
        builder.HasIndex(serviceCase => serviceCase.Status);
        builder.HasIndex(serviceCase => serviceCase.OwnerUserId);
        builder.HasIndex(serviceCase => serviceCase.SlaTargetAtUtc);
        builder.HasOne<Investor>().WithMany().HasForeignKey(serviceCase => serviceCase.InvestorId).OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(serviceCase => serviceCase.Actions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(serviceCase => serviceCase.Escalations).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(serviceCase => serviceCase.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(serviceCase => serviceCase.Actions).WithOne().HasForeignKey(action => action.ServiceCaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(serviceCase => serviceCase.Escalations).WithOne().HasForeignKey(escalation => escalation.ServiceCaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(serviceCase => serviceCase.StatusHistory).WithOne().HasForeignKey(history => history.ServiceCaseId).OnDelete(DeleteBehavior.Restrict);

        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<ServiceCase> builder)
    {
        builder.OwnsOne(serviceCase => serviceCase.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
{
    public void Configure(EntityTypeBuilder<Complaint> builder)
    {
        builder.ToTable("complaints", "cases");
        builder.HasKey(complaint => complaint.Id);
        builder.Property(complaint => complaint.Id).ValueGeneratedNever();
        builder.Property(complaint => complaint.ComplaintReference).HasMaxLength(50).IsRequired();
        builder.Property(complaint => complaint.Source).HasMaxLength(80).IsRequired();
        builder.Property(complaint => complaint.RegulatoryCategory).HasMaxLength(120);
        builder.HasOne<ServiceCase>().WithOne().HasForeignKey<Complaint>(complaint => complaint.ServiceCaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(complaint => complaint.ComplaintReference).IsUnique();
        builder.HasIndex(complaint => complaint.ServiceCaseId).IsUnique();
        builder.HasIndex(complaint => complaint.ReceivedAtUtc);
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<Complaint> builder)
    {
        builder.OwnsOne(complaint => complaint.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class CaseActionConfiguration : IEntityTypeConfiguration<CaseAction>
{
    public void Configure(EntityTypeBuilder<CaseAction> builder)
    {
        builder.ToTable("case_actions", "cases");
        builder.HasKey(action => action.Id);
        builder.Property(action => action.Id).ValueGeneratedNever();
        builder.Property(action => action.ActionType).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(action => action.Summary).HasMaxLength(2000).IsRequired();
        builder.Property(action => action.EvidenceReference).HasMaxLength(1000);
        builder.Property(action => action.ActionedByUserId).HasMaxLength(200).IsRequired();
        builder.HasIndex(action => new { action.ServiceCaseId, action.ActionedAtUtc });
    }
}

internal sealed class CaseEscalationConfiguration : IEntityTypeConfiguration<CaseEscalation>
{
    public void Configure(EntityTypeBuilder<CaseEscalation> builder)
    {
        builder.ToTable("case_escalations", "cases");
        builder.HasKey(escalation => escalation.Id);
        builder.Property(escalation => escalation.Id).ValueGeneratedNever();
        builder.Property(escalation => escalation.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(escalation => escalation.EscalatedToRole).HasMaxLength(120).IsRequired();
        builder.Property(escalation => escalation.EscalatedToUserId).HasMaxLength(200);
        builder.Property(escalation => escalation.EscalatedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(escalation => escalation.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(escalation => escalation.ResolvedByUserId).HasMaxLength(200);
        builder.HasIndex(escalation => new { escalation.ServiceCaseId, escalation.Status });
        builder.HasIndex(escalation => escalation.EscalatedAtUtc);
    }
}

internal sealed class CaseSlaPolicyConfiguration : IEntityTypeConfiguration<CaseSlaPolicy>
{
    public void Configure(EntityTypeBuilder<CaseSlaPolicy> builder)
    {
        builder.ToTable("case_sla_policies", "cases");
        builder.HasKey(policy => policy.Id);
        builder.Property(policy => policy.Id).ValueGeneratedNever();
        builder.Property(policy => policy.Category).HasMaxLength(100).IsRequired();
        builder.Property(policy => policy.Priority).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.HasIndex(policy => new { policy.Category, policy.Priority, policy.IsActive }).IsUnique();
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<CaseSlaPolicy> builder)
    {
        builder.OwnsOne(policy => policy.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class CaseStatusHistoryConfiguration : IEntityTypeConfiguration<CaseStatusHistory>
{
    public void Configure(EntityTypeBuilder<CaseStatusHistory> builder)
    {
        builder.ToTable("case_status_history", "cases");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id).ValueGeneratedNever();
        builder.Property(history => history.FromStatus).HasConversion<string>().HasMaxLength(40);
        builder.Property(history => history.ToStatus).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(history => history.ChangedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(history => history.Reason).HasMaxLength(2000).IsRequired();
        builder.HasIndex(history => new { history.ServiceCaseId, history.ChangedAtUtc });
    }
}
