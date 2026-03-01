namespace AuthAPI.Models;

/// <summary>
/// Represents the downstream API target for proxy operations.
/// </summary>
public enum DownstreamApiTarget
{
    /// <summary>
    /// Core Login API.
    /// </summary>
    Login = 0,

    /// <summary>
    /// Activity history API.
    /// </summary>
    Activity = 1
}
