using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace WidgetPlatform.Tests;

public class CorsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CorsTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Preflight_from_any_origin_is_allowed_on_submissions()
    {
        var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Options, "/submissions");
        request.Headers.Add("Origin", "https://marko-pekara.ba");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await client.SendAsync(request);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("*", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Admin_endpoints_do_not_get_cors_headers()
    {
        var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Options, "/widgets");
        request.Headers.Add("Origin", "https://marko-pekara.ba");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}