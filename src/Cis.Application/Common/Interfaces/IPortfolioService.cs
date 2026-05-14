using Cis.Contracts.Portfolio;

namespace Cis.Application.Common.Interfaces;

public interface IPortfolioService
{
    Task<InstrumentDto> CreateInstrumentAsync(CreateInstrumentRequest request, CancellationToken cancellationToken = default);
    Task<InstrumentDto> GetInstrumentAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<InstrumentDto>> GetInstrumentsAsync(CancellationToken cancellationToken = default);

    Task<CounterpartyDto> CreateCounterpartyAsync(CreateCounterpartyRequest request, CancellationToken cancellationToken = default);
    Task<CounterpartyDto> GetCounterpartyAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CounterpartyDto>> GetCounterpartiesAsync(CancellationToken cancellationToken = default);

    Task<PlacementDto> CreatePlacementAsync(CreatePlacementRequest request, CancellationToken cancellationToken = default);
    Task<PlacementDto> SubmitPlacementAsync(Guid id, PlacementSubmitRequest request, CancellationToken cancellationToken = default);
    Task<PlacementDto> ApprovePlacementAsync(Guid id, PlacementApproveRequest request, CancellationToken cancellationToken = default);
    Task<PlacementDto> GetPlacementAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PortfolioHoldingDto>> GetHoldingsAsync(Guid schemeId, Guid schemeClassId, CancellationToken cancellationToken = default);

    Task<MaturityLadderDto> GetMaturityLadderAsync(Guid schemeId, Guid schemeClassId, CancellationToken cancellationToken = default);

    Task<IncomeDueDto> GetIncomeDueAsync(Guid schemeId, Guid schemeClassId, CancellationToken cancellationToken = default);

    Task<IncomeScheduleDto> RecordIncomeReceiptAsync(IncomeReceiptRequest request, CancellationToken cancellationToken = default);

    Task<RolloverEventDto> CreateRolloverAsync(CreateRolloverRequest request, CancellationToken cancellationToken = default);
}
