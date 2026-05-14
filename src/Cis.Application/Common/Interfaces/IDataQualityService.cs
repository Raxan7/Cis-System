using Cis.Contracts.DataQuality;

namespace Cis.Application.Common.Interfaces;

public interface IDataQualityService
{
    Task<DataQualityRuleDto> CreateRuleAsync(CreateDataQualityRuleRequest request, CancellationToken cancellationToken = default);

    Task<DataQualityCheckRunDto> CreateCheckRunAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DataQualityExceptionDto>> GetExceptionsAsync(CancellationToken cancellationToken = default);

    Task<DataQualityExceptionDto> AssignExceptionAsync(Guid id, AssignDataQualityExceptionRequest request, CancellationToken cancellationToken = default);

    Task<DataQualityExceptionDto> ResolveExceptionAsync(Guid id, ResolveDataQualityExceptionRequest request, CancellationToken cancellationToken = default);

    Task<DataQualityDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
