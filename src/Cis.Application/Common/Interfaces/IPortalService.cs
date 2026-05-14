using Cis.Contracts.Portal;

namespace Cis.Application.Common.Interfaces;

public interface IPortalService
{
    Task<PortalProfileDto> GetMeAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PortalHoldingDto>> GetHoldingsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PortalTransactionDto>> GetTransactionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PortalStatementDto>> GetStatementsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PortalTaxCertificateDto>> GetTaxCertificatesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<InvestorNoticeDto>> GetNoticesAsync(CancellationToken cancellationToken = default);

    Task<DigitalServiceRequestDto> CreateSubscriptionRequestAsync(CreatePortalSubscriptionRequest request, CancellationToken cancellationToken = default);

    Task<DigitalServiceRequestDto> CreateRedemptionRequestAsync(CreatePortalRedemptionRequest request, CancellationToken cancellationToken = default);

    Task<DigitalServiceRequestDto> CreateSwitchRequestAsync(CreatePortalSwitchRequest request, CancellationToken cancellationToken = default);

    Task<DigitalServiceRequestDto> CreateProfileUpdateRequestAsync(CreatePortalProfileUpdateRequest request, CancellationToken cancellationToken = default);

    Task<DigitalServiceRequestDto> UploadDocumentAsync(UploadPortalDocumentRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PortalActivityLogDto>> GetActivityAsync(CancellationToken cancellationToken = default);
}
