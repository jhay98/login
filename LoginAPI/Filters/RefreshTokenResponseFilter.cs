using LoginAPI.Interfaces;
using LoginAPI.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LoginAPI.Filters;

/// <summary>
/// Wraps annotated successful object responses with a refreshed access token payload.
/// </summary>
public class RefreshTokenResponseFilter : IAsyncResultFilter
{
    private readonly IAuthService _authService;
    private readonly ILogger<RefreshTokenResponseFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenResponseFilter"/> class.
    /// </summary>
    /// <param name="authService">Authentication service.</param>
    /// <param name="logger">Logger instance.</param>
    public RefreshTokenResponseFilter(IAuthService authService, ILogger<RefreshTokenResponseFilter> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        var shouldRefresh = endpoint?.Metadata.GetMetadata<RefreshTokenAttribute>() != null;

        if (!shouldRefresh)
        {
            await next();
            return;
        }

        if (context.Result is not ObjectResult objectResult)
        {
            await next();
            return;
        }

        if (objectResult.StatusCode is >= 400)
        {
            await next();
            return;
        }

        try
        {
            var token = _authService.GenerateTokenFromPrincipal(context.HttpContext.User);
            objectResult.Value = new RefreshTokenResponseDto<object?>
            {
                Token = token,
                Data = objectResult.Value
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Unable to refresh token for endpoint {Endpoint}", endpoint?.DisplayName);
            context.Result = new ObjectResult(new ErrorResponseDto { Message = "Unable to refresh access token" })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
        }

        await next();
    }
}
