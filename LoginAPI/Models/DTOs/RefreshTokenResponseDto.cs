namespace LoginAPI.Models.DTOs;

/// <summary>
/// Response envelope for endpoints that return refreshed access token alongside data.
/// </summary>
/// <typeparam name="T">Type of wrapped response data.</typeparam>
public class RefreshTokenResponseDto<T>
{
    /// <summary>
    /// Gets or sets the newly issued JWT access token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets wrapped endpoint response data.
    /// </summary>
    public T? Data { get; set; }
}
