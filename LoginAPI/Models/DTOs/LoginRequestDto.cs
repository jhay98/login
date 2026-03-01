using System.ComponentModel.DataAnnotations;

namespace LoginAPI.Models.DTOs;

/// <summary>
/// Request payload for user login.
/// </summary>
public class LoginRequestDto
{
    /// <summary>
    /// Gets or sets the user email address.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the plaintext password.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}
