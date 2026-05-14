using System.Text.Json;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.UnitRegister;
using Cis.Domain.Audit;
using Cis.Domain.Common;
using Cis.Domain.Investors;
using Cis.Domain.Schemes;
using Cis.Domain.UnitRegister;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.UnitRegister;

internal sealed class UnitRegisterService : IUnitRegisterService
{
    private const string ModuleName = "UnitRegister";

    private readonly CisDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UnitRegisterService(
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

    public async Task<SchemeClassUnitRegisterDto> GetSchemeClassRegisterAsync(Guid schemeId, Guid schemeClassId, CancellationToken cancellationToken = default)
    {
        await EnsureActiveSchemeClassAsync(schemeId, schemeClassId, cancellationToken);

        var holdingEntities = await _dbContext.UnitHoldings
            .AsNoTracking()
            .Where(holding => holding.SchemeId == schemeId && holding.SchemeClassId == schemeClassId)
            .OrderBy(holding => holding.InvestorId)
            .ToListAsync(cancellationToken);
        var holdings = holdingEntities.Select(MapHolding).ToArray();

        var snapshotEntities = await _dbContext.UnitRegisterSnapshots
            .AsNoTracking()
            .Where(snapshot => snapshot.SchemeId == schemeId && snapshot.SchemeClassId == schemeClassId)
            .ToListAsync(cancellationToken);
        var latestSnapshot = snapshotEntities
            .OrderByDescending(snapshot => snapshot.SnapshotDate.Value)
            .ThenByDescending(snapshot => snapshot.Audit.CreatedAtUtc)
            .Select(MapSnapshot)
            .FirstOrDefault();

        return new SchemeClassUnitRegisterDto(
            schemeId,
            schemeClassId,
            holdings.Sum(holding => holding.Units),
            holdings.Sum(holding => holding.LienedUnits),
            holdings.Sum(holding => holding.RedeemableUnits),
            holdings.Length,
            holdings,
            latestSnapshot);
    }

    public async Task<IReadOnlyCollection<UnitHoldingDto>> GetInvestorHoldingsAsync(Guid investorId, CancellationToken cancellationToken = default)
    {
        await EnsureInvestorExistsAsync(investorId, cancellationToken);

        var holdings = await _dbContext.UnitHoldings
            .AsNoTracking()
            .Where(holding => holding.InvestorId == investorId)
            .OrderBy(holding => holding.SchemeId)
            .ThenBy(holding => holding.SchemeClassId)
            .ToListAsync(cancellationToken);
        return holdings.Select(MapHolding).ToArray();
    }

    public async Task<IReadOnlyCollection<HistoricalHoldingDto>> GetInvestorHistoricalHoldingsAsync(Guid investorId, DateOnly date, CancellationToken cancellationToken = default)
    {
        await EnsureInvestorExistsAsync(investorId, cancellationToken);
        var asOf = BusinessDate.From(date);
        var records = await _dbContext.HistoricalHoldingViews
            .AsNoTracking()
            .Where(record => record.InvestorId == investorId)
            .ToListAsync(cancellationToken);

        return records
            .Where(record => record.ValuationDate.Value <= asOf.Value)
            .GroupBy(record => new { record.InvestorId, record.SchemeId, record.SchemeClassId })
            .Select(group => group
                .OrderBy(record => record.ValuationDate.Value)
                .ThenBy(record => record.RecordedAtUtc)
                .ThenBy(record => record.Id)
                .Last())
            .OrderBy(record => record.SchemeId)
            .ThenBy(record => record.SchemeClassId)
            .Select(record => new HistoricalHoldingDto(
                record.InvestorId,
                record.SchemeId,
                record.SchemeClassId,
                record.ValuationDate.Value,
                record.TransactionReference,
                record.Units,
                record.LienedUnits,
                record.RedeemableUnits,
                record.UnitPrecision))
            .ToArray();
    }

    public async Task<UnitAdjustmentDto> CreateAdjustmentAsync(CreateUnitAdjustmentRequest request, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var normalizedIdempotencyKey = NormalizeIdempotencyKey(idempotencyKey);

        if (normalizedIdempotencyKey is not null)
        {
            var existing = await _dbContext.UnitAdjustments
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.IdempotencyKey == normalizedIdempotencyKey, cancellationToken);
            if (existing is not null)
            {
                if (!string.Equals(existing.RequestedByUserId, actor, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ConflictException("Idempotency key has already been used by a different user.");
                }

                if (!MatchesCreateRequest(existing, request))
                {
                    throw new ConflictException("Idempotency key cannot be reused with a different unit adjustment payload.");
                }

                return MapAdjustment(existing);
            }
        }

        await EnsureApprovedInvestorAsync(request.InvestorId, cancellationToken);
        await EnsureActiveSchemeClassAsync(request.SchemeId, request.SchemeClassId, cancellationToken);
        await EnsureUniqueTransactionReferenceAsync(request.TransactionReference, cancellationToken);

        var adjustment = CreateValidated(() => UnitAdjustment.Create(
            request.InvestorId,
            request.SchemeId,
            request.SchemeClassId,
            request.Units,
            BusinessDate.From(request.ValuationDate),
            request.TransactionReference,
            request.UnitPrecision,
            request.Reason,
            normalizedIdempotencyKey,
            actor,
            now));

        _dbContext.UnitAdjustments.Add(adjustment);
        await SaveHandlingValidationAsync(cancellationToken);

        var dto = MapAdjustment(adjustment);
        await WriteAuditAsync(AuditEventType.Created, "UnitAdjustmentCreated", "UnitAdjustment", adjustment.Id.ToString(), null, Snapshot(dto), "Unit adjustment submitted for approval.", cancellationToken, normalizedIdempotencyKey);
        return dto;
    }

    public async Task<UnitAdjustmentDto> ApproveAdjustmentAsync(Guid id, ApproveUnitAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var adjustment = await _dbContext.UnitAdjustments.FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException("Unit adjustment was not found.");
        var beforeJson = Snapshot(MapAdjustment(adjustment));

        try
        {
            adjustment.Approve(actor, now, request.Comment);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("unitAdjustment", exception.Message);
        }

        var source = UnitMovementSource.Create(
            UnitMovementSourceType.Adjustment,
            adjustment.Id,
            adjustment.InvestorId,
            adjustment.SchemeId,
            adjustment.SchemeClassId,
            adjustment.TransactionReference,
            isApproved: true,
            actor,
            now);
        var entry = CreateValidated(() => UnitLedgerEntry.Create(
            UnitMovementType.Adjustment,
            adjustment.InvestorId,
            adjustment.SchemeId,
            adjustment.SchemeClassId,
            adjustment.ValuationDate,
            adjustment.Units,
            source,
            adjustment.TransactionReference,
            adjustment.UnitPrecision,
            actor,
            now,
            adjustment.Reason,
            adjustment.Id));

        _dbContext.UnitMovementSources.Add(source);
        _dbContext.UnitLedgerEntries.Add(entry);

        var holdings = await _dbContext.UnitHoldings
            .Where(holding => holding.SchemeId == adjustment.SchemeId && holding.SchemeClassId == adjustment.SchemeClassId)
            .ToListAsync(cancellationToken);
        var holding = holdings.SingleOrDefault(candidate => candidate.InvestorId == adjustment.InvestorId);
        if (holding is null)
        {
            holding = UnitHolding.Create(adjustment.InvestorId, adjustment.SchemeId, adjustment.SchemeClassId, adjustment.UnitPrecision, actor, now);
            holdings.Add(holding);
            _dbContext.UnitHoldings.Add(holding);
        }

        ApplyLedgerEntry(holding, entry);
        var position = await _dbContext.InvestorPositions.SingleOrDefaultAsync(candidate =>
            candidate.InvestorId == adjustment.InvestorId
            && candidate.SchemeId == adjustment.SchemeId
            && candidate.SchemeClassId == adjustment.SchemeClassId, cancellationToken);
        if (position is null)
        {
            position = InvestorPosition.Create(adjustment.InvestorId, adjustment.SchemeId, adjustment.SchemeClassId, adjustment.UnitPrecision, actor, now);
            _dbContext.InvestorPositions.Add(position);
        }

        position.RefreshFrom(holding);

        var historicalRecord = HistoricalHoldingView.Create(
            adjustment.InvestorId,
            adjustment.SchemeId,
            adjustment.SchemeClassId,
            adjustment.ValuationDate,
            adjustment.TransactionReference,
            holding.Units,
            holding.LienedUnits,
            adjustment.UnitPrecision,
            entry.Id,
            actor,
            now);
        _dbContext.HistoricalHoldingViews.Add(historicalRecord);

        var snapshots = await _dbContext.UnitRegisterSnapshots
            .Where(candidate => candidate.SchemeId == adjustment.SchemeId && candidate.SchemeClassId == adjustment.SchemeClassId)
            .ToListAsync(cancellationToken);
        var snapshot = snapshots.SingleOrDefault(candidate => candidate.SnapshotDate == adjustment.ValuationDate);
        if (snapshot is null)
        {
            snapshot = UnitRegisterSnapshot.Create(adjustment.SchemeId, adjustment.SchemeClassId, adjustment.ValuationDate, actor, now);
            _dbContext.UnitRegisterSnapshots.Add(snapshot);
        }

        snapshot.Refresh(holdings.Sum(candidate => candidate.Units), holdings.Sum(candidate => candidate.LienedUnits), holdings.Count(candidate => candidate.Units != 0m || candidate.LienedUnits != 0m), adjustment.TransactionReference);

        await SaveHandlingValidationAsync(cancellationToken);

        var dto = MapAdjustment(adjustment);
        var afterJson = Snapshot(dto);
        var movementJson = Snapshot(MapLedgerEntry(entry));
        await WriteAuditAsync(AuditEventType.Approved, "UnitAdjustmentApproved", "UnitAdjustment", adjustment.Id.ToString(), beforeJson, afterJson, request.Comment ?? "Unit adjustment approved.", cancellationToken);
        await WriteAuditAsync(AuditEventType.Created, "UnitMovementPosted", "UnitLedgerEntry", entry.Id.ToString(), null, movementJson, "Unit movement posted from approved adjustment.", cancellationToken);
        return dto;
    }

    private async Task EnsureInvestorExistsAsync(Guid investorId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Investors.AnyAsync(investor => investor.Id == investorId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("Investor was not found.");
        }
    }

    private async Task EnsureApprovedInvestorAsync(Guid investorId, CancellationToken cancellationToken)
    {
        var investor = await _dbContext.Investors.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == investorId, cancellationToken)
            ?? throw new NotFoundException("Investor was not found.");
        if (investor.Status != InvestorStatus.Approved)
        {
            throw Validation("investorId", "Unit movements cannot be created for an unapproved investor.");
        }
    }

    private async Task EnsureActiveSchemeClassAsync(Guid schemeId, Guid schemeClassId, CancellationToken cancellationToken)
    {
        var scheme = await _dbContext.Schemes.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == schemeId, cancellationToken)
            ?? throw new NotFoundException("Scheme was not found.");
        if (scheme.Status != SchemeStatus.Active)
        {
            throw Validation("schemeId", "Unit register movements require an active scheme.");
        }

        var schemeClass = await _dbContext.SchemeClasses.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == schemeClassId && candidate.SchemeId == schemeId, cancellationToken)
            ?? throw new NotFoundException("Scheme class was not found.");
        if (!schemeClass.IsActive)
        {
            throw Validation("schemeClassId", "Unit register movements require an active scheme class.");
        }
    }

    private async Task EnsureUniqueTransactionReferenceAsync(string transactionReference, CancellationToken cancellationToken)
    {
        var normalized = transactionReference.Trim().ToUpperInvariant();
        var adjustmentExists = await _dbContext.UnitAdjustments.AnyAsync(adjustment => adjustment.TransactionReference == normalized, cancellationToken);
        var movementExists = await _dbContext.UnitLedgerEntries.AnyAsync(entry => entry.TransactionReference == normalized, cancellationToken);
        if (adjustmentExists || movementExists)
        {
            throw new ConflictException("Unit transaction reference already exists.");
        }
    }

    private static void ApplyLedgerEntry(UnitHolding holding, UnitLedgerEntry entry)
    {
        try
        {
            holding.Apply(entry);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("units", exception.Message);
        }
    }

    private static UnitAdjustmentDto MapAdjustment(UnitAdjustment adjustment)
    {
        return new UnitAdjustmentDto(
            adjustment.Id,
            adjustment.InvestorId,
            adjustment.SchemeId,
            adjustment.SchemeClassId,
            adjustment.Units,
            adjustment.ValuationDate.Value,
            adjustment.TransactionReference,
            adjustment.UnitPrecision,
            adjustment.Reason,
            adjustment.Status.ToString(),
            adjustment.RequestedByUserId,
            adjustment.RequestedAtUtc,
            adjustment.ApprovedByUserId,
            adjustment.ApprovedAtUtc);
    }

    private static UnitLedgerEntryDto MapLedgerEntry(UnitLedgerEntry entry)
    {
        return new UnitLedgerEntryDto(
            entry.Id,
            entry.MovementType.ToString(),
            entry.InvestorId,
            entry.SchemeId,
            entry.SchemeClassId,
            entry.ValuationDate.Value,
            entry.Units,
            entry.BalanceUnits,
            entry.LienUnits,
            entry.SourceType.ToString(),
            entry.SourceEntityId,
            entry.TransactionReference,
            entry.UnitPrecision,
            entry.PostedByUserId,
            entry.PostedAtUtc,
            entry.Narrative);
    }

    private static UnitHoldingDto MapHolding(UnitHolding holding)
    {
        return new UnitHoldingDto(
            holding.Id,
            holding.InvestorId,
            holding.SchemeId,
            holding.SchemeClassId,
            holding.Units,
            holding.LienedUnits,
            holding.RedeemableUnits,
            holding.UnitPrecision,
            holding.LastMovementDate?.Value,
            holding.LastTransactionReference);
    }

    private static UnitRegisterSnapshotDto MapSnapshot(UnitRegisterSnapshot snapshot)
    {
        return new UnitRegisterSnapshotDto(
            snapshot.Id,
            snapshot.SchemeId,
            snapshot.SchemeClassId,
            snapshot.SnapshotDate.Value,
            snapshot.TotalUnits,
            snapshot.LienedUnits,
            snapshot.RedeemableUnits,
            snapshot.HoldingCount,
            snapshot.LastTransactionReference);
    }

    private static string Snapshot(object value)
    {
        return JsonSerializer.Serialize(value);
    }

    private Task WriteAuditAsync(
        AuditEventType eventType,
        string action,
        string entityName,
        string entityId,
        string? beforeJson,
        string afterJson,
        string reason,
        CancellationToken cancellationToken,
        string? idempotencyKey = null)
    {
        return _auditWriter.WriteAsync(new AuditLogEntry(
            ModuleName,
            action,
            entityName,
            entityId,
            EventType: eventType,
            Summary: reason,
            BeforeJson: beforeJson,
            AfterJson: afterJson,
            Reason: reason,
            IdempotencyKey: idempotencyKey), cancellationToken);
    }

    private static bool MatchesCreateRequest(UnitAdjustment adjustment, CreateUnitAdjustmentRequest request)
    {
        return adjustment.InvestorId == request.InvestorId
            && adjustment.SchemeId == request.SchemeId
            && adjustment.SchemeClassId == request.SchemeClassId
            && adjustment.ValuationDate.Value == request.ValuationDate
            && adjustment.UnitPrecision == request.UnitPrecision
            && adjustment.Units == Math.Round(request.Units, request.UnitPrecision, MidpointRounding.AwayFromZero)
            && string.Equals(adjustment.TransactionReference, request.TransactionReference.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(adjustment.Reason, request.Reason.Trim(), StringComparison.Ordinal);
    }

    private static string? NormalizeIdempotencyKey(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return null;
        }

        var normalized = idempotencyKey.Trim();
        if (normalized.Length > 200)
        {
            throw Validation("idempotencyKey", "Idempotency key cannot exceed 200 characters.");
        }

        return normalized;
    }

    private async Task SaveHandlingValidationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is not null)
        {
            throw new ConflictException("The unit register change conflicts with an existing record.");
        }
    }

    private static T CreateValidated<T>(Func<T> factory)
    {
        try
        {
            return factory();
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("unitRegister", exception.Message);
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "unitRegister", exception.Message);
        }
    }

    private static ValidationException Validation(string fieldName, string message)
    {
        return new ValidationException(new Dictionary<string, string[]>
        {
            [fieldName] = [message]
        });
    }

    private string CurrentUserIdOrThrow()
    {
        return _currentUserContext.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user id is required.");
    }
}
