namespace Cis.Domain.Integrations;

public enum IntegrationType
{
    Banking = 1,
    MobileMoney = 2,
    Custodian = 3,
    PricingValuationSource = 4,
    Email = 5,
    SMS = 6,
    DocumentRepository = 7,
    AccountingErpExport = 8
}

public enum IntegrationDirection
{
    Inbound = 1,
    Outbound = 2
}

public enum IntegrationMessageStatus
{
    Received = 1,
    Processed = 2,
    Failed = 3,
    PendingRetry = 4,
    Delivered = 5
}

public enum IntegrationIngestionStatus
{
    Completed = 1,
    Failed = 2,
    Duplicate = 3
}

public enum IntegrationDeliveryStatus
{
    Succeeded = 1,
    FailedRetryable = 2,
    FailedPermanent = 3
}
