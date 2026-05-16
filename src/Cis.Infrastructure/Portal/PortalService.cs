using System.Text.Json;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.Portal;
using Cis.Contracts.Workflows;
using Cis.Domain.Audit;
using Cis.Domain.Dealing;
using Cis.Domain.NAV;
using Cis.Domain.Portal;
using Cis.Domain.UnitRegister;
using Cis.Domain.Workflows;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.Portal;

internal sealed class PortalService : IPortalService
{
    private const string ModuleName = "Portal";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly CisDbContext _dbContext;
    private readonly IWorkflowService _workflowService;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFileSecurityValidator _fileSecurityValidator;

    public PortalService(
        CisDbContext dbContext,
        IWorkflowService workflowService,
        IAuditWriter auditWriter,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        IFileSecurityValidator fileSecurityValidator)
    {
        _dbContext = dbContext;
        _workflowService = workflowService;
        _auditWriter = auditWriter;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
        _fileSecurityValidator = fileSecurityValidator;
    }

    public async Task<PortalProfileDto> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetPortalContextAsync(cancellationToken);
        await LogActivityAsync(context, PortalActivityType.ViewedProfile, "Portal profile viewed.", "Investor", context.Investor.Id.ToString(), cancellationToken);
        return MapProfile(context);
    }

    public async Task<IReadOnlyCollection<PortalHoldingDto>> GetHoldingsAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetPortalContextAsync(cancellationToken);
        var holdings = await _dbContext.UnitHoldings
            .AsNoTracking()
            .Where(holding => holding.InvestorId == context.Profile.InvestorId)
            .OrderBy(holding => holding.SchemeId)
            .ThenBy(holding => holding.SchemeClassId)
            .ToListAsync(cancellationToken);

        var schemeClassIds = holdings
            .Select(holding => holding.SchemeClassId)
            .Distinct()
            .ToArray();

        var latestNavByClass = schemeClassIds.Length == 0
            ? new Dictionary<Guid, PublishedNavSnapshot>()
            : await (from nav in _dbContext.NavPerUnits.AsNoTracking()
                     join run in _dbContext.ValuationRuns.AsNoTracking()
                         on nav.ValuationRunId equals run.Id
                     where schemeClassIds.Contains(nav.SchemeClassId)
                           && run.Status == ValuationRunStatus.Published
                           && run.PublishedAtUtc != null
                     orderby run.PublishedAtUtc descending
                     select new PublishedNavSnapshot(
                         nav.SchemeClassId,
                         nav.ReportedUnitPrice,
                         run.ValuationDate.Value))
                .GroupBy(snapshot => snapshot.SchemeClassId)
                .ToDictionaryAsync(group => group.Key, group => group.First(), cancellationToken);

        var result = holdings
            .Select(holding =>
            {
                latestNavByClass.TryGetValue(holding.SchemeClassId, out var navSnapshot);
                var unitPrice = navSnapshot?.UnitPrice;
                return new PortalHoldingDto(
                    holding.SchemeId,
                    holding.SchemeClassId,
                    holding.Units,
                    holding.LienedUnits,
                    holding.RedeemableUnits,
                    unitPrice,
                    unitPrice is null ? null : decimal.Round(holding.Units * unitPrice.Value, holding.UnitPrecision, MidpointRounding.AwayFromZero),
                    unitPrice is null ? null : decimal.Round(holding.RedeemableUnits * unitPrice.Value, holding.UnitPrecision, MidpointRounding.AwayFromZero),
                    holding.UnitPrecision,
                    navSnapshot?.ValuationDate,
                    holding.LastMovementDate == null ? null : holding.LastMovementDate.Value,
                    holding.LastTransactionReference);
            })
            .ToArray();

        await LogActivityAsync(context, PortalActivityType.ViewedHoldings, "Portal holdings viewed.", null, null, cancellationToken);
        return result;
    }

    public async Task<IReadOnlyCollection<PortalTransactionDto>> GetTransactionsAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetPortalContextAsync(cancellationToken);
        var ledgerTransactions = await _dbContext.UnitLedgerEntries
            .AsNoTracking()
            .Where(entry => entry.InvestorId == context.Profile.InvestorId)
            .OrderByDescending(entry => entry.ValuationDate)
            .ThenByDescending(entry => entry.PostedAtUtc)
            .Select(entry => new PortalTransactionDto(
                entry.Id,
                "UnitRegister",
                entry.MovementType.ToString(),
                entry.SchemeId,
                entry.SchemeClassId,
                entry.ValuationDate.Value,
                null,
                entry.Units,
                "Posted",
                entry.TransactionReference))
            .ToListAsync(cancellationToken);

        var dealingTransactions = await _dbContext.DealingInstructions
            .AsNoTracking()
            .Include(instruction => instruction.SubscriptionInstructions)
            .Include(instruction => instruction.RedemptionInstructions)
            .Include(instruction => instruction.SwitchInstructions)
            .Where(instruction => instruction.InvestorId == context.Profile.InvestorId)
            .OrderByDescending(instruction => instruction.ReceivedAtUtc)
            .ToListAsync(cancellationToken);

        var combined = ledgerTransactions
            .Concat(dealingTransactions.Select(MapDealingTransaction))
            .OrderByDescending(transaction => transaction.BusinessDate)
            .ThenBy(transaction => transaction.Reference)
            .ToArray();
        await LogActivityAsync(context, PortalActivityType.ViewedTransactions, "Portal transactions viewed.", null, null, cancellationToken);
        return combined;
    }

    public async Task<IReadOnlyCollection<PortalStatementDto>> GetStatementsAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetPortalContextAsync(cancellationToken);
        var now = _dateTimeProvider.UtcNow;
        var holdings = await _dbContext.UnitHoldings
            .AsNoTracking()
            .Where(holding => holding.InvestorId == context.Profile.InvestorId)
            .ToListAsync(cancellationToken);
        var statement = new PortalStatementDto(
            context.Profile.InvestorId,
            context.Profile.InvestorId,
            DateOnly.FromDateTime(now),
            $"STMT-{context.Investor.InvestorNumber}-{now:yyyyMMdd}",
            holdings.Sum(holding => holding.Units),
            holdings.Count);

        _dbContext.PortalDocumentDownloads.Add(PortalDocumentDownload.Create(context.Profile.InvestorId, context.Profile.UserId, "Statement", statement.StatementReference, now, _currentUserContext.IpAddress));
        await LogActivityAsync(context, PortalActivityType.DownloadedStatement, "Portal statement downloaded.", "PortalStatement", statement.StatementReference, cancellationToken, saveChanges: false);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(AuditEventType.Exported, "PortalStatementDownloaded", "PortalStatement", statement.StatementReference, null, Snapshot(statement), "Investor portal statement downloaded.", cancellationToken);
        return [statement];
    }

    public async Task<IReadOnlyCollection<PortalTaxCertificateDto>> GetTaxCertificatesAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetPortalContextAsync(cancellationToken);
        var now = _dateTimeProvider.UtcNow;
        var profiles = await _dbContext.InvestorTaxProfiles
            .AsNoTracking()
            .Where(profile => profile.InvestorId == context.Profile.InvestorId)
            .ToListAsync(cancellationToken);
        var certificates = profiles.Select(profile => new PortalTaxCertificateDto(
            profile.Id,
            profile.InvestorId,
            profile.TaxNumber,
            profile.CountryOfTaxResidence,
            DateOnly.FromDateTime(now),
            $"TAX-{context.Investor.InvestorNumber}-{now:yyyyMMdd}-{profile.TaxNumber}"))
            .ToArray();

        foreach (var certificate in certificates)
        {
            _dbContext.PortalDocumentDownloads.Add(PortalDocumentDownload.Create(context.Profile.InvestorId, context.Profile.UserId, "TaxCertificate", certificate.CertificateReference, now, _currentUserContext.IpAddress));
        }

        await LogActivityAsync(context, PortalActivityType.DownloadedTaxCertificate, "Portal tax certificates downloaded.", "PortalTaxCertificate", context.Profile.InvestorId.ToString(), cancellationToken, saveChanges: false);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(AuditEventType.Exported, "PortalTaxCertificatesDownloaded", "PortalTaxCertificate", context.Profile.InvestorId.ToString(), null, Snapshot(certificates), "Investor portal tax certificates downloaded.", cancellationToken);
        return certificates;
    }

    public async Task<IReadOnlyCollection<InvestorNoticeDto>> GetNoticesAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetPortalContextAsync(cancellationToken);
        var notices = await _dbContext.InvestorNotices
            .AsNoTracking()
            .Where(notice => notice.Status == InvestorNoticeStatus.Published && (notice.InvestorId == null || notice.InvestorId == context.Profile.InvestorId))
            .OrderByDescending(notice => notice.PublishedDate)
            .Select(notice => new InvestorNoticeDto(notice.Id, notice.Title, notice.Body, notice.PublishedDate.Value, notice.PublishedAtUtc))
            .ToListAsync(cancellationToken);

        await LogActivityAsync(context, PortalActivityType.ViewedNotices, "Portal notices viewed.", null, null, cancellationToken);
        return notices;
    }

    public Task<DigitalServiceRequestDto> CreateSubscriptionRequestAsync(CreatePortalSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0m)
        {
            throw Validation("amount", "Subscription amount must be greater than zero.");
        }

        return CreateDigitalRequestAsync(DigitalServiceRequestType.SubscriptionRequest, request, cancellationToken);
    }

    public Task<DigitalServiceRequestDto> CreateRedemptionRequestAsync(CreatePortalRedemptionRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.FullRedemption && (!request.Amount.HasValue || request.Amount <= 0m) && (!request.Units.HasValue || request.Units <= 0m))
        {
            throw Validation("redemption", "Partial redemption requires a positive amount or units.");
        }

        return CreateDigitalRequestAsync(DigitalServiceRequestType.RedemptionRequest, request, cancellationToken);
    }

    public Task<DigitalServiceRequestDto> CreateSwitchRequestAsync(CreatePortalSwitchRequest request, CancellationToken cancellationToken = default)
    {
        if ((!request.Amount.HasValue || request.Amount <= 0m) && (!request.Units.HasValue || request.Units <= 0m))
        {
            throw Validation("switch", "Switch request requires a positive amount or units.");
        }

        return CreateDigitalRequestAsync(DigitalServiceRequestType.SwitchRequest, request, cancellationToken);
    }

    public Task<DigitalServiceRequestDto> CreateProfileUpdateRequestAsync(CreatePortalProfileUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return CreateDigitalRequestAsync(DigitalServiceRequestType.ProfileUpdateRequest, request, cancellationToken);
    }

    public Task<DigitalServiceRequestDto> UploadDocumentAsync(UploadPortalDocumentRequest request, CancellationToken cancellationToken = default)
    {
        return UploadDocumentInternalAsync(request, cancellationToken);
    }

    private async Task<DigitalServiceRequestDto> UploadDocumentInternalAsync(UploadPortalDocumentRequest request, CancellationToken cancellationToken)
    {
        await _fileSecurityValidator.ValidateAsync(
            new FileUploadDescriptor(
                request.FileName,
                request.ContentType,
                request.SizeBytes,
                request.StorageReference,
                "PortalDocumentUpload"),
            cancellationToken);

        return await CreateDigitalRequestAsync(DigitalServiceRequestType.DocumentUploadRequest, request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PortalActivityLogDto>> GetActivityAsync(CancellationToken cancellationToken = default)
    {
        var context = await GetPortalContextAsync(cancellationToken);
        var activity = await _dbContext.PortalActivityLogs
            .AsNoTracking()
            .Where(log => log.InvestorId == context.Profile.InvestorId && log.UserId == context.Profile.UserId)
            .OrderByDescending(log => log.OccurredAtUtc)
            .Select(log => new PortalActivityLogDto(log.Id, log.ActivityType.ToString(), log.Summary, log.EntityType, log.EntityId, log.OccurredAtUtc))
            .Take(200)
            .ToListAsync(cancellationToken);
        await LogActivityAsync(context, PortalActivityType.ViewedProfile, "Portal activity history viewed.", null, null, cancellationToken);
        return activity;
    }

    private async Task<DigitalServiceRequestDto> CreateDigitalRequestAsync<TRequest>(DigitalServiceRequestType requestType, TRequest request, CancellationToken cancellationToken)
    {
        var context = await GetPortalContextAsync(cancellationToken);
        var now = _dateTimeProvider.UtcNow;
        var payload = Snapshot(new { context.Profile.InvestorId, Request = request });
        var digitalRequest = DigitalServiceRequest.Create(context.Profile.InvestorId, context.Profile.UserId, requestType, payload, context.Profile.UserId.ToString(), now);
        _dbContext.DigitalServiceRequests.Add(digitalRequest);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var workflow = await _workflowService.CreateAsync(new CreateWorkflowRequest(
            WorkflowType.PortalDigitalServiceRequest.ToString(),
            nameof(DigitalServiceRequest),
            digitalRequest.Id.ToString(),
            $"Investor portal {requestType} for {context.Investor.InvestorNumber}",
            $"Investor portal {requestType} submitted."), cancellationToken);

        digitalRequest.LinkWorkflow(workflow.Id);
        await LogActivityAsync(
            context,
            requestType == DigitalServiceRequestType.DocumentUploadRequest ? PortalActivityType.UploadedDocument : PortalActivityType.SubmittedDigitalRequest,
            $"Portal {requestType} submitted.",
            nameof(DigitalServiceRequest),
            digitalRequest.Id.ToString(),
            cancellationToken,
            saveChanges: false);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = MapDigitalRequest(digitalRequest);
        await WriteAuditAsync(AuditEventType.Submitted, "PortalDigitalServiceRequestSubmitted", nameof(DigitalServiceRequest), digitalRequest.Id.ToString(), null, Snapshot(dto), $"Portal {requestType} submitted for approval workflow.", cancellationToken, workflow.Id);
        return dto;
    }

    private async Task<PortalContext> GetPortalContextAsync(CancellationToken cancellationToken)
    {
        var userIdText = _currentUserContext.UserId ?? throw new UnauthorizedAccessException("An authenticated portal user is required.");
        if (!Guid.TryParse(userIdText, out var userId))
        {
            throw new UnauthorizedAccessException("The authenticated user id is invalid.");
        }

        var profile = await _dbContext.PortalUserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Portal profile was not found.");
        if (profile.Status != PortalProfileStatus.Active)
        {
            throw new UnauthorizedAccessException("Portal profile is not active.");
        }

        var investor = await _dbContext.Investors
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == profile.InvestorId, cancellationToken)
            ?? throw new NotFoundException("Linked investor record was not found.");
        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == profile.UserId, cancellationToken);
        var mfa = await _dbContext.PortalMfaSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserId == profile.UserId, cancellationToken);
        var mfaRequired = mfa?.MfaRequired ?? false;
        var mfaSatisfied = !mfaRequired || mfa?.MfaVerified == true || user.MfaEnabled;
        if (!mfaSatisfied)
        {
            throw new UnauthorizedAccessException("MFA is required before using investor portal endpoints.");
        }

        var session = await GetOrCreateSessionAsync(profile.UserId, profile.InvestorId, mfaSatisfied, cancellationToken);
        return new PortalContext(profile, investor, session, mfaRequired, mfaSatisfied);
    }

    private async Task<PortalSession> GetOrCreateSessionAsync(Guid userId, Guid investorId, bool mfaSatisfied, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var session = await _dbContext.PortalSessions
            .Where(candidate => candidate.UserId == userId && candidate.Status == PortalSessionStatus.Active)
            .OrderByDescending(candidate => candidate.LastSeenAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (session is null)
        {
            session = PortalSession.Start(userId, investorId, _currentUserContext.IpAddress, _currentUserContext.UserAgent, mfaSatisfied, now);
            _dbContext.PortalSessions.Add(session);
        }
        else
        {
            session.Touch(now);
        }

        return session;
    }

    private async Task LogActivityAsync(PortalContext context, PortalActivityType activityType, string summary, string? entityType, string? entityId, CancellationToken cancellationToken, bool saveChanges = true)
    {
        var now = _dateTimeProvider.UtcNow;
        _dbContext.PortalActivityLogs.Add(PortalActivityLog.Create(context.Profile.UserId, context.Profile.InvestorId, context.Session.Id, activityType, summary, entityType, entityId, _currentUserContext.IpAddress, _currentUserContext.CorrelationId, now));
        if (saveChanges)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static PortalProfileDto MapProfile(PortalContext context)
    {
        return new PortalProfileDto(
            context.Profile.UserId,
            context.Profile.InvestorId,
            context.Investor.InvestorNumber,
            context.Investor.DisplayName,
            context.Investor.Email,
            context.Investor.PhoneNumber,
            context.Investor.Status.ToString(),
            context.MfaRequired,
            context.MfaSatisfied);
    }

    private static PortalTransactionDto MapDealingTransaction(DealingInstruction instruction)
    {
        decimal? amount = instruction.InstructionType switch
        {
            DealingInstructionType.Subscription => instruction.SubscriptionInstructions.Single().Amount,
            DealingInstructionType.Redemption => instruction.RedemptionInstructions.Single().Amount,
            DealingInstructionType.Switch => instruction.SwitchInstructions.Single().Amount,
            _ => null
        };
        decimal? units = instruction.InstructionType switch
        {
            DealingInstructionType.Subscription => instruction.SubscriptionInstructions.Single().Units,
            DealingInstructionType.Redemption => instruction.RedemptionInstructions.Single().Units,
            DealingInstructionType.Switch => instruction.SwitchInstructions.Single().Units,
            _ => null
        };

        return new PortalTransactionDto(
            instruction.Id,
            "Dealing",
            instruction.InstructionType.ToString(),
            instruction.SchemeId,
            instruction.SchemeClassId,
            instruction.BusinessDate.Value,
            amount,
            units,
            instruction.Status.ToString(),
            instruction.InstructionNumber);
    }

    private static DigitalServiceRequestDto MapDigitalRequest(DigitalServiceRequest request)
    {
        return new DigitalServiceRequestDto(
            request.Id,
            request.InvestorId,
            request.UserId,
            request.RequestType.ToString(),
            request.Status.ToString(),
            request.WorkflowId,
            request.RequestPayloadJson,
            request.SubmittedAtUtc);
    }

    private async Task WriteAuditAsync(AuditEventType eventType, string action, string entityName, string entityId, string? beforeJson, string? afterJson, string reason, CancellationToken cancellationToken, Guid? workflowId = null)
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
            WorkflowId: workflowId), cancellationToken);
    }

    private static string Snapshot<T>(T value)
    {
        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static ValidationException Validation(string field, string message)
    {
        return new ValidationException(new Dictionary<string, string[]> { [field] = [message] });
    }

    private sealed record PortalContext(PortalUserProfile Profile, Domain.Investors.Investor Investor, PortalSession Session, bool MfaRequired, bool MfaSatisfied);
    private sealed record PublishedNavSnapshot(Guid SchemeClassId, decimal UnitPrice, DateOnly ValuationDate);
}
