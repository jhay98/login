using System.Net;
using System.Net.Http.Json;
using ActivityAPI.Models.DTOs;

namespace ActivityAPI.IntegrationTests;

public class ActivityEndpointsIntegrationTests : IClassFixture<CustomActivityWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomActivityWebApplicationFactory _factory;

    public ActivityEndpointsIntegrationTests(CustomActivityWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Internal-Api-Key", "integration-internal-key");
    }

    [Fact]
    public async Task CreateActivity_WithValidPayload_ReturnsCreatedRecord()
    {
        var response = await _client.PostAsJsonAsync("/api/activity", new CreateActivityRequestDto
        {
            UserId = 7,
            EventType = "  login_success  ",
            IpAddress = " 127.0.0.1 ",
            UserAgent = " test-agent ",
            Metadata = " k=v "
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ActivityDto>();
        Assert.NotNull(payload);
        Assert.Equal(7, payload!.UserId);
        Assert.Equal("login_success", payload.EventType);
        Assert.Equal("127.0.0.1", payload.IpAddress);
        Assert.Equal("test-agent", payload.UserAgent);
        Assert.Equal("k=v", payload.Metadata);
    }

    [Fact]
    public async Task CreateActivity_WithInvalidUserId_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/activity", new CreateActivityRequestDto
        {
            UserId = 0,
            EventType = "login_success"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetRecentActivity_WhenCountExceedsLimit_ReturnsAtMostTwoHundredItems()
    {
        for (var i = 0; i < 205; i++)
        {
            var createResponse = await _client.PostAsJsonAsync("/api/activity", new CreateActivityRequestDto
            {
                UserId = i + 1,
                EventType = $"event_{i}"
            });

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        }

        var response = await _client.GetAsync("/api/activity/500");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<List<ActivityDto>>();
        Assert.NotNull(payload);
        Assert.Equal(200, payload!.Count);
    }

    [Fact]
    public async Task GetRecentActivity_WithInvalidCount_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/activity/0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RequestsWithoutInternalApiKey_ReturnUnauthorized()
    {
        using var unauthorizedClient = _factory.CreateClient();

        var response = await unauthorizedClient.GetAsync("/api/activity/5");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
