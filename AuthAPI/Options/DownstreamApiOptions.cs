namespace AuthAPI.Options;

/// <summary>
/// Configuration for proxying requests to the core API.
/// </summary>
public class DownstreamApiOptions
{
    /// <summary>
    /// Gets or sets the base URL of the core Login API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional shared internal API key used for service-to-service calls.
    /// </summary>
    public string InternalApiKey { get; set; } = string.Empty;
}
