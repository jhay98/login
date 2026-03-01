namespace LoginAPI.Models.DTOs;

/// <summary>
/// Response payload returned after successful login.
/// </summary>
public class LoginResponseDto
{
    /// <summary>
    /// Gets or sets the JWT access token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authenticated user details.
    /// </summary>
    public UserDto User { get; set; } = new();
}
