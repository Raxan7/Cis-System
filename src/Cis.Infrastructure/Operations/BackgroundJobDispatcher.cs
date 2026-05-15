using System.Threading.Channels;
using Cis.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cis.Infrastructure.Operations;

public sealed class BackgroundJobDispatcher : BackgroundService, IBackgroundJobDispatcher
{
    private readonly Channel<IQueuedJob> _channel;
    private readonly ILogger<BackgroundJobDispatcher> _logger;
    private int _queuedCount;
    private int _activeCount;
    private long _completedCount;
    private long _failedCount;
    private DateTime? _lastCompletedAtUtc;
    private DateTime? _lastFailedAtUtc;

    public BackgroundJobDispatcher(IOptions<PerformanceExecutionOptions> options, ILogger<BackgroundJobDispatcher> logger)
    {
        var queueCapacity = Math.Max(16, options.Value.BackgroundQueueCapacity);
        _channel = Channel.CreateBounded<IQueuedJob>(new BoundedChannelOptions(queueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(string jobName, Func<CancellationToken, Task<T>> workItem, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);
        ArgumentNullException.ThrowIfNull(workItem);

        var queuedJob = new QueuedJob<T>(jobName, workItem);
        Interlocked.Increment(ref _queuedCount);
        await _channel.Writer.WriteAsync(queuedJob, cancellationToken);
        return await queuedJob.Completion.Task.WaitAsync(cancellationToken);
    }

    public BackgroundJobQueueSnapshot GetSnapshot()
    {
        return new BackgroundJobQueueSnapshot(
            Volatile.Read(ref _queuedCount),
            Volatile.Read(ref _activeCount),
            Interlocked.Read(ref _completedCount),
            Interlocked.Read(ref _failedCount),
            _lastCompletedAtUtc,
            _lastFailedAtUtc);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var queuedJob in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            Interlocked.Decrement(ref _queuedCount);
            Interlocked.Increment(ref _activeCount);

            try
            {
                _logger.LogInformation("Processing background job {JobName}.", queuedJob.Name);
                await queuedJob.ExecuteAsync(stoppingToken);
                Interlocked.Increment(ref _completedCount);
                _lastCompletedAtUtc = DateTime.UtcNow;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                queuedJob.TrySetCanceled(stoppingToken);
                throw;
            }
            catch (Exception exception)
            {
                Interlocked.Increment(ref _failedCount);
                _lastFailedAtUtc = DateTime.UtcNow;
                queuedJob.TrySetException(exception);
                _logger.LogError(exception, "Background job {JobName} failed.", queuedJob.Name);
            }
            finally
            {
                Interlocked.Decrement(ref _activeCount);
            }
        }
    }

    private interface IQueuedJob
    {
        string Name { get; }

        Task ExecuteAsync(CancellationToken cancellationToken);

        void TrySetCanceled(CancellationToken cancellationToken);

        void TrySetException(Exception exception);
    }

    private sealed class QueuedJob<T> : IQueuedJob
    {
        private readonly Func<CancellationToken, Task<T>> _workItem;

        public QueuedJob(string name, Func<CancellationToken, Task<T>> workItem)
        {
            Name = name;
            _workItem = workItem;
        }

        public string Name { get; }

        public TaskCompletionSource<T> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                var result = await _workItem(cancellationToken);
                Completion.TrySetResult(result);
            }
            catch (OperationCanceledException exception)
            {
                Completion.TrySetCanceled(exception.CancellationToken);
            }
            catch (Exception exception)
            {
                Completion.TrySetException(exception);
                throw;
            }
        }

        public void TrySetCanceled(CancellationToken cancellationToken)
        {
            Completion.TrySetCanceled(cancellationToken);
        }

        public void TrySetException(Exception exception)
        {
            Completion.TrySetException(exception);
        }
    }
}
