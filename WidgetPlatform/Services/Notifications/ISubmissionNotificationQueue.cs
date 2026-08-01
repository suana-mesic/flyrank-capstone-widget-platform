namespace WidgetPlatform.Services.Notifications
{
    public interface ISubmissionNotificationQueue
    {
        void Enqueue(SubmissionNotification notification);
        IAsyncEnumerable<SubmissionNotification> DequeueAllAsync(CancellationToken ct);
    }
}
