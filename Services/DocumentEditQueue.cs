using System.Threading.Channels;

namespace CollabDocs.Services;

public class DocumentEditQueue
{
    private readonly Channel<QueuedEditOperation> _queue;

    public DocumentEditQueue()
    {
        var options = new UnboundedChannelOptions
        {
            SingleReader = true, // Force sequential processing
            SingleWriter = false
        };
        _queue = Channel.CreateUnbounded<QueuedEditOperation>(options);
    }

    public async ValueTask EnqueueEditAsync(QueuedEditOperation operation)
    {
        await _queue.Writer.WriteAsync(operation);
    }

    public IAsyncEnumerable<QueuedEditOperation> DequeueEditsAsync(CancellationToken cancellationToken)
    {
        return _queue.Reader.ReadAllAsync(cancellationToken);
    }
}

public record QueuedEditOperation(
    ApplyEditOperationRequest Request,
    Guid UserId,
    string ConnectionId);
