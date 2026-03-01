using AuthAPI.Interfaces;
using AuthAPI.Models;

namespace AuthAPI.Extensions;

/// <summary>
/// Provides endpoint registration for AuthAPI minimal endpoints.
/// </summary>
public static class AuthApiEndpointExtensions
{
    /// <summary>
    /// Maps all AuthAPI routes to the endpoint builder.
    /// </summary>
    /// <param name="endpoints">Endpoint route builder instance.</param>
    /// <returns>The same route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapAuthApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", HandleHealth);

        endpoints.MapPost(
            "/api/register",
            HandleRegisterAsync);

        endpoints.MapPost(
            "/api/login",
            HandleLoginAsync);

        endpoints.MapGet(
                "/api/me",
                HandleMeAsync)
            .RequireAuthorization();

        endpoints.MapGet(
                "/api/users",
                HandleUsersAsync)
            .RequireAuthorization("AdminOnly");

        endpoints.MapPost(
                "/api/activity",
                HandleCreateActivityAsync)
            .RequireAuthorization();

        endpoints.MapGet(
                "/api/activity/{count:int}",
                HandleRecentActivityAsync)
            .RequireAuthorization();

        return endpoints;
    }

    /// <summary>
    /// Returns a basic liveness response.
    /// </summary>
    /// <returns>Object containing service status.</returns>
    private static IResult HandleHealth()
    {
        return Results.Ok(new { status = "ok" });
    }

    /// <summary>
    /// Proxies user registration request to LoginAPI.
    /// </summary>
    /// <param name="httpContext">Current request context.</param>
    /// <param name="proxyService">Downstream proxy service.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Registration response from downstream API.</returns>
    private static Task<IResult> HandleRegisterAsync(
        HttpContext httpContext,
        IDownstreamProxyService proxyService,
        CancellationToken cancellationToken)
    {
        return proxyService.ProxyRegisterAndRecordActivityAsync(httpContext, "/api/auth/register", cancellationToken);
    }

    /// <summary>
    /// Proxies login request to LoginAPI and enriches successful response with JWT.
    /// </summary>
    /// <param name="httpContext">Current request context.</param>
    /// <param name="proxyService">Downstream proxy service.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Login response envelope.</returns>
    private static Task<IResult> HandleLoginAsync(
        HttpContext httpContext,
        IDownstreamProxyService proxyService,
        CancellationToken cancellationToken)
    {
        return proxyService.ProxyLoginWithTokenAsync(httpContext, "/api/auth/login", cancellationToken);
    }

    /// <summary>
    /// Proxies current-user lookup and returns refreshed token when successful.
    /// </summary>
    /// <param name="httpContext">Current request context.</param>
    /// <param name="proxyService">Downstream proxy service.</param>
    /// <param name="jwtTokenService">JWT service used for user id claim parsing.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Current-user response envelope or unauthorized result.</returns>
    private static Task<IResult> HandleMeAsync(
        HttpContext httpContext,
        IDownstreamProxyService proxyService,
        IJwtTokenService jwtTokenService,
        CancellationToken cancellationToken)
    {
        if (!jwtTokenService.TryGetUserId(httpContext.User, out var userId))
        {
            return Task.FromResult(Results.Unauthorized() as IResult);
        }

        return proxyService.ProxyWithRefreshAsync(httpContext, $"/api/auth/me/{userId}", cancellationToken);
    }

    /// <summary>
    /// Proxies admin user list request to LoginAPI.
    /// </summary>
    /// <param name="httpContext">Current request context.</param>
    /// <param name="proxyService">Downstream proxy service.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>User list response from downstream API.</returns>
    private static Task<IResult> HandleUsersAsync(
        HttpContext httpContext,
        IDownstreamProxyService proxyService,
        CancellationToken cancellationToken)
    {
        return proxyService.ProxyWithRefreshAsync(httpContext, "/api/auth/users/internal", cancellationToken);
    }

    /// <summary>
    /// Proxies activity creation requests to ActivityAPI.
    /// </summary>
    /// <param name="httpContext">Current request context.</param>
    /// <param name="proxyService">Downstream proxy service.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Created activity response from downstream API.</returns>
    private static Task<IResult> HandleCreateActivityAsync(
        HttpContext httpContext,
        IDownstreamProxyService proxyService,
        CancellationToken cancellationToken)
    {
        return proxyService.ProxyWithRefreshAsync(httpContext, "/api/activity", cancellationToken, DownstreamApiTarget.Activity);
    }

    /// <summary>
    /// Proxies recent activity lookup requests to ActivityAPI.
    /// </summary>
    /// <param name="count">Maximum number of records to return.</param>
    /// <param name="httpContext">Current request context.</param>
    /// <param name="proxyService">Downstream proxy service.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Recent activity list from downstream API.</returns>
    private static Task<IResult> HandleRecentActivityAsync(
        int count,
        HttpContext httpContext,
        IDownstreamProxyService proxyService,
        CancellationToken cancellationToken)
    {
        return proxyService.ProxyWithRefreshAsync(httpContext, $"/api/activity/{count}", cancellationToken, DownstreamApiTarget.Activity);
    }
}
