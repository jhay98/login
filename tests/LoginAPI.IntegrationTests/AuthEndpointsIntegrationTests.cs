using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoginAPI.Models.DTOs;

namespace LoginAPI.IntegrationTests;

public class AuthEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ReturnsCreatedUser()
    {
        var request = CreateRegisterRequest();

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(payload);
        Assert.Equal(request.Email.ToLowerInvariant(), payload!.Email);
        Assert.Equal(request.FirstName, payload.FirstName);
        Assert.Equal(request.LastName, payload.LastName);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var request = CreateRegisterRequest();
        await _client.PostAsJsonAsync("/api/auth/register", request);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = request.Email,
            Password = request.Password
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var payload = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Token));
        Assert.Equal(request.Email.ToLowerInvariant(), payload.User.Email);
    }

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsCurrentUser()
    {
        var request = CreateRegisterRequest();
        await _client.PostAsJsonAsync("/api/auth/register", request);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = request.Email,
            Password = request.Password
        });

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginPayload);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload!.Token);
        var meResponse = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var mePayload = await meResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(mePayload);
        Assert.Equal(request.Email.ToLowerInvariant(), mePayload!.Email);
    }

    [Fact]
    public async Task GetAllUsers_WithNonAdminUser_ReturnsForbidden()
    {
        var request = CreateRegisterRequest();
        await _client.PostAsJsonAsync("/api/auth/register", request);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = request.Email,
            Password = request.Password
        });

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(loginPayload);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload!.Token);
        var usersResponse = await _client.GetAsync("/api/auth/users");

        Assert.Equal(HttpStatusCode.Forbidden, usersResponse.StatusCode);
    }

    private static RegisterRequestDto CreateRegisterRequest()
    {
        var unique = Guid.NewGuid().ToString("N")[..8];

        return new RegisterRequestDto
        {
            Email = $"integration-{unique}@example.com",
            Password = "Passw0rd!",
            FirstName = "Integration",
            LastName = "Test"
        };
    }
}
