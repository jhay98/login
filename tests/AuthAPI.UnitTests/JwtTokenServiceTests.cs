using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using AuthAPI.Models;
using AuthAPI.Services;
using Microsoft.Extensions.Options;

namespace AuthAPI.UnitTests;

public class JwtTokenServiceTests
{
    [Fact]
    public void GenerateTokenFromPrincipal_WithValidClaims_EmbedsDistinctRoles()
    {
        var service = CreateService();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("userId", "42"),
            new Claim(ClaimTypes.Email, "user@example.com"),
            new Claim(ClaimTypes.GivenName, "Test"),
            new Claim(ClaimTypes.Surname, "User"),
            new Claim(ClaimTypes.Role, "User"),
            new Claim(ClaimTypes.Role, "user"),
            new Claim(ClaimTypes.Role, "Admin")
        ], "test"));

        var token = service.GenerateTokenFromPrincipal(principal);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("42", jwt.Claims.First(c => c.Type == "sub").Value);
        Assert.Equal("user@example.com", jwt.Claims.First(c => c.Type == "email").Value);

        var roles = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Equal(2, roles.Count);
        Assert.Contains("User", roles);
        Assert.Contains("Admin", roles);
    }

    [Fact]
    public void GenerateTokenFromPrincipal_WhenRequiredClaimsMissing_ThrowsInvalidOperationException()
    {
        var service = CreateService();
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.Throws<InvalidOperationException>(() => service.GenerateTokenFromPrincipal(principal));
    }

    [Fact]
    public void GenerateTokenFromLoginPayload_WhenPayloadMissingFields_ThrowsInvalidOperationException()
    {
        var service = CreateService();
        using var document = JsonDocument.Parse("""
            {
              "id": 7,
              "email": "user@example.com",
              "firstName": "Test"
            }
            """);

        Assert.Throws<InvalidOperationException>(() =>
            service.GenerateTokenFromLoginPayload(document.RootElement, ["User"]));
    }

    [Fact]
    public void TryGetUserId_ResolvesFromNameIdentifierClaim()
    {
        var service = CreateService();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "123")
        ], "test"));

        var success = service.TryGetUserId(principal, out var userId);

        Assert.True(success);
        Assert.Equal(123, userId);
    }

    [Fact]
    public void TryGetUserId_WhenClaimIsInvalid_ReturnsFalse()
    {
        var service = CreateService();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("userId", "not-a-number")
        ], "test"));

        var success = service.TryGetUserId(principal, out var userId);

        Assert.False(success);
        Assert.Equal(0, userId);
    }

    private static JwtTokenService CreateService()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new JwtSettings
        {
            Issuer = "Tests",
            Audience = "TestsClient",
            SecretKey = "authapi-unit-test-secret-key-with-at-least-32-characters",
            ExpirationMinutes = 60
        });

        return new JwtTokenService(options);
    }
}
