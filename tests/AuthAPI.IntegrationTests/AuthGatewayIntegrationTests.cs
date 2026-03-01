using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AuthAPI.IntegrationTests;

public class AuthGatewayIntegrationTests : IClassFixture<CustomAuthWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthGatewayIntegrationTests(CustomAuthWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsWrappedPayloadWithRefreshedToken()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(userId: 7, role: "User"));

        var response = await _client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<RefreshEnvelope<UserDto>>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Token));
        Assert.NotNull(payload.Data);
        Assert.Equal(7, payload.Data!.Id);
        Assert.Equal("user@example.com", payload.Data.Email);
    }

    [Fact]
    public async Task GetUsers_WithNonAdminToken_ReturnsForbidden()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(userId: 7, role: "User"));

        var response = await _client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_ProxiesRequestToDownstream()
    {
        var response = await _client.PostAsJsonAsync("/api/register", new
        {
            Email = "new.user@example.com",
            Password = "Passw0rd!",
            FirstName = "New",
            LastName = "User"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(payload);
        Assert.Equal("new.user@example.com", payload!.Email);
    }

    [Fact]
    public async Task Login_ReturnsAuthApiIssuedTokenAndUserPayload()
    {
        var response = await _client.PostAsJsonAsync("/api/login", new
        {
            Email = "user@example.com",
            Password = "Passw0rd!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<LoginEnvelope<UserDto>>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Token));
        Assert.NotNull(payload.User);
        Assert.Equal(7, payload.User!.Id);
        Assert.Equal("user@example.com", payload.User.Email);
    }

    private static string CreateJwt(int userId, string role)
    {
        const string issuer = "IntegrationTests";
        const string audience = "IntegrationTestsClient";
        const string secret = "authapi-integration-secret-key-with-at-least-32-characters";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new("userId", userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, "user@example.com"),
            new(ClaimTypes.Email, "user@example.com"),
            new(JwtRegisteredClaimNames.GivenName, "Test"),
            new(JwtRegisteredClaimNames.FamilyName, "User"),
            new(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class RefreshEnvelope<T>
    {
        public string Token { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    private sealed class LoginEnvelope<T>
    {
        public string Token { get; set; } = string.Empty;
        public T? User { get; set; }
    }

    private sealed class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
