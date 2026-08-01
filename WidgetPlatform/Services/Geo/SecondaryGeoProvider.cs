namespace WidgetPlatform.Services.Geo;

public sealed class SecondaryGeoProvider : IGeoProvider
{
    public string Name => "geo-secondary";

    public Task<GeoLocation?> LookupAsync(string ip, CancellationToken ct)
        => Task.FromResult<GeoLocation?>(
            new GeoLocation("Bosnia and Herzegovina", "Mostar"));
}