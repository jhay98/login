using AuthAPI.Models;

namespace AuthAPI.Interfaces;

/// <summary>
/// Proxies requests from AuthAPI to LoginAPI and shapes response payloads.
/// </summary>
public interface IDownstreamProxyService
{
    /// <summary>
    /// Proxies an incoming request to the downstream API and returns the raw downstream payload.
    /// </summary>
    /// <param name="httpContext">Current HTTP request context.</param>
    /// <param name="downstreamPath">Relative downstream route path.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>HTTP result with the downstream status code and payload.</returns>
    Task<IResult> ProxyAsync(HttpContext httpContext, string downstreamPath, CancellationToken cancellationToken, DownstreamApiTarget target = DownstreamApiTarget.Login);

    /// <summary>
    /// Proxies an authenticated request and returns response data together with a refreshed JWT.
    /// </summary>
    /// <param name="httpContext">Current HTTP request context.</param>
    /// <param name="downstreamPath">Relative downstream route path.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>HTTP result containing a refreshed token envelope when successful.</returns>
    Task<IResult> ProxyWithRefreshAsync(HttpContext httpContext, string downstreamPath, CancellationToken cancellationToken, DownstreamApiTarget target = DownstreamApiTarget.Login);

    /// <summary>
    /// Proxies login requests and augments successful responses with a signed JWT.
    /// </summary>
    /// <param name="httpContext">Current HTTP request context.</param>
    /// <param name="downstreamPath">Relative downstream route path.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>HTTP result containing login data and JWT when successful.</returns>
    Task<IResult> ProxyLoginWithTokenAsync(HttpContext httpContext, string downstreamPath, CancellationToken cancellationToken, DownstreamApiTarget target = DownstreamApiTarget.Login);

    /// <summary>
    /// Proxies registration requests and records account-creation activity when successful.
    /// </summary>
    /// <param name="httpContext">Current HTTP request context.</param>
    /// <param name="downstreamPath">Relative downstream route path.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>HTTP result with downstream registration response payload.</returns>
    Task<IResult> ProxyRegisterAndRecordActivityAsync(HttpContext httpContext, string downstreamPath, CancellationToken cancellationToken);
}
