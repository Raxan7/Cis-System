using Cis.Contracts;
using Cis.Contracts.Schemes;

namespace Cis.Application.Common.Interfaces;

public interface ISchemeService
{
    Task<SchemeDto> CreateAsync(CreateSchemeRequest request, CancellationToken cancellationToken = default);

    Task<PagedResult<SchemeDto>> GetAsync(PaginationRequest pagination, CancellationToken cancellationToken = default);

    Task<SchemeDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SchemeDto> UpdateAsync(Guid id, UpdateSchemeRequest request, CancellationToken cancellationToken = default);

    Task<SchemeDto> SubmitAsync(Guid id, SchemeWorkflowActionRequest request, CancellationToken cancellationToken = default);

    Task<SchemeDto> CheckAsync(Guid id, SchemeWorkflowActionRequest request, CancellationToken cancellationToken = default);

    Task<SchemeDto> ApproveAsync(Guid id, SchemeWorkflowActionRequest request, CancellationToken cancellationToken = default);

    Task<SchemeDto> AddClassAsync(Guid id, AddSchemeClassRequest request, CancellationToken cancellationToken = default);

    Task<SchemeDto> AddFeeScheduleAsync(Guid id, AddFeeScheduleRequest request, CancellationToken cancellationToken = default);

    Task<SchemeDto> AddEligibilityRuleAsync(Guid id, AddEligibilityRuleRequest request, CancellationToken cancellationToken = default);

    Task<SchemeDto> AddApprovedInstrumentAsync(Guid id, AddApprovedInstrumentRuleRequest request, CancellationToken cancellationToken = default);

    Task<SchemeDto> AddBankAccountAsync(Guid id, AddSchemeBankAccountRequest request, CancellationToken cancellationToken = default);

    Task<SchemeDto> AddCustodianMappingAsync(Guid id, AddSchemeCustodianMappingRequest request, CancellationToken cancellationToken = default);

    Task<SchemeDto> AddConfigurationAsync(Guid id, AddSchemeConfigurationRequest request, CancellationToken cancellationToken = default);

    Task<SchemeDto> AddRiskProfileAsync(Guid id, AddSchemeRiskProfileRequest request, CancellationToken cancellationToken = default);

    Task<SchemeDto> AddLiquidityThresholdAsync(Guid id, AddLiquidityThresholdRequest request, CancellationToken cancellationToken = default);

    Task<SchemeDto> AddDistributionRuleAsync(Guid id, AddDistributionRuleRequest request, CancellationToken cancellationToken = default);

    Task<SchemeDto> AddTemplateMappingAsync(Guid id, AddTemplateMappingRequest request, CancellationToken cancellationToken = default);
}
