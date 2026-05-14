using System.ComponentModel.DataAnnotations;

namespace Cis.Contracts.Integrations;

public sealed record UploadBankStatementIntegrationRequest(
    Guid SchemeBankAccountId,
    DateOnly StatementDate,
    [Required, StringLength(200)] string FileName,
    [Required] string CsvContent);

public sealed record MobileMoneyCallbackRequest(
    [Required, StringLength(100)] string TransactionReference,
    [Required, StringLength(30)] string PhoneNumber,
    decimal Amount,
    [Required, StringLength(3)] string Currency,
    [Required, StringLength(30)] string Status,
    DateTime OccurredAtUtc);

public sealed record CustodianHoldingsUploadRequest(
    Guid CustodianId,
    Guid? CustodianAccountId,
    DateOnly StatementDate,
    [Required, StringLength(200)] string FileName,
    [Required] string CsvContent);

public sealed record PricingUploadRequest(
    Guid SchemeId,
    Guid SchemeClassId,
    [Required, StringLength(100)] string InstrumentType,
    [Required, StringLength(100)] string PrimarySource,
    [Required, StringLength(100)] string SecondarySource,
    bool ManualFallbackAllowed,
    [Range(0, 3660)] int MaxPriceAgeDays,
    decimal VarianceTolerancePercent,
    DateOnly PriceDate,
    [Required, StringLength(200)] string FileName,
    [Required] string CsvContent);

public sealed record TestEmailRequest(
    [Required, EmailAddress, StringLength(320)] string To,
    [Required, StringLength(200)] string Subject,
    [Required, StringLength(4000)] string Body,
    bool SimulateFailure,
    bool RetryableFailure);

public sealed record ErpExportRequest(
    DateOnly BusinessDate,
    [Required, StringLength(100)] string ExportCode);

public sealed record IntegrationMessageDto(
    Guid Id,
    string IntegrationType,
    string Direction,
    string EndpointCode,
    string ExternalReference,
    string PayloadHash,
    string? PayloadStorageReference,
    string Status,
    DateTime ReceivedAtUtc,
    DateTime? ProcessedAtUtc);

public sealed record IntegrationIngestionRunDto(
    Guid Id,
    Guid IntegrationMessageId,
    string IntegrationType,
    string AdapterName,
    string FileName,
    string SourceHash,
    int RecordCount,
    string Status,
    DateTime StartedAtUtc,
    DateTime CompletedAtUtc);

public sealed record IntegrationDeliveryAttemptDto(
    Guid Id,
    Guid IntegrationMessageId,
    int AttemptNumber,
    string Status,
    bool Retryable,
    string? ResponseCode,
    string? ResponseMessage,
    DateTime AttemptedAtUtc);

public sealed record IntegrationErrorDto(
    Guid Id,
    Guid? IntegrationMessageId,
    string IntegrationType,
    string ErrorCode,
    string ErrorMessage,
    bool Retryable,
    string? SourceReference,
    DateTime OccurredAtUtc);

public sealed record IntegrationResultDto(
    IntegrationMessageDto Message,
    IntegrationIngestionRunDto? IngestionRun,
    IntegrationDeliveryAttemptDto? DeliveryAttempt,
    IntegrationErrorDto? Error,
    string? StoredDocumentReference,
    string? OutputContent);
