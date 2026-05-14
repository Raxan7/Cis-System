using Cis.Contracts.Integrations;

namespace Cis.Application.Common.Interfaces;

public interface IBankStatementProvider
{
    Task<IntegrationResultDto> UploadBankStatementAsync(UploadBankStatementIntegrationRequest request, string? idempotencyKey, CancellationToken cancellationToken = default);
}

public interface IPaymentStatusProvider
{
    Task<IntegrationResultDto> HandleMobileMoneyCallbackAsync(MobileMoneyCallbackRequest request, string? idempotencyKey, CancellationToken cancellationToken = default);
}

public interface IMobileMoneyCallbackHandler : IPaymentStatusProvider
{
}

public interface ICustodianStatementProvider
{
    Task<IntegrationResultDto> UploadHoldingsAsync(CustodianHoldingsUploadRequest request, string? idempotencyKey, CancellationToken cancellationToken = default);
}

public interface IPricingSourceProvider
{
    Task<IntegrationResultDto> UploadPricingAsync(PricingUploadRequest request, string? idempotencyKey, CancellationToken cancellationToken = default);
}

public interface IEmailSender
{
    Task<IntegrationResultDto> SendTestEmailAsync(TestEmailRequest request, CancellationToken cancellationToken = default);
}

public interface ISmsSender
{
    Task<IntegrationResultDto> SendAsync(string to, string body, CancellationToken cancellationToken = default);
}

public interface IDocumentStorage
{
    Task<string> StoreAsync(string fileName, string content, CancellationToken cancellationToken = default);
}

public interface IErpExporter
{
    Task<IntegrationResultDto> ExportAsync(ErpExportRequest request, CancellationToken cancellationToken = default);
}

public interface IIntegrationService : IBankStatementProvider, IMobileMoneyCallbackHandler, ICustodianStatementProvider, IPricingSourceProvider, IEmailSender, IErpExporter
{
    Task<IReadOnlyCollection<IntegrationMessageDto>> GetMessagesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<IntegrationErrorDto>> GetErrorsAsync(CancellationToken cancellationToken = default);
}
