using Cis.Contracts.FeesTaxDistribution;

namespace Cis.Application.Common.Interfaces;

public interface IFeesTaxDistributionService
{
    Task<FeeAccrualRunDto> CreateFeeAccrualRunAsync(CreateFeeAccrualRunRequest request, CancellationToken cancellationToken = default);

    Task<FeeAccrualRunDto> GetFeeAccrualRunAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FeeWaiverRequestDto> CreateFeeWaiverAsync(CreateFeeWaiverRequest request, CancellationToken cancellationToken = default);

    Task<FeeWaiverRequestDto> ApproveFeeWaiverAsync(Guid id, ApproveFeeWaiverRequest request, CancellationToken cancellationToken = default);

    Task<TaxRuleDto> CreateTaxRuleAsync(CreateTaxRuleRequest request, CancellationToken cancellationToken = default);

    Task<DistributionDeclarationDto> CreateDistributionDeclarationAsync(CreateDistributionDeclarationRequest request, CancellationToken cancellationToken = default);

    Task<DistributionDeclarationDto> ApproveDistributionDeclarationAsync(Guid id, ApproveDistributionDeclarationRequest request, CancellationToken cancellationToken = default);

    Task<DistributionRunDto> CreateDistributionRunAsync(CreateDistributionRunRequest request, CancellationToken cancellationToken = default);

    Task<DistributionRunDto> PublishDistributionRunAsync(Guid id, PublishDistributionRunRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<InvestorDistributionDto>> GetInvestorDistributionsAsync(Guid investorId, CancellationToken cancellationToken = default);
}
