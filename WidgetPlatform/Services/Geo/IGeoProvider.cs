namespace WidgetPlatform.Services.Geo
{
    public interface IGeoProvider
    {
        string Name { get; }
        Task<GeoLocation?> LookupAsync(string ip, CancellationToken ct);
    }
}
