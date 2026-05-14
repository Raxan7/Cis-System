using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Contracts;
using Cis.Contracts.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cis.Api.Identity;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IIdentityService _identityService;

    public AuthController(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiRateLimitingPolicyNames.PublicAuth)]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _identityService.LoginAsync(request, cancellationToken);
        var apiResponse = ApiResponse<AuthTokenResponse>.Success(response, HttpContext.TraceIdentifier);
        var json = System.Text.Json.JsonSerializer.Serialize(apiResponse, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        return Content(json, "application/json");
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiRateLimitingPolicyNames.PublicAuth)]
    [ProducesResponseType(typeof(ApiResponse<AuthTokenResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _identityService.RefreshAsync(request, cancellationToken);
        var apiResponse = ApiResponse<AuthTokenResponse>.Success(response, HttpContext.TraceIdentifier);
        var json = System.Text.Json.JsonSerializer.Serialize(apiResponse, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        return Content(json, "application/json");
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken cancellationToken)
    {
        await _identityService.LogoutAsync(request?.RefreshToken, cancellationToken);
        return NoContent();
    }
}
