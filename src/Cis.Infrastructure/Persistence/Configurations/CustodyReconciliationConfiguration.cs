using Cis.Domain.CustodyReconciliation;
using Cis.Domain.Schemes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class CustodianConfiguration : IEntityTypeConfiguration<Custodian>
{
    public void Configure(EntityTypeBuilder<Custodian> builder)
    {
        builder.ToTable("custodians", "custody_reconciliation");
        builder.HasKey(custodian => custodian.Id);
        builder.Property(custodian => custodian.Code).HasMaxLength(50).IsRequired();
        builder.Property(custodian => custodian.Name).HasMaxLength(200).IsRequired();
        builder.Property(custodian => custodian.SwiftCode).HasMaxLength(20);
        builder.Property(custodian => custodian.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(custodian => custodian.CreatedByUserId).HasMaxLength(200).IsRequired();
        builder.HasMany(custodian => custodian.Accounts).WithOne().HasForeignKey(account => account.CustodianId).OnDelete(DeleteBehavior.Restrict);
        builder.Metadata.FindNavigation(nameof(Custodian.Accounts))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(custodian => custodian.Code).IsUnique();
        builder.HasIndex(custodian => custodian.SwiftCode);
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class CustodianAccountConfiguration : IEntityTypeConfiguration<CustodianAccount>
{
    public void Configure(EntityTypeBuilder<CustodianAccount> builder)
    {
        builder.ToTable("custodian_accounts", "custody_reconciliation");
        builder.HasKey(account => account.Id);
        builder.Property(account => account.AccountNumber).HasMaxLength(100).IsRequired();
        builder.Property(account => account.AccountName).HasMaxLength(200).IsRequired();
        builder.Property(account => account.Currency).HasMaxLength(3).IsRequired();
        builder.HasOne<Scheme>().WithMany().HasForeignKey(account => account.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(account => account.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(account => new { account.CustodianId, account.AccountNumber, account.Currency }).IsUnique();
        builder.HasIndex(account => new { account.SchemeId, account.SchemeClassId });
    }
}

internal sealed class CustodianStatementImportConfiguration : IEntityTypeConfiguration<CustodianStatementImport>
{
    public void Configure(EntityTypeBuilder<CustodianStatementImport> builder)
    {
        builder.ToTable("custodian_statement_imports", "custody_reconciliation");
        builder.HasKey(import => import.Id);
        builder.Property(import => import.StatementType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(import => import.SourceFileName).HasMaxLength(200).IsRequired();
        builder.Property(import => import.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(import => import.SourceHash).HasMaxLength(128).IsRequired();
        builder.Property(import => import.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(import => import.ImportedByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<Custodian>().WithMany().HasForeignKey(import => import.CustodianId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CustodianAccount>().WithMany().HasForeignKey(import => import.CustodianAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(import => import.HoldingLines).WithOne().HasForeignKey(line => line.CustodianStatementImportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(import => import.CashLines).WithOne().HasForeignKey(line => line.CustodianStatementImportId).OnDelete(DeleteBehavior.Restrict);
        builder.Metadata.FindNavigation(nameof(CustodianStatementImport.HoldingLines))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(CustodianStatementImport.CashLines))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(import => import.IdempotencyKey).IsUnique();
        builder.HasIndex(import => new { import.CustodianId, import.StatementType, import.StatementDate });
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class CustodianHoldingLineConfiguration : IEntityTypeConfiguration<CustodianHoldingLine>
{
    public void Configure(EntityTypeBuilder<CustodianHoldingLine> builder)
    {
        builder.ToTable("custodian_holding_lines", "custody_reconciliation");
        builder.HasKey(line => line.Id);
        builder.Property(line => line.InstrumentCode).HasMaxLength(100).IsRequired();
        builder.Property(line => line.InstrumentName).HasMaxLength(200).IsRequired();
        builder.Property(line => line.Currency).HasMaxLength(3).IsRequired();
        builder.Property(line => line.SettlementReference).HasMaxLength(100);
        builder.HasOne<Custodian>().WithMany().HasForeignKey(line => line.CustodianId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CustodianAccount>().WithMany().HasForeignKey(line => line.CustodianAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(line => line.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(line => line.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(line => new { line.CustodianStatementImportId, line.SchemeId, line.SchemeClassId, line.InstrumentCode, line.Currency });
    }
}

internal sealed class CustodianCashLineConfiguration : IEntityTypeConfiguration<CustodianCashLine>
{
    public void Configure(EntityTypeBuilder<CustodianCashLine> builder)
    {
        builder.ToTable("custodian_cash_lines", "custody_reconciliation");
        builder.HasKey(line => line.Id);
        builder.Property(line => line.AccountNumber).HasMaxLength(100).IsRequired();
        builder.Property(line => line.Currency).HasMaxLength(3).IsRequired();
        builder.Property(line => line.SettlementReference).HasMaxLength(100);
        builder.HasOne<Custodian>().WithMany().HasForeignKey(line => line.CustodianId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CustodianAccount>().WithMany().HasForeignKey(line => line.CustodianAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(line => line.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(line => new { line.CustodianStatementImportId, line.SchemeId, line.AccountNumber, line.Currency });
    }
}

internal sealed class CustodyReconciliationRunConfiguration : IEntityTypeConfiguration<CustodyReconciliationRun>
{
    public void Configure(EntityTypeBuilder<CustodyReconciliationRun> builder)
    {
        builder.ToTable("custody_reconciliation_runs", "custody_reconciliation");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.SourceDataJson).HasColumnType("jsonb").IsRequired();
        builder.Property(run => run.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(run => run.RunByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<Custodian>().WithMany().HasForeignKey(run => run.CustodianId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CustodianStatementImport>().WithMany().HasForeignKey(run => run.HoldingsImportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CustodianStatementImport>().WithMany().HasForeignKey(run => run.CashImportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.HoldingBreaks).WithOne().HasForeignKey(item => item.ReconciliationRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(run => run.CashBreaks).WithOne().HasForeignKey(item => item.ReconciliationRunId).OnDelete(DeleteBehavior.Restrict);
        builder.Metadata.FindNavigation(nameof(CustodyReconciliationRun.HoldingBreaks))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(CustodyReconciliationRun.CashBreaks))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(run => new { run.CustodianId, run.BusinessDate });
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class HoldingsReconciliationBreakConfiguration : IEntityTypeConfiguration<HoldingsReconciliationBreak>
{
    public void Configure(EntityTypeBuilder<HoldingsReconciliationBreak> builder)
    {
        builder.ToTable("holdings_reconciliation_breaks", "custody_reconciliation");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.InstrumentCode).HasMaxLength(100).IsRequired();
        builder.Property(item => item.BreakType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.Severity).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.OwnerUserId).HasMaxLength(200);
        builder.Property(item => item.ResolvedByUserId).HasMaxLength(200);
        builder.Property(item => item.ResolutionEvidenceReference).HasMaxLength(500);
        builder.Property(item => item.CreatedByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<CustodianHoldingLine>().WithMany().HasForeignKey(item => item.HoldingLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(item => item.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(item => item.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.Status, item.Severity });
        builder.HasIndex(item => new { item.ReconciliationRunId, item.BreakType, item.InstrumentCode, item.HoldingLineId });
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class CashReconciliationBreakConfiguration : IEntityTypeConfiguration<CashReconciliationBreak>
{
    public void Configure(EntityTypeBuilder<CashReconciliationBreak> builder)
    {
        builder.ToTable("cash_reconciliation_breaks", "custody_reconciliation");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.AccountNumber).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Currency).HasMaxLength(3).IsRequired();
        builder.Property(item => item.BreakType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.Severity).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.OwnerUserId).HasMaxLength(200);
        builder.Property(item => item.ResolvedByUserId).HasMaxLength(200);
        builder.Property(item => item.ResolutionEvidenceReference).HasMaxLength(500);
        builder.Property(item => item.CreatedByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<CustodianCashLine>().WithMany().HasForeignKey(item => item.CashLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(item => item.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.Status, item.Severity });
        builder.HasIndex(item => new { item.ReconciliationRunId, item.BreakType, item.AccountNumber, item.CashLineId });
        StatutoryLimitConfiguration.Audit(builder);
    }
}

internal sealed class BreakAgingConfiguration : IEntityTypeConfiguration<BreakAging>
{
    public void Configure(EntityTypeBuilder<BreakAging> builder)
    {
        builder.ToTable("break_aging", "custody_reconciliation");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne<HoldingsReconciliationBreak>().WithMany().HasForeignKey(item => item.HoldingBreakId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CashReconciliationBreak>().WithMany().HasForeignKey(item => item.CashBreakId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.HoldingBreakId, item.CalculatedAtUtc });
        builder.HasIndex(item => new { item.CashBreakId, item.CalculatedAtUtc });
    }
}

internal sealed class BreakActionNoteConfiguration : IEntityTypeConfiguration<BreakActionNote>
{
    public void Configure(EntityTypeBuilder<BreakActionNote> builder)
    {
        builder.ToTable("break_action_notes", "custody_reconciliation");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Note).HasMaxLength(1000).IsRequired();
        builder.Property(item => item.EvidenceReference).HasMaxLength(500);
        builder.Property(item => item.CreatedByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<HoldingsReconciliationBreak>().WithMany().HasForeignKey(item => item.HoldingBreakId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CashReconciliationBreak>().WithMany().HasForeignKey(item => item.CashBreakId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.HoldingBreakId, item.CreatedAtUtc });
        builder.HasIndex(item => new { item.CashBreakId, item.CreatedAtUtc });
    }
}

internal sealed class SafekeepingConfirmationConfiguration : IEntityTypeConfiguration<SafekeepingConfirmation>
{
    public void Configure(EntityTypeBuilder<SafekeepingConfirmation> builder)
    {
        builder.ToTable("safekeeping_confirmations", "custody_reconciliation");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.SettlementReference).HasMaxLength(100);
        builder.Property(item => item.ConfirmationPayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(item => item.GeneratedByUserId).HasMaxLength(200).IsRequired();
        builder.HasOne<Custodian>().WithMany().HasForeignKey(item => item.CustodianId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(item => item.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CustodyReconciliationRun>().WithMany().HasForeignKey(item => item.SourceReconciliationRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.CustodianId, item.SchemeId, item.BusinessDate });
        StatutoryLimitConfiguration.Audit(builder);
    }
}
