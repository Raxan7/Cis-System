using Cis.Contracts.Archive;

namespace Cis.Application.Common.Interfaces;

public interface IImmutableArchiveService
{
    Task<ArchiveRecordDto> CreateAsync(CreateArchiveRecordRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ArchiveRecordDto>> GetAsync(CancellationToken cancellationToken = default);

    Task<ArchiveRecordDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
