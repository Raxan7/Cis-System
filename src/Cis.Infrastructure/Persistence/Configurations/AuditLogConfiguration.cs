using Cis.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs", "audit");

        builder.HasKey(auditLog => auditLog.Id);

        builder.Property(auditLog => auditLog.Module)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(auditLog => auditLog.EventType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(auditLog => auditLog.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(auditLog => auditLog.EntityName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(auditLog => auditLog.EntityId)
            .HasMaxLength(100);

        builder.Property(auditLog => auditLog.ActorId)
            .HasMaxLength(200);

        builder.Property(auditLog => auditLog.ActorDisplayName)
            .HasMaxLength(200);

        builder.Property(auditLog => auditLog.ActorRole)
            .HasMaxLength(500);

        builder.Property(auditLog => auditLog.OccurredAtUtc)
            .IsRequired();

        builder.Property(auditLog => auditLog.CorrelationId)
            .HasMaxLength(100);

        builder.Property(auditLog => auditLog.IpAddress)
            .HasMaxLength(100);

        builder.Property(auditLog => auditLog.UserAgent)
            .HasMaxLength(500);

        builder.Property(auditLog => auditLog.Summary)
            .HasMaxLength(1000);

        builder.Property(auditLog => auditLog.ChangesJson)
            .HasColumnType("jsonb");

        builder.Property(auditLog => auditLog.BeforeJson)
            .HasColumnType("jsonb");

        builder.Property(auditLog => auditLog.AfterJson)
            .HasColumnType("jsonb");

        builder.Property(auditLog => auditLog.Reason)
            .HasMaxLength(1000);

        builder.Property(auditLog => auditLog.IdempotencyKey)
            .HasMaxLength(200);

        builder.Ignore(auditLog => auditLog.EntityType);
        builder.Ignore(auditLog => auditLog.ActorUserId);
        builder.Ignore(auditLog => auditLog.TimestampUtc);

        builder.HasIndex(auditLog => new { auditLog.Module, auditLog.OccurredAtUtc });
        builder.HasIndex(auditLog => new { auditLog.Module, auditLog.EventType, auditLog.OccurredAtUtc });
        builder.HasIndex(auditLog => new { auditLog.EntityName, auditLog.EntityId });
        builder.HasIndex(auditLog => new { auditLog.EntityName, auditLog.OccurredAtUtc });
        builder.HasIndex(auditLog => new { auditLog.ActorId, auditLog.OccurredAtUtc });
        builder.HasIndex(auditLog => auditLog.CorrelationId);
        builder.HasIndex(auditLog => auditLog.EventType);
        builder.HasIndex(auditLog => auditLog.WorkflowId);
        builder.HasIndex(auditLog => auditLog.IdempotencyKey)
            .HasFilter("idempotency_key IS NOT NULL");
    }
}
