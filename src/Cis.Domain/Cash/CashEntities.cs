using Cis.Domain.Common;

namespace Cis.Domain.Cash;

public sealed class BankStatementImport : AuditableAggregateRoot
{
    private readonly List<BankStatementLine> _lines = [];

    private BankStatementImport()
    {
    }

    private BankStatementImport(
        Guid schemeBankAccountId,
        string fileName,
        DateOnly statementDate,
        int dateToleranceDays,
        string? idempotencyKey,
        string sourceHash,
        string importedByUserId,
        DateTime importedAtUtc)
    {
        SchemeBankAccountId = schemeBankAccountId;
        FileName = CashValidation.Required(fileName, nameof(fileName), 200);
        StatementDate = statementDate;
        DateToleranceDays = dateToleranceDays < 0 ? throw new ArgumentException("Date tolerance cannot be negative.", nameof(dateToleranceDays)) : dateToleranceDays;
        IdempotencyKey = CashValidation.Optional(idempotencyKey, 200);
        SourceHash = CashValidation.Required(sourceHash, nameof(sourceHash), 128);
        ImportedByUserId = CashValidation.Required(importedByUserId, nameof(importedByUserId), 200);
        ImportedAtUtc = importedAtUtc;
        Status = BankStatementImportStatus.Imported;
        MarkCreated(ImportedByUserId, ImportedAtUtc);
    }

    public Guid SchemeBankAccountId { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public DateOnly StatementDate { get; private set; }

    public int DateToleranceDays { get; private set; }

    public string? IdempotencyKey { get; private set; }

    public string SourceHash { get; private set; } = string.Empty;

    public string ImportedByUserId { get; private set; } = string.Empty;

    public DateTime ImportedAtUtc { get; private set; }

    public BankStatementImportStatus Status { get; private set; }

    public int LineCount { get; private set; }

    public int MatchedLineCount { get; private set; }

    public int SuspenseLineCount { get; private set; }

    public IReadOnlyCollection<BankStatementLine> Lines => _lines.AsReadOnly();

    public static BankStatementImport Create(
        Guid schemeBankAccountId,
        string fileName,
        DateOnly statementDate,
        int dateToleranceDays,
        string? idempotencyKey,
        string sourceHash,
        string importedByUserId,
        DateTime importedAtUtc,
        IReadOnlyCollection<BankStatementLine> lines)
    {
        var import = new BankStatementImport(schemeBankAccountId, fileName, statementDate, dateToleranceDays, idempotencyKey, sourceHash, importedByUserId, importedAtUtc);
        import._lines.AddRange(lines);
        import.LineCount = import._lines.Count;
        return import;
    }

    public void MarkProcessed(int matchedLineCount, int suspenseLineCount)
    {
        MatchedLineCount = matchedLineCount;
        SuspenseLineCount = suspenseLineCount;
        Status = suspenseLineCount == 0 ? BankStatementImportStatus.Matched : BankStatementImportStatus.PartiallyMatched;
    }
}

public sealed class BankStatementLine : Entity
{
    private BankStatementLine()
    {
    }

    private BankStatementLine(
        Guid bankStatementImportId,
        Guid schemeBankAccountId,
        int lineNumber,
        DateOnly transactionDate,
        string reference,
        string? description,
        decimal amount,
        BankStatementLineDirection direction,
        Guid? investorId,
        Guid? schemeId,
        Guid? schemeClassId)
    {
        BankStatementImportId = bankStatementImportId;
        SchemeBankAccountId = schemeBankAccountId;
        LineNumber = lineNumber <= 0 ? throw new ArgumentException("Line number must be greater than zero.", nameof(lineNumber)) : lineNumber;
        TransactionDate = transactionDate;
        Reference = CashValidation.Required(reference, nameof(reference), 100);
        Description = CashValidation.Optional(description, 1000);
        Amount = CashValidation.Positive(amount, nameof(amount));
        Direction = direction;
        InvestorId = investorId;
        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        MatchStatus = BankStatementMatchStatus.Unmatched;
    }

    public Guid BankStatementImportId { get; private set; }

    public int LineNumber { get; private set; }

    public Guid SchemeBankAccountId { get; private set; }

    public DateOnly TransactionDate { get; private set; }

    public string Reference { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal Amount { get; private set; }

    public BankStatementLineDirection Direction { get; private set; }

    public Guid? InvestorId { get; private set; }

    public Guid? SchemeId { get; private set; }

    public Guid? SchemeClassId { get; private set; }

    public BankStatementMatchStatus MatchStatus { get; private set; }

    public Guid? CashBookEntryId { get; private set; }

    public Guid? SuspenseItemId { get; private set; }

    public Guid? RelatedEntityId { get; private set; }

    public string? RelatedEntityType { get; private set; }

    public CashMatchRule? MatchRule { get; private set; }

    public DateTime? MatchedAtUtc { get; private set; }

    public string? MatchedByUserId { get; private set; }

    public static BankStatementLine Create(
        Guid bankStatementImportId,
        Guid schemeBankAccountId,
        int lineNumber,
        DateOnly transactionDate,
        string reference,
        string? description,
        decimal amount,
        BankStatementLineDirection direction,
        Guid? investorId,
        Guid? schemeId,
        Guid? schemeClassId)
    {
        return new BankStatementLine(bankStatementImportId, schemeBankAccountId, lineNumber, transactionDate, reference, description, amount, direction, investorId, schemeId, schemeClassId);
    }

    public void MarkMatched(Guid cashBookEntryId, Guid? relatedEntityId, string? relatedEntityType, CashMatchRule matchRule, string matchedByUserId, DateTime matchedAtUtc)
    {
        CashBookEntryId = cashBookEntryId;
        RelatedEntityId = relatedEntityId;
        RelatedEntityType = CashValidation.Optional(relatedEntityType, 100);
        MatchRule = matchRule;
        MatchStatus = BankStatementMatchStatus.Matched;
        MatchedByUserId = CashValidation.Required(matchedByUserId, nameof(matchedByUserId), 200);
        MatchedAtUtc = matchedAtUtc;
    }

    public void MarkSuspense(Guid suspenseItemId, string matchedByUserId, DateTime matchedAtUtc)
    {
        SuspenseItemId = suspenseItemId;
        MatchStatus = BankStatementMatchStatus.Suspense;
        MatchedByUserId = CashValidation.Required(matchedByUserId, nameof(matchedByUserId), 200);
        MatchedAtUtc = matchedAtUtc;
    }
}

public sealed class CashBookEntry : AuditableAggregateRoot
{
    private CashBookEntry()
    {
    }

    private CashBookEntry(
        Guid schemeBankAccountId,
        Guid? investorId,
        Guid? schemeId,
        Guid? schemeClassId,
        DateOnly entryDate,
        decimal amount,
        string currency,
        CashBookEntryDirection direction,
        CashBookEntrySourceType sourceType,
        Guid sourceEntityId,
        string reference,
        string? narrative,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        SchemeBankAccountId = schemeBankAccountId;
        InvestorId = investorId;
        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        EntryDate = entryDate;
        Amount = CashValidation.Positive(amount, nameof(amount));
        Currency = CashValidation.Currency(currency, nameof(currency));
        Direction = direction;
        SourceType = sourceType;
        SourceEntityId = sourceEntityId;
        Reference = CashValidation.Required(reference, nameof(reference), 100);
        Narrative = CashValidation.Optional(narrative, 1000);
        Status = CashBookEntryStatus.Posted;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid SchemeBankAccountId { get; private set; }

    public Guid? InvestorId { get; private set; }

    public Guid? SchemeId { get; private set; }

    public Guid? SchemeClassId { get; private set; }

    public DateOnly EntryDate { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public CashBookEntryDirection Direction { get; private set; }

    public CashBookEntrySourceType SourceType { get; private set; }

    public Guid SourceEntityId { get; private set; }

    public string Reference { get; private set; } = string.Empty;

    public string? Narrative { get; private set; }

    public CashBookEntryStatus Status { get; private set; }

    public static CashBookEntry Create(
        Guid schemeBankAccountId,
        Guid? investorId,
        Guid? schemeId,
        Guid? schemeClassId,
        DateOnly entryDate,
        decimal amount,
        string currency,
        CashBookEntryDirection direction,
        CashBookEntrySourceType sourceType,
        Guid sourceEntityId,
        string reference,
        string? narrative,
        string createdByUserId,
        DateTime createdAtUtc)
    {
        return new CashBookEntry(schemeBankAccountId, investorId, schemeId, schemeClassId, entryDate, amount, currency, direction, sourceType, sourceEntityId, reference, narrative, createdByUserId, createdAtUtc);
    }

    public void Reverse(string reversedByUserId, DateTime reversedAtUtc)
    {
        Status = CashBookEntryStatus.Reversed;
        MarkModified(reversedByUserId, reversedAtUtc);
    }
}

public sealed class CashMatch : Entity
{
    private CashMatch()
    {
    }

    private CashMatch(Guid bankStatementLineId, Guid cashBookEntryId, CashMatchRule matchRule, Guid? relatedEntityId, string? relatedEntityType, string matchedByUserId, DateTime matchedAtUtc)
    {
        BankStatementLineId = bankStatementLineId;
        CashBookEntryId = cashBookEntryId;
        MatchRule = matchRule;
        RelatedEntityId = relatedEntityId;
        RelatedEntityType = CashValidation.Optional(relatedEntityType, 100);
        MatchedByUserId = CashValidation.Required(matchedByUserId, nameof(matchedByUserId), 200);
        MatchedAtUtc = matchedAtUtc;
    }

    public Guid BankStatementLineId { get; private set; }

    public Guid CashBookEntryId { get; private set; }

    public CashMatchRule MatchRule { get; private set; }

    public Guid? RelatedEntityId { get; private set; }

    public string? RelatedEntityType { get; private set; }

    public string MatchedByUserId { get; private set; } = string.Empty;

    public DateTime MatchedAtUtc { get; private set; }

    public static CashMatch Create(Guid bankStatementLineId, Guid cashBookEntryId, CashMatchRule matchRule, Guid? relatedEntityId, string? relatedEntityType, string matchedByUserId, DateTime matchedAtUtc)
    {
        return new CashMatch(bankStatementLineId, cashBookEntryId, matchRule, relatedEntityId, relatedEntityType, matchedByUserId, matchedAtUtc);
    }
}

public sealed class SuspenseItem : AuditableAggregateRoot
{
    private SuspenseItem()
    {
    }

    private SuspenseItem(Guid bankStatementLineId, Guid schemeBankAccountId, decimal amount, string currency, string reference, string reason, string openedByUserId, DateTime openedAtUtc)
    {
        BankStatementLineId = bankStatementLineId;
        SchemeBankAccountId = schemeBankAccountId;
        Amount = CashValidation.Positive(amount, nameof(amount));
        Currency = CashValidation.Currency(currency, nameof(currency));
        Reference = CashValidation.Required(reference, nameof(reference), 100);
        Reason = CashValidation.Required(reason, nameof(reason), 1000);
        OpenedByUserId = CashValidation.Required(openedByUserId, nameof(openedByUserId), 200);
        OpenedAtUtc = openedAtUtc;
        Status = SuspenseItemStatus.Open;
        MarkCreated(OpenedByUserId, OpenedAtUtc);
    }

    public Guid BankStatementLineId { get; private set; }

    public Guid SchemeBankAccountId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public string Reference { get; private set; } = string.Empty;

    public string Reason { get; private set; } = string.Empty;

    public SuspenseItemStatus Status { get; private set; }

    public string OpenedByUserId { get; private set; } = string.Empty;

    public DateTime OpenedAtUtc { get; private set; }

    public string? ResolvedByUserId { get; private set; }

    public DateTime? ResolvedAtUtc { get; private set; }

    public string? ResolutionComment { get; private set; }

    public Guid? PaymentInstructionId { get; private set; }

    public Guid? CashBookEntryId { get; private set; }

    public static SuspenseItem Create(Guid bankStatementLineId, Guid schemeBankAccountId, decimal amount, string currency, string reference, string reason, string openedByUserId, DateTime openedAtUtc)
    {
        return new SuspenseItem(bankStatementLineId, schemeBankAccountId, amount, currency, reference, reason, openedByUserId, openedAtUtc);
    }

    public void Resolve(string resolvedByUserId, DateTime resolvedAtUtc, string? comment, Guid? paymentInstructionId, Guid? cashBookEntryId)
    {
        Status = SuspenseItemStatus.Resolved;
        ResolvedByUserId = CashValidation.Required(resolvedByUserId, nameof(resolvedByUserId), 200);
        ResolvedAtUtc = resolvedAtUtc;
        ResolutionComment = CashValidation.Optional(comment, 1000);
        PaymentInstructionId = paymentInstructionId;
        CashBookEntryId = cashBookEntryId;
        MarkModified(resolvedByUserId, resolvedAtUtc);
    }
}

public sealed class PaymentInstruction : AuditableAggregateRoot
{
    private readonly List<PaymentStatusEvent> _statusEvents = [];

    private PaymentInstruction()
    {
    }

    private PaymentInstruction(
        Guid investorId,
        Guid schemeId,
        Guid schemeClassId,
        Guid schemeBankAccountId,
        decimal amount,
        string currency,
        string reference,
        PaymentInstructionType paymentType,
        Guid? relatedDealingInstructionId,
        string requestedByUserId,
        DateTime requestedAtUtc)
    {
        InvestorId = investorId;
        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        SchemeBankAccountId = schemeBankAccountId;
        Amount = CashValidation.Positive(amount, nameof(amount));
        Currency = CashValidation.Currency(currency, nameof(currency));
        Reference = CashValidation.Required(reference, nameof(reference), 100);
        PaymentType = paymentType;
        RelatedDealingInstructionId = relatedDealingInstructionId;
        RequestedByUserId = CashValidation.Required(requestedByUserId, nameof(requestedByUserId), 200);
        RequestedAtUtc = requestedAtUtc;
        Status = PaymentInstructionStatus.Draft;
        MarkCreated(RequestedByUserId, RequestedAtUtc);
    }

    public Guid InvestorId { get; private set; }

    public Guid SchemeId { get; private set; }

    public Guid SchemeClassId { get; private set; }

    public Guid SchemeBankAccountId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public string Reference { get; private set; } = string.Empty;

    public PaymentInstructionType PaymentType { get; private set; }

    public Guid? RelatedDealingInstructionId { get; private set; }

    public PaymentInstructionStatus Status { get; private set; }

    public string RequestedByUserId { get; private set; } = string.Empty;

    public DateTime RequestedAtUtc { get; private set; }

    public string? IdempotencyKey { get; private set; }

    public string? ExternalReference { get; private set; }

    public string? FailedReason { get; private set; }

    public IReadOnlyCollection<PaymentStatusEvent> StatusEvents => _statusEvents.AsReadOnly();

    public static PaymentInstruction Create(
        Guid investorId,
        Guid schemeId,
        Guid schemeClassId,
        Guid schemeBankAccountId,
        decimal amount,
        string currency,
        string reference,
        PaymentInstructionType paymentType,
        Guid? relatedDealingInstructionId,
        string requestedByUserId,
        DateTime requestedAtUtc,
        string? idempotencyKey)
    {
        var instruction = new PaymentInstruction(investorId, schemeId, schemeClassId, schemeBankAccountId, amount, currency, reference, paymentType, relatedDealingInstructionId, requestedByUserId, requestedAtUtc)
        {
            IdempotencyKey = CashValidation.Optional(idempotencyKey, 200)
        };

        return instruction;
    }

    public void AddStatusEvent(PaymentInstructionStatus status, PaymentStatusEventType eventType, DateTime occurredAtUtc, string? reason, string? externalReference, string changedByUserId)
    {
        Status = status;
        ExternalReference = CashValidation.Optional(externalReference, 100);
        FailedReason = status is PaymentInstructionStatus.Failed or PaymentInstructionStatus.Returned
            ? CashValidation.Optional(reason, 1000)
            : FailedReason;
        _statusEvents.Add(PaymentStatusEvent.Create(Id, status, eventType, occurredAtUtc, reason, externalReference, changedByUserId));
        MarkModified(changedByUserId, occurredAtUtc);
    }

    public void MarkCompletedFromBankMatch(string? externalReference, string changedByUserId, DateTime changedAtUtc)
    {
        Status = PaymentInstructionStatus.Completed;
        ExternalReference = CashValidation.Optional(externalReference, 100);
        FailedReason = null;
        MarkModified(changedByUserId, changedAtUtc);
    }
}

public sealed class PaymentStatusEvent : Entity
{
    private PaymentStatusEvent()
    {
    }

    private PaymentStatusEvent(Guid paymentInstructionId, PaymentInstructionStatus status, PaymentStatusEventType eventType, DateTime occurredAtUtc, string? reason, string? externalReference, string changedByUserId)
    {
        PaymentInstructionId = paymentInstructionId;
        Status = status;
        EventType = eventType;
        OccurredAtUtc = occurredAtUtc;
        Reason = CashValidation.Optional(reason, 1000);
        ExternalReference = CashValidation.Optional(externalReference, 100);
        ChangedByUserId = CashValidation.Required(changedByUserId, nameof(changedByUserId), 200);
    }

    public Guid PaymentInstructionId { get; private set; }

    public PaymentInstructionStatus Status { get; private set; }

    public PaymentStatusEventType EventType { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public string? Reason { get; private set; }

    public string? ExternalReference { get; private set; }

    public string ChangedByUserId { get; private set; } = string.Empty;

    public static PaymentStatusEvent Create(Guid paymentInstructionId, PaymentInstructionStatus status, PaymentStatusEventType eventType, DateTime occurredAtUtc, string? reason, string? externalReference, string changedByUserId)
    {
        return new PaymentStatusEvent(paymentInstructionId, status, eventType, occurredAtUtc, reason, externalReference, changedByUserId);
    }
}

public sealed class ReturnedFund : AuditableAggregateRoot
{
    private ReturnedFund()
    {
    }

    private ReturnedFund(Guid paymentInstructionId, decimal amount, string currency, string reason, string returnedByUserId, DateTime returnedAtUtc)
    {
        PaymentInstructionId = paymentInstructionId;
        Amount = CashValidation.Positive(amount, nameof(amount));
        Currency = CashValidation.Currency(currency, nameof(currency));
        Reason = CashValidation.Required(reason, nameof(reason), 1000);
        ReturnedByUserId = CashValidation.Required(returnedByUserId, nameof(returnedByUserId), 200);
        ReturnedAtUtc = returnedAtUtc;
        Status = ReturnedFundStatus.Open;
        MarkCreated(ReturnedByUserId, ReturnedAtUtc);
    }

    public Guid PaymentInstructionId { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public string Reason { get; private set; } = string.Empty;

    public ReturnedFundStatus Status { get; private set; }

    public string ReturnedByUserId { get; private set; } = string.Empty;

    public DateTime ReturnedAtUtc { get; private set; }

    public string? ResolutionComment { get; private set; }

    public static ReturnedFund Create(Guid paymentInstructionId, decimal amount, string currency, string reason, string returnedByUserId, DateTime returnedAtUtc)
    {
        return new ReturnedFund(paymentInstructionId, amount, currency, reason, returnedByUserId, returnedAtUtc);
    }

    public void Resolve(string resolvedByUserId, DateTime resolvedAtUtc, string? comment)
    {
        Status = ReturnedFundStatus.Resolved;
        ResolutionComment = CashValidation.Optional(comment, 1000);
        MarkModified(resolvedByUserId, resolvedAtUtc);
    }
}

public sealed class ReversalRequest : AuditableAggregateRoot
{
    private ReversalRequest()
    {
    }

    private ReversalRequest(Guid paymentInstructionId, string reason, string requestedByUserId, DateTime requestedAtUtc)
    {
        PaymentInstructionId = paymentInstructionId;
        Reason = CashValidation.Required(reason, nameof(reason), 1000);
        RequestedByUserId = CashValidation.Required(requestedByUserId, nameof(requestedByUserId), 200);
        RequestedAtUtc = requestedAtUtc;
        Status = ReversalRequestStatus.PendingApproval;
        MarkCreated(RequestedByUserId, RequestedAtUtc);
    }

    public Guid PaymentInstructionId { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public ReversalRequestStatus Status { get; private set; }

    public string RequestedByUserId { get; private set; } = string.Empty;

    public DateTime RequestedAtUtc { get; private set; }

    public string? ApprovedByUserId { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    public string? ApprovalComment { get; private set; }

    public static ReversalRequest Create(Guid paymentInstructionId, string reason, string requestedByUserId, DateTime requestedAtUtc)
    {
        return new ReversalRequest(paymentInstructionId, reason, requestedByUserId, requestedAtUtc);
    }

    public void Approve(string approvedByUserId, DateTime approvedAtUtc, string? comment)
    {
        if (Status != ReversalRequestStatus.PendingApproval)
        {
            throw new InvalidOperationException("Only pending reversal requests can be approved.");
        }

        var actor = CashValidation.Required(approvedByUserId, nameof(approvedByUserId), 200);
        if (string.Equals(actor, RequestedByUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Segregation of duties violation: requester cannot approve the same reversal request.");
        }

        ApprovedByUserId = actor;
        ApprovedAtUtc = approvedAtUtc;
        ApprovalComment = CashValidation.Optional(comment, 1000);
        Status = ReversalRequestStatus.Approved;
        MarkModified(actor, approvedAtUtc);
    }
}

public sealed class ReconciliationRun : AuditableAggregateRoot
{
    private ReconciliationRun()
    {
    }

    private ReconciliationRun(Guid schemeBankAccountId, DateOnly runDate, int agingThresholdDays, string requestedByUserId, DateTime requestedAtUtc)
    {
        SchemeBankAccountId = schemeBankAccountId;
        RunDate = runDate;
        AgingThresholdDays = agingThresholdDays < 0 ? throw new ArgumentException("Aging threshold cannot be negative.", nameof(agingThresholdDays)) : agingThresholdDays;
        RequestedByUserId = CashValidation.Required(requestedByUserId, nameof(requestedByUserId), 200);
        RequestedAtUtc = requestedAtUtc;
        Status = ReconciliationRunStatus.Running;
        MarkCreated(RequestedByUserId, RequestedAtUtc);
    }

    public Guid SchemeBankAccountId { get; private set; }

    public DateOnly RunDate { get; private set; }

    public int AgingThresholdDays { get; private set; }

    public ReconciliationRunStatus Status { get; private set; }

    public string RequestedByUserId { get; private set; } = string.Empty;

    public DateTime RequestedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public string? CompletedByUserId { get; private set; }

    public int MatchedCount { get; private set; }

    public int SuspenseCount { get; private set; }

    public int BreakCount { get; private set; }

    public int AgedBreakCount { get; private set; }

    public string? Summary { get; private set; }

    public static ReconciliationRun Create(Guid schemeBankAccountId, DateOnly runDate, int agingThresholdDays, string requestedByUserId, DateTime requestedAtUtc)
    {
        return new ReconciliationRun(schemeBankAccountId, runDate, agingThresholdDays, requestedByUserId, requestedAtUtc);
    }

    public void Complete(int matchedCount, int suspenseCount, int breakCount, int agedBreakCount, string completedByUserId, DateTime completedAtUtc, string? summary)
    {
        MatchedCount = matchedCount;
        SuspenseCount = suspenseCount;
        BreakCount = breakCount;
        AgedBreakCount = agedBreakCount;
        CompletedByUserId = CashValidation.Required(completedByUserId, nameof(completedByUserId), 200);
        CompletedAtUtc = completedAtUtc;
        Summary = CashValidation.Optional(summary, 1000);
        Status = ReconciliationRunStatus.Completed;
        MarkModified(CompletedByUserId, completedAtUtc);
    }
}
