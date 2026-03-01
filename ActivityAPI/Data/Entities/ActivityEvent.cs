namespace ActivityAPI.Data.Entities;

/// <summary>
/// Represents a single user activity event.
/// </summary>
public class ActivityEvent
{
    /// <summary>
    /// Gets or sets database identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets user identifier associated with the event.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets event type (for example, login_success).
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets IP address captured for the event.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets user agent captured for the event.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets optional metadata as text JSON.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets event creation time in UTC.
    /// </summary>
    public DateTimeOffset OccurredAtUtc { get; set; }
}
