namespace AuthAPI.Models;

/// <summary>
/// Response envelope containing an access token and authenticated user payload.
/// </summary>
/// <typeparam name="T">Type of user payload.</typeparam>
public class LoginResponseEnvelope<T>
{
    /// <summary>
    /// Gets or sets the signed JWT access token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authenticated user payload.
    /// </summary>
    public T? User { get; set; }
}
