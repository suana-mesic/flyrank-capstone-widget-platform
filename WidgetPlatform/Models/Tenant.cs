namespace WidgetPlatform.Models
{
    public sealed record Tenant(
        Guid Id,
        string Email,
        string PasswordHash,
        DateTime CreatedAtUtc);
}
