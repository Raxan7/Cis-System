using Cis.Api.Common;
using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cis.Api.Operations;

[ApiController]
[Route("api/operations")]
public sealed class OperationsController : ControllerBase
{
    private readonly IOperationsService _operationsService;
    private readonly HealthCheckService _healthCheckService;

    public OperationsController(IOperationsService operationsService, HealthCheckService healthCheckService)
    {
        _operationsService = operationsService;
        _healthCheckService = healthCheckService;
    }

    [HttpGet("health/deep")]
    [RequirePermission(Permissions.Operations.HealthRead)]
    public async Task<ActionResult<ApiResponse<DeepHealthDto>>> DeepHealth(CancellationToken cancellationToken)
    {
        var report = await _healthCheckService.CheckHealthAsync(cancellationToken);
        var dto = new DeepHealthDto(
            report.Status.ToString(),
            DateTime.UtcNow,
            decimal.Round((decimal)report.TotalDuration.TotalMilliseconds, 3),
            report.Entries.Select(entry => new DeepHealthEntryDto(
                entry.Key,
                entry.Value.Status.ToString(),
                decimal.Round((decimal)entry.Value.Duration.TotalMilliseconds, 3),
                entry.Value.Description,
                entry.Value.Exception?.Message)).ToArray());

        return Ok(ApiResponse<DeepHealthDto>.Success(dto, HttpContext.TraceIdentifier));
    }

    [HttpGet("backup-runs")]
    [RequirePermission(Permissions.Operations.BackupRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<BackupRunRecordDto>>>> GetBackupRuns([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var records = await _operationsService.GetBackupRunsAsync(cancellationToken);
        return this.OkPaged(records, pagination);
    }

    [HttpPost("dr-tests")]
    [RequirePermission(Permissions.Operations.DrTestsManage)]
    public async Task<ActionResult<ApiResponse<DrTestRecordDto>>> CreateDrTest([FromBody] CreateDrTestRequest request, CancellationToken cancellationToken)
    {
        var record = await _operationsService.CreateDrTestAsync(request, cancellationToken);
        return Created($"/api/operations/dr-tests/{record.Id}", ApiResponse<DrTestRecordDto>.Success(record, HttpContext.TraceIdentifier));
    }

    [HttpGet("dr-tests")]
    [RequirePermission(Permissions.Operations.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<DrTestRecordDto>>>> GetDrTests([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var records = await _operationsService.GetDrTestsAsync(cancellationToken);
        return this.OkPaged(records, pagination);
    }
}
