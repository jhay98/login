using System.Security.Claims;
using System.Text.Json;

namespace AuthAPI.Interfaces;

/// <summary>
/// Provides JWT token generation and identity claim utilities.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT from the current authenticated principal.
    /// </summary>
    /// <param name="principal">Authenticated principal containing user claims.</param>
    /// <returns>Signed JWT token string.</returns>
    string GenerateTokenFromPrincipal(ClaimsPrincipal principal);

    /// <summary>
    /// Generates a JWT from downstream login payload data.
    /// </summary>
    /// <param name="userElement">JSON user object from downstream API response.</param>
    /// <param name="roles">Role names associated with the user.</param>
    /// <returns>Signed JWT token string.</returns>
    string GenerateTokenFromLoginPayload(JsonElement userElement, IReadOnlyCollection<string> roles);

    /// <summary>
    /// Attempts to read the user identifier from a principal.
    /// </summary>
    /// <param name="principal">Principal whose claims should be read.</param>
    /// <param name="userId">Resolved user identifier when available and valid.</param>
    /// <returns><c>true</c> when a valid user identifier is present; otherwise, <c>false</c>.</returns>
    bool TryGetUserId(ClaimsPrincipal principal, out int userId);
}
