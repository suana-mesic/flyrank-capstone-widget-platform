namespace WidgetPlatform.Services.Notifications;

public sealed record SubmissionNotification(
    Guid SubmissionId,
    Guid WidgetId,
    Guid TenantId,
    string? Country,
    string? City,
    DateTime CreatedAtUtc);