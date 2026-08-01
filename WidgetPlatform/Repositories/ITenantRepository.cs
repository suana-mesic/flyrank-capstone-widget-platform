using WidgetPlatform.Models;

namespace WidgetPlatform.Repositories
{
    public interface ITenantRepository
    {
        Task<Tenant?> GetByEmailAsync(string email, CancellationToken ct);
        Task<Tenant> AddAsync(string email, string passwordHash, CancellationToken ct);

    }
}
