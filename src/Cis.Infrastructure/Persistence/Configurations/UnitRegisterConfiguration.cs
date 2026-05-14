using Cis.Domain.Common;
using Cis.Domain.Investors;
using Cis.Domain.Schemes;
using Cis.Domain.UnitRegister;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class UnitLedgerEntryConfiguration : IEntityTypeConfiguration<UnitLedgerEntry>
{
    public void Configure(EntityTypeBuilder<UnitLedgerEntry> builder)
    {
        builder.ToTable("unit_ledger_entries", "unit_register");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.MovementType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(entry => entry.SourceType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(entry => entry.TransactionReference).HasMaxLength(100).IsRequired();
        builder.Property(entry => entry.PostedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(entry => entry.Narrative).HasMaxLength(1000);
        builder.Ignore(entry => entry.BalanceUnits);
        builder.Ignore(entry => entry.LienUnits);
        ConfigureAudit(builder);

        builder.HasOne<Investor>().WithMany().HasForeignKey(entry => entry.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(entry => entry.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(entry => entry.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitMovementSource>().WithMany().HasForeignKey(entry => entry.SourceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitAdjustment>().WithMany().HasForeignKey(entry => entry.AdjustmentId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entry => entry.TransactionReference).IsUnique();
        builder.HasIndex(entry => new { entry.InvestorId, entry.SchemeId, entry.SchemeClassId, entry.ValuationDate });
        builder.HasIndex(entry => new { entry.SourceType, entry.SourceEntityId, entry.MovementType });
        builder.HasIndex(entry => entry.PostedAtUtc);
    }

    private static void ConfigureAudit<T>(EntityTypeBuilder<T> builder)
        where T : AuditableAggregateRoot
    {
        builder.OwnsOne(entity => entity.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class UnitMovementSourceConfiguration : IEntityTypeConfiguration<UnitMovementSource>
{
    public void Configure(EntityTypeBuilder<UnitMovementSource> builder)
    {
        builder.ToTable("unit_movement_sources", "unit_register");
        builder.HasKey(source => source.Id);
        builder.Property(source => source.SourceType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(source => source.Reference).HasMaxLength(100).IsRequired();
        builder.Property(source => source.VerifiedByUserId).HasMaxLength(200).IsRequired();
        ConfigureAudit(builder);
        builder.HasOne<Investor>().WithMany().HasForeignKey(source => source.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(source => source.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(source => source.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(source => new { source.SourceType, source.SourceId }).IsUnique();
        builder.HasIndex(source => new { source.InvestorId, source.SchemeId, source.SchemeClassId });
        builder.HasIndex(source => source.IsApproved);
    }

    private static void ConfigureAudit<T>(EntityTypeBuilder<T> builder)
        where T : AuditableAggregateRoot
    {
        builder.OwnsOne(entity => entity.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class UnitHoldingConfiguration : IEntityTypeConfiguration<UnitHolding>
{
    public void Configure(EntityTypeBuilder<UnitHolding> builder)
    {
        builder.ToTable("unit_holdings", "unit_register");
        builder.HasKey(holding => holding.Id);
        builder.Property(holding => holding.LastTransactionReference).HasMaxLength(100);
        builder.Ignore(holding => holding.RedeemableUnits);
        ConfigureAudit(builder);
        builder.HasOne<Investor>().WithMany().HasForeignKey(holding => holding.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(holding => holding.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(holding => holding.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(holding => new { holding.InvestorId, holding.SchemeId, holding.SchemeClassId }).IsUnique();
        builder.HasIndex(holding => new { holding.SchemeId, holding.SchemeClassId });
    }

    private static void ConfigureAudit<T>(EntityTypeBuilder<T> builder)
        where T : AuditableAggregateRoot
    {
        builder.OwnsOne(entity => entity.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class InvestorPositionConfiguration : IEntityTypeConfiguration<InvestorPosition>
{
    public void Configure(EntityTypeBuilder<InvestorPosition> builder)
    {
        builder.ToTable("investor_positions", "unit_register");
        builder.HasKey(position => position.Id);
        builder.Property(position => position.LastTransactionReference).HasMaxLength(100);
        ConfigureAudit(builder);
        builder.HasOne<Investor>().WithMany().HasForeignKey(position => position.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(position => position.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(position => position.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(position => new { position.InvestorId, position.SchemeId, position.SchemeClassId }).IsUnique();
        builder.HasIndex(position => position.AsOfDate);
    }

    private static void ConfigureAudit<T>(EntityTypeBuilder<T> builder)
        where T : AuditableAggregateRoot
    {
        builder.OwnsOne(entity => entity.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class UnitRegisterSnapshotConfiguration : IEntityTypeConfiguration<UnitRegisterSnapshot>
{
    public void Configure(EntityTypeBuilder<UnitRegisterSnapshot> builder)
    {
        builder.ToTable("unit_register_snapshots", "unit_register");
        builder.HasKey(snapshot => snapshot.Id);
        builder.Property(snapshot => snapshot.LastTransactionReference).HasMaxLength(100);
        ConfigureAudit(builder);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(snapshot => snapshot.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(snapshot => snapshot.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(snapshot => new { snapshot.SchemeId, snapshot.SchemeClassId, snapshot.SnapshotDate }).IsUnique();
        builder.HasIndex(snapshot => snapshot.SnapshotDate);
    }

    private static void ConfigureAudit<T>(EntityTypeBuilder<T> builder)
        where T : AuditableAggregateRoot
    {
        builder.OwnsOne(entity => entity.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class HistoricalHoldingViewConfiguration : IEntityTypeConfiguration<HistoricalHoldingView>
{
    public void Configure(EntityTypeBuilder<HistoricalHoldingView> builder)
    {
        builder.ToTable("historical_holding_views", "unit_register");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.TransactionReference).HasMaxLength(100).IsRequired();
        ConfigureAudit(builder);
        builder.HasOne<Investor>().WithMany().HasForeignKey(record => record.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(record => record.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(record => record.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitLedgerEntry>().WithMany().HasForeignKey(record => record.UnitLedgerEntryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(record => new { record.InvestorId, record.ValuationDate });
        builder.HasIndex(record => record.TransactionReference);
        builder.HasIndex(record => record.UnitLedgerEntryId).IsUnique();
    }

    private static void ConfigureAudit<T>(EntityTypeBuilder<T> builder)
        where T : AuditableAggregateRoot
    {
        builder.OwnsOne(entity => entity.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class UnitAdjustmentConfiguration : IEntityTypeConfiguration<UnitAdjustment>
{
    public void Configure(EntityTypeBuilder<UnitAdjustment> builder)
    {
        builder.ToTable("unit_adjustments", "unit_register");
        builder.HasKey(adjustment => adjustment.Id);
        builder.Property(adjustment => adjustment.TransactionReference).HasMaxLength(100).IsRequired();
        builder.Property(adjustment => adjustment.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(adjustment => adjustment.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(adjustment => adjustment.RequestedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(adjustment => adjustment.ApprovedByUserId).HasMaxLength(200);
        builder.Property(adjustment => adjustment.DecisionComment).HasMaxLength(1000);
        builder.Property(adjustment => adjustment.IdempotencyKey).HasMaxLength(200);
        ConfigureAudit(builder);
        builder.HasOne<Investor>().WithMany().HasForeignKey(adjustment => adjustment.InvestorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(adjustment => adjustment.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(adjustment => adjustment.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(adjustment => adjustment.TransactionReference).IsUnique();
        builder.HasIndex(adjustment => adjustment.IdempotencyKey).IsUnique().HasFilter("idempotency_key IS NOT NULL");
        builder.HasIndex(adjustment => adjustment.Status);
        builder.HasIndex(adjustment => new { adjustment.InvestorId, adjustment.SchemeId, adjustment.SchemeClassId });
    }

    private static void ConfigureAudit<T>(EntityTypeBuilder<T> builder)
        where T : AuditableAggregateRoot
    {
        builder.OwnsOne(entity => entity.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}
