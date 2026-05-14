using Cis.Contracts.Reports;

namespace Cis.Application.Common.Interfaces;

public interface IReportService
{
    Task<IReadOnlyCollection<ReportDefinitionDto>> GetDefinitionsAsync(CancellationToken cancellationToken = default);

    Task<ReportScheduleDto> CreateScheduleAsync(CreateReportScheduleRequest request, CancellationToken cancellationToken = default);

    Task<ReportRunDto> RunReportAsync(string code, RunReportRequest request, CancellationToken cancellationToken = default);

    Task<ReportQueryResultDto> QueryReportAsync(string code, ReportQueryParameters parameters, CancellationToken cancellationToken = default);

    Task<ReportCsvExportDto> ExportReportCsvAsync(string code, ReportQueryParameters parameters, CancellationToken cancellationToken = default);

    Task<ReportRunDto> ApproveRunAsync(Guid id, ReportActionRequest request, CancellationToken cancellationToken = default);

    Task<ReportRunDto> PublishRunAsync(Guid id, ReportActionRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ReportRunDto>> GetRunsAsync(CancellationToken cancellationToken = default);

    Task<ReportDownloadDto> DownloadRunAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ReportBundleDto> CreateBundleAsync(CreateReportBundleRequest request, CancellationToken cancellationToken = default);

    Task<ReportBundleDto> PublishBundleAsync(Guid id, CancellationToken cancellationToken = default);
}
