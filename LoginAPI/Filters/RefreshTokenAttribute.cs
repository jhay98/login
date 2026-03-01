namespace LoginAPI.Filters;

/// <summary>
/// Marks endpoint responses that should include a refreshed access token.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RefreshTokenAttribute : Attribute
{
}
