using System.Net;

namespace AuthAPI.IntegrationTests;

public class HealthEndpointTests : IClassFixture<CustomAuthWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(CustomAuthWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
