namespace WidgetPlatform.Models
{
    public sealed record Widget(
        Guid Id,
        Guid TenantId,
        string Type,
        string Title,
        IReadOnlyList<WidgetField> Fields,
        bool IsActive,
        int Version,
        DateTime CreatedAtUtc);
}
