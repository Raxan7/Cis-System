using Cis.Contracts.UnitRegister;

namespace Cis.Application.Common.Interfaces;

public interface IUnitRegisterService
{
    Task<SchemeClassUnitRegisterDto> GetSchemeClassRegisterAsync(Guid schemeId, Guid schemeClassId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UnitHoldingDto>> GetInvestorHoldingsAsync(Guid investorId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<HistoricalHoldingDto>> GetInvestorHistoricalHoldingsAsync(Guid investorId, DateOnly date, CancellationToken cancellationToken = default);

    Task<UnitAdjustmentDto> CreateAdjustmentAsync(CreateUnitAdjustmentRequest request, string? idempotencyKey = null, CancellationToken cancellationToken = default);

    Task<UnitAdjustmentDto> ApproveAdjustmentAsync(Guid id, ApproveUnitAdjustmentRequest request, CancellationToken cancellationToken = default);
}
