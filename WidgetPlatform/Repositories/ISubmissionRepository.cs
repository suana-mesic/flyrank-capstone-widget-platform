using WidgetPlatform.Models;

namespace WidgetPlatform.Repositories
{
    public interface ISubmissionRepository
    {
        Task<Submission> AddAsync(Submission submission, CancellationToken ct);
        Task<IReadOnlyList<Submission>> GetForTenantAsync(Guid tenantId, Guid? widgetId, int limit, int offset, CancellationToken ct);

        Task<SubmissionStats> GetStatsAsync(Guid tenantId, CancellationToken ct);
    }
}
