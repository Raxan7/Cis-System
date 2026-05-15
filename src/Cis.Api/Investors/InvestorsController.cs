using Cis.Api.Common;
using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Investors;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.Investors;

[ApiController]
[Route("api/investors")]
public sealed class InvestorsController : ControllerBase
{
    private readonly IInvestorService _investorService;

    public InvestorsController(IInvestorService investorService)
    {
        _investorService = investorService;
    }

    [HttpPost]
    [RequirePermission(Permissions.Investors.Create)]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<InvestorDto>>> Create([FromBody] CreateInvestorRequest request, CancellationToken cancellationToken)
    {
        var investor = await _investorService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = investor.Id }, ApiResponse<InvestorDto>.Success(investor, HttpContext.TraceIdentifier));
    }

    [HttpGet]
    [RequirePermission(Permissions.Investors.Read)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<InvestorDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<InvestorDto>>>> Get([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var investors = await _investorService.GetAsync(pagination, cancellationToken);
        return this.OkPaged(investors);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.Investors.Read)]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InvestorDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var investor = await _investorService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<InvestorDto>.Success(investor, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.Investors.Amend)]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InvestorDto>>> Update(Guid id, [FromBody] UpdateInvestorRequest request, CancellationToken cancellationToken)
    {
        var investor = await _investorService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<InvestorDto>.Success(investor, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/documents")]
    [RequirePermission(Permissions.Investors.DocumentsUpload)]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InvestorDto>>> AddDocument(Guid id, [FromBody] AddKycDocumentRequest request, CancellationToken cancellationToken)
    {
        var investor = await _investorService.AddDocumentAsync(id, request, cancellationToken);
        return Ok(ApiResponse<InvestorDto>.Success(investor, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/bank-accounts")]
    [RequirePermission(Permissions.Investors.Amend)]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InvestorDto>>> AddBankAccount(Guid id, [FromBody] AddInvestorBankAccountRequest request, CancellationToken cancellationToken)
    {
        var investor = await _investorService.AddBankAccountAsync(id, request, cancellationToken);
        return Ok(ApiResponse<InvestorDto>.Success(investor, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/submit-kyc")]
    [RequirePermission(Permissions.Investors.SubmitKyc)]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InvestorDto>>> SubmitKyc(Guid id, CancellationToken cancellationToken)
    {
        var investor = await _investorService.SubmitKycAsync(id, cancellationToken);
        return Ok(ApiResponse<InvestorDto>.Success(investor, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/approve")]
    [RequirePermission(Permissions.Investors.Approve)]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InvestorDto>>> Approve(Guid id, [FromBody] InvestorWorkflowDecisionRequest request, CancellationToken cancellationToken)
    {
        var investor = await _investorService.ApproveAsync(id, request, cancellationToken);
        return Ok(ApiResponse<InvestorDto>.Success(investor, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/reject")]
    [RequirePermission(Permissions.Investors.Reject)]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InvestorDto>>> Reject(Guid id, [FromBody] InvestorWorkflowDecisionRequest request, CancellationToken cancellationToken)
    {
        var investor = await _investorService.RejectAsync(id, request, cancellationToken);
        return Ok(ApiResponse<InvestorDto>.Success(investor, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/suspend")]
    [RequirePermission(Permissions.Investors.Suspend)]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InvestorDto>>> Suspend(Guid id, [FromBody] InvestorWorkflowDecisionRequest request, CancellationToken cancellationToken)
    {
        var investor = await _investorService.SuspendAsync(id, request, cancellationToken);
        return Ok(ApiResponse<InvestorDto>.Success(investor, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/close")]
    [RequirePermission(Permissions.Investors.Close)]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InvestorDto>>> Close(Guid id, [FromBody] InvestorWorkflowDecisionRequest request, CancellationToken cancellationToken)
    {
        var investor = await _investorService.CloseAsync(id, request, cancellationToken);
        return Ok(ApiResponse<InvestorDto>.Success(investor, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/aml-screening")]
    [RequirePermission(Permissions.Investors.AmlScreen)]
    [ProducesResponseType(typeof(ApiResponse<InvestorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<InvestorDto>>> StartAmlScreening(Guid id, [FromBody] StartAmlScreeningRequest request, CancellationToken cancellationToken)
    {
        var investor = await _investorService.StartAmlScreeningAsync(id, request, cancellationToken);
        return Ok(ApiResponse<InvestorDto>.Success(investor, HttpContext.TraceIdentifier));
    }
}
