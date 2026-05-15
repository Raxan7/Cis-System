using Cis.Api.Common;
using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Schemes;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.Schemes;

[ApiController]
[Route("api/schemes")]
public sealed class SchemesController : ControllerBase
{
    private readonly ISchemeService _schemeService;

    public SchemesController(ISchemeService schemeService)
    {
        _schemeService = schemeService;
    }

    [HttpPost]
    [RequirePermission(Permissions.Schemes.Create)]
    [ProducesResponseType(typeof(ApiResponse<SchemeDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<SchemeDto>>> Create(
        [FromBody] CreateSchemeRequest request,
        CancellationToken cancellationToken)
    {
        var scheme = await _schemeService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = scheme.Id }, ApiResponse<SchemeDto>.Success(scheme, HttpContext.TraceIdentifier));
    }

    [HttpGet]
    [RequirePermission(Permissions.Schemes.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<SchemeDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SchemeDto>>>> Get([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var schemes = await _schemeService.GetAsync(pagination, cancellationToken);
        return this.OkPaged(schemes);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.Schemes.Read)]
    [ProducesResponseType(typeof(ApiResponse<SchemeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SchemeDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var scheme = await _schemeService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<SchemeDto>.Success(scheme, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.Schemes.Amend)]
    [ProducesResponseType(typeof(ApiResponse<SchemeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SchemeDto>>> Update(
        Guid id,
        [FromBody] UpdateSchemeRequest request,
        CancellationToken cancellationToken)
    {
        var scheme = await _schemeService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SchemeDto>.Success(scheme, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/submit")]
    [RequirePermission(Permissions.Schemes.Submit)]
    [ProducesResponseType(typeof(ApiResponse<SchemeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SchemeDto>>> Submit(
        Guid id,
        [FromBody] SchemeWorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        var scheme = await _schemeService.SubmitAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SchemeDto>.Success(scheme, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/check")]
    [RequirePermission(Permissions.Schemes.Check)]
    [ProducesResponseType(typeof(ApiResponse<SchemeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SchemeDto>>> Check(
        Guid id,
        [FromBody] SchemeWorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        var scheme = await _schemeService.CheckAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SchemeDto>.Success(scheme, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/approve")]
    [RequirePermission(Permissions.Schemes.Approve)]
    [ProducesResponseType(typeof(ApiResponse<SchemeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SchemeDto>>> Approve(
        Guid id,
        [FromBody] SchemeWorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        var scheme = await _schemeService.ApproveAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SchemeDto>.Success(scheme, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/classes")]
    [RequirePermission(Permissions.Schemes.Amend)]
    [ProducesResponseType(typeof(ApiResponse<SchemeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SchemeDto>>> AddClass(
        Guid id,
        [FromBody] AddSchemeClassRequest request,
        CancellationToken cancellationToken)
    {
        var scheme = await _schemeService.AddClassAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SchemeDto>.Success(scheme, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/fee-schedules")]
    [RequirePermission(Permissions.Schemes.Amend)]
    [ProducesResponseType(typeof(ApiResponse<SchemeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SchemeDto>>> AddFeeSchedule(
        Guid id,
        [FromBody] AddFeeScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var scheme = await _schemeService.AddFeeScheduleAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SchemeDto>.Success(scheme, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/eligibility-rules")]
    [RequirePermission(Permissions.Schemes.Amend)]
    [ProducesResponseType(typeof(ApiResponse<SchemeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SchemeDto>>> AddEligibilityRule(
        Guid id,
        [FromBody] AddEligibilityRuleRequest request,
        CancellationToken cancellationToken)
    {
        var scheme = await _schemeService.AddEligibilityRuleAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SchemeDto>.Success(scheme, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/approved-instruments")]
    [RequirePermission(Permissions.Schemes.Amend)]
    [ProducesResponseType(typeof(ApiResponse<SchemeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SchemeDto>>> AddApprovedInstrument(
        Guid id,
        [FromBody] AddApprovedInstrumentRuleRequest request,
        CancellationToken cancellationToken)
    {
        var scheme = await _schemeService.AddApprovedInstrumentAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SchemeDto>.Success(scheme, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/bank-accounts")]
    [RequirePermission(Permissions.Schemes.Amend)]
    [ProducesResponseType(typeof(ApiResponse<SchemeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SchemeDto>>> AddBankAccount(
        Guid id,
        [FromBody] AddSchemeBankAccountRequest request,
        CancellationToken cancellationToken)
    {
        var scheme = await _schemeService.AddBankAccountAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SchemeDto>.Success(scheme, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/custodian-mappings")]
    [RequirePermission(Permissions.Schemes.Amend)]
    [ProducesResponseType(typeof(ApiResponse<SchemeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SchemeDto>>> AddCustodianMapping(
        Guid id,
        [FromBody] AddSchemeCustodianMappingRequest request,
        CancellationToken cancellationToken)
    {
        var scheme = await _schemeService.AddCustodianMappingAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SchemeDto>.Success(scheme, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/configuration")]
    [RequirePermission(Permissions.Schemes.Amend)]
    [ProducesResponseType(typeof(ApiResponse<SchemeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SchemeDto>>> AddConfiguration(
        Guid id,
        [FromBody] AddSchemeConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var scheme = await _schemeService.AddConfigurationAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SchemeDto>.Success(scheme, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/risk-profiles")]
    [RequirePermission(Permissions.Schemes.Amend)]
    [ProducesResponseType(typeof(ApiResponse<SchemeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SchemeDto>>> AddRiskProfile(
        Guid id,
        [FromBody] AddSchemeRiskProfileRequest request,
        CancellationToken cancellationToken)
    {
        var scheme = await _schemeService.AddRiskProfileAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SchemeDto>.Success(scheme, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/liquidity-thresholds")]
    [RequirePermission(Permissions.Schemes.Amend)]
    [ProducesResponseType(typeof(ApiResponse<SchemeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SchemeDto>>> AddLiquidityThreshold(
        Guid id,
        [FromBody] AddLiquidityThresholdRequest request,
        CancellationToken cancellationToken)
    {
        var scheme = await _schemeService.AddLiquidityThresholdAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SchemeDto>.Success(scheme, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/distribution-rules")]
    [RequirePermission(Permissions.Schemes.Amend)]
    [ProducesResponseType(typeof(ApiResponse<SchemeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SchemeDto>>> AddDistributionRule(
        Guid id,
        [FromBody] AddDistributionRuleRequest request,
        CancellationToken cancellationToken)
    {
        var scheme = await _schemeService.AddDistributionRuleAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SchemeDto>.Success(scheme, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/template-mappings")]
    [RequirePermission(Permissions.Schemes.Amend)]
    [ProducesResponseType(typeof(ApiResponse<SchemeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SchemeDto>>> AddTemplateMapping(
        Guid id,
        [FromBody] AddTemplateMappingRequest request,
        CancellationToken cancellationToken)
    {
        var scheme = await _schemeService.AddTemplateMappingAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SchemeDto>.Success(scheme, HttpContext.TraceIdentifier));
    }
}
