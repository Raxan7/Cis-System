using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Dealing;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.Dealing;

[ApiController]
[Route("api/dealing")]
public sealed class DealingController : ControllerBase
{
    private readonly IDealingService _dealingService;

    public DealingController(IDealingService dealingService)
    {
        _dealingService = dealingService;
    }

    [HttpPost("subscriptions")]
    [RequirePermission(Permissions.Dealing.Create)]
    [ProducesResponseType(typeof(ApiResponse<DealingInstructionDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<DealingInstructionDto>>> CreateSubscription([FromBody] CreateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var instruction = await _dealingService.CreateSubscriptionAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPending), ApiResponse<DealingInstructionDto>.Success(instruction, HttpContext.TraceIdentifier));
    }

    [HttpPost("redemptions")]
    [RequirePermission(Permissions.Dealing.Create)]
    [ProducesResponseType(typeof(ApiResponse<DealingInstructionDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<DealingInstructionDto>>> CreateRedemption([FromBody] CreateRedemptionRequest request, CancellationToken cancellationToken)
    {
        var instruction = await _dealingService.CreateRedemptionAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPending), ApiResponse<DealingInstructionDto>.Success(instruction, HttpContext.TraceIdentifier));
    }

    [HttpPost("switches")]
    [RequirePermission(Permissions.Dealing.Create)]
    [ProducesResponseType(typeof(ApiResponse<DealingInstructionDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<DealingInstructionDto>>> CreateSwitch([FromBody] CreateSwitchRequest request, CancellationToken cancellationToken)
    {
        var instruction = await _dealingService.CreateSwitchAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPending), ApiResponse<DealingInstructionDto>.Success(instruction, HttpContext.TraceIdentifier));
    }

    [HttpPost("transfers")]
    [RequirePermission(Permissions.Dealing.Create)]
    [ProducesResponseType(typeof(ApiResponse<DealingInstructionDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<DealingInstructionDto>>> CreateTransfer([FromBody] CreateTransferRequest request, CancellationToken cancellationToken)
    {
        var instruction = await _dealingService.CreateTransferAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPending), ApiResponse<DealingInstructionDto>.Success(instruction, HttpContext.TraceIdentifier));
    }

    [HttpPost("liens")]
    [RequirePermission(Permissions.Dealing.LiensManage)]
    [ProducesResponseType(typeof(ApiResponse<LienDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<LienDto>>> CreateLien([FromBody] CreateLienRequest request, CancellationToken cancellationToken)
    {
        var lien = await _dealingService.CreateLienAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPending), ApiResponse<LienDto>.Success(lien, HttpContext.TraceIdentifier));
    }

    [HttpPost("liens/{id:guid}/release")]
    [RequirePermission(Permissions.Dealing.LiensManage)]
    [ProducesResponseType(typeof(ApiResponse<LienDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LienDto>>> ReleaseLien(Guid id, [FromBody] ReleaseLienRequest request, CancellationToken cancellationToken)
    {
        var lien = await _dealingService.ReleaseLienAsync(id, request, cancellationToken);
        return Ok(ApiResponse<LienDto>.Success(lien, HttpContext.TraceIdentifier));
    }

    [HttpPost("recurring-plans")]
    [RequirePermission(Permissions.Dealing.RecurringPlansManage)]
    [ProducesResponseType(typeof(ApiResponse<RecurringContributionPlanDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<RecurringContributionPlanDto>>> CreateRecurringPlan([FromBody] CreateRecurringContributionPlanRequest request, CancellationToken cancellationToken)
    {
        var plan = await _dealingService.CreateRecurringPlanAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPending), ApiResponse<RecurringContributionPlanDto>.Success(plan, HttpContext.TraceIdentifier));
    }

    [HttpPut("recurring-plans/{id:guid}")]
    [RequirePermission(Permissions.Dealing.RecurringPlansManage)]
    [ProducesResponseType(typeof(ApiResponse<RecurringContributionPlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RecurringContributionPlanDto>>> AmendRecurringPlan(Guid id, [FromBody] AmendRecurringContributionPlanRequest request, CancellationToken cancellationToken)
    {
        var plan = await _dealingService.AmendRecurringPlanAsync(id, request, cancellationToken);
        return Ok(ApiResponse<RecurringContributionPlanDto>.Success(plan, HttpContext.TraceIdentifier));
    }

    [HttpPost("recurring-plans/{id:guid}/pause")]
    [RequirePermission(Permissions.Dealing.RecurringPlansManage)]
    [ProducesResponseType(typeof(ApiResponse<RecurringContributionPlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RecurringContributionPlanDto>>> PauseRecurringPlan(Guid id, CancellationToken cancellationToken)
    {
        var plan = await _dealingService.PauseRecurringPlanAsync(id, cancellationToken);
        return Ok(ApiResponse<RecurringContributionPlanDto>.Success(plan, HttpContext.TraceIdentifier));
    }

    [HttpPost("recurring-plans/{id:guid}/resume")]
    [RequirePermission(Permissions.Dealing.RecurringPlansManage)]
    [ProducesResponseType(typeof(ApiResponse<RecurringContributionPlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RecurringContributionPlanDto>>> ResumeRecurringPlan(Guid id, CancellationToken cancellationToken)
    {
        var plan = await _dealingService.ResumeRecurringPlanAsync(id, cancellationToken);
        return Ok(ApiResponse<RecurringContributionPlanDto>.Success(plan, HttpContext.TraceIdentifier));
    }

    [HttpPost("recurring-plans/{id:guid}/cancel")]
    [RequirePermission(Permissions.Dealing.RecurringPlansManage)]
    [ProducesResponseType(typeof(ApiResponse<RecurringContributionPlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RecurringContributionPlanDto>>> CancelRecurringPlan(Guid id, CancellationToken cancellationToken)
    {
        var plan = await _dealingService.CancelRecurringPlanAsync(id, cancellationToken);
        return Ok(ApiResponse<RecurringContributionPlanDto>.Success(plan, HttpContext.TraceIdentifier));
    }

    [HttpPost("recurring-plans/{id:guid}/failed-debit")]
    [RequirePermission(Permissions.Dealing.RecurringPlansManage)]
    [ProducesResponseType(typeof(ApiResponse<RecurringContributionPlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RecurringContributionPlanDto>>> RegisterFailedDebit(Guid id, CancellationToken cancellationToken)
    {
        var plan = await _dealingService.RegisterRecurringPlanFailedDebitAsync(id, cancellationToken);
        return Ok(ApiResponse<RecurringContributionPlanDto>.Success(plan, HttpContext.TraceIdentifier));
    }

    [HttpPost("recurring-plans/{id:guid}/missed-collection")]
    [RequirePermission(Permissions.Dealing.RecurringPlansManage)]
    [ProducesResponseType(typeof(ApiResponse<RecurringContributionPlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RecurringContributionPlanDto>>> RegisterMissedCollection(Guid id, CancellationToken cancellationToken)
    {
        var plan = await _dealingService.RegisterRecurringPlanMissedCollectionAsync(id, cancellationToken);
        return Ok(ApiResponse<RecurringContributionPlanDto>.Success(plan, HttpContext.TraceIdentifier));
    }

    [HttpPost("instructions/{id:guid}/submit")]
    [RequirePermission(Permissions.Dealing.Submit)]
    [ProducesResponseType(typeof(ApiResponse<DealingInstructionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DealingInstructionDto>>> Submit(Guid id, [FromBody] DealingInstructionActionRequest request, CancellationToken cancellationToken)
    {
        var instruction = await _dealingService.SubmitInstructionAsync(id, request, cancellationToken);
        return Ok(ApiResponse<DealingInstructionDto>.Success(instruction, HttpContext.TraceIdentifier));
    }

    [HttpPost("instructions/{id:guid}/approve")]
    [RequirePermission(Permissions.Dealing.Approve)]
    [ProducesResponseType(typeof(ApiResponse<DealingInstructionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DealingInstructionDto>>> Approve(Guid id, [FromBody] DealingInstructionActionRequest request, CancellationToken cancellationToken)
    {
        var instruction = await _dealingService.ApproveInstructionAsync(id, request, cancellationToken);
        return Ok(ApiResponse<DealingInstructionDto>.Success(instruction, HttpContext.TraceIdentifier));
    }

    [HttpPost("instructions/{id:guid}/reject")]
    [RequirePermission(Permissions.Dealing.Reject)]
    [ProducesResponseType(typeof(ApiResponse<DealingInstructionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DealingInstructionDto>>> Reject(Guid id, [FromBody] DealingInstructionActionRequest request, CancellationToken cancellationToken)
    {
        var instruction = await _dealingService.RejectInstructionAsync(id, request, cancellationToken);
        return Ok(ApiResponse<DealingInstructionDto>.Success(instruction, HttpContext.TraceIdentifier));
    }

    [HttpPost("instructions/{id:guid}/cancel")]
    [RequirePermission(Permissions.Dealing.Cancel)]
    [ProducesResponseType(typeof(ApiResponse<DealingInstructionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DealingInstructionDto>>> Cancel(Guid id, [FromBody] DealingInstructionActionRequest request, CancellationToken cancellationToken)
    {
        var instruction = await _dealingService.CancelInstructionAsync(id, request, cancellationToken);
        return Ok(ApiResponse<DealingInstructionDto>.Success(instruction, HttpContext.TraceIdentifier));
    }

    [HttpGet("pending")]
    [RequirePermission(Permissions.Dealing.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<DealingInstructionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<DealingInstructionDto>>>> GetPending(CancellationToken cancellationToken)
    {
        var instructions = await _dealingService.GetPendingAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<DealingInstructionDto>>.Success(instructions, HttpContext.TraceIdentifier));
    }

    [HttpGet("cutoff-breaches")]
    [RequirePermission(Permissions.Dealing.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<CutOffBreachDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CutOffBreachDto>>>> GetCutOffBreaches(CancellationToken cancellationToken)
    {
        var breaches = await _dealingService.GetCutOffBreachesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<CutOffBreachDto>>.Success(breaches, HttpContext.TraceIdentifier));
    }
}
