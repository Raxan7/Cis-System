using Cis.Domain.Common;

namespace Cis.Domain.CustodyReconciliation;

public sealed class Custodian : AuditableAggregateRoot
{
    private readonly List<CustodianAccount> _accounts = [];

    private Custodian()
    {
    }

    private Custodian(string code, string name, string? swiftCode, string createdByUserId, DateTime createdAtUtc)
    {
        Code = CustodyReconciliationValidation.Required(code, nameof(code), 50).ToUpperInvariant();
        Name = CustodyReconciliationValidation.Required(name, nameof(name), 200);
        SwiftCode = CustodyReconciliationValidation.Optional(swiftCode, 20)?.ToUpperInvariant();
        Status = CustodianStatus.Active;
        CreatedByUserId = CustodyReconciliationValidation.Required(createdByUserId, nameof(createdByUserId), 200);
        CreatedAtUtc = CustodyReconciliationValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        MarkCreated(CreatedByUserId, CreatedAtUtc);
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? SwiftCode { get; private set; }
    public CustodianStatus Status { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public IReadOnlyCollection<CustodianAccount> Accounts => _accounts.AsReadOnly();

    public static Custodian Create(string code, string name, string? swiftCode, string createdByUserId, DateTime createdAtUtc)
    {
        return new Custodian(code, name, swiftCode, createdByUserId, createdAtUtc);
    }

    public void AddAccount(Guid schemeId, Guid? schemeClassId, string accountNumber, string accountName, string currency)
    {
        _accounts.Add(CustodianAccount.Create(Id, schemeId, schemeClassId, accountNumber, accountName, currency));
    }
}

public sealed class CustodianAccount : Entity
{
    private CustodianAccount()
    {
    }

    private CustodianAccount(Guid custodianId, Guid schemeId, Guid? schemeClassId, string accountNumber, string accountName, string currency)
    {
        CustodianId = custodianId != Guid.Empty ? custodianId : throw new ArgumentException("Custodian id cannot be empty.", nameof(custodianId));
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        SchemeClassId = schemeClassId;
        AccountNumber = CustodyReconciliationValidation.Required(accountNumber, nameof(accountNumber), 100);
        AccountName = CustodyReconciliationValidation.Required(accountName, nameof(accountName), 200);
        Currency = CustodyReconciliationValidation.Currency(currency, nameof(currency));
        IsActive = true;
    }

    public Guid CustodianId { get; private set; }
    public Guid SchemeId { get; private set; }
    public Guid? SchemeClassId { get; private set; }
    public string AccountNumber { get; private set; } = string.Empty;
    public string AccountName { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public static CustodianAccount Create(Guid custodianId, Guid schemeId, Guid? schemeClassId, string accountNumber, string accountName, string currency)
    {
        return new CustodianAccount(custodianId, schemeId, schemeClassId, accountNumber, accountName, currency);
    }
}

public sealed class CustodianStatementImport : AuditableAggregateRoot
{
    private readonly List<CustodianHoldingLine> _holdingLines = [];
    private readonly List<CustodianCashLine> _cashLines = [];

    private CustodianStatementImport()
    {
    }

    private CustodianStatementImport(Guid id, Guid custodianId, Guid? custodianAccountId, CustodianStatementType statementType, BusinessDate statementDate, string sourceFileName, string idempotencyKey, string sourceHash, string importedByUserId, DateTime importedAtUtc)
    {
        Id = id != Guid.Empty ? id : throw new ArgumentException("Import id cannot be empty.", nameof(id));
        CustodianId = custodianId != Guid.Empty ? custodianId : throw new ArgumentException("Custodian id cannot be empty.", nameof(custodianId));
        CustodianAccountId = custodianAccountId;
        StatementType = statementType;
        StatementDate = statementDate;
        SourceFileName = CustodyReconciliationValidation.Required(sourceFileName, nameof(sourceFileName), 200);
        IdempotencyKey = CustodyReconciliationValidation.Required(idempotencyKey, nameof(idempotencyKey), 200);
        SourceHash = CustodyReconciliationValidation.Required(sourceHash, nameof(sourceHash), 128);
        ImportedByUserId = CustodyReconciliationValidation.Required(importedByUserId, nameof(importedByUserId), 200);
        ImportedAtUtc = CustodyReconciliationValidation.EnsureUtc(importedAtUtc, nameof(importedAtUtc));
        Status = CustodianStatementImportStatus.Imported;
        MarkCreated(ImportedByUserId, ImportedAtUtc);
    }

    public Guid CustodianId { get; private set; }
    public Guid? CustodianAccountId { get; private set; }
    public CustodianStatementType StatementType { get; private set; }
    public BusinessDate StatementDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public string SourceFileName { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string SourceHash { get; private set; } = string.Empty;
    public CustodianStatementImportStatus Status { get; private set; }
    public string ImportedByUserId { get; private set; } = string.Empty;
    public DateTime ImportedAtUtc { get; private set; }
    public int LineCount { get; private set; }
    public IReadOnlyCollection<CustodianHoldingLine> HoldingLines => _holdingLines.AsReadOnly();
    public IReadOnlyCollection<CustodianCashLine> CashLines => _cashLines.AsReadOnly();

    public static CustodianStatementImport CreateHoldings(Guid custodianId, Guid? custodianAccountId, BusinessDate statementDate, string sourceFileName, string idempotencyKey, string sourceHash, string importedByUserId, DateTime importedAtUtc, IReadOnlyCollection<CustodianHoldingLine> lines)
    {
        var importId = lines.FirstOrDefault()?.CustodianStatementImportId ?? Guid.NewGuid();
        var import = new CustodianStatementImport(importId, custodianId, custodianAccountId, CustodianStatementType.Holdings, statementDate, sourceFileName, idempotencyKey, sourceHash, importedByUserId, importedAtUtc);
        import._holdingLines.AddRange(lines);
        import.LineCount = import._holdingLines.Count;
        return import;
    }

    public static CustodianStatementImport CreateCash(Guid custodianId, Guid? custodianAccountId, BusinessDate statementDate, string sourceFileName, string idempotencyKey, string sourceHash, string importedByUserId, DateTime importedAtUtc, IReadOnlyCollection<CustodianCashLine> lines)
    {
        var importId = lines.FirstOrDefault()?.CustodianStatementImportId ?? Guid.NewGuid();
        var import = new CustodianStatementImport(importId, custodianId, custodianAccountId, CustodianStatementType.Cash, statementDate, sourceFileName, idempotencyKey, sourceHash, importedByUserId, importedAtUtc);
        import._cashLines.AddRange(lines);
        import.LineCount = import._cashLines.Count;
        return import;
    }

    public void MarkReconciled()
    {
        Status = CustodianStatementImportStatus.Reconciled;
    }
}

public sealed class CustodianHoldingLine : Entity
{
    private CustodianHoldingLine()
    {
    }

    private CustodianHoldingLine(Guid custodianStatementImportId, Guid custodianId, Guid? custodianAccountId, Guid schemeId, Guid? schemeClassId, Guid? instrumentId, string instrumentCode, string instrumentName, decimal quantity, decimal marketValue, string currency, string? settlementReference, bool isSettled)
    {
        CustodianStatementImportId = custodianStatementImportId;
        CustodianId = custodianId;
        CustodianAccountId = custodianAccountId;
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        SchemeClassId = schemeClassId;
        InstrumentId = instrumentId;
        InstrumentCode = CustodyReconciliationValidation.Required(instrumentCode, nameof(instrumentCode), 100).ToUpperInvariant();
        InstrumentName = CustodyReconciliationValidation.Required(instrumentName, nameof(instrumentName), 200);
        Quantity = CustodyReconciliationValidation.NonNegative(quantity, nameof(quantity));
        MarketValue = CustodyReconciliationValidation.NonNegative(marketValue, nameof(marketValue));
        Currency = CustodyReconciliationValidation.Currency(currency, nameof(currency));
        SettlementReference = CustodyReconciliationValidation.Optional(settlementReference, 100);
        IsSettled = isSettled;
    }

    public Guid CustodianStatementImportId { get; private set; }
    public Guid CustodianId { get; private set; }
    public Guid? CustodianAccountId { get; private set; }
    public Guid SchemeId { get; private set; }
    public Guid? SchemeClassId { get; private set; }
    public Guid? InstrumentId { get; private set; }
    public string InstrumentCode { get; private set; } = string.Empty;
    public string InstrumentName { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal MarketValue { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string? SettlementReference { get; private set; }
    public bool IsSettled { get; private set; }

    public static CustodianHoldingLine Create(Guid custodianStatementImportId, Guid custodianId, Guid? custodianAccountId, Guid schemeId, Guid? schemeClassId, Guid? instrumentId, string instrumentCode, string instrumentName, decimal quantity, decimal marketValue, string currency, string? settlementReference, bool isSettled)
    {
        return new CustodianHoldingLine(custodianStatementImportId, custodianId, custodianAccountId, schemeId, schemeClassId, instrumentId, instrumentCode, instrumentName, quantity, marketValue, currency, settlementReference, isSettled);
    }
}

public sealed class CustodianCashLine : Entity
{
    private CustodianCashLine()
    {
    }

    private CustodianCashLine(Guid custodianStatementImportId, Guid custodianId, Guid? custodianAccountId, Guid schemeId, string accountNumber, string currency, BusinessDate balanceDate, decimal cashBalance, string? settlementReference, bool isSettled)
    {
        CustodianStatementImportId = custodianStatementImportId;
        CustodianId = custodianId;
        CustodianAccountId = custodianAccountId;
        SchemeId = schemeId != Guid.Empty ? schemeId : throw new ArgumentException("Scheme id cannot be empty.", nameof(schemeId));
        AccountNumber = CustodyReconciliationValidation.Required(accountNumber, nameof(accountNumber), 100);
        Currency = CustodyReconciliationValidation.Currency(currency, nameof(currency));
        BalanceDate = balanceDate;
        CashBalance = cashBalance;
        SettlementReference = CustodyReconciliationValidation.Optional(settlementReference, 100);
        IsSettled = isSettled;
    }

    public Guid CustodianStatementImportId { get; private set; }
    public Guid CustodianId { get; private set; }
    public Guid? CustodianAccountId { get; private set; }
    public Guid SchemeId { get; private set; }
    public string AccountNumber { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public BusinessDate BalanceDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public decimal CashBalance { get; private set; }
    public string? SettlementReference { get; private set; }
    public bool IsSettled { get; private set; }

    public static CustodianCashLine Create(Guid custodianStatementImportId, Guid custodianId, Guid? custodianAccountId, Guid schemeId, string accountNumber, string currency, BusinessDate balanceDate, decimal cashBalance, string? settlementReference, bool isSettled)
    {
        return new CustodianCashLine(custodianStatementImportId, custodianId, custodianAccountId, schemeId, accountNumber, currency, balanceDate, cashBalance, settlementReference, isSettled);
    }
}

public sealed class CustodyReconciliationRun : AuditableAggregateRoot
{
    private readonly List<HoldingsReconciliationBreak> _holdingBreaks = [];
    private readonly List<CashReconciliationBreak> _cashBreaks = [];

    private CustodyReconciliationRun()
    {
    }

    private CustodyReconciliationRun(Guid custodianId, Guid? holdingsImportId, Guid? cashImportId, BusinessDate businessDate, string sourceDataJson, string runByUserId, DateTime runAtUtc)
    {
        CustodianId = custodianId != Guid.Empty ? custodianId : throw new ArgumentException("Custodian id cannot be empty.", nameof(custodianId));
        HoldingsImportId = holdingsImportId;
        CashImportId = cashImportId;
        BusinessDate = businessDate;
        SourceDataJson = CustodyReconciliationValidation.Required(sourceDataJson, nameof(sourceDataJson), 20000);
        Status = CustodyReconciliationRunStatus.Completed;
        RunByUserId = CustodyReconciliationValidation.Required(runByUserId, nameof(runByUserId), 200);
        RunAtUtc = CustodyReconciliationValidation.EnsureUtc(runAtUtc, nameof(runAtUtc));
        MarkCreated(RunByUserId, RunAtUtc);
    }

    public Guid CustodianId { get; private set; }
    public Guid? HoldingsImportId { get; private set; }
    public Guid? CashImportId { get; private set; }
    public BusinessDate BusinessDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public string SourceDataJson { get; private set; } = "{}";
    public CustodyReconciliationRunStatus Status { get; private set; }
    public string RunByUserId { get; private set; } = string.Empty;
    public DateTime RunAtUtc { get; private set; }
    public int HoldingBreakCount { get; private set; }
    public int CashBreakCount { get; private set; }
    public IReadOnlyCollection<HoldingsReconciliationBreak> HoldingBreaks => _holdingBreaks.AsReadOnly();
    public IReadOnlyCollection<CashReconciliationBreak> CashBreaks => _cashBreaks.AsReadOnly();

    public static CustodyReconciliationRun Create(Guid custodianId, Guid? holdingsImportId, Guid? cashImportId, BusinessDate businessDate, string sourceDataJson, string runByUserId, DateTime runAtUtc)
    {
        return new CustodyReconciliationRun(custodianId, holdingsImportId, cashImportId, businessDate, sourceDataJson, runByUserId, runAtUtc);
    }

    public void AddHoldingBreak(HoldingsReconciliationBreak reconciliationBreak)
    {
        _holdingBreaks.Add(reconciliationBreak ?? throw new ArgumentNullException(nameof(reconciliationBreak)));
        HoldingBreakCount = _holdingBreaks.Count;
    }

    public void AddCashBreak(CashReconciliationBreak reconciliationBreak)
    {
        _cashBreaks.Add(reconciliationBreak ?? throw new ArgumentNullException(nameof(reconciliationBreak)));
        CashBreakCount = _cashBreaks.Count;
    }
}

public sealed class HoldingsReconciliationBreak : AuditableAggregateRoot
{
    private HoldingsReconciliationBreak()
    {
    }

    private HoldingsReconciliationBreak(Guid reconciliationRunId, Guid? holdingLineId, Guid schemeId, Guid? schemeClassId, string instrumentCode, decimal internalQuantity, decimal custodianQuantity, decimal internalMarketValue, decimal custodianMarketValue, ReconciliationBreakType breakType, ReconciliationBreakSeverity severity, string createdByUserId, DateTime createdAtUtc)
    {
        ReconciliationRunId = reconciliationRunId;
        HoldingLineId = holdingLineId;
        SchemeId = schemeId;
        SchemeClassId = schemeClassId;
        InstrumentCode = CustodyReconciliationValidation.Required(instrumentCode, nameof(instrumentCode), 100).ToUpperInvariant();
        InternalQuantity = CustodyReconciliationValidation.NonNegative(internalQuantity, nameof(internalQuantity));
        CustodianQuantity = CustodyReconciliationValidation.NonNegative(custodianQuantity, nameof(custodianQuantity));
        QuantityDifference = CustodianQuantity - InternalQuantity;
        InternalMarketValue = CustodyReconciliationValidation.NonNegative(internalMarketValue, nameof(internalMarketValue));
        CustodianMarketValue = CustodyReconciliationValidation.NonNegative(custodianMarketValue, nameof(custodianMarketValue));
        MarketValueDifference = CustodianMarketValue - InternalMarketValue;
        BreakType = breakType;
        Severity = severity;
        Status = ReconciliationBreakStatus.Open;
        CreatedByUserId = CustodyReconciliationValidation.Required(createdByUserId, nameof(createdByUserId), 200);
        CreatedAtUtc = CustodyReconciliationValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        MarkCreated(CreatedByUserId, CreatedAtUtc);
    }

    public Guid ReconciliationRunId { get; private set; }
    public Guid? HoldingLineId { get; private set; }
    public Guid SchemeId { get; private set; }
    public Guid? SchemeClassId { get; private set; }
    public string InstrumentCode { get; private set; } = string.Empty;
    public decimal InternalQuantity { get; private set; }
    public decimal CustodianQuantity { get; private set; }
    public decimal QuantityDifference { get; private set; }
    public decimal InternalMarketValue { get; private set; }
    public decimal CustodianMarketValue { get; private set; }
    public decimal MarketValueDifference { get; private set; }
    public ReconciliationBreakType BreakType { get; private set; }
    public ReconciliationBreakSeverity Severity { get; private set; }
    public ReconciliationBreakStatus Status { get; private set; }
    public string? OwnerUserId { get; private set; }
    public DateTime? AssignedAtUtc { get; private set; }
    public string? ResolvedByUserId { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public string? ResolutionEvidenceReference { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static HoldingsReconciliationBreak Create(Guid reconciliationRunId, Guid? holdingLineId, Guid schemeId, Guid? schemeClassId, string instrumentCode, decimal internalQuantity, decimal custodianQuantity, decimal internalMarketValue, decimal custodianMarketValue, ReconciliationBreakType breakType, ReconciliationBreakSeverity severity, string createdByUserId, DateTime createdAtUtc)
    {
        return new HoldingsReconciliationBreak(reconciliationRunId, holdingLineId, schemeId, schemeClassId, instrumentCode, internalQuantity, custodianQuantity, internalMarketValue, custodianMarketValue, breakType, severity, createdByUserId, createdAtUtc);
    }

    public void Assign(string ownerUserId, DateTime assignedAtUtc)
    {
        OwnerUserId = CustodyReconciliationValidation.Required(ownerUserId, nameof(ownerUserId), 200);
        AssignedAtUtc = CustodyReconciliationValidation.EnsureUtc(assignedAtUtc, nameof(assignedAtUtc));
        Status = ReconciliationBreakStatus.Assigned;
    }

    public void Resolve(string evidenceReference, string resolvedByUserId, DateTime resolvedAtUtc)
    {
        ResolutionEvidenceReference = CustodyReconciliationValidation.Required(evidenceReference, nameof(evidenceReference), 500);
        ResolvedByUserId = CustodyReconciliationValidation.Required(resolvedByUserId, nameof(resolvedByUserId), 200);
        ResolvedAtUtc = CustodyReconciliationValidation.EnsureUtc(resolvedAtUtc, nameof(resolvedAtUtc));
        Status = ReconciliationBreakStatus.Resolved;
    }
}

public sealed class CashReconciliationBreak : AuditableAggregateRoot
{
    private CashReconciliationBreak()
    {
    }

    private CashReconciliationBreak(Guid reconciliationRunId, Guid? cashLineId, Guid schemeId, string accountNumber, string currency, decimal internalCashBalance, decimal custodianCashBalance, ReconciliationBreakType breakType, ReconciliationBreakSeverity severity, string createdByUserId, DateTime createdAtUtc)
    {
        ReconciliationRunId = reconciliationRunId;
        CashLineId = cashLineId;
        SchemeId = schemeId;
        AccountNumber = CustodyReconciliationValidation.Required(accountNumber, nameof(accountNumber), 100);
        Currency = CustodyReconciliationValidation.Currency(currency, nameof(currency));
        InternalCashBalance = internalCashBalance;
        CustodianCashBalance = custodianCashBalance;
        Difference = CustodianCashBalance - InternalCashBalance;
        BreakType = breakType;
        Severity = severity;
        Status = ReconciliationBreakStatus.Open;
        CreatedByUserId = CustodyReconciliationValidation.Required(createdByUserId, nameof(createdByUserId), 200);
        CreatedAtUtc = CustodyReconciliationValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        MarkCreated(CreatedByUserId, CreatedAtUtc);
    }

    public Guid ReconciliationRunId { get; private set; }
    public Guid? CashLineId { get; private set; }
    public Guid SchemeId { get; private set; }
    public string AccountNumber { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;
    public decimal InternalCashBalance { get; private set; }
    public decimal CustodianCashBalance { get; private set; }
    public decimal Difference { get; private set; }
    public ReconciliationBreakType BreakType { get; private set; }
    public ReconciliationBreakSeverity Severity { get; private set; }
    public ReconciliationBreakStatus Status { get; private set; }
    public string? OwnerUserId { get; private set; }
    public DateTime? AssignedAtUtc { get; private set; }
    public string? ResolvedByUserId { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public string? ResolutionEvidenceReference { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static CashReconciliationBreak Create(Guid reconciliationRunId, Guid? cashLineId, Guid schemeId, string accountNumber, string currency, decimal internalCashBalance, decimal custodianCashBalance, ReconciliationBreakType breakType, ReconciliationBreakSeverity severity, string createdByUserId, DateTime createdAtUtc)
    {
        return new CashReconciliationBreak(reconciliationRunId, cashLineId, schemeId, accountNumber, currency, internalCashBalance, custodianCashBalance, breakType, severity, createdByUserId, createdAtUtc);
    }

    public void Assign(string ownerUserId, DateTime assignedAtUtc)
    {
        OwnerUserId = CustodyReconciliationValidation.Required(ownerUserId, nameof(ownerUserId), 200);
        AssignedAtUtc = CustodyReconciliationValidation.EnsureUtc(assignedAtUtc, nameof(assignedAtUtc));
        Status = ReconciliationBreakStatus.Assigned;
    }

    public void Resolve(string evidenceReference, string resolvedByUserId, DateTime resolvedAtUtc)
    {
        ResolutionEvidenceReference = CustodyReconciliationValidation.Required(evidenceReference, nameof(evidenceReference), 500);
        ResolvedByUserId = CustodyReconciliationValidation.Required(resolvedByUserId, nameof(resolvedByUserId), 200);
        ResolvedAtUtc = CustodyReconciliationValidation.EnsureUtc(resolvedAtUtc, nameof(resolvedAtUtc));
        Status = ReconciliationBreakStatus.Resolved;
    }
}

public sealed class BreakAging : Entity
{
    private BreakAging()
    {
    }

    private BreakAging(Guid? holdingBreakId, Guid? cashBreakId, BusinessDate businessDate, int ageDays, ReconciliationBreakStatus status, DateTime calculatedAtUtc)
    {
        if (!holdingBreakId.HasValue && !cashBreakId.HasValue)
        {
            throw new ArgumentException("A break aging record must reference either a holding break or a cash break.");
        }

        HoldingBreakId = holdingBreakId;
        CashBreakId = cashBreakId;
        BusinessDate = businessDate;
        AgeDays = CustodyReconciliationValidation.NonNegative(ageDays, nameof(ageDays));
        Status = status;
        CalculatedAtUtc = CustodyReconciliationValidation.EnsureUtc(calculatedAtUtc, nameof(calculatedAtUtc));
    }

    public Guid? HoldingBreakId { get; private set; }
    public Guid? CashBreakId { get; private set; }
    public BusinessDate BusinessDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public int AgeDays { get; private set; }
    public ReconciliationBreakStatus Status { get; private set; }
    public DateTime CalculatedAtUtc { get; private set; }

    public static BreakAging ForHolding(Guid holdingBreakId, BusinessDate businessDate, int ageDays, ReconciliationBreakStatus status, DateTime calculatedAtUtc)
    {
        return new BreakAging(holdingBreakId, null, businessDate, ageDays, status, calculatedAtUtc);
    }

    public static BreakAging ForCash(Guid cashBreakId, BusinessDate businessDate, int ageDays, ReconciliationBreakStatus status, DateTime calculatedAtUtc)
    {
        return new BreakAging(null, cashBreakId, businessDate, ageDays, status, calculatedAtUtc);
    }
}

public sealed class BreakActionNote : Entity
{
    private BreakActionNote()
    {
    }

    private BreakActionNote(Guid? holdingBreakId, Guid? cashBreakId, string note, string? evidenceReference, string createdByUserId, DateTime createdAtUtc)
    {
        if (!holdingBreakId.HasValue && !cashBreakId.HasValue)
        {
            throw new ArgumentException("A break action note must reference either a holding break or a cash break.");
        }

        HoldingBreakId = holdingBreakId;
        CashBreakId = cashBreakId;
        Note = CustodyReconciliationValidation.Required(note, nameof(note), 1000);
        EvidenceReference = CustodyReconciliationValidation.Optional(evidenceReference, 500);
        CreatedByUserId = CustodyReconciliationValidation.Required(createdByUserId, nameof(createdByUserId), 200);
        CreatedAtUtc = CustodyReconciliationValidation.EnsureUtc(createdAtUtc, nameof(createdAtUtc));
    }

    public Guid? HoldingBreakId { get; private set; }
    public Guid? CashBreakId { get; private set; }
    public string Note { get; private set; } = string.Empty;
    public string? EvidenceReference { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static BreakActionNote ForHolding(Guid holdingBreakId, string note, string? evidenceReference, string createdByUserId, DateTime createdAtUtc)
    {
        return new BreakActionNote(holdingBreakId, null, note, evidenceReference, createdByUserId, createdAtUtc);
    }

    public static BreakActionNote ForCash(Guid cashBreakId, string note, string? evidenceReference, string createdByUserId, DateTime createdAtUtc)
    {
        return new BreakActionNote(null, cashBreakId, note, evidenceReference, createdByUserId, createdAtUtc);
    }
}

public sealed class SafekeepingConfirmation : AuditableAggregateRoot
{
    private SafekeepingConfirmation()
    {
    }

    private SafekeepingConfirmation(Guid custodianId, Guid? schemeId, Guid? sourceReconciliationRunId, BusinessDate businessDate, int holdingsCount, int cashLineCount, decimal totalMarketValue, decimal totalCashBalance, string? settlementReference, bool settlementConfirmed, string confirmationPayloadJson, string generatedByUserId, DateTime generatedAtUtc)
    {
        CustodianId = custodianId != Guid.Empty ? custodianId : throw new ArgumentException("Custodian id cannot be empty.", nameof(custodianId));
        SchemeId = schemeId;
        SourceReconciliationRunId = sourceReconciliationRunId;
        BusinessDate = businessDate;
        HoldingsCount = CustodyReconciliationValidation.NonNegative(holdingsCount, nameof(holdingsCount));
        CashLineCount = CustodyReconciliationValidation.NonNegative(cashLineCount, nameof(cashLineCount));
        TotalMarketValue = CustodyReconciliationValidation.NonNegative(totalMarketValue, nameof(totalMarketValue));
        TotalCashBalance = totalCashBalance;
        SettlementReference = CustodyReconciliationValidation.Optional(settlementReference, 100);
        SettlementConfirmed = settlementConfirmed;
        ConfirmationPayloadJson = CustodyReconciliationValidation.Required(confirmationPayloadJson, nameof(confirmationPayloadJson), 20000);
        GeneratedByUserId = CustodyReconciliationValidation.Required(generatedByUserId, nameof(generatedByUserId), 200);
        GeneratedAtUtc = CustodyReconciliationValidation.EnsureUtc(generatedAtUtc, nameof(generatedAtUtc));
        Status = SafekeepingConfirmationStatus.Issued;
        MarkCreated(GeneratedByUserId, GeneratedAtUtc);
    }

    public Guid CustodianId { get; private set; }
    public Guid? SchemeId { get; private set; }
    public Guid? SourceReconciliationRunId { get; private set; }
    public BusinessDate BusinessDate { get; private set; } = BusinessDate.From(DateOnly.MinValue);
    public int HoldingsCount { get; private set; }
    public int CashLineCount { get; private set; }
    public decimal TotalMarketValue { get; private set; }
    public decimal TotalCashBalance { get; private set; }
    public string? SettlementReference { get; private set; }
    public bool SettlementConfirmed { get; private set; }
    public string ConfirmationPayloadJson { get; private set; } = "{}";
    public SafekeepingConfirmationStatus Status { get; private set; }
    public string GeneratedByUserId { get; private set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; private set; }

    public static SafekeepingConfirmation Create(Guid custodianId, Guid? schemeId, Guid? sourceReconciliationRunId, BusinessDate businessDate, int holdingsCount, int cashLineCount, decimal totalMarketValue, decimal totalCashBalance, string? settlementReference, bool settlementConfirmed, string confirmationPayloadJson, string generatedByUserId, DateTime generatedAtUtc)
    {
        return new SafekeepingConfirmation(custodianId, schemeId, sourceReconciliationRunId, businessDate, holdingsCount, cashLineCount, totalMarketValue, totalCashBalance, settlementReference, settlementConfirmed, confirmationPayloadJson, generatedByUserId, generatedAtUtc);
    }
}
