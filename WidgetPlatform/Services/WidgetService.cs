using WidgetPlatform.Models;
using WidgetPlatform.Repositories;

namespace WidgetPlatform.Services
{
    public class WidgetService
    {
        private static readonly string[] AllowType = { "newsletter", "cta", "popover" };

        private readonly IWidgetRepository _widgets;

        public WidgetService(IWidgetRepository widgets) => _widgets = widgets;

        public Task<IReadOnlyList<Widget>> GetAllAsync(Guid tenantId, CancellationToken ct)
         => _widgets.GetAllForTenantAsync(tenantId, ct);

        public Task<Widget?> GetAsync(Guid id, Guid tenantId, CancellationToken ct)
            => _widgets.GetByIdForTenantAsync(id, tenantId, ct);

        public async Task<Widget> CreateAsync(Guid tenantId, string type, string title, IReadOnlyList<WidgetField> fields, CancellationToken ct)
        {
            Validate(type, title, fields);

            var widget = new Widget(
               Guid.NewGuid(), tenantId, type, title.Trim(), fields, true, 1, DateTime.UtcNow);

            return await _widgets.AddAsync(widget, ct);
        }

        public async Task<bool> UpdateAsync(Guid id, Guid tenantId, string type, string title, IReadOnlyList<WidgetField> fields, bool isActive, CancellationToken ct)
        {
            Validate(type, title, fields);

            var widget = new Widget(id, tenantId, type, title.Trim(), fields, isActive, 0, default);
            return await _widgets.UpdateAsync(widget, ct);
        }

        public Task<bool> DeleteAsync(Guid id, Guid tenantId, CancellationToken ct)
             => _widgets.DeleteAsync(id, tenantId, ct);

        public Task<Widget?> GetPublicAsync(Guid id, CancellationToken ct)
        => _widgets.GetActiveByIdAsync(id, ct);

        private static void Validate(string type, string title, IReadOnlyList<WidgetField> fields)
        {
            if (!AllowType.Contains(type))
                throw new ArgumentException($"Type must be one of: {string.Join(", ", AllowType)}");

            if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
                throw new ArgumentException("Title is required and must be at most 200 characters.");

            if (fields is null || fields.Count == 0 || fields.Count > 10)
                throw new ArgumentException("Between 1 and 10 fields are required.");

            if (fields.Any(f => string.IsNullOrWhiteSpace(f.Name) || string.IsNullOrWhiteSpace(f.Label)))
                throw new ArgumentException("Every field needs a name and a label.");
        }

    }
}
