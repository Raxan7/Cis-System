using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cis.Application.Common.Exceptions;
using Cis.Application.Common.Interfaces;
using Cis.Contracts.Integrations;
using Cis.Domain.Audit;
using Cis.Domain.Integrations;
using Cis.Domain.NAV;
using Cis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cis.Infrastructure.Integrations;

internal sealed class IntegrationService : IIntegrationService, ISmsSender
{
    private const string ModuleName = "Integrations";
    private const int RetryLimit = 3;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly CisDbContext _dbContext;
    private readonly IAuditWriter _auditWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IDocumentStorage _documentStorage;

    public IntegrationService(CisDbContext dbContext, IAuditWriter auditWriter, ICurrentUserContext currentUserContext, IDateTimeProvider dateTimeProvider, IDocumentStorage documentStorage)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
        _documentStorage = documentStorage;
    }

    public Task<IntegrationResultDto> UploadBankStatementAsync(UploadBankStatementIntegrationRequest request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        return ProcessInboundAsync(
            IntegrationType.Banking,
            "BANK-STATEMENT-UPLOAD",
            request.FileName,
            request.CsvContent,
            request,
            idempotencyKey,
            "FileBasedImportAdapter",
            async () =>
            {
                await EnsureSchemeBankAccountExistsAsync(request.SchemeBankAccountId, cancellationToken);
                var rows = CountCsvRows(request.CsvContent, "csvContent");
                return rows;
            },
            cancellationToken);
    }

    public Task<IntegrationResultDto> HandleMobileMoneyCallbackAsync(MobileMoneyCallbackRequest request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        return ProcessInboundAsync(
            IntegrationType.MobileMoney,
            "MOBILE-MONEY-CALLBACK",
            $"{request.TransactionReference}.json",
            Snapshot(request),
            request,
            idempotencyKey,
            "ApiStubAdapter",
            () =>
            {
                if (request.OccurredAtUtc.Kind != DateTimeKind.Utc)
                {
                    throw Validation("occurredAtUtc", "OccurredAtUtc must be UTC.");
                }

                if (request.Amount <= 0)
                {
                    throw Validation("amount", "Amount must be greater than zero.");
                }

                return Task.FromResult(1);
            },
            cancellationToken);
    }

    public Task<IntegrationResultDto> UploadHoldingsAsync(CustodianHoldingsUploadRequest request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        return ProcessInboundAsync(
            IntegrationType.Custodian,
            "CUSTODIAN-HOLDINGS-UPLOAD",
            request.FileName,
            request.CsvContent,
            request,
            idempotencyKey,
            "ManualUploadAdapter",
            async () =>
            {
                await EnsureCustodianExistsAsync(request.CustodianId, cancellationToken);
                return CountCsvRows(request.CsvContent, "csvContent");
            },
            cancellationToken);
    }

    public Task<IntegrationResultDto> UploadPricingAsync(PricingUploadRequest request, string? idempotencyKey, CancellationToken cancellationToken = default)
    {
        return ProcessInboundAsync(
            IntegrationType.PricingValuationSource,
            "PRICING-UPLOAD",
            request.FileName,
            request.CsvContent,
            request,
            idempotencyKey,
            "FileBasedImportAdapter",
            async () =>
            {
                await EnsureSchemeAndClassExistAsync(request.SchemeId, request.SchemeClassId, cancellationToken);
                var rows = CountCsvRows(request.CsvContent, "csvContent");
                var now = _dateTimeProvider.UtcNow;
                var actor = CurrentUserIdOrThrow();
                var exists = await _dbContext.PriceSourceHierarchies.AnyAsync(source =>
                    source.SchemeId == request.SchemeId &&
                    source.SchemeClassId == request.SchemeClassId &&
                    source.InstrumentType == request.InstrumentType &&
                    source.IsActive,
                    cancellationToken);
                if (exists)
                {
                    throw new ConflictException("Active price source hierarchy already exists for this scheme, class, and instrument type.");
                }

                _dbContext.PriceSourceHierarchies.Add(PriceSourceHierarchy.Create(
                    request.SchemeId,
                    request.SchemeClassId,
                    request.InstrumentType,
                    request.PrimarySource,
                    request.SecondarySource,
                    request.ManualFallbackAllowed,
                    request.MaxPriceAgeDays,
                    request.VarianceTolerancePercent,
                    actor,
                    now));
                return rows;
            },
            cancellationToken);
    }

    public async Task<IntegrationResultDto> SendTestEmailAsync(TestEmailRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var payload = Snapshot(request);
        var hash = Sha256(payload);
        var message = IntegrationMessage.Create(IntegrationType.Email, IntegrationDirection.Outbound, "TEST-EMAIL", request.To, hash, null, actor, now);
        _dbContext.IntegrationMessages.Add(message);

        IntegrationDeliveryAttempt attempt;
        IntegrationError? error = null;
        if (request.SimulateFailure)
        {
            message.MarkFailed(request.RetryableFailure, now);
            attempt = IntegrationDeliveryAttempt.Create(message.Id, 1, request.RetryableFailure ? IntegrationDeliveryStatus.FailedRetryable : IntegrationDeliveryStatus.FailedPermanent, request.RetryableFailure, "SMTP-STUB-FAILED", "Simulated email failure.", now, actor);
            error = IntegrationError.Create(message.Id, IntegrationType.Email, request.RetryableFailure ? "EMAIL_RETRYABLE_FAILURE" : "EMAIL_PERMANENT_FAILURE", "Simulated email notification failure.", request.RetryableFailure, request.To, now, actor);
            _dbContext.IntegrationErrors.Add(error);
        }
        else
        {
            message.MarkDelivered(now);
            attempt = IntegrationDeliveryAttempt.Create(message.Id, 1, IntegrationDeliveryStatus.Succeeded, false, "SMTP-STUB-OK", "Email accepted by API stub adapter.", now, actor);
        }

        _dbContext.IntegrationDeliveryAttempts.Add(attempt);
        await _dbContext.SaveChangesAsync(cancellationToken);
        var dto = new IntegrationResultDto(MapMessage(message), null, MapAttempt(attempt), error is null ? null : MapError(error), null, null);
        await WriteAuditAsync(AuditEventType.Created, error is null ? "IntegrationEmailSent" : "IntegrationNotificationFailed", "IntegrationMessage", message.Id.ToString(), null, Snapshot(dto), error is null ? "Test email delivered through API stub adapter." : "Notification exception logged for DGT-03.", cancellationToken);
        return dto;
    }

    public async Task<IntegrationResultDto> SendAsync(string to, string body, CancellationToken cancellationToken = default)
    {
        return await SendTestEmailAsync(new TestEmailRequest(to, "SMS stub", body, SimulateFailure: false, RetryableFailure: false), cancellationToken);
    }

    public async Task<IntegrationResultDto> ExportAsync(ErpExportRequest request, CancellationToken cancellationToken = default)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var journalCount = await _dbContext.Journals.AsNoTracking().CountAsync(cancellationToken);
        var ledgerEntryCount = await _dbContext.LedgerEntries.AsNoTracking().CountAsync(cancellationToken);
        var csv = new StringBuilder()
            .AppendLine("ExportCode,BusinessDate,JournalCount,LedgerEntryCount,GeneratedAtUtc")
            .Append(request.ExportCode).Append(',')
            .Append(request.BusinessDate.ToString("yyyy-MM-dd")).Append(',')
            .Append(journalCount).Append(',')
            .Append(ledgerEntryCount).Append(',')
            .AppendLine(now.ToString("O"))
            .ToString();

        var hash = Sha256(csv);
        var storageReference = await _documentStorage.StoreAsync($"{request.ExportCode}-{request.BusinessDate:yyyyMMdd}.csv", csv, cancellationToken);
        var message = IntegrationMessage.Create(IntegrationType.AccountingErpExport, IntegrationDirection.Outbound, "ERP-EXPORT", request.ExportCode, hash, storageReference, actor, now);
        message.MarkDelivered(now);
        var attempt = IntegrationDeliveryAttempt.Create(message.Id, 1, IntegrationDeliveryStatus.Succeeded, false, "ERP-STUB-OK", "ERP export CSV generated.", now, actor);
        _dbContext.IntegrationMessages.Add(message);
        _dbContext.IntegrationDeliveryAttempts.Add(attempt);
        await _dbContext.SaveChangesAsync(cancellationToken);
        var dto = new IntegrationResultDto(MapMessage(message), null, MapAttempt(attempt), null, storageReference, csv);
        await WriteAuditAsync(AuditEventType.Exported, "IntegrationErpExported", "IntegrationMessage", message.Id.ToString(), null, Snapshot(dto), "ERP export stub produced summary CSV.", cancellationToken);
        return dto;
    }

    public async Task<IReadOnlyCollection<IntegrationMessageDto>> GetMessagesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.IntegrationMessages
            .AsNoTracking()
            .OrderByDescending(message => message.ReceivedAtUtc)
            .Take(200)
            .Select(message => MapMessage(message))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<IntegrationErrorDto>> GetErrorsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.IntegrationErrors
            .AsNoTracking()
            .OrderByDescending(error => error.OccurredAtUtc)
            .Take(200)
            .Select(error => MapError(error))
            .ToListAsync(cancellationToken);
    }

    private async Task<IntegrationResultDto> ProcessInboundAsync<TRequest>(
        IntegrationType integrationType,
        string endpointCode,
        string fileName,
        string content,
        TRequest request,
        string? idempotencyKey,
        string adapterName,
        Func<Task<int>> process,
        CancellationToken cancellationToken)
    {
        var actor = CurrentUserIdOrThrow();
        var now = _dateTimeProvider.UtcNow;
        var normalizedKey = NormalizeIdempotencyKey(idempotencyKey);
        var requestJson = Snapshot(request);
        var requestHash = Sha256(requestJson);
        if (normalizedKey is not null)
        {
            var existing = await _dbContext.IntegrationIdempotencyKeys.AsNoTracking()
                .SingleOrDefaultAsync(key => key.EndpointCode == endpointCode && key.Key == normalizedKey, cancellationToken);
            if (existing is not null)
            {
                if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
                {
                    throw new ConflictException("Idempotency key cannot be reused with a different integration payload.");
                }

                var replay = JsonSerializer.Deserialize<IntegrationResultDto>(existing.ResponseJson, JsonOptions)
                    ?? throw new InvalidOperationException("Stored idempotent integration response could not be deserialized.");
                return replay;
            }
        }

        var sourceHash = Sha256(content);
        var storageReference = await _documentStorage.StoreAsync(fileName, content, cancellationToken);
        var message = IntegrationMessage.Create(integrationType, IntegrationDirection.Inbound, endpointCode, fileName, sourceHash, storageReference, actor, now);
        _dbContext.IntegrationMessages.Add(message);

        try
        {
            var recordCount = await process();
            message.MarkProcessed(now);
            var run = IntegrationIngestionRun.Create(message.Id, integrationType, adapterName, fileName, sourceHash, recordCount, IntegrationIngestionStatus.Completed, actor, now, now);
            _dbContext.IntegrationIngestionRuns.Add(run);
            await _dbContext.SaveChangesAsync(cancellationToken);
            var dto = new IntegrationResultDto(MapMessage(message), MapRun(run), null, null, storageReference, null);
            await StoreIdempotencyAsync(normalizedKey, endpointCode, requestHash, message.Id, dto, actor, now, cancellationToken);
            await WriteAuditAsync(AuditEventType.Created, "IntegrationInboundProcessed", "IntegrationMessage", message.Id.ToString(), null, Snapshot(dto), $"{endpointCode} processed through {adapterName}.", cancellationToken);
            return dto;
        }
        catch (Exception exception) when (exception is ValidationException or ConflictException or ArgumentException)
        {
            message.MarkFailed(retryable: false, now);
            var error = IntegrationError.Create(message.Id, integrationType, "INGESTION_FAILED", exception.Message, retryable: false, fileName, now, actor);
            var run = IntegrationIngestionRun.Create(message.Id, integrationType, adapterName, fileName, sourceHash, 0, IntegrationIngestionStatus.Failed, actor, now, now);
            _dbContext.IntegrationErrors.Add(error);
            _dbContext.IntegrationIngestionRuns.Add(run);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await WriteAuditAsync(AuditEventType.Created, "IntegrationInboundFailed", "IntegrationMessage", message.Id.ToString(), null, Snapshot(MapError(error)), $"{endpointCode} failed validation.", cancellationToken);
            if (exception is ValidationException validation)
            {
                throw validation;
            }

            if (exception is ConflictException conflict)
            {
                throw conflict;
            }

            throw Validation("integration", exception.Message);
        }
    }

    private async Task StoreIdempotencyAsync(string? idempotencyKey, string endpointCode, string requestHash, Guid messageId, IntegrationResultDto dto, string actor, DateTime now, CancellationToken cancellationToken)
    {
        if (idempotencyKey is null)
        {
            return;
        }

        _dbContext.IntegrationIdempotencyKeys.Add(IntegrationIdempotencyKey.Create(idempotencyKey, endpointCode, requestHash, messageId, Snapshot(dto), actor, now));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSchemeBankAccountExistsAsync(Guid schemeBankAccountId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.SchemeBankAccounts.AnyAsync(account => account.Id == schemeBankAccountId, cancellationToken))
        {
            throw Validation("schemeBankAccountId", "Scheme bank account was not found.");
        }
    }

    private async Task EnsureCustodianExistsAsync(Guid custodianId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.Custodians.AnyAsync(custodian => custodian.Id == custodianId, cancellationToken))
        {
            throw Validation("custodianId", "Custodian was not found.");
        }
    }

    private async Task EnsureSchemeAndClassExistAsync(Guid schemeId, Guid schemeClassId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.Schemes.AnyAsync(scheme => scheme.Id == schemeId, cancellationToken))
        {
            throw Validation("schemeId", "Scheme was not found.");
        }

        if (!await _dbContext.SchemeClasses.AnyAsync(schemeClass => schemeClass.Id == schemeClassId && schemeClass.SchemeId == schemeId, cancellationToken))
        {
            throw Validation("schemeClassId", "Scheme class was not found for the scheme.");
        }
    }

    private static int CountCsvRows(string csvContent, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            throw Validation(fieldName, "CSV content is required.");
        }

        if (csvContent.Contains("FAIL", StringComparison.OrdinalIgnoreCase))
        {
            throw Validation(fieldName, "CSV content contains a simulated failure marker.");
        }

        var rows = csvContent.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (rows.Length < 2)
        {
            throw Validation(fieldName, "CSV content must contain a header and at least one data row.");
        }

        return rows.Length - 1;
    }

    private string CurrentUserIdOrThrow()
    {
        return _currentUserContext.UserId ?? throw new UnauthorizedAccessException("An authenticated user is required.");
    }

    private static string? NormalizeIdempotencyKey(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw Validation("idempotencyKey", "Idempotency-Key header is required for inbound integrations.");
        }

        idempotencyKey = idempotencyKey.Trim();
        if (idempotencyKey.Length > 200)
        {
            throw Validation("idempotencyKey", "Idempotency key cannot exceed 200 characters.");
        }

        return idempotencyKey;
    }

    private async Task WriteAuditAsync(AuditEventType eventType, string action, string entityName, string entityId, string? beforeJson, string? afterJson, string reason, CancellationToken cancellationToken)
    {
        await _auditWriter.WriteAsync(new AuditLogEntry(ModuleName, action, entityName, entityId, eventType, Summary: reason, BeforeJson: beforeJson, AfterJson: afterJson, Reason: reason), cancellationToken);
    }

    private static IntegrationMessageDto MapMessage(IntegrationMessage message)
    {
        return new IntegrationMessageDto(message.Id, message.IntegrationType.ToString(), message.Direction.ToString(), message.EndpointCode, message.ExternalReference, message.PayloadHash, message.PayloadStorageReference, message.Status.ToString(), message.ReceivedAtUtc, message.ProcessedAtUtc);
    }

    private static IntegrationIngestionRunDto MapRun(IntegrationIngestionRun run)
    {
        return new IntegrationIngestionRunDto(run.Id, run.IntegrationMessageId, run.IntegrationType.ToString(), run.AdapterName, run.FileName, run.SourceHash, run.RecordCount, run.Status.ToString(), run.StartedAtUtc, run.CompletedAtUtc);
    }

    private static IntegrationDeliveryAttemptDto MapAttempt(IntegrationDeliveryAttempt attempt)
    {
        return new IntegrationDeliveryAttemptDto(attempt.Id, attempt.IntegrationMessageId, attempt.AttemptNumber, attempt.Status.ToString(), attempt.Retryable, attempt.ResponseCode, attempt.ResponseMessage, attempt.AttemptedAtUtc);
    }

    private static IntegrationErrorDto MapError(IntegrationError error)
    {
        return new IntegrationErrorDto(error.Id, error.IntegrationMessageId, error.IntegrationType.ToString(), error.ErrorCode, error.ErrorMessage, error.Retryable, error.SourceReference, error.OccurredAtUtc);
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
}
