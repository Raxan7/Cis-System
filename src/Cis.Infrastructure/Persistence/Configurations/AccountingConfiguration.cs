using Cis.Domain.Accounting;
using Cis.Domain.NAV;
using Cis.Domain.Schemes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class ChartOfAccountsConfiguration : IEntityTypeConfiguration<ChartOfAccounts>
{
    public void Configure(EntityTypeBuilder<ChartOfAccounts> builder)
    {
        builder.ToTable("chart_of_accounts", "accounting");
        builder.HasKey(chart => chart.Id);
        builder.Property(chart => chart.Code).HasMaxLength(30).IsRequired();
        builder.Property(chart => chart.Name).HasMaxLength(200).IsRequired();
        builder.Property(chart => chart.BaseCurrency).HasMaxLength(3).IsRequired();
        builder.HasOne<Scheme>().WithMany().HasForeignKey(chart => chart.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(chart => new { chart.SchemeId, chart.Code }).IsUnique();
        builder.HasMany(chart => chart.Accounts).WithOne().HasForeignKey(account => account.ChartOfAccountsId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(chart => chart.JournalTemplates).WithOne().HasForeignKey(template => template.ChartOfAccountsId).OnDelete(DeleteBehavior.Restrict);
        builder.Metadata.FindNavigation(nameof(ChartOfAccounts.Accounts))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata.FindNavigation(nameof(ChartOfAccounts.JournalTemplates))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.OwnsOne(chart => chart.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts", "accounting");
        builder.HasKey(account => account.Id);
        builder.Property(account => account.Code).HasMaxLength(30).IsRequired();
        builder.Property(account => account.Name).HasMaxLength(200).IsRequired();
        builder.Property(account => account.AccountType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(account => account.NormalBalance).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(account => account.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne<Scheme>().WithMany().HasForeignKey(account => account.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(account => new { account.ChartOfAccountsId, account.Code }).IsUnique();
        builder.HasIndex(account => new { account.SchemeId, account.AccountType });
    }
}

internal sealed class LedgerConfiguration : IEntityTypeConfiguration<Ledger>
{
    public void Configure(EntityTypeBuilder<Ledger> builder)
    {
        builder.ToTable("ledgers", "accounting");
        builder.HasKey(ledger => ledger.Id);
        builder.Property(ledger => ledger.Code).HasMaxLength(50).IsRequired();
        builder.Property(ledger => ledger.Name).HasMaxLength(200).IsRequired();
        builder.Property(ledger => ledger.Currency).HasMaxLength(3).IsRequired();
        builder.Property(ledger => ledger.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne<Scheme>().WithMany().HasForeignKey(ledger => ledger.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(ledger => ledger.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ChartOfAccounts>().WithMany().HasForeignKey(ledger => ledger.ChartOfAccountsId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(ledger => new { ledger.SchemeId, ledger.SchemeClassId }).IsUnique();
        builder.HasIndex(ledger => new { ledger.SchemeId, ledger.Code }).IsUnique();
        builder.OwnsOne(ledger => ledger.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class JournalConfiguration : IEntityTypeConfiguration<Journal>
{
    public void Configure(EntityTypeBuilder<Journal> builder)
    {
        builder.ToTable("journals", "accounting");
        builder.HasKey(journal => journal.Id);
        builder.Property(journal => journal.JournalNumber).HasMaxLength(40).IsRequired();
        builder.Property(journal => journal.Source).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(journal => journal.AutomatedJournalType).HasConversion<string>().HasMaxLength(50);
        builder.Property(journal => journal.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(journal => journal.Currency).HasMaxLength(3).IsRequired();
        builder.Property(journal => journal.Description).HasMaxLength(500).IsRequired();
        builder.Property(journal => journal.OriginatingEventType).HasMaxLength(100);
        builder.Property(journal => journal.OriginatingEventId).HasMaxLength(100);
        builder.Property(journal => journal.CreatedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(journal => journal.SubmittedByUserId).HasMaxLength(200);
        builder.Property(journal => journal.ApprovedByUserId).HasMaxLength(200);
        builder.Property(journal => journal.PostedByUserId).HasMaxLength(200);
        builder.Property(journal => journal.ApprovalComment).HasMaxLength(1000);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(journal => journal.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(journal => journal.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Ledger>().WithMany().HasForeignKey(journal => journal.LedgerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AccountingPeriod>().WithMany().HasForeignKey(journal => journal.AccountingPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Journal>().WithMany().HasForeignKey(journal => journal.CorrectedJournalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(journal => journal.JournalNumber).IsUnique();
        builder.HasIndex(journal => new { journal.SchemeId, journal.PostingDate });
        builder.HasIndex(journal => new { journal.OriginatingEventType, journal.OriginatingEventId });
        builder.HasMany(journal => journal.Lines).WithOne().HasForeignKey(line => line.JournalId).OnDelete(DeleteBehavior.Restrict);
        builder.Metadata.FindNavigation(nameof(Journal.Lines))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.OwnsOne(journal => journal.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class JournalLineConfiguration : IEntityTypeConfiguration<JournalLine>
{
    public void Configure(EntityTypeBuilder<JournalLine> builder)
    {
        builder.ToTable("journal_lines", "accounting");
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Description).HasMaxLength(500).IsRequired();
        builder.Property(line => line.Currency).HasMaxLength(3).IsRequired();
        builder.HasOne<Account>().WithMany().HasForeignKey(line => line.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(line => new { line.JournalId, line.LineNumber }).IsUnique();
        builder.HasIndex(line => line.AccountId);
    }
}

internal sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("ledger_entries", "accounting");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Description).HasMaxLength(500).IsRequired();
        builder.Property(entry => entry.Currency).HasMaxLength(3).IsRequired();
        builder.Property(entry => entry.OriginatingEventType).HasMaxLength(100);
        builder.Property(entry => entry.OriginatingEventId).HasMaxLength(100);
        builder.HasOne<Journal>().WithMany().HasForeignKey(entry => entry.JournalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<JournalLine>().WithMany().HasForeignKey(entry => entry.JournalLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Ledger>().WithMany().HasForeignKey(entry => entry.LedgerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Account>().WithMany().HasForeignKey(entry => entry.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(entry => entry.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(entry => entry.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entry => new { entry.SchemeId, entry.SchemeClassId, entry.PostingDate });
        builder.HasIndex(entry => new { entry.AccountId, entry.PostingDate });
        builder.HasIndex(entry => new { entry.OriginatingEventType, entry.OriginatingEventId });
        builder.HasIndex(entry => entry.JournalLineId).IsUnique();
    }
}

internal sealed class JournalTemplateConfiguration : IEntityTypeConfiguration<JournalTemplate>
{
    public void Configure(EntityTypeBuilder<JournalTemplate> builder)
    {
        builder.ToTable("journal_templates", "accounting");
        builder.HasKey(template => template.Id);
        builder.Property(template => template.JournalType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(template => template.Name).HasMaxLength(100).IsRequired();
        builder.Property(template => template.DebitAccountCode).HasMaxLength(30).IsRequired();
        builder.Property(template => template.CreditAccountCode).HasMaxLength(30).IsRequired();
        builder.HasOne<Scheme>().WithMany().HasForeignKey(template => template.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(template => new { template.ChartOfAccountsId, template.JournalType }).IsUnique();
    }
}

internal sealed class AccountingPeriodConfiguration : IEntityTypeConfiguration<AccountingPeriod>
{
    public void Configure(EntityTypeBuilder<AccountingPeriod> builder)
    {
        builder.ToTable("accounting_periods", "accounting");
        builder.HasKey(period => period.Id);
        builder.Property(period => period.Name).HasMaxLength(100).IsRequired();
        builder.Property(period => period.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(period => period.ClosedByUserId).HasMaxLength(200);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(period => period.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(period => new { period.SchemeId, period.StartDate, period.EndDate }).IsUnique();
        builder.HasIndex(period => new { period.SchemeId, period.Status });
        builder.OwnsOne(period => period.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class TrialBalanceConfiguration : IEntityTypeConfiguration<TrialBalance>
{
    public void Configure(EntityTypeBuilder<TrialBalance> builder)
    {
        builder.ToTable("trial_balances", "accounting");
        builder.HasKey(trialBalance => trialBalance.Id);
        builder.Property(trialBalance => trialBalance.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.HasOne<Scheme>().WithMany().HasForeignKey(trialBalance => trialBalance.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(trialBalance => trialBalance.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(trialBalance => new { trialBalance.SchemeId, trialBalance.SchemeClassId, trialBalance.AsOfDate });
    }
}

internal sealed class FinancialStatementConfiguration : IEntityTypeConfiguration<FinancialStatement>
{
    public void Configure(EntityTypeBuilder<FinancialStatement> builder)
    {
        builder.ToTable("financial_statements", "accounting");
        builder.HasKey(statement => statement.Id);
        builder.Property(statement => statement.StatementType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(statement => statement.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.HasOne<Scheme>().WithMany().HasForeignKey(statement => statement.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(statement => statement.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AccountingPeriod>().WithMany().HasForeignKey(statement => statement.AccountingPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(statement => new { statement.SchemeId, statement.AccountingPeriodId, statement.StatementType });
    }
}

internal sealed class SuspenseLedgerEntryConfiguration : IEntityTypeConfiguration<SuspenseLedgerEntry>
{
    public void Configure(EntityTypeBuilder<SuspenseLedgerEntry> builder)
    {
        builder.ToTable("suspense_ledger_entries", "accounting");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Reason).HasMaxLength(500).IsRequired();
        builder.Property(entry => entry.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne<Journal>().WithMany().HasForeignKey(entry => entry.JournalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LedgerEntry>().WithMany().HasForeignKey(entry => entry.LedgerEntryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entry => entry.LedgerEntryId).IsUnique();
        builder.HasIndex(entry => entry.Status);
    }
}

internal sealed class FairValueAdjustmentConfiguration : IEntityTypeConfiguration<FairValueAdjustment>
{
    public void Configure(EntityTypeBuilder<FairValueAdjustment> builder)
    {
        builder.ToTable("fair_value_adjustments", "accounting");
        builder.HasKey(adjustment => adjustment.Id);
        builder.Property(adjustment => adjustment.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(adjustment => adjustment.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne<Journal>().WithMany().HasForeignKey(adjustment => adjustment.JournalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Scheme>().WithMany().HasForeignKey(adjustment => adjustment.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(adjustment => adjustment.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(adjustment => new { adjustment.SchemeId, adjustment.SchemeClassId, adjustment.ValuationDate });
        builder.OwnsOne(adjustment => adjustment.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class AccountingNavReconciliationConfiguration : IEntityTypeConfiguration<AccountingNavReconciliation>
{
    public void Configure(EntityTypeBuilder<AccountingNavReconciliation> builder)
    {
        builder.ToTable("accounting_nav_reconciliations", "accounting");
        builder.HasKey(reconciliation => reconciliation.Id);
        builder.Property(reconciliation => reconciliation.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasOne<Scheme>().WithMany().HasForeignKey(reconciliation => reconciliation.SchemeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SchemeClass>().WithMany().HasForeignKey(reconciliation => reconciliation.SchemeClassId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(reconciliation => new { reconciliation.SchemeId, reconciliation.SchemeClassId, reconciliation.ValuationDate });
        builder.HasIndex(reconciliation => reconciliation.GeneratedAtUtc);
    }
}
