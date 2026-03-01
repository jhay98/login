namespace ActivityAPI.Models.DTOs;

/// <summary>
/// Request payload to create a new activity event.
/// </summary>
public class CreateActivityRequestDto
{
    /// <summary>
    /// Gets or sets user identifier associated with the event.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets event type.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional IP address.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets optional user agent.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets optional metadata.
    /// </summary>
    public string? Metadata { get; set; }
}
