using LoginAPI.Models.DTOs;
using System.Security.Claims;

namespace LoginAPI.Interfaces;

/// <summary>
/// Defines authentication and user account operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="request">Registration payload containing user profile and password data.</param>
    /// <returns>The created user details.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the email is already registered.</exception>
    Task<UserDto> RegisterAsync(RegisterRequestDto request);

    /// <summary>
    /// Authenticates a user and returns a JWT plus user details.
    /// </summary>
    /// <param name="request">Login payload containing email and password.</param>
    /// <returns>The login response including token and user data.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when credentials are invalid.</exception>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);

    /// <summary>
    /// Retrieves a user by identifier.
    /// </summary>
    /// <param name="userId">The unique user identifier.</param>
    /// <returns>The matching user details.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the user does not exist.</exception>
    Task<UserDto> GetUserByIdAsync(int userId);

    /// <summary>
    /// Builds a new JWT token based on claims from an authenticated principal.
    /// </summary>
    /// <param name="principal">Authenticated user principal.</param>
    /// <returns>A signed JWT token string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required claims are missing.</exception>
    string GenerateTokenFromPrincipal(ClaimsPrincipal principal);

    /// <summary>
    /// Retrieves all users in the system.
    /// </summary>
    /// <returns>A list of users.</returns>
    Task<List<UserDto>> GetAllUsersAsync();
}
