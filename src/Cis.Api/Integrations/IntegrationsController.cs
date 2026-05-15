using Cis.Api.Common;
using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Integrations;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.Integrations;

[ApiController]
[Route("api/integrations")]
public sealed class IntegrationsController : ControllerBase
{
    private readonly IIntegrationService _integrationService;

    public IntegrationsController(IIntegrationService integrationService)
    {
        _integrationService = integrationService;
    }

    [HttpPost("bank-statements/upload")]
    [RequirePermission(Permissions.Integrations.InboundUpload)]
    public async Task<ActionResult<ApiResponse<IntegrationResultDto>>> UploadBankStatement(
        [FromBody] UploadBankStatementIntegrationRequest request,
        [FromHeader(Name = StandardHeaders.IdempotencyKey)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _integrationService.UploadBankStatementAsync(request, idempotencyKey, cancellationToken);
        return Created($"/api/integrations/messages/{result.Message.Id}", ApiResponse<IntegrationResultDto>.Success(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("mobile-money/callback")]
    [RequirePermission(Permissions.Integrations.InboundUpload)]
    public async Task<ActionResult<ApiResponse<IntegrationResultDto>>> MobileMoneyCallback(
        [FromBody] MobileMoneyCallbackRequest request,
        [FromHeader(Name = StandardHeaders.IdempotencyKey)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _integrationService.HandleMobileMoneyCallbackAsync(request, idempotencyKey, cancellationToken);
        return Created($"/api/integrations/messages/{result.Message.Id}", ApiResponse<IntegrationResultDto>.Success(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("custodian/holdings/upload")]
    [RequirePermission(Permissions.Integrations.InboundUpload)]
    public async Task<ActionResult<ApiResponse<IntegrationResultDto>>> UploadCustodianHoldings(
        [FromBody] CustodianHoldingsUploadRequest request,
        [FromHeader(Name = StandardHeaders.IdempotencyKey)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _integrationService.UploadHoldingsAsync(request, idempotencyKey, cancellationToken);
        return Created($"/api/integrations/messages/{result.Message.Id}", ApiResponse<IntegrationResultDto>.Success(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("pricing/upload")]
    [RequirePermission(Permissions.Integrations.InboundUpload)]
    public async Task<ActionResult<ApiResponse<IntegrationResultDto>>> UploadPricing(
        [FromBody] PricingUploadRequest request,
        [FromHeader(Name = StandardHeaders.IdempotencyKey)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _integrationService.UploadPricingAsync(request, idempotencyKey, cancellationToken);
        return Created($"/api/integrations/messages/{result.Message.Id}", ApiResponse<IntegrationResultDto>.Success(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("notifications/test-email")]
    [RequirePermission(Permissions.Integrations.NotificationsTest)]
    public async Task<ActionResult<ApiResponse<IntegrationResultDto>>> TestEmail([FromBody] TestEmailRequest request, CancellationToken cancellationToken)
    {
        var result = await _integrationService.SendTestEmailAsync(request, cancellationToken);
        return Ok(ApiResponse<IntegrationResultDto>.Success(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("erp/export")]
    [RequirePermission(Permissions.Integrations.ErpExport)]
    public async Task<ActionResult<ApiResponse<IntegrationResultDto>>> ExportErp([FromBody] ErpExportRequest request, CancellationToken cancellationToken)
    {
        var result = await _integrationService.ExportAsync(request, cancellationToken);
        return Ok(ApiResponse<IntegrationResultDto>.Success(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("messages")]
    [RequirePermission(Permissions.Integrations.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<IntegrationMessageDto>>>> GetMessages([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var messages = await _integrationService.GetMessagesAsync(cancellationToken);
        return this.OkPaged(messages, pagination);
    }

    [HttpGet("errors")]
    [RequirePermission(Permissions.Integrations.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<IntegrationErrorDto>>>> GetErrors([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var errors = await _integrationService.GetErrorsAsync(cancellationToken);
        return this.OkPaged(errors, pagination);
    }
}
