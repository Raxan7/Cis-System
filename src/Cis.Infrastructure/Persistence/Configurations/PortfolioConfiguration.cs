using Cis.Domain.Portfolio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class PortfolioInstrumentConfiguration : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> builder)
    {
        builder.ToTable("instruments", "portfolio");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Isin).HasMaxLength(20).IsRequired();
        builder.Property(i => i.Name).HasMaxLength(200).IsRequired();
        builder.Property(i => i.InstrumentType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(i => i.Currency).HasMaxLength(3).IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasIndex(i => i.Isin).IsUnique();
        builder.OwnsOne(i => i.Audit, audit =>
        {
            audit.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(a => a.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(a => a.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class PortfolioCounterpartyConfiguration : IEntityTypeConfiguration<Counterparty>
{
    public void Configure(EntityTypeBuilder<Counterparty> builder)
    {
        builder.ToTable("counterparties", "portfolio");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Code).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Contact).HasMaxLength(200);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasIndex(c => c.Code).IsUnique();
        builder.OwnsOne(c => c.Audit, audit =>
        {
            audit.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(a => a.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(a => a.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class PortfolioIssuerConfiguration : IEntityTypeConfiguration<Issuer>
{
    public void Configure(EntityTypeBuilder<Issuer> builder)
    {
        builder.ToTable("issuers", "portfolio");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Code).HasMaxLength(50).IsRequired();
        builder.Property(i => i.Name).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Contact).HasMaxLength(200);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasIndex(i => i.Code).IsUnique();
        builder.OwnsOne(i => i.Audit, audit =>
        {
            audit.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(a => a.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(a => a.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class PortfolioPlacementConfiguration : IEntityTypeConfiguration<Placement>
{
    public void Configure(EntityTypeBuilder<Placement> builder)
    {
        builder.ToTable("placements", "portfolio");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.SchemeId).IsRequired();
        builder.Property(p => p.SchemeClassId).IsRequired();
        builder.Property(p => p.InstrumentId).IsRequired();
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(p => p.SettlementStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(p => p.SubmittedByUserId).HasMaxLength(200);
        builder.Property(p => p.ApprovedByUserId).HasMaxLength(200);
        builder.Property(p => p.RejectedByUserId).HasMaxLength(200);
        builder.Property(p => p.RejectionReason).HasMaxLength(1000);
        builder.HasIndex(p => new { p.SchemeId, p.SchemeClassId });
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.MaturityDate);
        builder.HasMany(p => p.IncomeSchedules).WithOne().HasForeignKey(i => i.PlacementId).OnDelete(DeleteBehavior.Restrict);
        builder.Metadata.FindNavigation(nameof(Placement.IncomeSchedules))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.OwnsOne(p => p.Audit, audit =>
        {
            audit.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(a => a.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(a => a.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class PortfolioIncomeScheduleConfiguration : IEntityTypeConfiguration<IncomeSchedule>
{
    public void Configure(EntityTypeBuilder<IncomeSchedule> builder)
    {
        builder.ToTable("income_schedules", "portfolio");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.PlacementId).IsRequired();
        builder.Property(i => i.IncomeType).HasMaxLength(50).IsRequired();
        builder.Property(i => i.Received).IsRequired();
        builder.Property(i => i.ReceivedByUserId).HasMaxLength(200);
        builder.HasIndex(i => new { i.PlacementId, i.DueDate });
        builder.HasIndex(i => new { i.Received, i.DueDate });
    }
}

internal sealed class PortfolioHoldingConfiguration : IEntityTypeConfiguration<PortfolioHolding>
{
    public void Configure(EntityTypeBuilder<PortfolioHolding> builder)
    {
        builder.ToTable("portfolio_holdings", "portfolio");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.SchemeId).IsRequired();
        builder.Property(h => h.SchemeClassId).IsRequired();
        builder.Property(h => h.InstrumentId).IsRequired();
        builder.Property(h => h.Currency).HasMaxLength(3).IsRequired();
        builder.HasIndex(h => new { h.SchemeId, h.SchemeClassId, h.InstrumentId }).IsUnique();
        builder.HasIndex(h => h.LastUpdatedAtUtc);
    }
}

internal sealed class PortfolioInvestmentTransactionConfiguration : IEntityTypeConfiguration<InvestmentTransaction>
{
    public void Configure(EntityTypeBuilder<InvestmentTransaction> builder)
    {
        builder.ToTable("investment_transactions", "portfolio");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.PlacementId).IsRequired();
        builder.Property(t => t.TransactionType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.Currency).HasMaxLength(3).IsRequired();
        builder.Property(t => t.SettlementStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(t => t.Reference).HasMaxLength(50).IsRequired();
        builder.Property(t => t.Notes).HasMaxLength(1000);
        builder.HasIndex(t => new { t.PlacementId, t.TransactionDate });
        builder.HasIndex(t => t.Reference);
        builder.OwnsOne(t => t.Audit, audit =>
        {
            audit.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(a => a.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(a => a.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class PortfolioMandateValidationResultConfiguration : IEntityTypeConfiguration<MandateValidationResult>
{
    public void Configure(EntityTypeBuilder<MandateValidationResult> builder)
    {
        builder.ToTable("mandate_validation_results", "portfolio");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.PlacementId).IsRequired();
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(m => m.ValidatedField).HasMaxLength(100).IsRequired();
        builder.Property(m => m.ValidationMessage).HasMaxLength(500);
        builder.HasIndex(m => new { m.PlacementId, m.Status });
        builder.HasIndex(m => m.ValidatedAtUtc);
    }
}

internal sealed class PortfolioCounterpartyExposureConfiguration : IEntityTypeConfiguration<CounterpartyExposure>
{
    public void Configure(EntityTypeBuilder<CounterpartyExposure> builder)
    {
        builder.ToTable("counterparty_exposures", "portfolio");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.SchemeId).IsRequired();
        builder.Property(e => e.CounterpartyId).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasIndex(e => new { e.SchemeId, e.CounterpartyId }).IsUnique();
        builder.HasIndex(e => e.LastCalculatedAtUtc);
    }
}
