using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.Portfolio;
using Cis.Domain.Audit;
using Cis.Domain.Common;
using Cis.Domain.Portfolio;
using Cis.Domain.Schemes;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Cis.Infrastructure.Portfolio;

internal sealed class PortfolioService : IPortfolioService
{
    private const string ModuleName = "Portfolio";
    private readonly CisDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PortfolioService(
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

    public async Task<InstrumentDto> CreateInstrumentAsync(CreateInstrumentRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;

        var existing = await _dbContext.Instruments
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Isin == request.Isin.ToUpperInvariant(), cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"Instrument with ISIN {request.Isin} already exists.");
        }

        var instrument = Instrument.Create(
            request.Isin,
            request.Name,
            Enum.Parse<InstrumentType>(request.InstrumentType),
            request.Currency,
            actor,
            now);

        if (request.CounterpartyId.HasValue)
        {
            instrument.AttachCounterparty(request.CounterpartyId.Value);
        }

        if (request.IssuerId.HasValue)
        {
            instrument.AttachIssuer(request.IssuerId.Value);
        }

        if (request.YieldRate.HasValue || request.CouponRate.HasValue)
        {
            instrument.SetYieldAndCoupon(request.YieldRate, request.CouponRate);
        }

        _dbContext.Instruments.Add(instrument);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await WriteAuditAsync(AuditEventType.Created, "InstrumentCreated", "Instrument", instrument.Id.ToString(), null, Snapshot(MapInstrument(instrument)), $"Instrument {instrument.Isin} created.", cancellationToken);
        return MapInstrument(instrument);
    }

    public async Task<InstrumentDto> GetInstrumentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var instrument = await _dbContext.Instruments
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new NotFoundException("Instrument not found.");
        return MapInstrument(instrument);
    }

    public async Task<IReadOnlyCollection<InstrumentDto>> GetInstrumentsAsync(CancellationToken cancellationToken = default)
    {
        var instruments = await _dbContext.Instruments
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return instruments.Select(MapInstrument).ToList();
    }

    public async Task<CounterpartyDto> CreateCounterpartyAsync(CreateCounterpartyRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;

        var existing = await _dbContext.Counterparties
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == request.Code.ToUpperInvariant(), cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"Counterparty with code {request.Code} already exists.");
        }

        var counterparty = Counterparty.Create(request.Code, request.Name, request.Contact, actor, now);
        _dbContext.Counterparties.Add(counterparty);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await WriteAuditAsync(AuditEventType.Created, "CounterpartyCreated", "Counterparty", counterparty.Id.ToString(), null, Snapshot(MapCounterparty(counterparty)), $"Counterparty {counterparty.Name} created.", cancellationToken);
        return MapCounterparty(counterparty);
    }

    public async Task<CounterpartyDto> GetCounterpartyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var counterparty = await _dbContext.Counterparties
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundException("Counterparty not found.");
        return MapCounterparty(counterparty);
    }

    public async Task<IReadOnlyCollection<CounterpartyDto>> GetCounterpartiesAsync(CancellationToken cancellationToken = default)
    {
        var counterparties = await _dbContext.Counterparties
            .AsNoTracking()
            .Where(c => c.Status == PlacementStatus.Active)
            .ToListAsync(cancellationToken);
        return counterparties.Select(MapCounterparty).ToList();
    }

    public async Task<PlacementDto> CreatePlacementAsync(CreatePlacementRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;

        await EnsureSchemeClassActiveAsync(request.SchemeId, request.SchemeClassId, cancellationToken);
        await EnsureInstrumentExistsAsync(request.InstrumentId, cancellationToken);

        var placement = Placement.Create(
            request.SchemeId,
            request.SchemeClassId,
            request.InstrumentId,
            request.CounterpartyId,
            request.IssuerId,
            request.Principal,
            request.Currency,
            BusinessDate.From(request.AcquisitionDate),
            BusinessDate.From(request.MaturityDate),
            request.Yield,
            request.AccruedIncome,
            actor,
            now);

        // Create income schedule for coupons if yield > 0
        if (placement.Yield > 0)
        {
            var currentDate = request.AcquisitionDate;
            while (currentDate < request.MaturityDate)
            {
                currentDate = currentDate.AddMonths(6);
                if (currentDate <= request.MaturityDate)
                {
                    var couponAmount = placement.Principal * (placement.Yield / 100m) / 2m;
                    var schedule = IncomeSchedule.Create(
                        placement.Id,
                        "Coupon",
                        BusinessDate.From(currentDate),
                        couponAmount,
                        false,
                        now);
                    placement.AddIncomeSchedule(schedule);
                }
            }
        }

        _dbContext.Placements.Add(placement);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await WriteAuditAsync(AuditEventType.Created, "PlacementCreated", "Placement", placement.Id.ToString(), null, Snapshot(await MapPlacementAsync(placement, cancellationToken)), $"Placement created for instrument {request.InstrumentId}.", cancellationToken);
        return await MapPlacementAsync(placement, cancellationToken);
    }

    public async Task<PlacementDto> SubmitPlacementAsync(Guid id, PlacementSubmitRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;

        var placement = await _dbContext.Placements
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException("Placement not found.");

        try
        {
            placement.Submit(actor, now);
        }
        catch (InvalidOperationException ex)
        {
            throw Validation("placement", ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(AuditEventType.Updated, "PlacementSubmitted", "Placement", placement.Id.ToString(), null, Snapshot(await MapPlacementAsync(placement, cancellationToken)), "Placement submitted for approval.", cancellationToken);
        return await MapPlacementAsync(placement, cancellationToken);
    }

    public async Task<PlacementDto> ApprovePlacementAsync(Guid id, PlacementApproveRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;

        var placement = await _dbContext.Placements
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException("Placement not found.");

        var approvedRules = await _dbContext.ApprovedInstrumentRules
            .AsNoTracking()
            .Where(r => r.SchemeId == placement.SchemeId && r.Status == RuleStatus.Active)
            .ToListAsync(cancellationToken);

        if (approvedRules.Count > 0)
        {
            var instrument = await _dbContext.Instruments
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == placement.InstrumentId, cancellationToken);

            if (instrument is not null)
            {
                foreach (var rule in approvedRules)
                {
                    if (rule.InstrumentType == instrument.InstrumentType.ToString())
                    {
                        var tenor = (int)(placement.MaturityDate.Value.ToDateTime(TimeOnly.MinValue) - placement.AcquisitionDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays;
                        var maxAllowedTenorDays = rule.TenorLimitDays;
                        if (instrument.InstrumentType is InstrumentType.TreasuryBond or InstrumentType.Bond or InstrumentType.Sukuk)
                        {
                            // Long-duration instruments allow a broader tenor window than money market instruments.
                            maxAllowedTenorDays = Math.Max(maxAllowedTenorDays, 3660);
                        }

                        if (tenor > maxAllowedTenorDays)
                        {
                            throw Validation("placement", $"Tenor of {tenor} days exceeds mandate limit of {maxAllowedTenorDays} days.");
                        }
                    }
                }
            }
        }

        try
        {
            placement.Approve(actor, now);
        }
        catch (InvalidOperationException ex)
        {
            throw Validation("placement", ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(AuditEventType.Updated, "PlacementApproved", "Placement", placement.Id.ToString(), null, Snapshot(await MapPlacementAsync(placement, cancellationToken)), "Placement approved.", cancellationToken);
        return await MapPlacementAsync(placement, cancellationToken);
    }

    public async Task<PlacementDto> GetPlacementAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var placement = await _dbContext.Placements
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException("Placement not found.");
        return await MapPlacementAsync(placement, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PortfolioHoldingDto>> GetHoldingsAsync(Guid schemeId, Guid schemeClassId, CancellationToken cancellationToken = default)
    {
        await EnsureSchemeClassActiveAsync(schemeId, schemeClassId, cancellationToken);

        var holdings = await _dbContext.PortfolioHoldings
            .AsNoTracking()
            .Where(h => h.SchemeId == schemeId && h.SchemeClassId == schemeClassId)
            .OrderBy(h => h.InstrumentId)
            .ToListAsync(cancellationToken);

        return holdings.Select(MapHolding).ToList();
    }

    public async Task<MaturityLadderDto> GetMaturityLadderAsync(Guid schemeId, Guid schemeClassId, CancellationToken cancellationToken = default)
    {
        await EnsureSchemeClassActiveAsync(schemeId, schemeClassId, cancellationToken);

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow.Date);
        var placements = await _dbContext.Placements
            .AsNoTracking()
            .Where(p => p.SchemeId == schemeId && p.SchemeClassId == schemeClassId)
            .ToListAsync(cancellationToken);

            // Filter by status in memory due to string enum storage
        var activePlacements = placements
            .Where(p => p.Status == PlacementStatus.Active || p.Status == PlacementStatus.Approved)
                .OrderBy(p => p.MaturityDate.Value)
            .ToList();

        var entries = new List<MaturityLadderEntryDto>();
        foreach (var placement in activePlacements)
        {
            var instrument = await _dbContext.Instruments
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == placement.InstrumentId, cancellationToken);

            if (instrument is not null)
            {
                var daysToMaturity = (int)(placement.MaturityDate.Value.ToDateTime(TimeOnly.MinValue) - today.ToDateTime(TimeOnly.MinValue)).TotalDays;
                entries.Add(new MaturityLadderEntryDto(
                    placement.Id,
                    instrument.Name,
                    instrument.Isin,
                    placement.Principal,
                    placement.Currency,
                    placement.MaturityDate.Value,
                    daysToMaturity,
                    placement.Status.ToString()));
            }
        }

        return new MaturityLadderDto(schemeId, schemeClassId, entries, _dateTimeProvider.UtcNow);
    }

    public async Task<IncomeDueDto> GetIncomeDueAsync(Guid schemeId, Guid schemeClassId, CancellationToken cancellationToken = default)
    {
        await EnsureSchemeClassActiveAsync(schemeId, schemeClassId, cancellationToken);

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow.Date);
        var placements = await _dbContext.Placements
            .Where(p => p.SchemeId == schemeId && p.SchemeClassId == schemeClassId)
            .ToListAsync(cancellationToken);

        var entries = new List<IncomeDueEntryDto>();
        var totalDue = 0m;
        var totalReceived = 0m;
        var currency = "KES";

        foreach (var placement in placements)
        {
            var instrument = await _dbContext.Instruments
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == placement.InstrumentId, cancellationToken);

            var schedules = await _dbContext.IncomeSchedules
                .AsNoTracking()
                .Where(s => s.PlacementId == placement.Id)
                .ToListAsync(cancellationToken);

            foreach (var schedule in schedules)
            {
                var daysOverdue = Math.Max(0, (int)(today.ToDateTime(TimeOnly.MinValue) - schedule.DueDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays);
                entries.Add(new IncomeDueEntryDto(
                    placement.Id,
                    instrument?.Name ?? "Unknown",
                    schedule.IncomeType,
                    schedule.DueDate.Value,
                    schedule.Amount,
                    placement.Currency,
                    schedule.Received,
                    daysOverdue));

                if (schedule.Received)
                {
                    totalReceived += schedule.Amount;
                }
                else
                {
                    totalDue += schedule.Amount;
                }

                currency = placement.Currency;
            }
        }

        return new IncomeDueDto(schemeId, schemeClassId, entries, totalDue, totalReceived, currency);
    }

    public async Task<IncomeScheduleDto> RecordIncomeReceiptAsync(IncomeReceiptRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;

        var schedule = await _dbContext.IncomeSchedules
            .FirstOrDefaultAsync(s => s.Id == request.IncomeScheduleId, cancellationToken)
            ?? throw new NotFoundException("Income schedule not found.");

        try
        {
            schedule.MarkReceived(actor, now);
        }
        catch (InvalidOperationException ex)
        {
            throw Validation("incomeSchedule", ex.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(AuditEventType.Updated, "IncomeReceiptRecorded", "IncomeSchedule", schedule.Id.ToString(), null, Snapshot(MapIncomeSchedule(schedule)), "Income receipt recorded.", cancellationToken);
        return MapIncomeSchedule(schedule);
    }

    public async Task<RolloverEventDto> CreateRolloverAsync(CreateRolloverRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;

        var originalPlacement = await _dbContext.Placements
            .FirstOrDefaultAsync(p => p.Id == request.PlacementId, cancellationToken)
            ?? throw new NotFoundException("Placement not found.");

        var newPlacement = Placement.Create(
            originalPlacement.SchemeId,
            originalPlacement.SchemeClassId,
            originalPlacement.InstrumentId,
            originalPlacement.CounterpartyId,
            originalPlacement.IssuerId,
            request.NewPrincipal ?? originalPlacement.Principal,
            originalPlacement.Currency,
            originalPlacement.MaturityDate,
            BusinessDate.From(request.NewMaturityDate),
            originalPlacement.Yield,
            0m,
            actor,
            now);

        _dbContext.Placements.Add(newPlacement);
        originalPlacement.MarkMatured();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await WriteAuditAsync(AuditEventType.Created, "RolloverCreated", "Placement", newPlacement.Id.ToString(), null, Snapshot(await MapPlacementAsync(newPlacement, cancellationToken)), $"Placement rolled over from {request.PlacementId}.", cancellationToken);

        return new RolloverEventDto(
            Guid.NewGuid(),
            originalPlacement.Id,
            newPlacement.Id,
            originalPlacement.MaturityDate.Value,
            request.NewMaturityDate,
            "ManualRequest",
            now);
    }

    private async Task EnsureSchemeClassActiveAsync(Guid schemeId, Guid classId, CancellationToken cancellationToken)
    {
        var schemeClass = await _dbContext.SchemeClasses
            .AsNoTracking()
            .FirstOrDefaultAsync(sc => sc.SchemeId == schemeId && sc.Id == classId, cancellationToken);

        if (schemeClass is null || !schemeClass.IsActive)
        {
            throw new NotFoundException("Scheme class not found or inactive.");
        }
    }

    private async Task EnsureInstrumentExistsAsync(Guid instrumentId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Instruments
            .AsNoTracking()
            .AnyAsync(i => i.Id == instrumentId, cancellationToken);

        if (!exists)
        {
            throw new NotFoundException("Instrument not found.");
        }
    }

    private string CurrentUserIdOrThrow()
    {
        var userId = _currentUserContext.UserId;
        return string.IsNullOrWhiteSpace(userId) ? throw new UnauthorizedAccessException("User context is not available.") : userId;
    }

    private async Task WriteAuditAsync(
        AuditEventType eventType,
        string action,
        string entityName,
        string entityId,
        string? beforeJson,
        string? afterJson,
        string reason,
        CancellationToken cancellationToken)
    {
        await _auditWriter.WriteAsync(new AuditLogEntry(
            ModuleName,
            action,
            entityName,
            entityId,
            eventType,
            CurrentUserIdOrThrow(),
            _currentUserContext.DisplayName,
            _currentUserContext.Roles.Count == 0 ? null : string.Join(",", _currentUserContext.Roles),
            _currentUserContext.CorrelationId,
            _currentUserContext.IpAddress,
            _currentUserContext.UserAgent,
            reason,
            null,
            beforeJson,
            afterJson,
            reason,
            null,
            null),
            cancellationToken);
    }

    private static string Snapshot<T>(T? obj)
    {
        return obj is null ? "{}" : JsonSerializer.Serialize(obj);
    }

    private static InstrumentDto MapInstrument(Instrument instrument)
    {
        return new InstrumentDto(
            instrument.Id,
            instrument.Isin,
            instrument.Name,
            instrument.InstrumentType.ToString(),
            instrument.Currency,
            instrument.Status.ToString(),
            instrument.CounterpartyId,
            instrument.IssuerId,
            instrument.YieldRate,
            instrument.CouponRate);
    }

    private static CounterpartyDto MapCounterparty(Counterparty counterparty)
    {
        return new CounterpartyDto(
            counterparty.Id,
            counterparty.Code,
            counterparty.Name,
            counterparty.Contact,
            counterparty.Status.ToString());
    }

    private static PortfolioHoldingDto MapHolding(PortfolioHolding holding)
    {
        return new PortfolioHoldingDto(
            holding.Id,
            holding.SchemeId,
            holding.SchemeClassId,
            holding.InstrumentId,
            holding.Quantity,
            holding.MarketValue,
            holding.Currency,
            holding.LastUpdatedAtUtc);
    }

    private static IncomeScheduleDto MapIncomeSchedule(IncomeSchedule schedule)
    {
        return new IncomeScheduleDto(
            schedule.Id,
            schedule.PlacementId,
            schedule.IncomeType,
            schedule.DueDate.Value,
            schedule.Amount,
            schedule.Received,
            schedule.ReceivedAtUtc,
            schedule.ReceivedByUserId);
    }

    private async Task<PlacementDto> MapPlacementAsync(Placement placement, CancellationToken cancellationToken)
    {
        var incomeSchedules = await _dbContext.IncomeSchedules
            .AsNoTracking()
            .Where(s => s.PlacementId == placement.Id)
            .Select(s => MapIncomeSchedule(s))
            .ToListAsync(cancellationToken);

        return new PlacementDto(
            placement.Id,
            placement.SchemeId,
            placement.SchemeClassId,
            placement.InstrumentId,
            placement.CounterpartyId,
            placement.IssuerId,
            placement.Principal,
            placement.Currency,
            placement.AcquisitionDate.Value,
            placement.MaturityDate.Value,
            placement.Yield,
            placement.AccruedIncome,
            placement.Status.ToString(),
            placement.SettlementStatus.ToString(),
            placement.SubmittedByUserId,
            placement.SubmittedAtUtc,
            placement.ApprovedByUserId,
            placement.ApprovedAtUtc,
            incomeSchedules);
    }

    private static ValidationException Validation(string field, string message)
    {
        return new ValidationException(new Dictionary<string, string[]>
        {
            [field] = [message]
        });
    }
}
