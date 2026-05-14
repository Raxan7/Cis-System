using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Portal;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.Portal;

[ApiController]
[Route("api/portal")]
public sealed class PortalController : ControllerBase
{
    private readonly IPortalService _portalService;

    public PortalController(IPortalService portalService)
    {
        _portalService = portalService;
    }

    [HttpGet("me")]
    [RequirePermission(Permissions.Portal.Read)]
    public async Task<ActionResult<ApiResponse<PortalProfileDto>>> GetMe(CancellationToken cancellationToken)
    {
        var profile = await _portalService.GetMeAsync(cancellationToken);
        return Ok(ApiResponse<PortalProfileDto>.Success(profile, HttpContext.TraceIdentifier));
    }

    [HttpGet("holdings")]
    [RequirePermission(Permissions.Portal.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PortalHoldingDto>>>> GetHoldings(CancellationToken cancellationToken)
    {
        var holdings = await _portalService.GetHoldingsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<PortalHoldingDto>>.Success(holdings, HttpContext.TraceIdentifier));
    }

    [HttpGet("transactions")]
    [RequirePermission(Permissions.Portal.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PortalTransactionDto>>>> GetTransactions(CancellationToken cancellationToken)
    {
        var transactions = await _portalService.GetTransactionsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<PortalTransactionDto>>.Success(transactions, HttpContext.TraceIdentifier));
    }

    [HttpGet("statements")]
    [RequirePermission(Permissions.Portal.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PortalStatementDto>>>> GetStatements(CancellationToken cancellationToken)
    {
        var statements = await _portalService.GetStatementsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<PortalStatementDto>>.Success(statements, HttpContext.TraceIdentifier));
    }

    [HttpGet("tax-certificates")]
    [RequirePermission(Permissions.Portal.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PortalTaxCertificateDto>>>> GetTaxCertificates(CancellationToken cancellationToken)
    {
        var certificates = await _portalService.GetTaxCertificatesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<PortalTaxCertificateDto>>.Success(certificates, HttpContext.TraceIdentifier));
    }

    [HttpGet("notices")]
    [RequirePermission(Permissions.Portal.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<InvestorNoticeDto>>>> GetNotices(CancellationToken cancellationToken)
    {
        var notices = await _portalService.GetNoticesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<InvestorNoticeDto>>.Success(notices, HttpContext.TraceIdentifier));
    }

    [HttpPost("requests/subscription")]
    [RequirePermission(Permissions.Portal.RequestsCreate)]
    public async Task<ActionResult<ApiResponse<DigitalServiceRequestDto>>> CreateSubscription([FromBody] CreatePortalSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var digitalRequest = await _portalService.CreateSubscriptionRequestAsync(request, cancellationToken);
        return Created($"/api/portal/requests/{digitalRequest.Id}", ApiResponse<DigitalServiceRequestDto>.Success(digitalRequest, HttpContext.TraceIdentifier));
    }

    [HttpPost("requests/redemption")]
    [RequirePermission(Permissions.Portal.RequestsCreate)]
    public async Task<ActionResult<ApiResponse<DigitalServiceRequestDto>>> CreateRedemption([FromBody] CreatePortalRedemptionRequest request, CancellationToken cancellationToken)
    {
        var digitalRequest = await _portalService.CreateRedemptionRequestAsync(request, cancellationToken);
        return Created($"/api/portal/requests/{digitalRequest.Id}", ApiResponse<DigitalServiceRequestDto>.Success(digitalRequest, HttpContext.TraceIdentifier));
    }

    [HttpPost("requests/switch")]
    [RequirePermission(Permissions.Portal.RequestsCreate)]
    public async Task<ActionResult<ApiResponse<DigitalServiceRequestDto>>> CreateSwitch([FromBody] CreatePortalSwitchRequest request, CancellationToken cancellationToken)
    {
        var digitalRequest = await _portalService.CreateSwitchRequestAsync(request, cancellationToken);
        return Created($"/api/portal/requests/{digitalRequest.Id}", ApiResponse<DigitalServiceRequestDto>.Success(digitalRequest, HttpContext.TraceIdentifier));
    }

    [HttpPost("requests/profile-update")]
    [RequirePermission(Permissions.Portal.RequestsCreate)]
    public async Task<ActionResult<ApiResponse<DigitalServiceRequestDto>>> CreateProfileUpdate([FromBody] CreatePortalProfileUpdateRequest request, CancellationToken cancellationToken)
    {
        var digitalRequest = await _portalService.CreateProfileUpdateRequestAsync(request, cancellationToken);
        return Created($"/api/portal/requests/{digitalRequest.Id}", ApiResponse<DigitalServiceRequestDto>.Success(digitalRequest, HttpContext.TraceIdentifier));
    }

    [HttpPost("documents")]
    [RequirePermission(Permissions.Portal.DocumentsUpload)]
    public async Task<ActionResult<ApiResponse<DigitalServiceRequestDto>>> UploadDocument([FromBody] UploadPortalDocumentRequest request, CancellationToken cancellationToken)
    {
        var digitalRequest = await _portalService.UploadDocumentAsync(request, cancellationToken);
        return Created($"/api/portal/requests/{digitalRequest.Id}", ApiResponse<DigitalServiceRequestDto>.Success(digitalRequest, HttpContext.TraceIdentifier));
    }

    [HttpGet("activity")]
    [RequirePermission(Permissions.Portal.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PortalActivityLogDto>>>> GetActivity(CancellationToken cancellationToken)
    {
        var activity = await _portalService.GetActivityAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<PortalActivityLogDto>>.Success(activity, HttpContext.TraceIdentifier));
    }
}
