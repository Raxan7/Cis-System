using Cis.Contracts.Investors;

namespace Cis.Application.Common.Interfaces;

public interface IInvestorService
{
    Task<InvestorDto> CreateAsync(CreateInvestorRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<InvestorDto>> GetAsync(CancellationToken cancellationToken = default);

    Task<InvestorDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InvestorDto> UpdateAsync(Guid id, UpdateInvestorRequest request, CancellationToken cancellationToken = default);

    Task<InvestorDto> AddDocumentAsync(Guid id, AddKycDocumentRequest request, CancellationToken cancellationToken = default);

    Task<InvestorDto> AddBankAccountAsync(Guid id, AddInvestorBankAccountRequest request, CancellationToken cancellationToken = default);

    Task<InvestorDto> SubmitKycAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InvestorDto> ApproveAsync(Guid id, InvestorWorkflowDecisionRequest request, CancellationToken cancellationToken = default);

    Task<InvestorDto> RejectAsync(Guid id, InvestorWorkflowDecisionRequest request, CancellationToken cancellationToken = default);

    Task<InvestorDto> SuspendAsync(Guid id, InvestorWorkflowDecisionRequest request, CancellationToken cancellationToken = default);

    Task<InvestorDto> CloseAsync(Guid id, InvestorWorkflowDecisionRequest request, CancellationToken cancellationToken = default);

    Task<InvestorDto> StartAmlScreeningAsync(Guid id, StartAmlScreeningRequest request, CancellationToken cancellationToken = default);
}

public interface IKycQueryService
{
    Task<IReadOnlyCollection<KycDocumentDto>> GetExpiredDocumentsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<IncompleteKycInvestorDto>> GetIncompleteInvestorsAsync(CancellationToken cancellationToken = default);
}

public interface IAmlQueryService
{
    Task<IReadOnlyCollection<AmlExceptionDto>> GetExceptionsAsync(CancellationToken cancellationToken = default);
}
