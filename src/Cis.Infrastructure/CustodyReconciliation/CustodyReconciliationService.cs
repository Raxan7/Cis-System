using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.CustodyReconciliation;
using Cis.Domain.Audit;
using Cis.Domain.Common;
using Cis.Domain.CustodyReconciliation;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.CustodyReconciliation;

internal sealed class CustodyReconciliationService : ICustodyReconciliationService
{
    private const string ModuleName = "CustodyReconciliation";
    private const decimal QuantityTolerance = 0.000001m;
    private const decimal AmountTolerance = 0.000001m;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly CisDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CustodyReconciliationService(
        CisDbContext dbContext,
        IAuditWriter auditWriter,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<CustodianDto> CreateCustodianAsync(CreateCustodianRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var actor = CurrentUserIdOrThrow();
            var now = _dateTimeProvider.UtcNow;
            if (request.Accounts.Count == 0)
            {
                throw Validation("accounts", "At least one custodian account is required.");
            }

            foreach (var account in request.Accounts)
            {
                await EnsureSchemeReferencesAsync(account.SchemeId, account.SchemeClassId, cancellationToken);
            }

            var custodian = Custodian.Create(request.Code, request.Name, request.SwiftCode, actor, now);
            foreach (var account in request.Accounts)
            {
                custodian.AddAccount(account.SchemeId, account.SchemeClassId, account.AccountNumber, account.AccountName, account.Currency);
            }

            _dbContext.Custodians.Add(custodian);
            await SaveHandlingValidationAsync(cancellationToken);
            var dto = MapCustodian(custodian);
            await WriteAuditAsync(AuditEventType.Created, "CustodianCreated", "Custodian", custodian.Id.ToString(), null, Snapshot(dto), "Custodian master data created.", cancellationToken);
            return dto;
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "custodian", exception.Message);
        }
    }

    public async Task<CustodianStatementImportDto> ImportHoldingsAsync(ImportCustodianHoldingsRequest request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = RequiredIdempotencyKey(idempotencyKey);
            var sourceHash = SourceHash(request);
            var existing = await FindIdempotentImportAsync(key, sourceHash, cancellationToken);
            if (existing is not null)
            {
                return MapImport(existing);
            }

            var actor = CurrentUserIdOrThrow();
            var now = _dateTimeProvider.UtcNow;
            await EnsureCustodianAsync(request.CustodianId, cancellationToken);
            await EnsureCustodianAccountAsync(request.CustodianId, request.CustodianAccountId, cancellationToken);
            if (request.Lines.Count == 0)
            {
                throw Validation("lines", "At least one holding line is required.");
            }

            var importId = Guid.NewGuid();
            var lines = new List<CustodianHoldingLine>();
            foreach (var line in request.Lines)
            {
                await EnsureSchemeReferencesAsync(line.SchemeId, line.SchemeClassId, cancellationToken);
                await EnsureCustodianAccountAsync(request.CustodianId, line.CustodianAccountId, cancellationToken);
                lines.Add(CustodianHoldingLine.Create(importId, request.CustodianId, line.CustodianAccountId ?? request.CustodianAccountId, line.SchemeId, line.SchemeClassId, line.InstrumentId, line.InstrumentCode, line.InstrumentName, line.Quantity, line.MarketValue, line.Currency, line.SettlementReference, line.IsSettled));
            }

            var import = CustodianStatementImport.CreateHoldings(request.CustodianId, request.CustodianAccountId, BusinessDate.From(request.StatementDate), request.SourceFileName, key, sourceHash, actor, now, lines);
            _dbContext.CustodianStatementImports.Add(import);
            await SaveHandlingValidationAsync(cancellationToken);
            var dto = MapImport(import);
            await WriteAuditAsync(AuditEventType.Created, "CustodianHoldingsImported", "CustodianStatementImport", import.Id.ToString(), null, Snapshot(dto), "Custodian holdings statement imported.", cancellationToken, key);
            return dto;
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "holdingsImport", exception.Message);
        }
    }

    public async Task<CustodianStatementImportDto> ImportCashAsync(ImportCustodianCashRequest request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = RequiredIdempotencyKey(idempotencyKey);
            var sourceHash = SourceHash(request);
            var existing = await FindIdempotentImportAsync(key, sourceHash, cancellationToken);
            if (existing is not null)
            {
                return MapImport(existing);
            }

            var actor = CurrentUserIdOrThrow();
            var now = _dateTimeProvider.UtcNow;
            await EnsureCustodianAsync(request.CustodianId, cancellationToken);
            await EnsureCustodianAccountAsync(request.CustodianId, request.CustodianAccountId, cancellationToken);
            if (request.Lines.Count == 0)
            {
                throw Validation("lines", "At least one cash line is required.");
            }

            var importId = Guid.NewGuid();
            var lines = new List<CustodianCashLine>();
            foreach (var line in request.Lines)
            {
                await EnsureSchemeReferencesAsync(line.SchemeId, null, cancellationToken);
                await EnsureCustodianAccountAsync(request.CustodianId, line.CustodianAccountId, cancellationToken);
                lines.Add(CustodianCashLine.Create(importId, request.CustodianId, line.CustodianAccountId ?? request.CustodianAccountId, line.SchemeId, line.AccountNumber, line.Currency, BusinessDate.From(line.BalanceDate), line.CashBalance, line.SettlementReference, line.IsSettled));
            }

            var import = CustodianStatementImport.CreateCash(request.CustodianId, request.CustodianAccountId, BusinessDate.From(request.StatementDate), request.SourceFileName, key, sourceHash, actor, now, lines);
            _dbContext.CustodianStatementImports.Add(import);
            await SaveHandlingValidationAsync(cancellationToken);
            var dto = MapImport(import);
            await WriteAuditAsync(AuditEventType.Created, "CustodianCashImported", "CustodianStatementImport", import.Id.ToString(), null, Snapshot(dto), "Custodian cash statement imported.", cancellationToken, key);
            return dto;
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "cashImport", exception.Message);
        }
    }

    public async Task<CustodyReconciliationRunDto> CreateReconciliationRunAsync(CreateCustodyReconciliationRunRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var actor = CurrentUserIdOrThrow();
            var now = _dateTimeProvider.UtcNow;
            await EnsureCustodianAsync(request.CustodianId, cancellationToken);

            var holdingsImport = await LoadImportAsync(request.HoldingsImportId, CustodianStatementType.Holdings, request.CustodianId, cancellationToken);
            var cashImport = await LoadImportAsync(request.CashImportId, CustodianStatementType.Cash, request.CustodianId, cancellationToken);
            if (holdingsImport is null && cashImport is null)
            {
                throw Validation("imports", "At least one holdings or cash import is required for reconciliation.");
            }

            var sourceJson = Snapshot(new { request.InternalHoldings, request.InternalCashBalances });
            var run = CustodyReconciliationRun.Create(request.CustodianId, request.HoldingsImportId, request.CashImportId, BusinessDate.From(request.BusinessDate), sourceJson, actor, now);

            if (holdingsImport is not null)
            {
                foreach (var reconciliationBreak in BuildHoldingBreaks(run.Id, holdingsImport.HoldingLines, request.InternalHoldings, actor, now))
                {
                    run.AddHoldingBreak(reconciliationBreak);
                    _dbContext.BreakAgings.Add(BreakAging.ForHolding(reconciliationBreak.Id, run.BusinessDate, AgeDays(reconciliationBreak.CreatedAtUtc, now), reconciliationBreak.Status, now));
                }

                holdingsImport.MarkReconciled();
            }

            if (cashImport is not null)
            {
                foreach (var reconciliationBreak in BuildCashBreaks(run.Id, cashImport.CashLines, request.InternalCashBalances, actor, now))
                {
                    run.AddCashBreak(reconciliationBreak);
                    _dbContext.BreakAgings.Add(BreakAging.ForCash(reconciliationBreak.Id, run.BusinessDate, AgeDays(reconciliationBreak.CreatedAtUtc, now), reconciliationBreak.Status, now));
                }

                cashImport.MarkReconciled();
            }

            _dbContext.CustodyReconciliationRuns.Add(run);
            await SaveHandlingValidationAsync(cancellationToken);
            var loaded = await LoadRunAsync(run.Id, cancellationToken);
            var dto = await MapRunAsync(loaded, cancellationToken);
            await WriteAuditAsync(AuditEventType.Created, "CustodyReconciliationRunCreated", "CustodyReconciliationRun", run.Id.ToString(), null, Snapshot(dto), "Custody reconciliation run completed.", cancellationToken);
            foreach (var reconciliationBreak in dto.Breaks)
            {
                await WriteAuditAsync(AuditEventType.Created, "CustodyBreakGenerated", reconciliationBreak.BreakCategory == "Holdings" ? "HoldingsReconciliationBreak" : "CashReconciliationBreak", reconciliationBreak.Id.ToString(), null, Snapshot(reconciliationBreak), "Reconciliation break automatically generated.", cancellationToken);
            }

            return dto;
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "reconciliationRun", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("reconciliationRun", exception.Message);
        }
    }

    public async Task<IReadOnlyCollection<CustodyReconciliationBreakDto>> GetBreaksAsync(string? status, CancellationToken cancellationToken = default)
    {
        ReconciliationBreakStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            parsedStatus = ParseEnum<ReconciliationBreakStatus>(status, "status");
        }

        var holdingQuery = _dbContext.HoldingsReconciliationBreaks.AsNoTracking();
        var cashQuery = _dbContext.CashReconciliationBreaks.AsNoTracking();
        if (parsedStatus.HasValue)
        {
            holdingQuery = holdingQuery.Where(item => item.Status == parsedStatus.Value);
            cashQuery = cashQuery.Where(item => item.Status == parsedStatus.Value);
        }

        var holdingBreaks = await holdingQuery.OrderByDescending(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
        var cashBreaks = await cashQuery.OrderByDescending(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
        var result = new List<CustodyReconciliationBreakDto>();
        foreach (var item in holdingBreaks)
        {
            result.Add(await MapHoldingBreakAsync(item, cancellationToken));
        }

        foreach (var item in cashBreaks)
        {
            result.Add(await MapCashBreakAsync(item, cancellationToken));
        }

        return result.OrderByDescending(item => item.CreatedAtUtc).ToArray();
    }

    public async Task<CustodyReconciliationBreakDto> AssignBreakAsync(Guid id, AssignCustodyBreakRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var holdingBreak = await _dbContext.HoldingsReconciliationBreaks.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (holdingBreak is not null)
        {
            var before = Snapshot(await MapHoldingBreakAsync(holdingBreak, cancellationToken));
            holdingBreak.Assign(request.OwnerUserId, now);
            _dbContext.BreakActionNotes.Add(BreakActionNote.ForHolding(holdingBreak.Id, request.Note, null, actor, now));
            _dbContext.BreakAgings.Add(BreakAging.ForHolding(holdingBreak.Id, BusinessDate.From(DateOnly.FromDateTime(now)), AgeDays(holdingBreak.CreatedAtUtc, now), holdingBreak.Status, now));
            await SaveHandlingValidationAsync(cancellationToken);
            var dto = await MapHoldingBreakAsync(holdingBreak, cancellationToken);
            await WriteAuditAsync(AuditEventType.Updated, "CustodyBreakAssigned", "HoldingsReconciliationBreak", holdingBreak.Id.ToString(), before, Snapshot(dto), request.Note, cancellationToken);
            return dto;
        }

        var cashBreak = await _dbContext.CashReconciliationBreaks.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Reconciliation break was not found.");
        var cashBefore = Snapshot(await MapCashBreakAsync(cashBreak, cancellationToken));
        cashBreak.Assign(request.OwnerUserId, now);
        _dbContext.BreakActionNotes.Add(BreakActionNote.ForCash(cashBreak.Id, request.Note, null, actor, now));
        _dbContext.BreakAgings.Add(BreakAging.ForCash(cashBreak.Id, BusinessDate.From(DateOnly.FromDateTime(now)), AgeDays(cashBreak.CreatedAtUtc, now), cashBreak.Status, now));
        await SaveHandlingValidationAsync(cancellationToken);
        var cashDto = await MapCashBreakAsync(cashBreak, cancellationToken);
        await WriteAuditAsync(AuditEventType.Updated, "CustodyBreakAssigned", "CashReconciliationBreak", cashBreak.Id.ToString(), cashBefore, Snapshot(cashDto), request.Note, cancellationToken);
        return cashDto;
    }

    public async Task<CustodyReconciliationBreakDto> ResolveBreakAsync(Guid id, ResolveCustodyBreakRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var holdingBreak = await _dbContext.HoldingsReconciliationBreaks.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (holdingBreak is not null)
        {
            var before = Snapshot(await MapHoldingBreakAsync(holdingBreak, cancellationToken));
            holdingBreak.Resolve(request.EvidenceReference, actor, now);
            _dbContext.BreakActionNotes.Add(BreakActionNote.ForHolding(holdingBreak.Id, request.Note, request.EvidenceReference, actor, now));
            _dbContext.BreakAgings.Add(BreakAging.ForHolding(holdingBreak.Id, BusinessDate.From(DateOnly.FromDateTime(now)), AgeDays(holdingBreak.CreatedAtUtc, now), holdingBreak.Status, now));
            await SaveHandlingValidationAsync(cancellationToken);
            var dto = await MapHoldingBreakAsync(holdingBreak, cancellationToken);
            await WriteAuditAsync(AuditEventType.Approved, "CustodyBreakResolved", "HoldingsReconciliationBreak", holdingBreak.Id.ToString(), before, Snapshot(dto), request.Note, cancellationToken);
            return dto;
        }

        var cashBreak = await _dbContext.CashReconciliationBreaks.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Reconciliation break was not found.");
        var cashBefore = Snapshot(await MapCashBreakAsync(cashBreak, cancellationToken));
        cashBreak.Resolve(request.EvidenceReference, actor, now);
        _dbContext.BreakActionNotes.Add(BreakActionNote.ForCash(cashBreak.Id, request.Note, request.EvidenceReference, actor, now));
        _dbContext.BreakAgings.Add(BreakAging.ForCash(cashBreak.Id, BusinessDate.From(DateOnly.FromDateTime(now)), AgeDays(cashBreak.CreatedAtUtc, now), cashBreak.Status, now));
        await SaveHandlingValidationAsync(cancellationToken);
        var cashDto = await MapCashBreakAsync(cashBreak, cancellationToken);
        await WriteAuditAsync(AuditEventType.Approved, "CustodyBreakResolved", "CashReconciliationBreak", cashBreak.Id.ToString(), cashBefore, Snapshot(cashDto), request.Note, cancellationToken);
        return cashDto;
    }

    public async Task<SafekeepingConfirmationDto> CreateSafekeepingConfirmationAsync(CreateSafekeepingConfirmationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var actor = CurrentUserIdOrThrow();
            var now = _dateTimeProvider.UtcNow;
            await EnsureCustodianAsync(request.CustodianId, cancellationToken);
            if (request.SchemeId.HasValue)
            {
                await EnsureSchemeReferencesAsync(request.SchemeId.Value, null, cancellationToken);
            }

            var queryDate = BusinessDate.From(request.BusinessDate);
            var holdingsImportId = request.SourceReconciliationRunId.HasValue
                ? await _dbContext.CustodyReconciliationRuns.AsNoTracking().Where(run => run.Id == request.SourceReconciliationRunId.Value).Select(run => run.HoldingsImportId).SingleOrDefaultAsync(cancellationToken)
                : await _dbContext.CustodianStatementImports.AsNoTracking()
                    .Where(import => import.CustodianId == request.CustodianId && import.StatementType == CustodianStatementType.Holdings && import.StatementDate.Value <= queryDate.Value)
                    .OrderByDescending(import => import.StatementDate)
                    .Select(import => (Guid?)import.Id)
                    .FirstOrDefaultAsync(cancellationToken);

            var cashImportId = request.SourceReconciliationRunId.HasValue
                ? await _dbContext.CustodyReconciliationRuns.AsNoTracking().Where(run => run.Id == request.SourceReconciliationRunId.Value).Select(run => run.CashImportId).SingleOrDefaultAsync(cancellationToken)
                : await _dbContext.CustodianStatementImports.AsNoTracking()
                    .Where(import => import.CustodianId == request.CustodianId && import.StatementType == CustodianStatementType.Cash && import.StatementDate.Value <= queryDate.Value)
                    .OrderByDescending(import => import.StatementDate)
                    .Select(import => (Guid?)import.Id)
                    .FirstOrDefaultAsync(cancellationToken);

            var holdings = await _dbContext.CustodianHoldingLines.AsNoTracking()
                .Where(line => holdingsImportId.HasValue && line.CustodianStatementImportId == holdingsImportId.Value && (!request.SchemeId.HasValue || line.SchemeId == request.SchemeId.Value))
                .ToListAsync(cancellationToken);
            var cashLines = await _dbContext.CustodianCashLines.AsNoTracking()
                .Where(line => cashImportId.HasValue && line.CustodianStatementImportId == cashImportId.Value && (!request.SchemeId.HasValue || line.SchemeId == request.SchemeId.Value))
                .ToListAsync(cancellationToken);

            var payload = Snapshot(new
            {
                request.CustodianId,
                request.SchemeId,
                request.SourceReconciliationRunId,
                request.BusinessDate,
                Holdings = holdings.Select(line => new { line.SchemeId, line.SchemeClassId, line.InstrumentCode, line.Quantity, line.MarketValue, line.Currency, line.IsSettled }),
                Cash = cashLines.Select(line => new { line.SchemeId, line.AccountNumber, line.Currency, line.CashBalance, line.IsSettled })
            });

            var confirmation = SafekeepingConfirmation.Create(
                request.CustodianId,
                request.SchemeId,
                request.SourceReconciliationRunId,
                queryDate,
                holdings.Count,
                cashLines.Count,
                holdings.Sum(line => line.MarketValue),
                cashLines.Sum(line => line.CashBalance),
                request.SettlementReference,
                request.SettlementConfirmed,
                payload,
                actor,
                now);

            _dbContext.SafekeepingConfirmations.Add(confirmation);
            await SaveHandlingValidationAsync(cancellationToken);
            var dto = MapSafekeepingConfirmation(confirmation);
            await WriteAuditAsync(AuditEventType.Created, "SafekeepingConfirmationCreated", "SafekeepingConfirmation", confirmation.Id.ToString(), null, Snapshot(dto), "Safekeeping confirmation source record created.", cancellationToken);
            return dto;
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "safekeepingConfirmation", exception.Message);
        }
    }

    private IEnumerable<HoldingsReconciliationBreak> BuildHoldingBreaks(Guid runId, IEnumerable<CustodianHoldingLine> custodianLines, IReadOnlyCollection<InternalHoldingSnapshotRequest> internalHoldings, string actor, DateTime now)
    {
        var internalLookup = internalHoldings.GroupBy(HoldingKey.From).ToDictionary(group => group.Key, group => new InternalHoldingSnapshot(
            group.Key,
            group.Sum(item => item.Quantity),
            group.Sum(item => item.MarketValue),
            group.First().Currency));
        var matchedKeys = new HashSet<HoldingKey>();

        foreach (var line in custodianLines)
        {
            var key = HoldingKey.From(line);
            matchedKeys.Add(key);
            internalLookup.TryGetValue(key, out var internalHolding);
            var internalQuantity = internalHolding?.Quantity ?? 0m;
            var internalMarketValue = internalHolding?.MarketValue ?? 0m;
            if (internalHolding is null)
            {
                yield return HoldingsReconciliationBreak.Create(runId, line.Id, line.SchemeId, line.SchemeClassId, line.InstrumentCode, 0m, line.Quantity, 0m, line.MarketValue, ReconciliationBreakType.MissingInternal, DetermineSeverity(line.MarketValue), actor, now);
            }
            else if (Math.Abs(line.Quantity - internalQuantity) > QuantityTolerance)
            {
                yield return HoldingsReconciliationBreak.Create(runId, line.Id, line.SchemeId, line.SchemeClassId, line.InstrumentCode, internalQuantity, line.Quantity, internalMarketValue, line.MarketValue, ReconciliationBreakType.QuantityMismatch, DetermineSeverity(Math.Abs(line.MarketValue - internalMarketValue)), actor, now);
            }
            else if (Math.Abs(line.MarketValue - internalMarketValue) > AmountTolerance)
            {
                yield return HoldingsReconciliationBreak.Create(runId, line.Id, line.SchemeId, line.SchemeClassId, line.InstrumentCode, internalQuantity, line.Quantity, internalMarketValue, line.MarketValue, ReconciliationBreakType.MarketValueMismatch, DetermineSeverity(Math.Abs(line.MarketValue - internalMarketValue)), actor, now);
            }

            if (!line.IsSettled)
            {
                yield return HoldingsReconciliationBreak.Create(runId, line.Id, line.SchemeId, line.SchemeClassId, line.InstrumentCode, internalQuantity, line.Quantity, internalMarketValue, line.MarketValue, ReconciliationBreakType.SettlementUnconfirmed, ReconciliationBreakSeverity.High, actor, now);
            }
        }

        foreach (var missing in internalLookup.Where(item => !matchedKeys.Contains(item.Key)))
        {
            yield return HoldingsReconciliationBreak.Create(runId, null, missing.Key.SchemeId, missing.Key.SchemeClassId, missing.Key.InstrumentCode, missing.Value.Quantity, 0m, missing.Value.MarketValue, 0m, ReconciliationBreakType.MissingCustodian, DetermineSeverity(missing.Value.MarketValue), actor, now);
        }
    }

    private IEnumerable<CashReconciliationBreak> BuildCashBreaks(Guid runId, IEnumerable<CustodianCashLine> custodianLines, IReadOnlyCollection<InternalCashSnapshotRequest> internalCashBalances, string actor, DateTime now)
    {
        var internalLookup = internalCashBalances.GroupBy(CashKey.From).ToDictionary(group => group.Key, group => group.Sum(item => item.CashBalance));
        var matchedKeys = new HashSet<CashKey>();

        foreach (var line in custodianLines)
        {
            var key = CashKey.From(line);
            matchedKeys.Add(key);
            internalLookup.TryGetValue(key, out var internalBalance);
            if (!internalLookup.ContainsKey(key))
            {
                yield return CashReconciliationBreak.Create(runId, line.Id, line.SchemeId, line.AccountNumber, line.Currency, 0m, line.CashBalance, ReconciliationBreakType.MissingInternal, DetermineSeverity(Math.Abs(line.CashBalance)), actor, now);
            }
            else if (Math.Abs(line.CashBalance - internalBalance) > AmountTolerance)
            {
                yield return CashReconciliationBreak.Create(runId, line.Id, line.SchemeId, line.AccountNumber, line.Currency, internalBalance, line.CashBalance, ReconciliationBreakType.CashAmountMismatch, DetermineSeverity(Math.Abs(line.CashBalance - internalBalance)), actor, now);
            }

            if (!line.IsSettled)
            {
                yield return CashReconciliationBreak.Create(runId, line.Id, line.SchemeId, line.AccountNumber, line.Currency, internalBalance, line.CashBalance, ReconciliationBreakType.SettlementUnconfirmed, ReconciliationBreakSeverity.High, actor, now);
            }
        }

        foreach (var missing in internalLookup.Where(item => !matchedKeys.Contains(item.Key)))
        {
            yield return CashReconciliationBreak.Create(runId, null, missing.Key.SchemeId, missing.Key.AccountNumber, missing.Key.Currency, missing.Value, 0m, ReconciliationBreakType.MissingCustodian, DetermineSeverity(Math.Abs(missing.Value)), actor, now);
        }
    }

    private async Task<CustodianStatementImport?> FindIdempotentImportAsync(string idempotencyKey, string sourceHash, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.CustodianStatementImports
            .Include(item => item.HoldingLines)
            .Include(item => item.CashLines)
            .SingleOrDefaultAsync(item => item.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        if (!string.Equals(existing.SourceHash, sourceHash, StringComparison.Ordinal))
        {
            throw new ConflictException("The idempotency key has already been used with a different custodian import payload.");
        }

        return existing;
    }

    private async Task<CustodianStatementImport?> LoadImportAsync(Guid? importId, CustodianStatementType type, Guid custodianId, CancellationToken cancellationToken)
    {
        if (!importId.HasValue)
        {
            return null;
        }

        var import = await _dbContext.CustodianStatementImports
            .Include(item => item.HoldingLines)
            .Include(item => item.CashLines)
            .SingleOrDefaultAsync(item => item.Id == importId.Value, cancellationToken)
            ?? throw new NotFoundException($"{type} custodian import was not found.");
        if (import.CustodianId != custodianId || import.StatementType != type)
        {
            throw Validation("importId", $"{type} import does not belong to the selected custodian or statement type.");
        }

        return import;
    }

    private async Task<CustodyReconciliationRun> LoadRunAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.CustodyReconciliationRuns
            .Include(run => run.HoldingBreaks)
            .Include(run => run.CashBreaks)
            .SingleAsync(run => run.Id == id, cancellationToken);
    }

    private async Task EnsureCustodianAsync(Guid custodianId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.Custodians.AnyAsync(custodian => custodian.Id == custodianId, cancellationToken))
        {
            throw new NotFoundException("Custodian was not found.");
        }
    }

    private async Task EnsureCustodianAccountAsync(Guid custodianId, Guid? accountId, CancellationToken cancellationToken)
    {
        if (!accountId.HasValue)
        {
            return;
        }

        if (!await _dbContext.CustodianAccounts.AnyAsync(account => account.Id == accountId.Value && account.CustodianId == custodianId, cancellationToken))
        {
            throw new NotFoundException("Custodian account was not found.");
        }
    }

    private async Task EnsureSchemeReferencesAsync(Guid schemeId, Guid? schemeClassId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.Schemes.AnyAsync(scheme => scheme.Id == schemeId, cancellationToken))
        {
            throw new NotFoundException("Scheme was not found.");
        }

        if (schemeClassId.HasValue && !await _dbContext.SchemeClasses.AnyAsync(schemeClass => schemeClass.Id == schemeClassId.Value && schemeClass.SchemeId == schemeId, cancellationToken))
        {
            throw new NotFoundException("Scheme class was not found.");
        }
    }

    private async Task SaveHandlingValidationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is not null)
        {
            throw new ConflictException(exception.InnerException.Message);
        }
    }

    private async Task<CustodyReconciliationRunDto> MapRunAsync(CustodyReconciliationRun run, CancellationToken cancellationToken)
    {
        var breaks = new List<CustodyReconciliationBreakDto>();
        foreach (var item in run.HoldingBreaks)
        {
            breaks.Add(await MapHoldingBreakAsync(item, cancellationToken));
        }

        foreach (var item in run.CashBreaks)
        {
            breaks.Add(await MapCashBreakAsync(item, cancellationToken));
        }

        return new CustodyReconciliationRunDto(
            run.Id,
            run.CustodianId,
            run.HoldingsImportId,
            run.CashImportId,
            run.BusinessDate.Value,
            run.Status.ToString(),
            run.RunByUserId,
            run.RunAtUtc,
            run.HoldingBreakCount,
            run.CashBreakCount,
            breaks.OrderBy(item => item.BreakCategory).ThenBy(item => item.BreakType).ToArray());
    }

    private async Task<CustodyReconciliationBreakDto> MapHoldingBreakAsync(HoldingsReconciliationBreak item, CancellationToken cancellationToken)
    {
        var latestAge = await _dbContext.BreakAgings.AsNoTracking()
            .Where(age => age.HoldingBreakId == item.Id)
            .OrderByDescending(age => age.CalculatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var latestNote = await _dbContext.BreakActionNotes.AsNoTracking()
            .Where(note => note.HoldingBreakId == item.Id)
            .OrderByDescending(note => note.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new CustodyReconciliationBreakDto(
            item.Id,
            item.ReconciliationRunId,
            "Holdings",
            item.BreakType.ToString(),
            item.Severity.ToString(),
            item.Status.ToString(),
            item.SchemeId,
            item.SchemeClassId,
            item.InstrumentCode,
            null,
            null,
            item.InternalQuantity,
            item.CustodianQuantity,
            item.QuantityDifference,
            item.InternalMarketValue,
            item.CustodianMarketValue,
            item.MarketValueDifference,
            0m,
            0m,
            0m,
            item.OwnerUserId,
            latestAge?.AgeDays ?? 0,
            latestNote?.Note,
            item.ResolutionEvidenceReference,
            item.CreatedAtUtc,
            item.ResolvedAtUtc);
    }

    private async Task<CustodyReconciliationBreakDto> MapCashBreakAsync(CashReconciliationBreak item, CancellationToken cancellationToken)
    {
        var latestAge = await _dbContext.BreakAgings.AsNoTracking()
            .Where(age => age.CashBreakId == item.Id)
            .OrderByDescending(age => age.CalculatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var latestNote = await _dbContext.BreakActionNotes.AsNoTracking()
            .Where(note => note.CashBreakId == item.Id)
            .OrderByDescending(note => note.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new CustodyReconciliationBreakDto(
            item.Id,
            item.ReconciliationRunId,
            "Cash",
            item.BreakType.ToString(),
            item.Severity.ToString(),
            item.Status.ToString(),
            item.SchemeId,
            null,
            null,
            item.AccountNumber,
            item.Currency,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            item.InternalCashBalance,
            item.CustodianCashBalance,
            item.Difference,
            item.OwnerUserId,
            latestAge?.AgeDays ?? 0,
            latestNote?.Note,
            item.ResolutionEvidenceReference,
            item.CreatedAtUtc,
            item.ResolvedAtUtc);
    }

    private static CustodianDto MapCustodian(Custodian custodian)
    {
        return new CustodianDto(
            custodian.Id,
            custodian.Code,
            custodian.Name,
            custodian.SwiftCode,
            custodian.Status.ToString(),
            custodian.CreatedByUserId,
            custodian.CreatedAtUtc,
            custodian.Accounts.Select(MapAccount).ToArray());
    }

    private static CustodianAccountDto MapAccount(CustodianAccount account)
    {
        return new CustodianAccountDto(account.Id, account.CustodianId, account.SchemeId, account.SchemeClassId, account.AccountNumber, account.AccountName, account.Currency, account.IsActive);
    }

    private static CustodianStatementImportDto MapImport(CustodianStatementImport import)
    {
        return new CustodianStatementImportDto(
            import.Id,
            import.CustodianId,
            import.CustodianAccountId,
            import.StatementType.ToString(),
            import.StatementDate.Value,
            import.SourceFileName,
            import.IdempotencyKey,
            import.SourceHash,
            import.Status.ToString(),
            import.ImportedByUserId,
            import.ImportedAtUtc,
            import.LineCount,
            import.HoldingLines.Select(MapHoldingLine).ToArray(),
            import.CashLines.Select(MapCashLine).ToArray());
    }

    private static CustodianHoldingLineDto MapHoldingLine(CustodianHoldingLine line)
    {
        return new CustodianHoldingLineDto(line.Id, line.CustodianStatementImportId, line.CustodianId, line.CustodianAccountId, line.SchemeId, line.SchemeClassId, line.InstrumentId, line.InstrumentCode, line.InstrumentName, line.Quantity, line.MarketValue, line.Currency, line.SettlementReference, line.IsSettled);
    }

    private static CustodianCashLineDto MapCashLine(CustodianCashLine line)
    {
        return new CustodianCashLineDto(line.Id, line.CustodianStatementImportId, line.CustodianId, line.CustodianAccountId, line.SchemeId, line.AccountNumber, line.Currency, line.BalanceDate.Value, line.CashBalance, line.SettlementReference, line.IsSettled);
    }

    private static SafekeepingConfirmationDto MapSafekeepingConfirmation(SafekeepingConfirmation confirmation)
    {
        return new SafekeepingConfirmationDto(
            confirmation.Id,
            confirmation.CustodianId,
            confirmation.SchemeId,
            confirmation.SourceReconciliationRunId,
            confirmation.BusinessDate.Value,
            confirmation.HoldingsCount,
            confirmation.CashLineCount,
            confirmation.TotalMarketValue,
            confirmation.TotalCashBalance,
            confirmation.SettlementReference,
            confirmation.SettlementConfirmed,
            confirmation.ConfirmationPayloadJson,
            confirmation.Status.ToString(),
            confirmation.GeneratedByUserId,
            confirmation.GeneratedAtUtc);
    }

    private static ReconciliationBreakSeverity DetermineSeverity(decimal exposure)
    {
        var absolute = Math.Abs(exposure);
        if (absolute >= 1_000_000m)
        {
            return ReconciliationBreakSeverity.High;
        }

        return absolute >= 100_000m ? ReconciliationBreakSeverity.Medium : ReconciliationBreakSeverity.Low;
    }

    private static int AgeDays(DateTime createdAtUtc, DateTime nowUtc)
    {
        return Math.Max(0, (DateOnly.FromDateTime(nowUtc) .DayNumber - DateOnly.FromDateTime(createdAtUtc).DayNumber));
    }

    private string CurrentUserIdOrThrow()
    {
        return _currentUserContext.UserId ?? throw new UnauthorizedAccessException("An authenticated user is required.");
    }

    private static string RequiredIdempotencyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw Validation("idempotencyKey", "Idempotency-Key header is required for custodian statement imports.");
        }

        return key.Trim();
    }

    private static TEnum ParseEnum<TEnum>(string value, string fieldName)
        where TEnum : struct
    {
        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
        {
            throw Validation(fieldName, $"{fieldName} is invalid.");
        }

        return parsed;
    }

    private static string SourceHash<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    private static string Snapshot<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private async Task WriteAuditAsync(AuditEventType eventType, string action, string entityName, string entityId, string? beforeJson, string? afterJson, string reason, CancellationToken cancellationToken, string? idempotencyKey = null)
    {
        await _auditWriter.WriteAsync(new AuditLogEntry(
            ModuleName,
            action,
            entityName,
            entityId,
            eventType,
            Summary: reason,
            BeforeJson: beforeJson,
            AfterJson: afterJson,
            Reason: reason,
            IdempotencyKey: idempotencyKey), cancellationToken);
    }

    private static ValidationException Validation(string field, string message)
    {
        return new ValidationException(new Dictionary<string, string[]> { [field] = [message] });
    }

    private sealed record HoldingKey(Guid SchemeId, Guid? SchemeClassId, string InstrumentCode, string Currency)
    {
        public static HoldingKey From(InternalHoldingSnapshotRequest request)
        {
            return new HoldingKey(request.SchemeId, request.SchemeClassId, request.InstrumentCode.Trim().ToUpperInvariant(), request.Currency.Trim().ToUpperInvariant());
        }

        public static HoldingKey From(CustodianHoldingLine line)
        {
            return new HoldingKey(line.SchemeId, line.SchemeClassId, line.InstrumentCode.Trim().ToUpperInvariant(), line.Currency.Trim().ToUpperInvariant());
        }
    }

    private sealed record InternalHoldingSnapshot(HoldingKey Key, decimal Quantity, decimal MarketValue, string Currency);

    private sealed record CashKey(Guid SchemeId, string AccountNumber, string Currency)
    {
        public static CashKey From(InternalCashSnapshotRequest request)
        {
            return new CashKey(request.SchemeId, request.AccountNumber.Trim().ToUpperInvariant(), request.Currency.Trim().ToUpperInvariant());
        }

        public static CashKey From(CustodianCashLine line)
        {
            return new CashKey(line.SchemeId, line.AccountNumber.Trim().ToUpperInvariant(), line.Currency.Trim().ToUpperInvariant());
        }
    }
}
