namespace LoginAPI.Models;

/// <summary>
/// Represents JWT token configuration settings.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Gets or sets the symmetric signing key.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JWT issuer.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JWT audience.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets token expiration in minutes.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;
}
