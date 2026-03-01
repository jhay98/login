namespace LoginAPI.Data.Entities;

/// <summary>
/// Represents assignment of a role to a user.
/// </summary>
public class UserRole
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the related user.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the role identifier.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Gets or sets the related role.
    /// </summary>
    public Role Role { get; set; } = null!;

    /// <summary>
    /// Gets or sets when the role was assigned (UTC).
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
