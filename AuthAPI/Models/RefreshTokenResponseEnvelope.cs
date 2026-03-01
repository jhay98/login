namespace AuthAPI.Models;

/// <summary>
/// Response envelope containing a refreshed token and the original downstream response payload.
/// </summary>
/// <typeparam name="T">Type of wrapped response payload.</typeparam>
public class RefreshTokenResponseEnvelope<T>
{
    /// <summary>
    /// Gets or sets the refreshed JWT access token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the downstream response payload.
    /// </summary>
    public T? Data { get; set; }
}
