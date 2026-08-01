using Microsoft.Extensions.Logging.Abstractions;
using WidgetPlatform.Services.Geo;

namespace WidgetPlatform.Tests;

public class GeoEnricherTests
{
    private sealed class DownProvider : IGeoProvider
    {
        public string Name => "down";
        public Task<GeoLocation?> LookupAsync(string ip, CancellationToken ct)
            => throw new HttpRequestException("provider is down");
    }

    private sealed class UpProvider : IGeoProvider
    {
        public string Name => "up";
        public Task<GeoLocation?> LookupAsync(string ip, CancellationToken ct)
            => Task.FromResult<GeoLocation?>(new GeoLocation("Bosnia and Herzegovina", "Mostar"));
    }

    [Fact]
    public async Task Falls_back_to_the_second_provider_when_the_first_is_down()
    {
        var enricher = new GeoEnricher(
            new IGeoProvider[] { new DownProvider(), new UpProvider() },
            NullLogger<GeoEnricher>.Instance);

        var geo = await enricher.LookupAsync("1.2.3.4", CancellationToken.None);

        Assert.NotNull(geo);
        Assert.Equal("Mostar", geo!.City);
    }

    [Fact]
    public async Task Returns_null_when_every_provider_fails()
    {
        var enricher = new GeoEnricher(
            new IGeoProvider[] { new DownProvider(), new DownProvider() },
            NullLogger<GeoEnricher>.Instance);

        var geo = await enricher.LookupAsync("1.2.3.4", CancellationToken.None);

        Assert.Null(geo);            
    }
}