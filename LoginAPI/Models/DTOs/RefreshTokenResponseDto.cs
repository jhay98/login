namespace LoginAPI.Models.DTOs;

/// <summary>
/// Legacy response envelope retained for compatibility.
/// </summary>
/// <typeparam name="T">Type of wrapped response data.</typeparam>
public class RefreshTokenResponseDto<T>
{
    /// <summary>
    /// Gets or sets wrapped endpoint response data.
    /// </summary>
    public T? Data { get; set; }
}
