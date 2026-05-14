using Cis.Domain.Common;

namespace Cis.Domain.Accounting;

public sealed class ChartOfAccounts : AuditableAggregateRoot
{
    private readonly List<Account> _accounts = [];
    private readonly List<JournalTemplate> _journalTemplates = [];

    private ChartOfAccounts()
    {
    }

    private ChartOfAccounts(Guid schemeId, string code, string name, string baseCurrency, string createdByUserId, DateTime createdAtUtc)
    {
        AccountingValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        Code = AccountingValidation.Required(code, nameof(code), 30).ToUpperInvariant();
        Name = AccountingValidation.Required(name, nameof(name), 200);
        BaseCurrency = AccountingValidation.Currency(baseCurrency, nameof(baseCurrency));
        IsActive = true;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid SchemeId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string BaseCurrency { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public IReadOnlyCollection<Account> Accounts => _accounts.AsReadOnly();
    public IReadOnlyCollection<JournalTemplate> JournalTemplates => _journalTemplates.AsReadOnly();

    public static ChartOfAccounts Create(Guid schemeId, string code, string name, string baseCurrency, string createdByUserId, DateTime createdAtUtc)
    {
        return new ChartOfAccounts(schemeId, code, name, baseCurrency, createdByUserId, createdAtUtc);
    }

    public Account AddAccount(string code, string name, AccountType accountType, bool isControlAccount)
    {
        var account = Account.Create(Id, SchemeId, code, name, accountType, isControlAccount);
        if (_accounts.Any(existing => string.Equals(existing.Code, account.Code, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("An account with the same code already exists in this chart of accounts.");
        }

        _accounts.Add(account);
        return account;
    }

    public JournalTemplate AddJournalTemplate(AutomatedJournalType journalType, string name, string debitAccountCode, string creditAccountCode)
    {
        var template = JournalTemplate.Create(Id, SchemeId, journalType, name, debitAccountCode, creditAccountCode);
        _journalTemplates.Add(template);
        return template;
    }
}

public sealed class Account : Entity
{
    private Account()
    {
    }

    private Account(Guid chartOfAccountsId, Guid schemeId, string code, string name, AccountType accountType, bool isControlAccount)
    {
        ChartOfAccountsId = chartOfAccountsId != Guid.Empty ? chartOfAccountsId : throw new ArgumentException("Chart of accounts id cannot be empty.", nameof(chartOfAccountsId));
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        Code = AccountingValidation.Required(code, nameof(code), 30).ToUpperInvariant();
        Name = AccountingValidation.Required(name, nameof(name), 200);
        AccountType = accountType;
        NormalBalance = accountType is AccountType.Asset or AccountType.Expense ? LedgerEntryDirection.Debit : LedgerEntryDirection.Credit;
        IsControlAccount = isControlAccount;
        Status = AccountStatus.Active;
    }

    public Guid ChartOfAccountsId { get; private set; }
    public Guid SchemeId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public AccountType AccountType { get; private set; }
    public LedgerEntryDirection NormalBalance { get; private set; }
    public bool IsControlAccount { get; private set; }
    public AccountStatus Status { get; private set; }

    public static Account Create(Guid chartOfAccountsId, Guid schemeId, string code, string name, AccountType accountType, bool isControlAccount)
    {
        return new Account(chartOfAccountsId, schemeId, code, name, accountType, isControlAccount);
    }
}

public sealed class Ledger : AuditableAggregateRoot
{
    private Ledger()
    {
    }

    private Ledger(Guid schemeId, Guid? schemeClassId, Guid chartOfAccountsId, string code, string name, string currency, string createdByUserId, DateTime createdAtUtc)
    {
        AccountingValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        SchemeClassId = schemeClassId;
        ChartOfAccountsId = chartOfAccountsId != Guid.Empty ? chartOfAccountsId : throw new ArgumentException("Chart of accounts id cannot be empty.", nameof(chartOfAccountsId));
        Code = AccountingValidation.Required(code, nameof(code), 50).ToUpperInvariant();
        Name = AccountingValidation.Required(name, nameof(name), 200);
        Currency = AccountingValidation.Currency(currency, nameof(currency));
        Status = LedgerStatus.Active;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid SchemeId { get; private set; }
    public Guid? SchemeClassId { get; private set; }
    public Guid ChartOfAccountsId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public LedgerStatus Status { get; private set; }

    public static Ledger Create(Guid schemeId, Guid? schemeClassId, Guid chartOfAccountsId, string code, string name, string currency, string createdByUserId, DateTime createdAtUtc)
    {
        return new Ledger(schemeId, schemeClassId, chartOfAccountsId, code, name, currency, createdByUserId, createdAtUtc);
    }
}

public sealed class Journal : AuditableAggregateRoot
{
    private readonly List<JournalLine> _lines = [];

    private Journal()
    {
    }

    private Journal(
        Guid schemeId,
        Guid? schemeClassId,
        Guid ledgerId,
        Guid accountingPeriodId,
        JournalSource source,
        AutomatedJournalType? automatedJournalType,
        BusinessDate postingDate,
        string currency,
        string description,
        string? originatingEventType,
        string? originatingEventId,
        bool isBackdated,
        bool isCorrection,
        Guid? correctedJournalId,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        AccountingValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        SchemeClassId = schemeClassId;
        LedgerId = ledgerId != Guid.Empty ? ledgerId : throw new ArgumentException("Ledger id cannot be empty.", nameof(ledgerId));
        AccountingPeriodId = accountingPeriodId != Guid.Empty ? accountingPeriodId : throw new ArgumentException("Accounting period id cannot be empty.", nameof(accountingPeriodId));
        JournalNumber = $"JRN-{createdAtUtc:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..40].ToUpperInvariant();
        Source = source;
        AutomatedJournalType = automatedJournalType;
        PostingDate = postingDate;
        Currency = AccountingValidation.Currency(currency, nameof(currency));
        Description = AccountingValidation.Required(description, nameof(description), 500);
        OriginatingEventType = AccountingValidation.Optional(originatingEventType, 100);
        OriginatingEventId = AccountingValidation.Optional(originatingEventId, 100);
        IsBackdated = isBackdated;
        IsCorrection = isCorrection;
        CorrectedJournalId = correctedJournalId;
        Status = source == JournalSource.Automated ? JournalStatus.Approved : JournalStatus.Draft;
        CreatedByUserId = AccountingValidation.Required(createdByUserId, nameof(createdByUserId), 200);
        CreatedAtUtc = createdAtUtc;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid SchemeId { get; private set; }
    public Guid? SchemeClassId { get; private set; }
    public Guid LedgerId { get; private set; }
    public Guid AccountingPeriodId { get; private set; }
    public string JournalNumber { get; private set; } = string.Empty;
    public JournalSource Source { get; private set; }
    public AutomatedJournalType? AutomatedJournalType { get; private set; }
    public JournalStatus Status { get; private set; }
    public BusinessDate PostingDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public string Currency { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? OriginatingEventType { get; private set; }
    public string? OriginatingEventId { get; private set; }
    public bool IsBackdated { get; private set; }
    public bool IsCorrection { get; private set; }
    public Guid? CorrectedJournalId { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public string? SubmittedByUserId { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public string? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public string? PostedByUserId { get; private set; }
    public DateTime? PostedAtUtc { get; private set; }
    public string? ApprovalComment { get; private set; }
    public IReadOnlyCollection<JournalLine> Lines => _lines.AsReadOnly();
    public decimal TotalDebits => _lines.Sum(line => line.Debit);
    public decimal TotalCredits => _lines.Sum(line => line.Credit);

    public static Journal CreateManual(
        Guid schemeId,
        Guid? schemeClassId,
        Guid ledgerId,
        Guid accountingPeriodId,
        BusinessDate postingDate,
        string currency,
        string description,
        bool isBackdated,
        bool isCorrection,
        Guid? correctedJournalId,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        return new Journal(schemeId, schemeClassId, ledgerId, accountingPeriodId, isCorrection ? JournalSource.Correction : JournalSource.Manual, null, postingDate, currency, description, null, null, isBackdated, isCorrection, correctedJournalId, createdByUserId, createdAtUtc);
    }

    public static Journal CreateAutomated(
        Guid schemeId,
        Guid? schemeClassId,
        Guid ledgerId,
        Guid accountingPeriodId,
        AutomatedJournalType automatedJournalType,
        BusinessDate postingDate,
        string currency,
        string description,
        string originatingEventType,
        string originatingEventId,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        return new Journal(schemeId, schemeClassId, ledgerId, accountingPeriodId, JournalSource.Automated, automatedJournalType, postingDate, currency, description, AccountingValidation.Required(originatingEventType, nameof(originatingEventType), 100), AccountingValidation.Required(originatingEventId, nameof(originatingEventId), 100), false, false, null, createdByUserId, createdAtUtc);
    }

    public JournalLine AddLine(Guid accountId, string description, decimal debit, decimal credit)
    {
        if (Status is not (JournalStatus.Draft or JournalStatus.Approved))
        {
            throw new InvalidOperationException("Journal lines can only be added before posting.");
        }

        var line = JournalLine.Create(Id, accountId, _lines.Count + 1, description, debit, credit, Currency);
        _lines.Add(line);
        return line;
    }

    public void EnsureBalanced()
    {
        if (_lines.Count < 2)
        {
            throw new InvalidOperationException("A journal must have at least two lines.");
        }

        if (TotalDebits != TotalCredits)
        {
            throw new InvalidOperationException("Journal debits and credits must balance before posting.");
        }
    }

    public void Submit(string submittedByUserId, DateTime submittedAtUtc, string? comment)
    {
        AccountingValidation.EnsureUtc(submittedAtUtc, nameof(submittedAtUtc));
        if (Status != JournalStatus.Draft)
        {
            throw new InvalidOperationException("Only draft journals can be submitted.");
        }

        EnsureBalanced();
        SubmittedByUserId = AccountingValidation.Required(submittedByUserId, nameof(submittedByUserId), 200);
        SubmittedAtUtc = submittedAtUtc;
        ApprovalComment = AccountingValidation.Optional(comment, 1000);
        Status = JournalStatus.Submitted;
    }

    public void ApproveAndPost(string approvedByUserId, DateTime approvedAtUtc, string? comment)
    {
        AccountingValidation.EnsureUtc(approvedAtUtc, nameof(approvedAtUtc));
        if (Status != JournalStatus.Submitted && Status != JournalStatus.Approved)
        {
            throw new InvalidOperationException("Only submitted or approved journals can be posted.");
        }

        var actor = AccountingValidation.Required(approvedByUserId, nameof(approvedByUserId), 200);
        if (SubmittedByUserId is not null && string.Equals(actor, SubmittedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Segregation of duties violation: submitter cannot approve the same journal.");
        }

        EnsureBalanced();
        ApprovedByUserId = actor;
        ApprovedAtUtc = approvedAtUtc;
        PostedByUserId = actor;
        PostedAtUtc = approvedAtUtc;
        ApprovalComment = AccountingValidation.Optional(comment, 1000);
        Status = JournalStatus.Posted;
    }
}

public sealed class JournalLine : Entity
{
    private JournalLine()
    {
    }

    private JournalLine(Guid journalId, Guid accountId, int lineNumber, string description, decimal debit, decimal credit, string currency)
    {
        JournalId = journalId != Guid.Empty ? journalId : throw new ArgumentException("Journal id cannot be empty.", nameof(journalId));
        AccountId = accountId != Guid.Empty ? accountId : throw new ArgumentException("Account id cannot be empty.", nameof(accountId));
        LineNumber = lineNumber > 0 ? lineNumber : throw new ArgumentException("Line number must be positive.", nameof(lineNumber));
        Description = AccountingValidation.Required(description, nameof(description), 500);
        Debit = AccountingValidation.NonNegative(debit, nameof(debit));
        Credit = AccountingValidation.NonNegative(credit, nameof(credit));
        if ((Debit <= 0m && Credit <= 0m) || (Debit > 0m && Credit > 0m))
        {
            throw new ArgumentException("A journal line must contain either a debit or a credit, but not both.");
        }

        Currency = AccountingValidation.Currency(currency, nameof(currency));
    }

    public Guid JournalId { get; private set; }
    public Guid AccountId { get; private set; }
    public int LineNumber { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }
    public string Currency { get; private set; } = string.Empty;

    public static JournalLine Create(Guid journalId, Guid accountId, int lineNumber, string description, decimal debit, decimal credit, string currency)
    {
        return new JournalLine(journalId, accountId, lineNumber, description, debit, credit, currency);
    }
}

public sealed class LedgerEntry : Entity
{
    private LedgerEntry()
    {
    }

    private LedgerEntry(
        Guid journalId,
        Guid journalLineId,
        Guid ledgerId,
        Guid accountId,
        Guid schemeId,
        Guid? schemeClassId,
        BusinessDate postingDate,
        string description,
        decimal debit,
        decimal credit,
        string currency,
        string? originatingEventType,
        string? originatingEventId,
        DateTime postedAtUtc)
    {
        AccountingValidation.EnsureUtc(postedAtUtc, nameof(postedAtUtc));
        JournalId = journalId != Guid.Empty ? journalId : throw new ArgumentException("Journal id cannot be empty.", nameof(journalId));
        JournalLineId = journalLineId != Guid.Empty ? journalLineId : throw new ArgumentException("Journal line id cannot be empty.", nameof(journalLineId));
        LedgerId = ledgerId != Guid.Empty ? ledgerId : throw new ArgumentException("Ledger id cannot be empty.", nameof(ledgerId));
        AccountId = accountId != Guid.Empty ? accountId : throw new ArgumentException("Account id cannot be empty.", nameof(accountId));
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        SchemeClassId = schemeClassId;
        PostingDate = postingDate;
        Description = AccountingValidation.Required(description, nameof(description), 500);
        Debit = AccountingValidation.NonNegative(debit, nameof(debit));
        Credit = AccountingValidation.NonNegative(credit, nameof(credit));
        Currency = AccountingValidation.Currency(currency, nameof(currency));
        OriginatingEventType = AccountingValidation.Optional(originatingEventType, 100);
        OriginatingEventId = AccountingValidation.Optional(originatingEventId, 100);
        PostedAtUtc = postedAtUtc;
    }

    public Guid JournalId { get; private set; }
    public Guid JournalLineId { get; private set; }
    public Guid LedgerId { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid SchemeId { get; private set; }
    public Guid? SchemeClassId { get; private set; }
    public BusinessDate PostingDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public string Description { get; private set; } = string.Empty;
    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string? OriginatingEventType { get; private set; }
    public string? OriginatingEventId { get; private set; }
    public DateTime PostedAtUtc { get; private set; }

    public static LedgerEntry Create(Journal journal, JournalLine line, DateTime postedAtUtc)
    {
        return new LedgerEntry(journal.Id, line.Id, journal.LedgerId, line.AccountId, journal.SchemeId, journal.SchemeClassId, journal.PostingDate, line.Description, line.Debit, line.Credit, line.Currency, journal.OriginatingEventType, journal.OriginatingEventId, postedAtUtc);
    }
}

public sealed class JournalTemplate : Entity
{
    private JournalTemplate()
    {
    }

    private JournalTemplate(Guid chartOfAccountsId, Guid schemeId, AutomatedJournalType journalType, string name, string debitAccountCode, string creditAccountCode)
    {
        ChartOfAccountsId = chartOfAccountsId != Guid.Empty ? chartOfAccountsId : throw new ArgumentException("Chart of accounts id cannot be empty.", nameof(chartOfAccountsId));
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        JournalType = journalType;
        Name = AccountingValidation.Required(name, nameof(name), 100);
        DebitAccountCode = AccountingValidation.Required(debitAccountCode, nameof(debitAccountCode), 30).ToUpperInvariant();
        CreditAccountCode = AccountingValidation.Required(creditAccountCode, nameof(creditAccountCode), 30).ToUpperInvariant();
        IsActive = true;
    }

    public Guid ChartOfAccountsId { get; private set; }
    public Guid SchemeId { get; private set; }
    public AutomatedJournalType JournalType { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string DebitAccountCode { get; private set; } = string.Empty;
    public string CreditAccountCode { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public static JournalTemplate Create(Guid chartOfAccountsId, Guid schemeId, AutomatedJournalType journalType, string name, string debitAccountCode, string creditAccountCode)
    {
        return new JournalTemplate(chartOfAccountsId, schemeId, journalType, name, debitAccountCode, creditAccountCode);
    }
}

public sealed class AccountingPeriod : AuditableAggregateRoot
{
    private AccountingPeriod()
    {
    }

    private AccountingPeriod(Guid schemeId, string name, BusinessDate startDate, BusinessDate endDate, string createdByUserId, DateTime createdAtUtc)
    {
        AccountingValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        Name = AccountingValidation.Required(name, nameof(name), 100);
        StartDate = startDate;
        EndDate = endDate;
        if (EndDate.Value < StartDate.Value)
        {
            throw new ArgumentException("Accounting period end date cannot be before start date.", nameof(endDate));
        }

        Status = AccountingPeriodStatus.Open;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid SchemeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public BusinessDate StartDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public BusinessDate EndDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public AccountingPeriodStatus Status { get; private set; }
    public string? ClosedByUserId { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }

    public static AccountingPeriod Create(Guid schemeId, string name, BusinessDate startDate, BusinessDate endDate, string createdByUserId, DateTime createdAtUtc)
    {
        return new AccountingPeriod(schemeId, name, startDate, endDate, createdByUserId, createdAtUtc);
    }

    public bool Contains(BusinessDate businessDate)
    {
        return businessDate.Value >= StartDate.Value && businessDate.Value <= EndDate.Value;
    }

    public void Close(string closedByUserId, DateTime closedAtUtc)
    {
        AccountingValidation.EnsureUtc(closedAtUtc, nameof(closedAtUtc));
        if (Status != AccountingPeriodStatus.Open)
        {
            throw new InvalidOperationException("Only open accounting periods can be closed.");
        }

        ClosedByUserId = AccountingValidation.Required(closedByUserId, nameof(closedByUserId), 200);
        ClosedAtUtc = closedAtUtc;
        Status = AccountingPeriodStatus.Closed;
    }
}

public sealed class TrialBalance : Entity
{
    private TrialBalance()
    {
    }

    private TrialBalance(Guid schemeId, Guid? schemeClassId, BusinessDate asOfDate, decimal totalDebits, decimal totalCredits, string payloadJson, DateTime generatedAtUtc)
    {
        AccountingValidation.EnsureUtc(generatedAtUtc, nameof(generatedAtUtc));
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        SchemeClassId = schemeClassId;
        AsOfDate = asOfDate;
        TotalDebits = AccountingValidation.NonNegative(totalDebits, nameof(totalDebits));
        TotalCredits = AccountingValidation.NonNegative(totalCredits, nameof(totalCredits));
        PayloadJson = AccountingValidation.Required(payloadJson, nameof(payloadJson));
        GeneratedAtUtc = generatedAtUtc;
    }

    public Guid SchemeId { get; private set; }
    public Guid? SchemeClassId { get; private set; }
    public BusinessDate AsOfDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public decimal TotalDebits { get; private set; }
    public decimal TotalCredits { get; private set; }
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; private set; }

    public static TrialBalance Create(Guid schemeId, Guid? schemeClassId, BusinessDate asOfDate, decimal totalDebits, decimal totalCredits, string payloadJson, DateTime generatedAtUtc)
    {
        return new TrialBalance(schemeId, schemeClassId, asOfDate, totalDebits, totalCredits, payloadJson, generatedAtUtc);
    }
}

public sealed class FinancialStatement : Entity
{
    private FinancialStatement()
    {
    }

    private FinancialStatement(Guid schemeId, Guid? schemeClassId, Guid accountingPeriodId, FinancialStatementType statementType, string payloadJson, DateTime generatedAtUtc)
    {
        AccountingValidation.EnsureUtc(generatedAtUtc, nameof(generatedAtUtc));
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        SchemeClassId = schemeClassId;
        AccountingPeriodId = accountingPeriodId != Guid.Empty ? accountingPeriodId : throw new ArgumentException("Accounting period id cannot be empty.", nameof(accountingPeriodId));
        StatementType = statementType;
        PayloadJson = AccountingValidation.Required(payloadJson, nameof(payloadJson));
        GeneratedAtUtc = generatedAtUtc;
    }

    public Guid SchemeId { get; private set; }
    public Guid? SchemeClassId { get; private set; }
    public Guid AccountingPeriodId { get; private set; }
    public FinancialStatementType StatementType { get; private set; }
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; private set; }

    public static FinancialStatement Create(Guid schemeId, Guid? schemeClassId, Guid accountingPeriodId, FinancialStatementType statementType, string payloadJson, DateTime generatedAtUtc)
    {
        return new FinancialStatement(schemeId, schemeClassId, accountingPeriodId, statementType, payloadJson, generatedAtUtc);
    }
}

public sealed class SuspenseLedgerEntry : Entity
{
    private SuspenseLedgerEntry()
    {
    }

    private SuspenseLedgerEntry(Guid journalId, Guid ledgerEntryId, string reason, DateTime createdAtUtc)
    {
        AccountingValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        JournalId = journalId != Guid.Empty ? journalId : throw new ArgumentException("Journal id cannot be empty.", nameof(journalId));
        LedgerEntryId = ledgerEntryId != Guid.Empty ? ledgerEntryId : throw new ArgumentException("Ledger entry id cannot be empty.", nameof(ledgerEntryId));
        Reason = AccountingValidation.Required(reason, nameof(reason), 500);
        Status = SuspenseResolutionStatus.Open;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid JournalId { get; private set; }
    public Guid LedgerEntryId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public SuspenseResolutionStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static SuspenseLedgerEntry Create(Guid journalId, Guid ledgerEntryId, string reason, DateTime createdAtUtc)
    {
        return new SuspenseLedgerEntry(journalId, ledgerEntryId, reason, createdAtUtc);
    }
}

public sealed class FairValueAdjustment : AuditableAggregateRoot
{
    private FairValueAdjustment()
    {
    }

    private FairValueAdjustment(Guid journalId, Guid schemeId, Guid? schemeClassId, BusinessDate valuationDate, decimal adjustmentAmount, string reason, string createdByUserId, DateTime createdAtUtc)
    {
        AccountingValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        JournalId = journalId != Guid.Empty ? journalId : throw new ArgumentException("Journal id cannot be empty.", nameof(journalId));
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        SchemeClassId = schemeClassId;
        ValuationDate = valuationDate;
        AdjustmentAmount = adjustmentAmount;
        Reason = AccountingValidation.Required(reason, nameof(reason), 1000);
        Status = FairValueAdjustmentStatus.Posted;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid JournalId { get; private set; }
    public Guid SchemeId { get; private set; }
    public Guid? SchemeClassId { get; private set; }
    public BusinessDate ValuationDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public decimal AdjustmentAmount { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public FairValueAdjustmentStatus Status { get; private set; }

    public static FairValueAdjustment Create(Guid journalId, Guid schemeId, Guid? schemeClassId, BusinessDate valuationDate, decimal adjustmentAmount, string reason, string createdByUserId, DateTime createdAtUtc)
    {
        return new FairValueAdjustment(journalId, schemeId, schemeClassId, valuationDate, adjustmentAmount, reason, createdByUserId, createdAtUtc);
    }
}

public sealed class AccountingNavReconciliation : Entity
{
    private AccountingNavReconciliation()
    {
    }

    private AccountingNavReconciliation(Guid schemeId, Guid? schemeClassId, BusinessDate valuationDate, decimal accountingNav, decimal? publishedNav, decimal difference, AccountingNavReconciliationStatus status, DateTime generatedAtUtc)
    {
        AccountingValidation.EnsureUtc(generatedAtUtc, nameof(generatedAtUtc));
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        SchemeClassId = schemeClassId;
        ValuationDate = valuationDate;
        AccountingNav = accountingNav;
        PublishedNav = publishedNav;
        Difference = difference;
        Status = status;
        GeneratedAtUtc = generatedAtUtc;
    }

    public Guid SchemeId { get; private set; }
    public Guid? SchemeClassId { get; private set; }
    public BusinessDate ValuationDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public decimal AccountingNav { get; private set; }
    public decimal? PublishedNav { get; private set; }
    public decimal Difference { get; private set; }
    public AccountingNavReconciliationStatus Status { get; private set; }
    public DateTime GeneratedAtUtc { get; private set; }

    public static AccountingNavReconciliation Create(Guid schemeId, Guid? schemeClassId, BusinessDate valuationDate, decimal accountingNav, decimal? publishedNav, decimal difference, AccountingNavReconciliationStatus status, DateTime generatedAtUtc)
    {
        return new AccountingNavReconciliation(schemeId, schemeClassId, valuationDate, accountingNav, publishedNav, difference, status, generatedAtUtc);
    }
}
