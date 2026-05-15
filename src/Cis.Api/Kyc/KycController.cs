using Cis.Api.Common;
using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Investors;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.Kyc;

[ApiController]
[Route("api/kyc")]
public sealed class KycController : ControllerBase
{
    private readonly IKycQueryService _kycQueryService;

    public KycController(IKycQueryService kycQueryService)
    {
        _kycQueryService = kycQueryService;
    }

    [HttpGet("expired-documents")]
    [RequirePermission(Permissions.Investors.KycRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<KycDocumentDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<KycDocumentDto>>>> GetExpiredDocuments([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var documents = await _kycQueryService.GetExpiredDocumentsAsync(cancellationToken);
        return this.OkPaged(documents, pagination);
    }

    [HttpGet("incomplete")]
    [RequirePermission(Permissions.Investors.KycRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<IncompleteKycInvestorDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<IncompleteKycInvestorDto>>>> GetIncomplete([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var investors = await _kycQueryService.GetIncompleteInvestorsAsync(cancellationToken);
        return this.OkPaged(investors, pagination);
    }
}
