namespace WidgetPlatform.Services.Geo;

public sealed class PrimaryGeoProvider : IGeoProvider
{
    private readonly GeoProviderSwitch _switch;

    public PrimaryGeoProvider(GeoProviderSwitch @switch) => _switch = @switch;

    public string Name => "geo-primary";

    public Task<GeoLocation?> LookupAsync(string ip, CancellationToken ct)
    {
        if (!_switch.PrimaryIsUp)
            throw new HttpRequestException("geo-primary is down");

        return Task.FromResult<GeoLocation?>(
            new GeoLocation("Bosnia and Herzegovina", "Sarajevo"));
    }
}