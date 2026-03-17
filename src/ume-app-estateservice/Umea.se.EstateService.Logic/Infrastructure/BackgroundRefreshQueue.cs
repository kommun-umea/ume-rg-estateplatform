using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Umea.se.EstateService.Logic.Infrastructure;

/// <summary>
/// Bounded, deduplicated, concurrency-limited background work queue.
/// Items are enqueued by ID. Duplicate IDs are silently dropped while the original is still pending.
/// The consumer runs with a configurable concurrency limit via <see cref="SemaphoreSlim"/>.
/// </summary>
public sealed class BackgroundRefreshQueue
{
    private readonly Channel<int> _channel;
    private readonly ConcurrentDictionary<int, byte> _pending = new();
    private readonly SemaphoreSlim _concurrency;
    private readonly ILogger _logger;
    private readonly string _name;

    public BackgroundRefreshQueue(string name, int capacity, int maxConcurrency, ILogger logger)
    {
        _name = name;
        _logger = logger;
        _concurrency = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        _channel = Channel.CreateBounded<int>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = false,
            SingleWriter = false
        });
    }

    /// <summary>
    /// Enqueue an ID for background refresh. No-op if already queued or channel full.
    /// </summary>
    public void Enqueue(int id)
    {
        if (!_pending.TryAdd(id, 0))
        {
            return; // already pending
        }

        if (!_channel.Writer.TryWrite(id))
        {
            // Channel full — remove from pending so next request can re-enqueue
            _pending.TryRemove(id, out _);
        }
    }

    /// <summary>
    /// Long-running consumer loop. Reads IDs from channel, calls fetchAsync with bounded concurrency.
    /// </summary>
    public async Task RunConsumerAsync(Func<int, CancellationToken, Task> fetchAsync, CancellationToken stoppingToken)
    {
        _logger.LogInformation("{QueueName} background consumer started", _name);

        try
        {
            await foreach (int id in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                await _concurrency.WaitAsync(stoppingToken);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await fetchAsync(id, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        // shutting down
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "{QueueName}: background refresh failed for ID {Id}", _name, id);
                    }
                    finally
                    {
                        _pending.TryRemove(id, out _);
                        _concurrency.Release();
                    }
                }, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // normal shutdown
        }

        _logger.LogInformation("{QueueName} background consumer stopped", _name);
    }
}
