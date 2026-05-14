using Cis.Contracts.NAV;

namespace Cis.Application.Common.Interfaces;

public interface INavService
{
    Task<ValuationRunDto> CreateValuationRunAsync(CreateValuationRunRequest request, CancellationToken cancellationToken = default);

    Task<ValuationRunDto> CalculateValuationRunAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ValuationRunDto> SubmitValuationRunAsync(Guid id, NavWorkflowActionRequest request, CancellationToken cancellationToken = default);

    Task<ValuationRunDto> CheckValuationRunAsync(Guid id, NavWorkflowActionRequest request, CancellationToken cancellationToken = default);

    Task<ValuationRunDto> ApproveValuationRunAsync(Guid id, NavWorkflowActionRequest request, CancellationToken cancellationToken = default);

    Task<NavPublicationDto> PublishValuationRunAsync(Guid id, NavWorkflowActionRequest request, CancellationToken cancellationToken = default);

    Task<ManualValuationOverrideDto> CreateManualOverrideAsync(CreateManualValuationOverrideRequest request, CancellationToken cancellationToken = default);

    Task<ManualValuationOverrideDto> ApproveManualOverrideAsync(Guid id, ApproveManualValuationOverrideRequest request, CancellationToken cancellationToken = default);

    Task<NavRestatementDto> CreateRestatementAsync(CreateNavRestatementRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<NavPublicationDto>> GetHistoryAsync(Guid? schemeId, Guid? schemeClassId, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken = default);

    Task<NavReconstructionDto> ReconstructAsync(Guid schemeId, DateOnly valuationDate, CancellationToken cancellationToken = default);
}
