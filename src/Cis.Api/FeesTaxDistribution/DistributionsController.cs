using Cis.Api.Security;
using Cis.Api.Common;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.FeesTaxDistribution;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.FeesTaxDistribution;

[ApiController]
[Route("api/distributions")]
public sealed class DistributionsController : ControllerBase
{
    private readonly IFeesTaxDistributionService _feesTaxDistributionService;

    public DistributionsController(IFeesTaxDistributionService feesTaxDistributionService)
    {
        _feesTaxDistributionService = feesTaxDistributionService;
    }

    [HttpPost("declarations")]
    [RequirePermission(Permissions.FeesTaxDistribution.DistributionsCreate)]
    [ProducesResponseType(typeof(ApiResponse<DistributionDeclarationDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<DistributionDeclarationDto>>> CreateDeclaration([FromBody] CreateDistributionDeclarationRequest request, CancellationToken cancellationToken)
    {
        var declaration = await _feesTaxDistributionService.CreateDistributionDeclarationAsync(request, cancellationToken);
        return Created($"/api/distributions/declarations/{declaration.Id}", ApiResponse<DistributionDeclarationDto>.Success(declaration, HttpContext.TraceIdentifier));
    }

    [HttpPost("declarations/{id:guid}/approve")]
    [RequirePermission(Permissions.FeesTaxDistribution.DistributionsApprove)]
    [ProducesResponseType(typeof(ApiResponse<DistributionDeclarationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DistributionDeclarationDto>>> ApproveDeclaration(Guid id, [FromBody] ApproveDistributionDeclarationRequest request, CancellationToken cancellationToken)
    {
        var declaration = await _feesTaxDistributionService.ApproveDistributionDeclarationAsync(id, request, cancellationToken);
        return Ok(ApiResponse<DistributionDeclarationDto>.Success(declaration, HttpContext.TraceIdentifier));
    }

    [HttpPost("runs")]
    [RequirePermission(Permissions.FeesTaxDistribution.DistributionsCreate)]
    [ProducesResponseType(typeof(ApiResponse<DistributionRunDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<DistributionRunDto>>> CreateRun([FromBody] CreateDistributionRunRequest request, CancellationToken cancellationToken)
    {
        var run = await _feesTaxDistributionService.CreateDistributionRunAsync(request, cancellationToken);
        return Created($"/api/distributions/runs/{run.Id}", ApiResponse<DistributionRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpPost("runs/{id:guid}/publish")]
    [RequirePermission(Permissions.FeesTaxDistribution.DistributionsPublish)]
    [ProducesResponseType(typeof(ApiResponse<DistributionRunDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DistributionRunDto>>> PublishRun(Guid id, [FromBody] PublishDistributionRunRequest request, CancellationToken cancellationToken)
    {
        var run = await _feesTaxDistributionService.PublishDistributionRunAsync(id, request, cancellationToken);
        return Ok(ApiResponse<DistributionRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpGet("investor/{investorId:guid}")]
    [RequirePermission(Permissions.FeesTaxDistribution.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<InvestorDistributionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<InvestorDistributionDto>>>> GetInvestorDistributions(
        Guid investorId,
        [FromQuery] PaginationRequest pagination,
        CancellationToken cancellationToken)
    {
        var distributions = await _feesTaxDistributionService.GetInvestorDistributionsAsync(investorId, cancellationToken);
        return this.OkPaged(distributions, pagination);
    }
}
