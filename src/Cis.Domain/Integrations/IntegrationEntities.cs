using Cis.Domain.Common;

namespace Cis.Domain.Integrations;

public sealed class IntegrationEndpoint : AuditableAggregateRoot
{
    private IntegrationEndpoint()
    {
    }

    private IntegrationEndpoint(IntegrationType integrationType, string code, string name, string baseAddress, bool isActive, string createdByUserId, DateTime createdAtUtc)
    {
        IntegrationType = integrationType;
        Code = IntegrationValidation.Required(code, nameof(code), 80).ToUpperInvariant();
        Name = IntegrationValidation.Required(name, nameof(name), 200);
        BaseAddress = IntegrationValidation.Required(baseAddress, nameof(baseAddress), 500);
        IsActive = isActive;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public IntegrationType IntegrationType { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string BaseAddress { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public static IntegrationEndpoint Create(IntegrationType integrationType, string code, string name, string baseAddress, bool isActive, string createdByUserId, DateTime createdAtUtc)
    {
        return new IntegrationEndpoint(integrationType, code, name, baseAddress, isActive, createdByUserId, createdAtUtc);
    }
}

public sealed class IntegrationCredentialReference : AuditableAggregateRoot
{
    private IntegrationCredentialReference()
    {
    }

    private IntegrationCredentialReference(Guid integrationEndpointId, string credentialName, string secretReference, string createdByUserId, DateTime createdAtUtc)
    {
        IntegrationEndpointId = integrationEndpointId != Guid.Empty ? integrationEndpointId : throw new ArgumentException("Endpoint id cannot be empty.", nameof(integrationEndpointId));
        CredentialName = IntegrationValidation.Required(credentialName, nameof(credentialName), 100);
        SecretReference = IntegrationValidation.Required(secretReference, nameof(secretReference), 500);
        IsActive = true;
        MarkCreated(createdByUserId, createdAtUtc);
    }

    public Guid IntegrationEndpointId { get; private set; }
    public string CredentialName { get; private set; } = string.Empty;
    public string SecretReference { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public static IntegrationCredentialReference Create(Guid integrationEndpointId, string credentialName, string secretReference, string createdByUserId, DateTime createdAtUtc)
    {
        return new IntegrationCredentialReference(integrationEndpointId, credentialName, secretReference, createdByUserId, createdAtUtc);
    }
}

public sealed class IntegrationMessage : AuditableAggregateRoot
{
    private IntegrationMessage()
    {
    }

    private IntegrationMessage(IntegrationType integrationType, IntegrationDirection direction, string endpointCode, string externalReference, string payloadHash, string? payloadStorageReference, string actorUserId, DateTime receivedAtUtc)
    {
        IntegrationType = integrationType;
        Direction = direction;
        EndpointCode = IntegrationValidation.Required(endpointCode, nameof(endpointCode), 80).ToUpperInvariant();
        ExternalReference = IntegrationValidation.Required(externalReference, nameof(externalReference), 120);
        PayloadHash = IntegrationValidation.Required(payloadHash, nameof(payloadHash), 128);
        PayloadStorageReference = IntegrationValidation.Optional(payloadStorageReference, 1000);
        Status = IntegrationMessageStatus.Received;
        ReceivedAtUtc = IntegrationValidation.Utc(receivedAtUtc, nameof(receivedAtUtc));
        MarkCreated(actorUserId, ReceivedAtUtc);
    }

    public IntegrationType IntegrationType { get; private set; }
    public IntegrationDirection Direction { get; private set; }
    public string EndpointCode { get; private set; } = string.Empty;
    public string ExternalReference { get; private set; } = string.Empty;
    public string PayloadHash { get; private set; } = string.Empty;
    public string? PayloadStorageReference { get; private set; }
    public IntegrationMessageStatus Status { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }

    public static IntegrationMessage Create(IntegrationType integrationType, IntegrationDirection direction, string endpointCode, string externalReference, string payloadHash, string? payloadStorageReference, string actorUserId, DateTime receivedAtUtc)
    {
        return new IntegrationMessage(integrationType, direction, endpointCode, externalReference, payloadHash, payloadStorageReference, actorUserId, receivedAtUtc);
    }

    public void MarkProcessed(DateTime processedAtUtc)
    {
        Status = IntegrationMessageStatus.Processed;
        ProcessedAtUtc = IntegrationValidation.Utc(processedAtUtc, nameof(processedAtUtc));
    }

    public void MarkFailed(bool retryable, DateTime processedAtUtc)
    {
        Status = retryable ? IntegrationMessageStatus.PendingRetry : IntegrationMessageStatus.Failed;
        ProcessedAtUtc = IntegrationValidation.Utc(processedAtUtc, nameof(processedAtUtc));
    }

    public void MarkDelivered(DateTime processedAtUtc)
    {
        Status = IntegrationMessageStatus.Delivered;
        ProcessedAtUtc = IntegrationValidation.Utc(processedAtUtc, nameof(processedAtUtc));
    }
}

public sealed class IntegrationIngestionRun : AuditableAggregateRoot
{
    private IntegrationIngestionRun()
    {
    }

    private IntegrationIngestionRun(Guid integrationMessageId, IntegrationType integrationType, string adapterName, string fileName, string sourceHash, int recordCount, IntegrationIngestionStatus status, string actorUserId, DateTime startedAtUtc, DateTime completedAtUtc)
    {
        IntegrationMessageId = integrationMessageId != Guid.Empty ? integrationMessageId : throw new ArgumentException("Message id cannot be empty.", nameof(integrationMessageId));
        IntegrationType = integrationType;
        AdapterName = IntegrationValidation.Required(adapterName, nameof(adapterName), 100);
        FileName = IntegrationValidation.Required(fileName, nameof(fileName), 200);
        SourceHash = IntegrationValidation.Required(sourceHash, nameof(sourceHash), 128);
        RecordCount = recordCount >= 0 ? recordCount : throw new ArgumentException("Record count cannot be negative.", nameof(recordCount));
        Status = status;
        StartedAtUtc = IntegrationValidation.Utc(startedAtUtc, nameof(startedAtUtc));
        CompletedAtUtc = IntegrationValidation.Utc(completedAtUtc, nameof(completedAtUtc));
        MarkCreated(actorUserId, StartedAtUtc);
    }

    public Guid IntegrationMessageId { get; private set; }
    public IntegrationType IntegrationType { get; private set; }
    public string AdapterName { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string SourceHash { get; private set; } = string.Empty;
    public int RecordCount { get; private set; }
    public IntegrationIngestionStatus Status { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime CompletedAtUtc { get; private set; }

    public static IntegrationIngestionRun Create(Guid integrationMessageId, IntegrationType integrationType, string adapterName, string fileName, string sourceHash, int recordCount, IntegrationIngestionStatus status, string actorUserId, DateTime startedAtUtc, DateTime completedAtUtc)
    {
        return new IntegrationIngestionRun(integrationMessageId, integrationType, adapterName, fileName, sourceHash, recordCount, status, actorUserId, startedAtUtc, completedAtUtc);
    }
}

public sealed class IntegrationDeliveryAttempt : AuditableAggregateRoot
{
    private IntegrationDeliveryAttempt()
    {
    }

    private IntegrationDeliveryAttempt(Guid integrationMessageId, int attemptNumber, IntegrationDeliveryStatus status, bool retryable, string? responseCode, string? responseMessage, DateTime attemptedAtUtc, string actorUserId)
    {
        IntegrationMessageId = integrationMessageId != Guid.Empty ? integrationMessageId : throw new ArgumentException("Message id cannot be empty.", nameof(integrationMessageId));
        AttemptNumber = attemptNumber > 0 ? attemptNumber : throw new ArgumentException("Attempt number must be greater than zero.", nameof(attemptNumber));
        Status = status;
        Retryable = retryable;
        ResponseCode = IntegrationValidation.Optional(responseCode, 80);
        ResponseMessage = IntegrationValidation.Optional(responseMessage, 1000);
        AttemptedAtUtc = IntegrationValidation.Utc(attemptedAtUtc, nameof(attemptedAtUtc));
        MarkCreated(actorUserId, AttemptedAtUtc);
    }

    public Guid IntegrationMessageId { get; private set; }
    public int AttemptNumber { get; private set; }
    public IntegrationDeliveryStatus Status { get; private set; }
    public bool Retryable { get; private set; }
    public string? ResponseCode { get; private set; }
    public string? ResponseMessage { get; private set; }
    public DateTime AttemptedAtUtc { get; private set; }

    public static IntegrationDeliveryAttempt Create(Guid integrationMessageId, int attemptNumber, IntegrationDeliveryStatus status, bool retryable, string? responseCode, string? responseMessage, DateTime attemptedAtUtc, string actorUserId)
    {
        return new IntegrationDeliveryAttempt(integrationMessageId, attemptNumber, status, retryable, responseCode, responseMessage, attemptedAtUtc, actorUserId);
    }
}

public sealed class IntegrationError : AuditableAggregateRoot
{
    private IntegrationError()
    {
    }

    private IntegrationError(Guid? integrationMessageId, IntegrationType integrationType, string errorCode, string errorMessage, bool retryable, string? sourceReference, DateTime occurredAtUtc, string actorUserId)
    {
        IntegrationMessageId = integrationMessageId;
        IntegrationType = integrationType;
        ErrorCode = IntegrationValidation.Required(errorCode, nameof(errorCode), 80);
        ErrorMessage = IntegrationValidation.Required(errorMessage, nameof(errorMessage), 1000);
        Retryable = retryable;
        SourceReference = IntegrationValidation.Optional(sourceReference, 200);
        OccurredAtUtc = IntegrationValidation.Utc(occurredAtUtc, nameof(occurredAtUtc));
        MarkCreated(actorUserId, OccurredAtUtc);
    }

    public Guid? IntegrationMessageId { get; private set; }
    public IntegrationType IntegrationType { get; private set; }
    public string ErrorCode { get; private set; } = string.Empty;
    public string ErrorMessage { get; private set; } = string.Empty;
    public bool Retryable { get; private set; }
    public string? SourceReference { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    public static IntegrationError Create(Guid? integrationMessageId, IntegrationType integrationType, string errorCode, string errorMessage, bool retryable, string? sourceReference, DateTime occurredAtUtc, string actorUserId)
    {
        return new IntegrationError(integrationMessageId, integrationType, errorCode, errorMessage, retryable, sourceReference, occurredAtUtc, actorUserId);
    }
}

public sealed class IntegrationIdempotencyKey : AuditableAggregateRoot
{
    private IntegrationIdempotencyKey()
    {
    }

    private IntegrationIdempotencyKey(string key, string endpointCode, string requestHash, Guid integrationMessageId, string responseJson, string actorUserId, DateTime createdAtUtc)
    {
        Key = IntegrationValidation.Required(key, nameof(key), 200);
        EndpointCode = IntegrationValidation.Required(endpointCode, nameof(endpointCode), 80).ToUpperInvariant();
        RequestHash = IntegrationValidation.Required(requestHash, nameof(requestHash), 128);
        IntegrationMessageId = integrationMessageId != Guid.Empty ? integrationMessageId : throw new ArgumentException("Message id cannot be empty.", nameof(integrationMessageId));
        ResponseJson = IntegrationValidation.Required(responseJson, nameof(responseJson), 12000);
        MarkCreated(actorUserId, IntegrationValidation.Utc(createdAtUtc, nameof(createdAtUtc)));
    }

    public string Key { get; private set; } = string.Empty;
    public string EndpointCode { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public Guid IntegrationMessageId { get; private set; }
    public string ResponseJson { get; private set; } = "{}";

    public static IntegrationIdempotencyKey Create(string key, string endpointCode, string requestHash, Guid integrationMessageId, string responseJson, string actorUserId, DateTime createdAtUtc)
    {
        return new IntegrationIdempotencyKey(key, endpointCode, requestHash, integrationMessageId, responseJson, actorUserId, createdAtUtc);
    }
}
