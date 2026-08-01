using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace WidgetPlatform.Tests;

public class RateLimitTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RateLimitTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Returns_429_once_the_limit_is_exceeded()
    {
        var client = _factory.CreateClient();
        var codes = new List<int>();

        for (var i = 0; i < 7; i++)
        {
            var response = await client.PostAsJsonAsync("/submissions", new
            {
                widgetId = Guid.NewGuid(),
                data = new Dictionary<string, string> { ["email"] = "bot@x.z" },
                website = "http://spam.com"
            });

            codes.Add((int)response.StatusCode);
        }

        Assert.Equal(5, codes.Count(c => c == 201));
        Assert.Equal(2, codes.Count(c => c == 429));
    }
}