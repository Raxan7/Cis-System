using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Reports;
using Cis.Domain.Audit;
using Cis.Domain.Common;
using Cis.Domain.Reports;
using Cis.Infrastructure.Operations;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Cis.Infrastructure.Reports;

internal sealed class ReportService : IReportService
{
    private const string ModuleName = "Reports";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly CisDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ReportQueryHandlerRegistry _queryHandlerRegistry;
    private readonly IMemoryCache _memoryCache;
    private readonly IBackgroundJobDispatcher _backgroundJobDispatcher;
    private readonly PerformanceExecutionOptions _performanceExecutionOptions;

    public ReportService(
        CisDbContext dbContext,
        IAuditWriter auditWriter,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        IMemoryCache memoryCache,
        IBackgroundJobDispatcher backgroundJobDispatcher,
        IOptions<PerformanceExecutionOptions> performanceExecutionOptions)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
        _queryHandlerRegistry = new ReportQueryHandlerRegistry(dbContext, dateTimeProvider);
        _memoryCache = memoryCache;
        _backgroundJobDispatcher = backgroundJobDispatcher;
        _performanceExecutionOptions = performanceExecutionOptions.Value;
    }

    public async Task<PagedResult<ReportDefinitionDto>> GetDefinitionsAsync(PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var definitions = await _memoryCache.GetOrCreateAsync("reports:definitions:active", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            entry.SlidingExpiration = TimeSpan.FromMinutes(5);

            var loaded = await _dbContext.ReportDefinitions
                .AsNoTracking()
                .Include(definition => definition.Owners)
                .Where(definition => definition.IsActive)
                .OrderBy(definition => definition.Code)
                .ToListAsync(cancellationToken);

            return loaded.Select(MapDefinition).ToArray();
        }) ?? [];

        var accessible = definitions
            .Where(definition => CanAccess(definition.RequiredPermission))
            .ToArray();

        var filtered = ApplyReportDefinitionSearch(accessible, pagination);
        var totalCount = filtered.Count;
        var items = ApplyReportDefinitionSorting(filtered, pagination)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToArray();

        return new PagedResult<ReportDefinitionDto>(items, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<ReportScheduleDto> CreateScheduleAsync(CreateReportScheduleRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var definition = await LoadDefinitionByCodeAsync(request.ReportCode, cancellationToken);
        EnsureCanAccess(definition);
        var frequency = ParseEnum<ReportFrequency>(request.Frequency, "frequency");

        try
        {
            var schedule = ReportSchedule.Create(definition.Id, frequency, request.CronExpression, request.NextRunAtUtc, actor, now);
            _dbContext.ReportSchedules.Add(schedule);
            await SaveHandlingValidationAsync(cancellationToken);
            var dto = MapSchedule(schedule, definition.Code);
            await WriteAuditAsync(AuditEventType.Created, "ReportScheduleCreated", "ReportSchedule", schedule.Id.ToString(), null, Snapshot(dto), "Report schedule created.", cancellationToken);
            return dto;
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "schedule", exception.Message);
        }
    }

    public async Task<ReportRunDto> RunReportAsync(string code, RunReportRequest request, CancellationToken cancellationToken = default)
    {
        var definition = await LoadDefinitionByCodeAsync(code, cancellationToken);
        EnsureCanAccess(definition);

        if (request.Outputs.Count == 0)
        {
            throw Validation("outputs", "At least one report output format is required.");
        }

        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        if (ShouldRunAsBackgroundJob(definition.Code, request))
        {
            return await _backgroundJobDispatcher.ExecuteAsync(
                $"report:{definition.Code}:{request.BusinessDate:yyyyMMdd}",
                workerCancellationToken => RunReportCoreAsync(definition, request, actor, now, workerCancellationToken),
                cancellationToken);
        }

        return await RunReportCoreAsync(definition, request, actor, now, cancellationToken);
    }

    private async Task<ReportRunDto> RunReportCoreAsync(
        ReportDefinition definition,
        RunReportRequest request,
        string actor,
        DateTime now,
        CancellationToken cancellationToken)
    {

        try
        {
            var run = ReportRun.Create(definition.Id, definition.Code, BusinessDate.From(request.BusinessDate), now, actor, now);
            foreach (var parameter in request.Parameters)
            {
                run.AddParameter(parameter.Name, parameter.Value);
            }

            var sourceSnapshot = Snapshot(new
            {
                definition.Code,
                definition.Name,
                request.BusinessDate,
                request.Parameters,
                SourceTimestamp = now,
                Counts = new
                {
                    Schemes = await _dbContext.Schemes.CountAsync(cancellationToken),
                    Investors = await _dbContext.Investors.CountAsync(cancellationToken),
                    UnitHoldings = await _dbContext.UnitHoldings.CountAsync(cancellationToken),
                    ReportRuns = await _dbContext.ReportRuns.CountAsync(cancellationToken)
                }
            });

            foreach (var outputRequest in request.Outputs)
            {
                var format = ParseEnum<ReportOutputFormat>(outputRequest.Format, "format");
                var storageReference = $"reports/{definition.Code}/{request.BusinessDate:yyyyMMdd}/{run.Id:N}.{Extension(format)}";
                var content = $"{definition.Code}|{request.BusinessDate:yyyy-MM-dd}|{format}|{sourceSnapshot}";
                run.AddOutput(ReportOutput.Create(run.Id, format, storageReference, Sha256(content), Encoding.UTF8.GetByteCount(content), now));
            }

            _dbContext.ReportRuns.Add(run);
            await SaveHandlingValidationAsync(cancellationToken);
            var loaded = await LoadRunAsync(run.Id, asTracking: false, cancellationToken);
            var dto = MapRun(loaded);
            await WriteAuditAsync(AuditEventType.Created, "ReportRunGenerated", "ReportRun", run.Id.ToString(), null, Snapshot(dto), "Report run generated with parameters and source timestamp.", cancellationToken);
            return dto;
        }
        catch (ArgumentException exception)
        {
            throw Validation(exception.ParamName ?? "reportRun", exception.Message);
        }
    }

    public async Task<ReportQueryResultDto> QueryReportAsync(string code, ReportQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var definition = await LoadDefinitionByCodeAsync(code, cancellationToken);
        EnsureCanAccess(definition);
        var handler = _queryHandlerRegistry.Get(definition.Code);
        return await handler.QueryAsync(parameters, cancellationToken);
    }

    public async Task<ReportCsvExportDto> ExportReportCsvAsync(string code, ReportQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        var definition = await LoadDefinitionByCodeAsync(code, cancellationToken);
        EnsureCanAccess(definition);
        var handler = _queryHandlerRegistry.Get(definition.Code);
        var export = await handler.ExportCsvAsync(parameters, cancellationToken);
        await WriteAuditAsync(AuditEventType.Exported, "ReportCsvExported", "ReportDefinition", definition.Id.ToString(), null, Snapshot(export), $"CSV export generated for {definition.Code}.", cancellationToken);
        return export;
    }

    public async Task<ReportRunDto> ApproveRunAsync(Guid id, ReportActionRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var run = await LoadRunAsync(id, asTracking: true, cancellationToken);
        var definition = await _dbContext.ReportDefinitions.SingleAsync(definition => definition.Id == run.ReportDefinitionId, cancellationToken);
        EnsureCanAccess(definition);
        var before = Snapshot(MapRun(run));

        try
        {
            run.Approve(actor, now, request.Comment);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("reportRun", exception.Message);
        }

        MarkNewApprovalAdded(run.Id);
        await SaveHandlingValidationAsync(cancellationToken);
        var dto = MapRun(run);
        await WriteAuditAsync(AuditEventType.Approved, "ReportRunApproved", "ReportRun", run.Id.ToString(), before, Snapshot(dto), request.Comment ?? "Report run approved.", cancellationToken);
        return dto;
    }

    public async Task<ReportRunDto> PublishRunAsync(Guid id, ReportActionRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var run = await LoadRunAsync(id, asTracking: true, cancellationToken);
        var definition = await _dbContext.ReportDefinitions.SingleAsync(definition => definition.Id == run.ReportDefinitionId, cancellationToken);
        EnsureCanAccess(definition);
        var before = Snapshot(MapRun(run));

        try
        {
            run.Publish(actor, now, request.Comment);
        }
        catch (InvalidOperationException exception)
        {
            throw Validation("reportRun", exception.Message);
        }

        MarkNewDistributionAdded(run.Id);
        var payload = Snapshot(new { Definition = MapDefinition(definition), Run = MapRun(run) });
        _dbContext.ReportVersionArchives.Add(ReportVersionArchive.Create(run.Id, run.VersionNumber, payload, Sha256(payload), actor, now));
        await SaveHandlingValidationAsync(cancellationToken);
        var dto = MapRun(run);
        await WriteAuditAsync(AuditEventType.Published, "ReportRunPublished", "ReportRun", run.Id.ToString(), before, Snapshot(dto), request.Comment ?? "Official report published.", cancellationToken);
        return dto;
    }

    public async Task<PagedResult<ReportRunDto>> GetRunsAsync(PaginationRequest pagination, CancellationToken cancellationToken = default)
    {
        var definitions = await _dbContext.ReportDefinitions
            .AsNoTracking()
            .ToDictionaryAsync(definition => definition.Id, cancellationToken);
        var accessibleDefinitionIds = definitions
            .Where(item => CanAccess(item.Value))
            .Select(item => item.Key)
            .ToArray();

        var query = ApplyReportRunQuery(
            _dbContext.ReportRuns
                .AsNoTracking()
                .Include(run => run.Parameters)
                .Include(run => run.Outputs)
                .Include(run => run.Approvals)
                .Include(run => run.Distributions)
                .Where(run => accessibleDefinitionIds.Contains(run.ReportDefinitionId)),
            pagination);
        var totalCount = await query.CountAsync(cancellationToken);
        var runs = await ApplyReportRunSorting(query, pagination)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ReportRunDto>(runs.Select(MapRun).ToArray(), totalCount, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<ReportDownloadDto> DownloadRunAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var run = await LoadRunAsync(id, asTracking: false, cancellationToken);
        var definition = await _dbContext.ReportDefinitions.AsNoTracking().SingleAsync(definition => definition.Id == run.ReportDefinitionId, cancellationToken);
        EnsureCanAccess(definition);
        if (run.Status != ReportRunStatus.Published)
        {
            throw Validation("reportRun", "Only published report runs can be downloaded.");
        }

        var output = run.Outputs.OrderBy(output => output.Format).FirstOrDefault()
            ?? throw new NotFoundException("Report output was not found.");
        var dto = new ReportDownloadDto(run.Id, run.ReportCode, run.Status.ToString(), output.Format.ToString(), output.StorageReference, output.ContentHash, output.SizeBytes);
        await WriteAuditAsync(AuditEventType.Exported, "ReportRunDownloaded", "ReportRun", run.Id.ToString(), null, Snapshot(dto), "Published report downloaded.", cancellationToken);
        return dto;
    }

    public async Task<ReportBundleDto> CreateBundleAsync(CreateReportBundleRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        if (request.ReportRunIds.Count == 0)
        {
            throw Validation("reportRunIds", "At least one report run is required for a bundle.");
        }

        var runs = await _dbContext.ReportRuns.AsNoTracking().Where(run => request.ReportRunIds.Contains(run.Id)).ToListAsync(cancellationToken);
        if (runs.Count != request.ReportRunIds.Distinct().Count())
        {
            throw new NotFoundException("One or more report runs were not found.");
        }

        var bundle = ReportBundle.Create(request.BundleCode, request.Name, BusinessDate.From(request.BusinessDate), Snapshot(request.ReportRunIds.Distinct().ToArray()), actor, now);
        _dbContext.ReportBundles.Add(bundle);
        await SaveHandlingValidationAsync(cancellationToken);
        var dto = MapBundle(bundle);
        await WriteAuditAsync(AuditEventType.Created, "ReportBundleCreated", "ReportBundle", bundle.Id.ToString(), null, Snapshot(dto), "Report bundle created.", cancellationToken);
        return dto;
    }

    public async Task<ReportBundleDto> PublishBundleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var bundle = await _dbContext.ReportBundles.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new NotFoundException("Report bundle was not found.");
        var runIds = DeserializeRunIds(bundle.ReportRunIdsJson);
        var unpublishedCount = await _dbContext.ReportRuns.CountAsync(run => runIds.Contains(run.Id) && run.Status != ReportRunStatus.Published, cancellationToken);
        if (unpublishedCount > 0)
        {
            throw Validation("bundle", "All report runs in a bundle must be published before the bundle can be published.");
        }

        var before = Snapshot(MapBundle(bundle));
        bundle.Publish(actor, now);
        await SaveHandlingValidationAsync(cancellationToken);
        var dto = MapBundle(bundle);
        await WriteAuditAsync(AuditEventType.Published, "ReportBundlePublished", "ReportBundle", bundle.Id.ToString(), before, Snapshot(dto), "Report bundle published.", cancellationToken);
        return dto;
    }

    private async Task<ReportDefinition> LoadDefinitionByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _dbContext.ReportDefinitions
            .Include(definition => definition.Owners)
            .SingleOrDefaultAsync(definition => definition.Code == code.Trim().ToUpperInvariant() && definition.IsActive, cancellationToken)
            ?? throw new NotFoundException("Report definition was not found.");
    }

    private async Task<ReportRun> LoadRunAsync(Guid id, bool asTracking, CancellationToken cancellationToken)
    {
        var query = _dbContext.ReportRuns
            .Include(run => run.Parameters)
            .Include(run => run.Outputs)
            .Include(run => run.Approvals)
            .Include(run => run.Distributions)
            .Where(run => run.Id == id);
        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Report run was not found.");
    }

    private bool CanAccess(ReportDefinition definition)
    {
        return CanAccess(definition.RequiredPermission);
    }

    private bool CanAccess(string requiredPermission)
    {
        return HasPermission(Permissions.All)
            || HasPermission(requiredPermission);
    }

    private void EnsureCanAccess(ReportDefinition definition)
    {
        if (!CanAccess(definition))
        {
            throw new UnauthorizedAccessException("Current user is not permitted to access this report.");
        }
    }

    private bool HasPermission(string permission)
    {
        if (_currentUserContext.Roles.Count == 0)
        {
            return false;
        }

        foreach (var role in _currentUserContext.Roles)
        {
            if (!RolePermissionCatalog.RolePermissions.TryGetValue(role, out var permissions))
            {
                continue;
            }

            if (permissions.Contains(Permissions.All) || permissions.Contains(permission))
            {
                return true;
            }
        }

        return false;
    }

    private void MarkNewApprovalAdded(Guid reportRunId)
    {
        foreach (var entry in _dbContext.ChangeTracker.Entries<ReportApproval>().Where(entry => entry.Entity.ReportRunId == reportRunId && entry.State == EntityState.Modified))
        {
            entry.State = EntityState.Added;
        }
    }

    private void MarkNewDistributionAdded(Guid reportRunId)
    {
        foreach (var entry in _dbContext.ChangeTracker.Entries<ReportDistribution>().Where(entry => entry.Entity.ReportRunId == reportRunId && entry.State == EntityState.Modified))
        {
            entry.State = EntityState.Added;
        }
    }

    private static ReportDefinitionDto MapDefinition(ReportDefinition definition)
    {
        return new ReportDefinitionDto(
            definition.Id,
            definition.Code,
            definition.Name,
            definition.Category,
            definition.Frequency.ToString(),
            definition.PrimaryUsers,
            definition.RequiredPermission,
            definition.IsActive,
            definition.Owners.Select(owner => new ReportOwnerMatrixDto(owner.Id, owner.OwnerRole, owner.Responsibility)).ToArray());
    }

    private static ReportScheduleDto MapSchedule(ReportSchedule schedule, string reportCode)
    {
        return new ReportScheduleDto(schedule.Id, schedule.ReportDefinitionId, reportCode, schedule.Frequency.ToString(), schedule.CronExpression, schedule.NextRunAtUtc, schedule.Status.ToString());
    }

    private static ReportRunDto MapRun(ReportRun run)
    {
        return new ReportRunDto(
            run.Id,
            run.ReportDefinitionId,
            run.ReportCode,
            run.BusinessDate.Value,
            run.SourceDataTimestampUtc,
            run.Status.ToString(),
            run.VersionNumber,
            run.GeneratedByUserId,
            run.GeneratedAtUtc,
            run.ApprovedByUserId,
            run.ApprovedAtUtc,
            run.PublishedByUserId,
            run.PublishedAtUtc,
            run.Parameters.Select(parameter => new ReportParameterDto(parameter.Id, parameter.Name, parameter.Value)).ToArray(),
            run.Outputs.Select(output => new ReportOutputDto(output.Id, output.Format.ToString(), output.StorageReference, output.ContentHash, output.SizeBytes, output.CreatedAtUtc)).ToArray(),
            run.Approvals.Select(approval => new ReportApprovalDto(approval.Id, approval.Decision.ToString(), approval.DecidedByUserId, approval.DecidedAtUtc, approval.Comment)).ToArray(),
            run.Distributions.Select(distribution => new ReportDistributionDto(distribution.Id, distribution.Channel.ToString(), distribution.Recipient, distribution.DistributedByUserId, distribution.DistributedAtUtc, distribution.Comment)).ToArray());
    }

    private static ReportBundleDto MapBundle(ReportBundle bundle)
    {
        return new ReportBundleDto(bundle.Id, bundle.BundleCode, bundle.Name, bundle.BusinessDate.Value, DeserializeRunIds(bundle.ReportRunIdsJson), bundle.Status.ToString(), bundle.CreatedByUserId, bundle.CreatedAtUtc, bundle.PublishedByUserId, bundle.PublishedAtUtc);
    }

    private static IReadOnlyCollection<Guid> DeserializeRunIds(string json)
    {
        return JsonSerializer.Deserialize<Guid[]>(json, JsonOptions) ?? [];
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

    private string CurrentUserIdOrThrow()
    {
        return _currentUserContext.UserId ?? throw new UnauthorizedAccessException("An authenticated user is required.");
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

    private static string Extension(ReportOutputFormat format)
    {
        return format switch
        {
            ReportOutputFormat.PDF => "pdf",
            ReportOutputFormat.Excel => "xlsx",
            ReportOutputFormat.CSV => "csv",
            ReportOutputFormat.Word => "docx",
            ReportOutputFormat.HTML => "html",
            _ => "bin"
        };
    }

    private async Task WriteAuditAsync(AuditEventType eventType, string action, string entityName, string entityId, string? beforeJson, string? afterJson, string reason, CancellationToken cancellationToken)
    {
        await _auditWriter.WriteAsync(new AuditLogEntry(ModuleName, action, entityName, entityId, eventType, Summary: reason, BeforeJson: beforeJson, AfterJson: afterJson, Reason: reason), cancellationToken);
    }

    private static string Snapshot<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static string Sha256(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static ValidationException Validation(string field, string message)
    {
        return new ValidationException(new Dictionary<string, string[]> { [field] = [message] });
    }

    private static IReadOnlyCollection<ReportDefinitionDto> ApplyReportDefinitionSearch(IReadOnlyCollection<ReportDefinitionDto> definitions, PaginationRequest pagination)
    {
        if (string.IsNullOrWhiteSpace(pagination.Search))
        {
            return definitions;
        }

        var search = pagination.Search.Trim();
        return definitions.Where(definition =>
            definition.Code.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            definition.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            definition.Category.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static IEnumerable<ReportDefinitionDto> ApplyReportDefinitionSorting(IReadOnlyCollection<ReportDefinitionDto> definitions, PaginationRequest pagination)
    {
        var descending = pagination.IsDescending;
        return (pagination.SortBy ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "NAME" => descending ? definitions.OrderByDescending(definition => definition.Name).ThenBy(definition => definition.Code) : definitions.OrderBy(definition => definition.Name).ThenBy(definition => definition.Code),
            "CATEGORY" => descending ? definitions.OrderByDescending(definition => definition.Category).ThenBy(definition => definition.Code) : definitions.OrderBy(definition => definition.Category).ThenBy(definition => definition.Code),
            "FREQUENCY" => descending ? definitions.OrderByDescending(definition => definition.Frequency).ThenBy(definition => definition.Code) : definitions.OrderBy(definition => definition.Frequency).ThenBy(definition => definition.Code),
            _ => descending ? definitions.OrderByDescending(definition => definition.Code) : definitions.OrderBy(definition => definition.Code)
        };
    }

    private static IQueryable<ReportRun> ApplyReportRunQuery(IQueryable<ReportRun> query, PaginationRequest pagination)
    {
        if (string.IsNullOrWhiteSpace(pagination.Search))
        {
            return query;
        }

        var search = pagination.Search.Trim();
        return query.Where(run =>
            run.ReportCode.Contains(search) ||
            run.GeneratedByUserId.Contains(search) ||
            run.Status.ToString().Contains(search));
    }

    private static IQueryable<ReportRun> ApplyReportRunSorting(IQueryable<ReportRun> query, PaginationRequest pagination)
    {
        var descending = pagination.IsDescending;
        return (pagination.SortBy ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "BUSINESSDATE" => descending ? query.OrderByDescending(run => run.BusinessDate).ThenByDescending(run => run.GeneratedAtUtc) : query.OrderBy(run => run.BusinessDate).ThenBy(run => run.GeneratedAtUtc),
            "REPORTCODE" => descending ? query.OrderByDescending(run => run.ReportCode).ThenByDescending(run => run.GeneratedAtUtc) : query.OrderBy(run => run.ReportCode).ThenByDescending(run => run.GeneratedAtUtc),
            "STATUS" => descending ? query.OrderByDescending(run => run.Status).ThenByDescending(run => run.GeneratedAtUtc) : query.OrderBy(run => run.Status).ThenByDescending(run => run.GeneratedAtUtc),
            _ => descending || string.IsNullOrWhiteSpace(pagination.SortBy)
                ? query.OrderByDescending(run => run.GeneratedAtUtc).ThenByDescending(run => run.Id)
                : query.OrderBy(run => run.GeneratedAtUtc).ThenBy(run => run.Id)
        };
    }

    private bool ShouldRunAsBackgroundJob(string reportCode, RunReportRequest request)
    {
        if (request.Outputs.Count >= _performanceExecutionOptions.HeavyReportOutputThreshold)
        {
            return true;
        }

        return _performanceExecutionOptions.HeavyReportCodePrefixes.Any(prefix =>
            reportCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
