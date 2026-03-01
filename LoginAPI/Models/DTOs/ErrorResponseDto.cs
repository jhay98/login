namespace LoginAPI.Models.DTOs;

/// <summary>
/// Standard API error response payload.
/// </summary>
public class ErrorResponseDto
{
    /// <summary>
    /// Gets or sets the primary error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional validation or detail errors.
    /// </summary>
    public List<string>? Errors { get; set; }
}
