using System.Net;
using System.Net.Http.Json;
using LoginAPI.Models.DTOs;

namespace LoginAPI.IntegrationTests;

public class AuthEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Internal-Api-Key", "integration-internal-key");
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

        var payload = await loginResponse.Content.ReadFromJsonAsync<LoginResultDto>();
        Assert.NotNull(payload);
        Assert.Equal(request.Email.ToLowerInvariant(), payload!.User.Email);
        Assert.Contains("User", payload.Roles);
    }

    [Fact]
    public async Task GetMeByUserId_WithValidApiKey_ReturnsCurrentUser()
    {
        var request = CreateRegisterRequest();
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", request);
        var registeredUser = await registerResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(registeredUser);

        var meResponse = await _client.GetAsync($"/api/auth/me/{registeredUser!.Id}");

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var mePayload = await meResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(mePayload);
        Assert.Equal(request.Email.ToLowerInvariant(), mePayload!.Email);
    }

    [Fact]
    public async Task GetAllUsersInternal_WithValidApiKey_ReturnsUsers()
    {
        var request = CreateRegisterRequest();
        await _client.PostAsJsonAsync("/api/auth/register", request);

        var usersResponse = await _client.GetAsync("/api/auth/users/internal");

        Assert.Equal(HttpStatusCode.OK, usersResponse.StatusCode);
        var users = await usersResponse.Content.ReadFromJsonAsync<List<UserDto>>();
        Assert.NotNull(users);
        Assert.NotEmpty(users!);
    }

    [Fact]
    public async Task RequestsWithoutInternalApiKey_ReturnUnauthorized()
    {
        using var unauthorizedClient = _factory.CreateClient();

        var response = await unauthorizedClient.GetAsync("/api/auth/users/internal");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ReturnsBadRequest()
    {
        var request = CreateRegisterRequest();

        var firstResponse = await _client.PostAsJsonAsync("/api/auth/register", request);
        var duplicateResponse = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownUser_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "unknown@example.com",
            Password = "Passw0rd!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMeByUserId_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/auth/me/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidPayload_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
        {
            Email = "not-an-email",
            Password = "short",
            FirstName = string.Empty,
            LastName = string.Empty
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidPayload_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
        {
            Email = "",
            Password = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
