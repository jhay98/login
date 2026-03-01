namespace ActivityAPI.Models.DTOs;

/// <summary>
/// Standard error payload.
/// </summary>
public class ErrorResponseDto
{
    /// <summary>
    /// Gets or sets human-readable message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
