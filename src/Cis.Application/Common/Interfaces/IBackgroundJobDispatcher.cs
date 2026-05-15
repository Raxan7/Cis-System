namespace Cis.Application.Common.Interfaces;

public interface IBackgroundJobDispatcher
{
    Task<T> ExecuteAsync<T>(string jobName, Func<CancellationToken, Task<T>> workItem, CancellationToken cancellationToken = default);

    BackgroundJobQueueSnapshot GetSnapshot();
}

public sealed record BackgroundJobQueueSnapshot(
    int QueuedCount,
    int ActiveCount,
    long CompletedCount,
    long FailedCount,
    DateTime? LastCompletedAtUtc,
    DateTime? LastFailedAtUtc);
