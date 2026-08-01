using System.Text.Json;
using WidgetPlatform.Models;
using WidgetPlatform.Repositories;
using WidgetPlatform.Services.Geo;
using WidgetPlatform.Services.Notifications;

namespace WidgetPlatform.Services
{
    public sealed class SubmissionService
    {
        private const int MaxValueLength = 500;

        private readonly ISubmissionRepository _submissions;
        private readonly IWidgetRepository _widgets;
        private readonly GeoEnricher _geo;
        private readonly ISubmissionNotificationQueue _notifications;

        public SubmissionService(ISubmissionRepository submissions, IWidgetRepository widgets, GeoEnricher geo, ISubmissionNotificationQueue notifications
)
        {
            _submissions = submissions;
            _widgets = widgets;
            _geo = geo;
            _notifications = notifications;
        }

        public async Task<Submission?> CreateAsync(Guid widgetId, Dictionary<string, string>? data, string? ip, CancellationToken ct)
        {
            var widget = await _widgets.GetActiveByIdAsync(widgetId, ct);

            if (widget is null) return null;

            Validate(widget, data);

            var geo = await _geo.LookupAsync(ip, ct);

            var submission = new Submission(
             Guid.NewGuid(),
             widget.Id,
             widget.TenantId,
             JsonSerializer.Serialize(data),
             ip,
             geo?.Country,
             geo?.City,
             DateTime.UtcNow);

            var saved = await _submissions.AddAsync(submission, ct);

            _notifications.Enqueue(new SubmissionNotification(saved.Id, saved.WidgetId, saved.TenantId, saved.Country, saved.City, saved.CreatedAtUtc));

            return saved;
        }

        private static void Validate(Widget widget, Dictionary<string, string>? data)
        {
            if (data is null || data.Count == 0)
                throw new ArgumentException("Data is required.");

            var expected = widget.Fields.Select(f => f.Name).ToHashSet();

            foreach (var key in data.Keys)
                if (!expected.Contains(key))
                    throw new ArgumentException($"Unknown field: {key}");

            foreach (var field in widget.Fields)
            {
                if (!data.TryGetValue(field.Name, out var value) || string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException($"Field '{field.Name}' is required.");

                if (value.Length > MaxValueLength)
                    throw new ArgumentException($"Field '{field.Name}' is too long.");
            }
        }

        public Task<IReadOnlyList<Submission>> GetForTenantAsync(Guid tenantId, Guid? widgetId, int limit, int offset, CancellationToken ct)
        {
            limit = Math.Clamp(limit, 1, 100);
            offset = Math.Max(offset, 0);
            return _submissions.GetForTenantAsync(tenantId, widgetId, limit, offset, ct);
        }

        public Task<SubmissionStats> GetStatsAsync(Guid tenantId, CancellationToken ct)
            => _submissions.GetStatsAsync(tenantId, ct);
    }
}
