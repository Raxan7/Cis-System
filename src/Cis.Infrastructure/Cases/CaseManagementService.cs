using System.Text.Json;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.Cases;
using Cis.Domain.Audit;
using Cis.Domain.Cases;
using Cis.Domain.Common;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.Cases;

internal sealed class CaseManagementService : ICaseManagementService
{
    private const string ModuleName = "Cases";
    private readonly CisDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CaseManagementService(
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

    public async Task<ServiceCaseDto> CreateCaseAsync(CreateCaseRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var priority = ParseEnum<ServiceCasePriority>(request.Priority, nameof(request.Priority));
        var now = _dateTimeProvider.UtcNow;

        if (request.InvestorId.HasValue && !await _dbContext.Investors.AnyAsync(investor => investor.Id == request.InvestorId.Value, cancellationToken))
        {
            throw Validation(nameof(request.InvestorId), "Investor was not found.");
        }

        var policy = await FindSlaPolicyAsync(request.Category, priority, cancellationToken);
        var sequence = await _dbContext.ServiceCases.CountAsync(c => c.LoggedAtUtc.Date == now.Date, cancellationToken) + 1;
        var caseNumber = $"CASE-{now:yyyyMMdd}-{sequence:D6}";
        var complaintReference = $"CMP-{now:yyyyMMdd}-{sequence:D6}";
        var serviceCase = ServiceCase.Create(
            caseNumber,
            request.InvestorId,
            ServiceCaseType.Complaint,
            request.Category,
            request.Subject,
            request.Description,
            priority,
            actor,
            now,
            BusinessDate.From(now),
            now.AddHours(policy.TargetHours));

        var complaint = Complaint.Create(serviceCase.Id, complaintReference, request.ComplaintSource, now, request.IsRegulatory, request.RegulatoryCategory);
        _dbContext.ServiceCases.Add(serviceCase);
        _dbContext.Complaints.Add(complaint);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await AuditAsync("CaseCreated", "ServiceCase", serviceCase.Id, AuditEventType.Created, null, ToJson(serviceCase), "Complaint case logged.", cancellationToken);
        return await GetCaseAsync(serviceCase.Id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ServiceCaseDto>> GetCasesAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var complaints = await _dbContext.Complaints.AsNoTracking().ToDictionaryAsync(complaint => complaint.ServiceCaseId, cancellationToken);
        var cases = await IncludeCases(_dbContext.ServiceCases.AsNoTracking())
            .OrderByDescending(serviceCase => serviceCase.LoggedAtUtc)
            .ToListAsync(cancellationToken);

        return cases.Select(serviceCase => ToDto(serviceCase, complaints.GetValueOrDefault(serviceCase.Id), now)).ToArray();
    }

    public async Task<ServiceCaseDto> GetCaseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var serviceCase = await IncludeCases(_dbContext.ServiceCases.AsNoTracking())
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException("Case was not found.");
        var complaint = await _dbContext.Complaints.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.ServiceCaseId == id, cancellationToken);
        return ToDto(serviceCase, complaint, _dateTimeProvider.UtcNow);
    }

    public async Task<ServiceCaseDto> AssignCaseAsync(Guid id, AssignCaseRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var serviceCase = await LoadCaseAsync(id, cancellationToken);
        var originalRowVersion = serviceCase.RowVersion;
        var before = ToJson(ToDto(serviceCase, await LoadComplaintAsync(id, cancellationToken), _dateTimeProvider.UtcNow));
        serviceCase.Assign(request.OwnerUserId, actor, _dateTimeProvider.UtcNow);
        serviceCase.AddAction(CaseActionType.StatusUpdate, $"Assigned to {request.OwnerUserId}.", null, actor, _dateTimeProvider.UtcNow);
        _dbContext.Entry(serviceCase).Property(nameof(ServiceCase.RowVersion)).OriginalValue = originalRowVersion;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync("CaseAssigned", "ServiceCase", serviceCase.Id, AuditEventType.Updated, before, ToJson(serviceCase), $"Assigned to {request.OwnerUserId}.", cancellationToken);
        return await GetCaseAsync(id, cancellationToken);
    }

    public async Task<ServiceCaseDto> AddActionAsync(Guid id, AddCaseActionRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var actionType = ParseEnum<CaseActionType>(request.ActionType, nameof(request.ActionType));
        var serviceCase = await LoadCaseAsync(id, cancellationToken);
        var originalRowVersion = serviceCase.RowVersion;
        var before = ToJson(ToDto(serviceCase, await LoadComplaintAsync(id, cancellationToken), _dateTimeProvider.UtcNow));
        serviceCase.AddAction(actionType, request.Summary, request.EvidenceReference, actor, _dateTimeProvider.UtcNow);
        _dbContext.Entry(serviceCase).Property(nameof(ServiceCase.RowVersion)).OriginalValue = originalRowVersion;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync("CaseActionAdded", "ServiceCase", serviceCase.Id, AuditEventType.Updated, before, ToJson(serviceCase), request.Summary, cancellationToken);
        return await GetCaseAsync(id, cancellationToken);
    }

    public async Task<ServiceCaseDto> EscalateCaseAsync(Guid id, EscalateCaseRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var serviceCase = await LoadCaseAsync(id, cancellationToken);
        var originalRowVersion = serviceCase.RowVersion;
        var before = ToJson(ToDto(serviceCase, await LoadComplaintAsync(id, cancellationToken), _dateTimeProvider.UtcNow));
        serviceCase.Escalate(request.Reason, request.EscalatedToRole, request.EscalatedToUserId, actor, _dateTimeProvider.UtcNow);
        _dbContext.Entry(serviceCase).Property(nameof(ServiceCase.RowVersion)).OriginalValue = originalRowVersion;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync("CaseEscalated", "ServiceCase", serviceCase.Id, AuditEventType.Updated, before, ToJson(serviceCase), request.Reason, cancellationToken);
        return await GetCaseAsync(id, cancellationToken);
    }

    public async Task<ServiceCaseDto> ResolveCaseAsync(Guid id, ResolveCaseRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var serviceCase = await LoadCaseAsync(id, cancellationToken);
        var originalRowVersion = serviceCase.RowVersion;
        var complaint = await LoadComplaintAsync(id, cancellationToken);
        var before = ToJson(ToDto(serviceCase, complaint, _dateTimeProvider.UtcNow));
        var now = _dateTimeProvider.UtcNow;
        serviceCase.Resolve(request.ResolutionSummary, request.ResolutionEvidenceReference, actor, now);
        complaint?.MarkResolved(now);
        _dbContext.Entry(serviceCase).Property(nameof(ServiceCase.RowVersion)).OriginalValue = originalRowVersion;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await AuditAsync("CaseResolved", "ServiceCase", serviceCase.Id, AuditEventType.Updated, before, ToJson(serviceCase), request.ResolutionSummary, cancellationToken);
        return await GetCaseAsync(id, cancellationToken);
    }

    public async Task<CaseDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var cases = await _dbContext.ServiceCases.AsNoTracking().ToListAsync(cancellationToken);
        var turnaroundDays = await _dbContext.Complaints
            .AsNoTracking()
            .Where(complaint => complaint.TurnaroundDays.HasValue)
            .Select(complaint => (decimal?)complaint.TurnaroundDays!.Value)
            .AverageAsync(cancellationToken) ?? 0m;

        return new CaseDashboardDto(
            cases.Count,
            cases.Count(serviceCase => serviceCase.Status is not (ServiceCaseStatus.Resolved or ServiceCaseStatus.Closed)),
            cases.Count(serviceCase => serviceCase.Status == ServiceCaseStatus.Resolved),
            cases.Count(serviceCase => serviceCase.Status == ServiceCaseStatus.Escalated),
            cases.Count(serviceCase => serviceCase.IsSlaBreached(now)),
            Math.Round(turnaroundDays, 2));
    }

    private async Task<CaseSlaPolicy> FindSlaPolicyAsync(string category, ServiceCasePriority priority, CancellationToken cancellationToken)
    {
        return await _dbContext.CaseSlaPolicies
            .AsNoTracking()
            .Where(policy => policy.IsActive && policy.Priority == priority && (policy.Category == category || policy.Category == "General"))
            .OrderByDescending(policy => policy.Category == category)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw Validation(nameof(CreateCaseRequest.Priority), "No active SLA policy exists for the selected priority.");
    }

    private async Task<ServiceCase> LoadCaseAsync(Guid id, CancellationToken cancellationToken)
    {
        return await IncludeCases(_dbContext.ServiceCases)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException("Case was not found.");
    }

    private async Task<Complaint?> LoadComplaintAsync(Guid serviceCaseId, CancellationToken cancellationToken)
    {
        return await _dbContext.Complaints.SingleOrDefaultAsync(candidate => candidate.ServiceCaseId == serviceCaseId, cancellationToken);
    }

    private static IQueryable<ServiceCase> IncludeCases(IQueryable<ServiceCase> query)
    {
        return query
            .Include(serviceCase => serviceCase.Actions)
            .Include(serviceCase => serviceCase.Escalations)
            .Include(serviceCase => serviceCase.StatusHistory);
    }

    private static ServiceCaseDto ToDto(ServiceCase serviceCase, Complaint? complaint, DateTime now)
    {
        return new ServiceCaseDto(
            serviceCase.Id,
            serviceCase.CaseNumber,
            serviceCase.InvestorId,
            serviceCase.CaseType.ToString(),
            serviceCase.Category,
            serviceCase.Subject,
            serviceCase.Description,
            serviceCase.Priority.ToString(),
            serviceCase.Status.ToString(),
            serviceCase.OwnerUserId,
            serviceCase.LoggedByUserId,
            serviceCase.LoggedAtUtc,
            serviceCase.BusinessDate.Value,
            serviceCase.SlaTargetAtUtc,
            serviceCase.IsSlaBreached(now),
            serviceCase.AgingDays(now),
            serviceCase.ResolvedByUserId,
            serviceCase.ResolvedAtUtc,
            serviceCase.ResolutionSummary,
            serviceCase.ResolutionEvidenceReference,
            complaint is null ? null : new ComplaintDto(complaint.Id, complaint.ServiceCaseId, complaint.ComplaintReference, complaint.Source, complaint.ReceivedAtUtc, complaint.IsRegulatory, complaint.RegulatoryCategory, complaint.TurnaroundDays),
            serviceCase.Actions.OrderBy(action => action.ActionedAtUtc).Select(action => new CaseActionDto(action.Id, action.ActionType.ToString(), action.Summary, action.EvidenceReference, action.ActionedByUserId, action.ActionedAtUtc)).ToArray(),
            serviceCase.Escalations.OrderBy(escalation => escalation.EscalatedAtUtc).Select(escalation => new CaseEscalationDto(escalation.Id, escalation.Reason, escalation.EscalatedToRole, escalation.EscalatedToUserId, escalation.EscalatedByUserId, escalation.EscalatedAtUtc, escalation.Status.ToString(), escalation.ResolvedByUserId, escalation.ResolvedAtUtc)).ToArray(),
            serviceCase.StatusHistory.OrderBy(history => history.ChangedAtUtc).Select(history => new CaseStatusHistoryDto(history.Id, history.FromStatus?.ToString(), history.ToStatus.ToString(), history.ChangedByUserId, history.ChangedAtUtc, history.Reason)).ToArray());
    }

    private static TEnum ParseEnum<TEnum>(string value, string fieldName) where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, true, out var parsed)
            ? parsed
            : throw Validation(fieldName, $"{fieldName} has an invalid value.");
    }

    private string CurrentUserIdOrThrow()
    {
        return _currentUserContext.UserId ?? throw new UnauthorizedAccessException("Authenticated user is required.");
    }

    private Task AuditAsync(string action, string entityName, Guid entityId, AuditEventType eventType, string? beforeJson, string? afterJson, string reason, CancellationToken cancellationToken)
    {
        var role = _currentUserContext.Roles.Count == 0 ? null : string.Join(",", _currentUserContext.Roles);
        return _auditWriter.WriteAsync(new AuditLogEntry(
            ModuleName,
            action,
            entityName,
            entityId.ToString(),
            eventType,
            _currentUserContext.UserId,
            _currentUserContext.DisplayName,
            role,
            _currentUserContext.CorrelationId,
            _currentUserContext.IpAddress,
            _currentUserContext.UserAgent,
            reason,
            null,
            beforeJson,
            afterJson,
            reason), cancellationToken);
    }

    private static string ToJson(object value)
    {
        return JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static ValidationException Validation(string fieldName, string message)
    {
        return new ValidationException(new Dictionary<string, string[]> { [fieldName] = [message] });
    }
}
