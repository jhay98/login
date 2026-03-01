namespace AuthAPI.Models;

/// <summary>
/// Represents a downstream HTTP response snapshot.
/// </summary>
/// <param name="StatusCode">HTTP status code returned by downstream service.</param>
/// <param name="ContentType">Response content type.</param>
/// <param name="BodyBytes">Raw response body bytes.</param>
public sealed record DownstreamResponse(int StatusCode, string? ContentType, byte[] BodyBytes);
