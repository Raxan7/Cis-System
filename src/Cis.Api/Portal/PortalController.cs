using Cis.Api.Common;
using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Portal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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

    [HttpPost("self-registration")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiRateLimitingPolicyNames.PublicAuth)]
    public async Task<ActionResult<ApiResponse<PortalSelfRegistrationInitiatedDto>>> CreateSelfRegistration([FromBody] CreatePortalSelfRegistrationRequest request, CancellationToken cancellationToken)
    {
        var registration = await _portalService.CreateSelfRegistrationAsync(request, cancellationToken);
        return Accepted(ApiResponse<PortalSelfRegistrationInitiatedDto>.Success(registration, HttpContext.TraceIdentifier));
    }

    [HttpPost("self-registration/verify-otp")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiRateLimitingPolicyNames.PublicAuth)]
    public async Task<ActionResult<ApiResponse<PortalSelfRegistrationActivationDto>>> VerifySelfRegistrationOtp([FromBody] VerifyPortalSelfRegistrationOtpRequest request, CancellationToken cancellationToken)
    {
        var activation = await _portalService.VerifySelfRegistrationOtpAsync(request, cancellationToken);
        return Ok(ApiResponse<PortalSelfRegistrationActivationDto>.Success(activation, HttpContext.TraceIdentifier));
    }

    [HttpGet("me")]
    [RequirePermission(Permissions.Portal.Read)]
    public async Task<ActionResult<ApiResponse<PortalProfileDto>>> GetMe(CancellationToken cancellationToken)
    {
        var profile = await _portalService.GetMeAsync(cancellationToken);
        return Ok(ApiResponse<PortalProfileDto>.Success(profile, HttpContext.TraceIdentifier));
    }

    [HttpGet("fund-nav")]
    [RequirePermission(Permissions.Portal.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PortalFundNavDto>>>> GetFundNav([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var fundNav = await _portalService.GetFundNavAsync(cancellationToken);
        return this.OkPaged(fundNav, pagination);
    }

    [HttpGet("portfolio")]
    [RequirePermission(Permissions.Portal.Read)]
    public async Task<ActionResult<ApiResponse<PortalPortfolioSummaryDto>>> GetPortfolio(CancellationToken cancellationToken)
    {
        var portfolio = await _portalService.GetPortfolioSummaryAsync(cancellationToken);
        return Ok(ApiResponse<PortalPortfolioSummaryDto>.Success(portfolio, HttpContext.TraceIdentifier));
    }

    [HttpGet("kyc")]
    [RequirePermission(Permissions.Portal.Read)]
    public async Task<ActionResult<ApiResponse<PortalKycProfileDto>>> GetKycProfile(CancellationToken cancellationToken)
    {
        var profile = await _portalService.GetKycProfileAsync(cancellationToken);
        return Ok(ApiResponse<PortalKycProfileDto>.Success(profile, HttpContext.TraceIdentifier));
    }

    [HttpGet("holdings")]
    [RequirePermission(Permissions.Portal.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PortalHoldingDto>>>> GetHoldings([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var holdings = await _portalService.GetHoldingsAsync(cancellationToken);
        return this.OkPaged(holdings, pagination);
    }

    [HttpGet("transactions")]
    [RequirePermission(Permissions.Portal.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PortalTransactionDto>>>> GetTransactions([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var transactions = await _portalService.GetTransactionsAsync(cancellationToken);
        return this.OkPaged(transactions, pagination);
    }

    [HttpGet("statements")]
    [RequirePermission(Permissions.Portal.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PortalStatementDto>>>> GetStatements([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var statements = await _portalService.GetStatementsAsync(cancellationToken);
        return this.OkPaged(statements, pagination);
    }

    [HttpGet("tax-certificates")]
    [RequirePermission(Permissions.Portal.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PortalTaxCertificateDto>>>> GetTaxCertificates([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var certificates = await _portalService.GetTaxCertificatesAsync(cancellationToken);
        return this.OkPaged(certificates, pagination);
    }

    [HttpGet("notices")]
    [RequirePermission(Permissions.Portal.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<InvestorNoticeDto>>>> GetNotices([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var notices = await _portalService.GetNoticesAsync(cancellationToken);
        return this.OkPaged(notices, pagination);
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

    [HttpPost("requests/transfer")]
    [RequirePermission(Permissions.Portal.RequestsCreate)]
    public async Task<ActionResult<ApiResponse<DigitalServiceRequestDto>>> CreateTransfer([FromBody] CreatePortalTransferRequest request, CancellationToken cancellationToken)
    {
        var digitalRequest = await _portalService.CreateTransferRequestAsync(request, cancellationToken);
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
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PortalActivityLogDto>>>> GetActivity([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var activity = await _portalService.GetActivityAsync(cancellationToken);
        return this.OkPaged(activity, pagination);
    }
}
