using Cis.Contracts.ComplianceRisk;

namespace Cis.Application.Common.Interfaces;

public interface IComplianceRiskService
{
    Task<LimitCheckRunDto> CreateLimitCheckRunAsync(CreateLimitCheckRunRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<LimitBreachDto>> GetBreachesAsync(string? status, CancellationToken cancellationToken = default);

    Task<LimitBreachDto> AssignBreachAsync(Guid id, AssignBreachRequest request, CancellationToken cancellationToken = default);

    Task<LimitBreachDto> RemediateBreachAsync(Guid id, RemediateBreachRequest request, CancellationToken cancellationToken = default);

    Task<LimitBreachDto> CloseBreachAsync(Guid id, CloseBreachRequest request, CancellationToken cancellationToken = default);

    Task<LiquidityCoverageRunDto> CreateLiquidityCoverageRunAsync(CreateLiquidityCoverageRunRequest request, CancellationToken cancellationToken = default);

    Task<RedemptionStressScenarioDto> CreateStressScenarioAsync(CreateStressScenarioRequest request, CancellationToken cancellationToken = default);

    Task<RedemptionStressTestRunDto> CreateStressTestRunAsync(CreateStressTestRunRequest request, CancellationToken cancellationToken = default);

    Task<LiquidationTimeAnalysisRunDto> CreateLiquidationTimeAnalysisRunAsync(CreateLiquidationTimeAnalysisRequest request, CancellationToken cancellationToken = default);

    Task<RiskDashboardSnapshotDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
