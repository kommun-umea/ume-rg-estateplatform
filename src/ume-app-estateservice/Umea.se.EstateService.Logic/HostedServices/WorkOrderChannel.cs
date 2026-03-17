using System.Threading.Channels;

namespace Umea.se.EstateService.Logic.HostedServices;

/// <summary>
/// Unbounded channel used to signal the background submission service
/// that a new work order is ready for immediate processing.
/// </summary>
public sealed class WorkOrderChannel
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true });

    public ChannelWriter<Guid> Writer => _channel.Writer;
    public ChannelReader<Guid> Reader => _channel.Reader;

    /// <summary>
    /// Signal that a work order should be processed immediately.
    /// Fire-and-forget — never blocks, never throws on a full channel.
    /// </summary>
    public void Notify(Guid workOrderUid)
    {
        _channel.Writer.TryWrite(workOrderUid);
    }
}
