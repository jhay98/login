using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AuthAPI.Interfaces;
using AuthAPI.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthAPI.Services;

/// <summary>
/// Provides JWT generation from principals and downstream payloads.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
    /// </summary>
    /// <param name="jwtOptions">Configured JWT settings.</param>
    public JwtTokenService(IOptions<JwtSettings> jwtOptions)
    {
        _jwtSettings = jwtOptions.Value;
    }

    /// <inheritdoc />
    public string GenerateTokenFromPrincipal(ClaimsPrincipal principal)
    {
        var userId = principal.FindFirst("userId")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? principal.FindFirst(ClaimTypes.Email)?.Value;

        var firstName = principal.FindFirst(JwtRegisteredClaimNames.GivenName)?.Value
            ?? principal.FindFirst(ClaimTypes.GivenName)?.Value
            ?? string.Empty;

        var lastName = principal.FindFirst(JwtRegisteredClaimNames.FamilyName)?.Value
            ?? principal.FindFirst(ClaimTypes.Surname)?.Value
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Required identity claims are missing.");
        }

        var roles = principal.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return GenerateJwtToken(userId, email, firstName, lastName, roles);
    }

    /// <inheritdoc />
    public string GenerateTokenFromLoginPayload(JsonElement userElement, IReadOnlyCollection<string> roles)
    {
        if (!TryGetRequiredStringProperty(userElement, "email", out var email)
            || !TryGetRequiredStringProperty(userElement, "firstName", out var firstName)
            || !TryGetRequiredStringProperty(userElement, "lastName", out var lastName)
            || !TryGetRequiredStringProperty(userElement, "id", out var userId))
        {
            throw new InvalidOperationException("Required user fields are missing in downstream login payload.");
        }

        return GenerateJwtToken(userId, email, firstName, lastName, roles);
    }

    /// <inheritdoc />
    public bool TryGetUserId(ClaimsPrincipal principal, out int userId)
    {
        var userIdClaim = principal.FindFirst("userId")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return int.TryParse(userIdClaim, out userId);
    }

    /// <summary>
    /// Builds and signs a JWT for a user identity.
    /// </summary>
    /// <param name="userId">User identifier claim value.</param>
    /// <param name="email">User email claim value.</param>
    /// <param name="firstName">Given name claim value.</param>
    /// <param name="lastName">Family name claim value.</param>
    /// <param name="roles">Role claims to include in the token.</param>
    /// <returns>Signed JWT token string.</returns>
    private string GenerateJwtToken(
        string userId,
        string email,
        string firstName,
        string lastName,
        IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.GivenName, firstName),
            new(JwtRegisteredClaimNames.FamilyName, lastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("userId", userId)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var expirationMinutes = _jwtSettings.ExpirationMinutes <= 0 ? 2 : _jwtSettings.ExpirationMinutes;

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Attempts to read a required string-like property from a JSON object.
    /// </summary>
    /// <param name="element">Source JSON object.</param>
    /// <param name="propertyName">Property name to read.</param>
    /// <param name="value">Parsed property value.</param>
    /// <returns><c>true</c> when the value exists and is non-empty; otherwise, <c>false</c>.</returns>
    private static bool TryGetRequiredStringProperty(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;

        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            var stringValue = property.GetString();
            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                value = stringValue;
                return true;
            }

            return false;
        }

        if (property.ValueKind == JsonValueKind.Number)
        {
            value = property.ToString();
            return !string.IsNullOrWhiteSpace(value);
        }

        return false;
    }
}
