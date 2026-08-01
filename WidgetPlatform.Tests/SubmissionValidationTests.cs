using Microsoft.Extensions.Logging.Abstractions;
using WidgetPlatform.Models;
using WidgetPlatform.Repositories;
using WidgetPlatform.Services;
using WidgetPlatform.Services.Geo;
using WidgetPlatform.Services.Notifications;
using Xunit;

namespace WidgetPlatform.Tests;

public class SubmissionValidationTests
{
    private static readonly Guid WidgetId = Guid.NewGuid();

    private static readonly Widget TestWidget = new(
        WidgetId, Guid.NewGuid(), "newsletter", "Prijavi se",
        new[] { new WidgetField("email", "Tvoj email") },
        true, 1, DateTime.UtcNow);

    private sealed class FakeWidgets : IWidgetRepository
    {
        public Task<Widget?> GetActiveByIdAsync(Guid id, CancellationToken ct)
            => Task.FromResult<Widget?>(id == WidgetId ? TestWidget : null);

        public Task<IReadOnlyList<Widget>> GetAllForTenantAsync(Guid t, CancellationToken ct)
            => throw new NotImplementedException();
        public Task<Widget?> GetByIdForTenantAsync(Guid id, Guid t, CancellationToken ct)
            => throw new NotImplementedException();
        public Task<Widget> AddAsync(Widget w, CancellationToken ct)
            => throw new NotImplementedException();
        public Task<bool> UpdateAsync(Widget w, CancellationToken ct)
            => throw new NotImplementedException();
        public Task<bool> DeleteAsync(Guid id, Guid t, CancellationToken ct)
            => throw new NotImplementedException();
    }

    private sealed class FakeSubmissions : ISubmissionRepository
    {
        public List<Submission> Saved { get; } = new();

        public Task<Submission> AddAsync(Submission s, CancellationToken ct)
        {
            Saved.Add(s);
            return Task.FromResult(s);
        }

        public Task<IReadOnlyList<Submission>> GetForTenantAsync(
            Guid t, Guid? w, int limit, int offset, CancellationToken ct)
            => throw new NotImplementedException();
        public Task<SubmissionStats> GetStatsAsync(Guid t, CancellationToken ct)
            => throw new NotImplementedException();
    }

    private sealed class FakeQueue : ISubmissionNotificationQueue
    {
        public void Enqueue(SubmissionNotification n) { }
        public IAsyncEnumerable<SubmissionNotification> DequeueAllAsync(CancellationToken ct)
            => throw new NotImplementedException();
    }

    private static (SubmissionService service, FakeSubmissions repo) Build()
    {
        var repo = new FakeSubmissions();

        var enricher = new GeoEnricher(
            Array.Empty<IGeoProvider>(), NullLogger<GeoEnricher>.Instance);

        return (new SubmissionService(repo, new FakeWidgets(), enricher, new FakeQueue()), repo);
    }

    [Fact]
    public async Task Rejects_an_unknown_field()
    {
        var (service, _) = Build();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(WidgetId,
                new Dictionary<string, string> { ["admin"] = "true" },
                "1.2.3.4", CancellationToken.None));
    }

    [Fact]
    public async Task Rejects_an_oversized_value()
    {
        var (service, _) = Build();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(WidgetId,
                new Dictionary<string, string> { ["email"] = new string('x', 501) },
                "1.2.3.4", CancellationToken.None));
    }

    [Fact]
    public async Task Rejects_an_empty_payload()
    {
        var (service, _) = Build();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(WidgetId,
                new Dictionary<string, string>(),
                "1.2.3.4", CancellationToken.None));
    }

    [Fact]
    public async Task Returns_null_for_an_unknown_widget()
    {
        var (service, _) = Build();

        var result = await service.CreateAsync(Guid.NewGuid(),
            new Dictionary<string, string> { ["email"] = "ana@x.ba" },
            "1.2.3.4", CancellationToken.None);

        Assert.Null(result);            // -> ruta vraća 404
    }

    [Fact]
    public async Task Accepts_a_valid_payload()
    {
        var (service, repo) = Build();

        var result = await service.CreateAsync(WidgetId,
            new Dictionary<string, string> { ["email"] = "ana@x.ba" },
            "1.2.3.4", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(repo.Saved);
        Assert.Equal(TestWidget.TenantId, repo.Saved[0].TenantId);   // tenant iz widgeta
    }
}