using Cis.Api.Common;
using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Investors;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.Aml;

[ApiController]
[Route("api/aml")]
public sealed class AmlController : ControllerBase
{
    private readonly IAmlQueryService _amlQueryService;

    public AmlController(IAmlQueryService amlQueryService)
    {
        _amlQueryService = amlQueryService;
    }

    [HttpGet("exceptions")]
    [RequirePermission(Permissions.Investors.AmlRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyCollection<AmlExceptionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AmlExceptionDto>>>> GetExceptions([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var exceptions = await _amlQueryService.GetExceptionsAsync(cancellationToken);
        return this.OkPaged(exceptions, pagination);
    }
}
