using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.FeesTaxDistribution;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.FeesTaxDistribution;

[ApiController]
[Route("api/taxes")]
public sealed class TaxesController : ControllerBase
{
    private readonly IFeesTaxDistributionService _feesTaxDistributionService;

    public TaxesController(IFeesTaxDistributionService feesTaxDistributionService)
    {
        _feesTaxDistributionService = feesTaxDistributionService;
    }

    [HttpPost("rules")]
    [RequirePermission(Permissions.FeesTaxDistribution.TaxRulesCreate)]
    [ProducesResponseType(typeof(ApiResponse<TaxRuleDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<TaxRuleDto>>> CreateRule([FromBody] CreateTaxRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await _feesTaxDistributionService.CreateTaxRuleAsync(request, cancellationToken);
        return Created($"/api/taxes/rules/{rule.Id}", ApiResponse<TaxRuleDto>.Success(rule, HttpContext.TraceIdentifier));
    }
}
