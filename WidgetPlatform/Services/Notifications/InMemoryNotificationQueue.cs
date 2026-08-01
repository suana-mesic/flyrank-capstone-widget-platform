using System.Threading.Channels;

namespace WidgetPlatform.Services.Notifications;

public sealed class InMemoryNotificationQueue : ISubmissionNotificationQueue
{
    private readonly Channel<SubmissionNotification> _channel =
        Channel.CreateBounded<SubmissionNotification>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        });

    public void Enqueue(SubmissionNotification n) => _channel.Writer.TryWrite(n);

    public IAsyncEnumerable<SubmissionNotification> DequeueAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}