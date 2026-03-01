namespace ActivityAPI.Models.DTOs;

/// <summary>
/// Response payload for activity history records.
/// </summary>
public class ActivityDto
{
    /// <summary>
    /// Gets or sets record id.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets associated user id.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets activity type.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets recorded IP address.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets recorded user agent.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets optional metadata.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets occurrence timestamp in UTC.
    /// </summary>
    public DateTimeOffset OccurredAtUtc { get; set; }
}
