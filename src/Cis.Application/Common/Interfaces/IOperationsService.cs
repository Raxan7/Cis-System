using Cis.Contracts.Operations;

namespace Cis.Application.Common.Interfaces;

public interface IOperationsService
{
    Task<IReadOnlyCollection<BackupRunRecordDto>> GetBackupRunsAsync(CancellationToken cancellationToken = default);

    Task<DrTestRecordDto> CreateDrTestAsync(CreateDrTestRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DrTestRecordDto>> GetDrTestsAsync(CancellationToken cancellationToken = default);
}
