using WidgetPlatform.Models;

namespace WidgetPlatform.Repositories
{
    public interface IWidgetRepository
    {
        Task<IReadOnlyList<Widget>> GetAllForTenantAsync(Guid tenantId, CancellationToken ct);
        Task<Widget?> GetByIdForTenantAsync(Guid id, Guid tenantId, CancellationToken ct);
        Task<Widget> AddAsync(Widget widget, CancellationToken ct);
        Task<bool> UpdateAsync(Widget widget, CancellationToken ct);
        Task<bool> DeleteAsync(Guid id, Guid tenantId, CancellationToken ct);
        Task<Widget?> GetActiveByIdAsync(Guid id, CancellationToken ct);
    }
}
