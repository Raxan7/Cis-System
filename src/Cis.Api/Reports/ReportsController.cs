using Cis.Api.Common;
using Cis.Api.Security;
using Cis.Application.Common.Interfaces;
using Cis.Application.Common.Security;
using Cis.Contracts;
using Cis.Contracts.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Cis.Api.Reports;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("definitions")]
    [RequirePermission(Permissions.Reports.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ReportDefinitionDto>>>> GetDefinitions([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var definitions = await _reportService.GetDefinitionsAsync(pagination, cancellationToken);
        return this.OkPaged(definitions);
    }

    [HttpPost("schedules")]
    [RequirePermission(Permissions.Reports.SchedulesManage)]
    public async Task<ActionResult<ApiResponse<ReportScheduleDto>>> CreateSchedule([FromBody] CreateReportScheduleRequest request, CancellationToken cancellationToken)
    {
        var schedule = await _reportService.CreateScheduleAsync(request, cancellationToken);
        return Created($"/api/reports/schedules/{schedule.Id}", ApiResponse<ReportScheduleDto>.Success(schedule, HttpContext.TraceIdentifier));
    }

    [HttpPost("{code}/run")]
    [RequirePermission(Permissions.Reports.Run)]
    public async Task<ActionResult<ApiResponse<ReportRunDto>>> RunReport(string code, [FromBody] RunReportRequest request, CancellationToken cancellationToken)
    {
        var run = await _reportService.RunReportAsync(code, request, cancellationToken);
        return Created($"/api/reports/runs/{run.Id}", ApiResponse<ReportRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpGet("{code}/query")]
    [RequirePermission(Permissions.Reports.Read)]
    public async Task<ActionResult<ApiResponse<ReportQueryResultDto>>> QueryReport(string code, [FromQuery] ReportQueryParameters parameters, CancellationToken cancellationToken)
    {
        var result = await _reportService.QueryReportAsync(code, parameters, cancellationToken);
        return Ok(ApiResponse<ReportQueryResultDto>.Success(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{code}/export/csv")]
    [RequirePermission(Permissions.Reports.Read)]
    public async Task<ActionResult<ApiResponse<ReportCsvExportDto>>> ExportReportCsv(string code, [FromQuery] ReportQueryParameters parameters, CancellationToken cancellationToken)
    {
        var result = await _reportService.ExportReportCsvAsync(code, parameters, cancellationToken);
        return Ok(ApiResponse<ReportCsvExportDto>.Success(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("runs/{id:guid}/approve")]
    [RequirePermission(Permissions.Reports.Approve)]
    public async Task<ActionResult<ApiResponse<ReportRunDto>>> ApproveRun(Guid id, [FromBody] ReportActionRequest request, CancellationToken cancellationToken)
    {
        var run = await _reportService.ApproveRunAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ReportRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpPost("runs/{id:guid}/publish")]
    [RequirePermission(Permissions.Reports.Publish)]
    public async Task<ActionResult<ApiResponse<ReportRunDto>>> PublishRun(Guid id, [FromBody] ReportActionRequest request, CancellationToken cancellationToken)
    {
        var run = await _reportService.PublishRunAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ReportRunDto>.Success(run, HttpContext.TraceIdentifier));
    }

    [HttpGet("runs")]
    [RequirePermission(Permissions.Reports.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ReportRunDto>>>> GetRuns([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var runs = await _reportService.GetRunsAsync(pagination, cancellationToken);
        return this.OkPaged(runs);
    }

    [HttpGet("runs/{id:guid}/download")]
    [RequirePermission(Permissions.Reports.Download)]
    public async Task<ActionResult<ApiResponse<ReportDownloadDto>>> DownloadRun(Guid id, CancellationToken cancellationToken)
    {
        var download = await _reportService.DownloadRunAsync(id, cancellationToken);
        return Ok(ApiResponse<ReportDownloadDto>.Success(download, HttpContext.TraceIdentifier));
    }

    [HttpPost("bundles")]
    [RequirePermission(Permissions.Reports.BundlesManage)]
    public async Task<ActionResult<ApiResponse<ReportBundleDto>>> CreateBundle([FromBody] CreateReportBundleRequest request, CancellationToken cancellationToken)
    {
        var bundle = await _reportService.CreateBundleAsync(request, cancellationToken);
        return Created($"/api/reports/bundles/{bundle.Id}", ApiResponse<ReportBundleDto>.Success(bundle, HttpContext.TraceIdentifier));
    }

    [HttpPost("bundles/{id:guid}/publish")]
    [RequirePermission(Permissions.Reports.Publish)]
    public async Task<ActionResult<ApiResponse<ReportBundleDto>>> PublishBundle(Guid id, CancellationToken cancellationToken)
    {
        var bundle = await _reportService.PublishBundleAsync(id, cancellationToken);
        return Ok(ApiResponse<ReportBundleDto>.Success(bundle, HttpContext.TraceIdentifier));
    }
}
