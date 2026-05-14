using Cis.Contracts.Cases;

namespace Cis.Application.Common.Interfaces;

public interface ICaseManagementService
{
    Task<ServiceCaseDto> CreateCaseAsync(CreateCaseRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ServiceCaseDto>> GetCasesAsync(CancellationToken cancellationToken = default);

    Task<ServiceCaseDto> GetCaseAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ServiceCaseDto> AssignCaseAsync(Guid id, AssignCaseRequest request, CancellationToken cancellationToken = default);

    Task<ServiceCaseDto> AddActionAsync(Guid id, AddCaseActionRequest request, CancellationToken cancellationToken = default);

    Task<ServiceCaseDto> EscalateCaseAsync(Guid id, EscalateCaseRequest request, CancellationToken cancellationToken = default);

    Task<ServiceCaseDto> ResolveCaseAsync(Guid id, ResolveCaseRequest request, CancellationToken cancellationToken = default);

    Task<CaseDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
