namespace WidgetPlatform.Models
{
    public sealed record Submission(
        Guid Id,
        Guid WidgetId,
        Guid TenantId,
        string DataJson,
        string? IpAddress,
        string? Country,
        string? City,
        DateTime CreatedAtUtc
        );
}
