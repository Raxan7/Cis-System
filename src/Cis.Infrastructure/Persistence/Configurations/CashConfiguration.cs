using Cis.Domain.Cash;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cis.Infrastructure.Persistence.Configurations;

internal sealed class BankStatementImportConfiguration : IEntityTypeConfiguration<BankStatementImport>
{
    public void Configure(EntityTypeBuilder<BankStatementImport> builder)
    {
        builder.ToTable("bank_statement_imports", "cash");
        builder.HasKey(import => import.Id);
        builder.Property(import => import.FileName).HasMaxLength(200).IsRequired();
        builder.Property(import => import.IdempotencyKey).HasMaxLength(200);
        builder.Property(import => import.SourceHash).HasMaxLength(128).IsRequired();
        builder.Property(import => import.ImportedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(import => import.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.HasIndex(import => import.IdempotencyKey).IsUnique().HasFilter("idempotency_key IS NOT NULL");
        builder.HasIndex(import => new { import.SchemeBankAccountId, import.StatementDate });
        builder.HasMany(import => import.Lines).WithOne().HasForeignKey(line => line.BankStatementImportId).OnDelete(DeleteBehavior.Restrict);
        builder.Metadata.FindNavigation(nameof(BankStatementImport.Lines))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.OwnsOne(import => import.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class BankStatementLineConfiguration : IEntityTypeConfiguration<BankStatementLine>
{
    public void Configure(EntityTypeBuilder<BankStatementLine> builder)
    {
        builder.ToTable("bank_statement_lines", "cash");
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Reference).HasMaxLength(100).IsRequired();
        builder.Property(line => line.Description).HasMaxLength(1000);
        builder.Property(line => line.Direction).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(line => line.MatchStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(line => line.RelatedEntityType).HasMaxLength(100);
        builder.Property(line => line.MatchRule).HasConversion<string>().HasMaxLength(100);
        builder.Property(line => line.SchemeBankAccountId).IsRequired();
        builder.HasIndex(line => new { line.BankStatementImportId, line.LineNumber }).IsUnique();
        builder.HasIndex(line => new { line.SchemeBankAccountId, line.InvestorId, line.SchemeId, line.SchemeClassId });
        builder.HasIndex(line => line.MatchStatus);
        builder.HasIndex(line => line.SuspenseItemId);
    }
}

internal sealed class CashBookEntryConfiguration : IEntityTypeConfiguration<CashBookEntry>
{
    public void Configure(EntityTypeBuilder<CashBookEntry> builder)
    {
        builder.ToTable("cash_book_entries", "cash");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Currency).HasMaxLength(3).IsRequired();
        builder.Property(entry => entry.Reference).HasMaxLength(100).IsRequired();
        builder.Property(entry => entry.Narrative).HasMaxLength(1000);
        builder.Property(entry => entry.Direction).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(entry => entry.SourceType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(entry => entry.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(entry => new { entry.SchemeBankAccountId, entry.EntryDate });
        builder.HasIndex(entry => new { entry.SourceType, entry.SourceEntityId });
        builder.HasIndex(entry => entry.Reference);
        builder.OwnsOne(entry => entry.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class CashMatchConfiguration : IEntityTypeConfiguration<CashMatch>
{
    public void Configure(EntityTypeBuilder<CashMatch> builder)
    {
        builder.ToTable("cash_matches", "cash");
        builder.HasKey(match => match.Id);
        builder.Property(match => match.MatchRule).HasConversion<string>().HasMaxLength(100).IsRequired();
        builder.Property(match => match.RelatedEntityType).HasMaxLength(100);
        builder.Property(match => match.MatchedByUserId).HasMaxLength(200).IsRequired();
        builder.HasIndex(match => match.BankStatementLineId).IsUnique();
        builder.HasIndex(match => match.CashBookEntryId);
    }
}

internal sealed class SuspenseItemConfiguration : IEntityTypeConfiguration<SuspenseItem>
{
    public void Configure(EntityTypeBuilder<SuspenseItem> builder)
    {
        builder.ToTable("suspense_items", "cash");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Currency).HasMaxLength(3).IsRequired();
        builder.Property(item => item.Reference).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(item => item.OpenedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(item => item.ResolvedByUserId).HasMaxLength(200);
        builder.Property(item => item.ResolutionComment).HasMaxLength(1000);
        builder.HasIndex(item => new { item.SchemeBankAccountId, item.Status, item.OpenedAtUtc });
        builder.HasIndex(item => item.PaymentInstructionId);
        builder.HasIndex(item => item.CashBookEntryId);
        builder.OwnsOne(item => item.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class PaymentInstructionConfiguration : IEntityTypeConfiguration<PaymentInstruction>
{
    public void Configure(EntityTypeBuilder<PaymentInstruction> builder)
    {
        builder.ToTable("payment_instructions", "cash");
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Currency).HasMaxLength(3).IsRequired();
        builder.Property(payment => payment.Reference).HasMaxLength(100).IsRequired();
        builder.Property(payment => payment.PaymentType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(payment => payment.RequestedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(payment => payment.IdempotencyKey).HasMaxLength(200);
        builder.Property(payment => payment.ExternalReference).HasMaxLength(100);
        builder.Property(payment => payment.FailedReason).HasMaxLength(1000);
        builder.HasIndex(payment => payment.IdempotencyKey).IsUnique().HasFilter("idempotency_key IS NOT NULL");
        builder.HasIndex(payment => new { payment.SchemeBankAccountId, payment.Reference });
        builder.HasIndex(payment => payment.Status);
        builder.HasMany(payment => payment.StatusEvents).WithOne().HasForeignKey(statusEvent => statusEvent.PaymentInstructionId).OnDelete(DeleteBehavior.Restrict);
        builder.Metadata.FindNavigation(nameof(PaymentInstruction.StatusEvents))?.SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.OwnsOne(payment => payment.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class PaymentStatusEventConfiguration : IEntityTypeConfiguration<PaymentStatusEvent>
{
    public void Configure(EntityTypeBuilder<PaymentStatusEvent> builder)
    {
        builder.ToTable("payment_status_events", "cash");
        builder.HasKey(statusEvent => statusEvent.Id);
        builder.Property(statusEvent => statusEvent.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(statusEvent => statusEvent.EventType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(statusEvent => statusEvent.Reason).HasMaxLength(1000);
        builder.Property(statusEvent => statusEvent.ExternalReference).HasMaxLength(100);
        builder.Property(statusEvent => statusEvent.ChangedByUserId).HasMaxLength(200).IsRequired();
        builder.HasIndex(statusEvent => new { statusEvent.PaymentInstructionId, statusEvent.OccurredAtUtc });
    }
}

internal sealed class ReturnedFundConfiguration : IEntityTypeConfiguration<ReturnedFund>
{
    public void Configure(EntityTypeBuilder<ReturnedFund> builder)
    {
        builder.ToTable("returned_funds", "cash");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Currency).HasMaxLength(3).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(item => item.ReturnedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(item => item.ResolutionComment).HasMaxLength(1000);
        builder.HasIndex(item => item.PaymentInstructionId).IsUnique();
        builder.OwnsOne(item => item.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class ReversalRequestConfiguration : IEntityTypeConfiguration<ReversalRequest>
{
    public void Configure(EntityTypeBuilder<ReversalRequest> builder)
    {
        builder.ToTable("reversal_requests", "cash");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(item => item.RequestedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(item => item.ApprovedByUserId).HasMaxLength(200);
        builder.Property(item => item.ApprovalComment).HasMaxLength(1000);
        builder.HasIndex(item => item.PaymentInstructionId).IsUnique();
        builder.OwnsOne(item => item.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}

internal sealed class ReconciliationRunConfiguration : IEntityTypeConfiguration<ReconciliationRun>
{
    public void Configure(EntityTypeBuilder<ReconciliationRun> builder)
    {
        builder.ToTable("reconciliation_runs", "cash");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(item => item.RequestedByUserId).HasMaxLength(200).IsRequired();
        builder.Property(item => item.CompletedByUserId).HasMaxLength(200);
        builder.Property(item => item.Summary).HasMaxLength(1000);
        builder.HasIndex(item => new { item.SchemeBankAccountId, item.RunDate });
        builder.HasIndex(item => item.Status);
        builder.OwnsOne(item => item.Audit, audit =>
        {
            audit.Property(value => value.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
            audit.Property(value => value.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            audit.Property(value => value.LastModifiedBy).HasColumnName("last_modified_by").HasMaxLength(200);
            audit.Property(value => value.LastModifiedAtUtc).HasColumnName("last_modified_at_utc");
        });
    }
}
