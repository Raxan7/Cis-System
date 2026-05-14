using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.ComplianceRisk;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.ComplianceRisk;

[ApiController]
[Route("api/risk")]
public sealed class RiskController : ControllerBase
{
    private readonly IComplianceRiskService _complianceRiskService;

    public RiskController(IComplianceRiskService complianceRiskService)
    {
        _complianceRiskService = complianceRiskService;
    }

    [HttpPost("liquidity-coverage-runs")]
    [RequirePermission(Permissions.ComplianceRisk.LiquidityRunsCreate)]
    [ProducesResponseType(typeof(ApiResponse<LiquidityCoverageRunDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<LiquidityCoverageRunDto>>> CreateLiquidityCoverageRun([FromBody] CreateLiquidityCoverageRunRequest request, CancellationToken cancellationToken)
    {
        var run = await _complianceRiskService.CreateLiquidityCoverageRunAsync(request, cancellationToken);
        return Created($"/api/risk/liquidity-coverage-runs/{run.Id}", ApiResponse<LiquidityCoverageRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpPost("stress-scenarios")]
    [RequirePermission(Permissions.ComplianceRisk.StressScenariosCreate)]
    [ProducesResponseType(typeof(ApiResponse<RedemptionStressScenarioDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<RedemptionStressScenarioDto>>> CreateStressScenario([FromBody] CreateStressScenarioRequest request, CancellationToken cancellationToken)
    {
        var scenario = await _complianceRiskService.CreateStressScenarioAsync(request, cancellationToken);
        return Created($"/api/risk/stress-scenarios/{scenario.Id}", ApiResponse<RedemptionStressScenarioDto>.Success(scenario, HttpContext.TraceIdentifier));
    }

    [HttpPost("stress-test-runs")]
    [RequirePermission(Permissions.ComplianceRisk.StressRunsCreate)]
    [ProducesResponseType(typeof(ApiResponse<RedemptionStressTestRunDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<RedemptionStressTestRunDto>>> CreateStressTestRun([FromBody] CreateStressTestRunRequest request, CancellationToken cancellationToken)
    {
        var run = await _complianceRiskService.CreateStressTestRunAsync(request, cancellationToken);
        return Created($"/api/risk/stress-test-runs/{run.Id}", ApiResponse<RedemptionStressTestRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpPost("liquidation-time-analysis")]
    [RequirePermission(Permissions.ComplianceRisk.LiquidationAnalysisCreate)]
    [ProducesResponseType(typeof(ApiResponse<LiquidationTimeAnalysisRunDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<LiquidationTimeAnalysisRunDto>>> CreateLiquidationTimeAnalysis([FromBody] CreateLiquidationTimeAnalysisRequest request, CancellationToken cancellationToken)
    {
        var run = await _complianceRiskService.CreateLiquidationTimeAnalysisRunAsync(request, cancellationToken);
        return Created($"/api/risk/liquidation-time-analysis/{run.Id}", ApiResponse<LiquidationTimeAnalysisRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpGet("dashboard")]
    [RequirePermission(Permissions.ComplianceRisk.Read)]
    [ProducesResponseType(typeof(ApiResponse<RiskDashboardSnapshotDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RiskDashboardSnapshotDto>>> GetDashboard(CancellationToken cancellationToken)
    {
        var dashboard = await _complianceRiskService.GetDashboardAsync(cancellationToken);
        return Ok(ApiResponse<RiskDashboardSnapshotDto>.Success(dashboard, HttpContext.TraceIdentifier));
    }
}
