namespace AuthAPI.Models;

/// <summary>
/// JWT configuration settings used to validate access tokens.
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Gets or sets token issuer.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets token audience.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the signing secret key.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets token expiration in minutes.
    /// </summary>
    public int ExpirationMinutes { get; set; }
}
