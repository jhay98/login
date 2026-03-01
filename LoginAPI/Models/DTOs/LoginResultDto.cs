namespace LoginAPI.Models.DTOs;

/// <summary>
/// Response payload returned after successful credential validation.
/// </summary>
public class LoginResultDto
{
    /// <summary>
    /// Gets or sets the authenticated user details.
    /// </summary>
    public UserDto User { get; set; } = new();

    /// <summary>
    /// Gets or sets the normalized role names assigned to the user.
    /// </summary>
    public List<string> Roles { get; set; } = [];
}
