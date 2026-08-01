namespace WidgetPlatform.Services.Notifications;

public sealed class WebhookBackgroundService : BackgroundService
{
    private readonly ISubmissionNotificationQueue _queue;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<WebhookBackgroundService> _logger;

    public WebhookBackgroundService(
        ISubmissionNotificationQueue queue,
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<WebhookBackgroundService> logger)
    {
        _queue = queue;
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var url = _config["Webhook:Url"];

        await foreach (var notification in _queue.DequeueAllAsync(stoppingToken))
        {
            if (string.IsNullOrWhiteSpace(url))
                continue;

            try
            {
                var client = _httpFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                var response = await client.PostAsJsonAsync(url, notification, stoppingToken);

                if (response.IsSuccessStatusCode)
                    _logger.LogInformation("Webhook sent for {Id}", notification.SubmissionId);
                else
                    _logger.LogWarning("Webhook returned {Status} for {Id}",
                        (int)response.StatusCode, notification.SubmissionId);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook failed for {Id}", notification.SubmissionId);
            }
        }
    }
}