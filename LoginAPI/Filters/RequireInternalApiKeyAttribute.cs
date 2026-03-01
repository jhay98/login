using System.Security.Cryptography;
using System.Text;
using LoginAPI.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LoginAPI.Filters;

/// <summary>
/// Restricts endpoint access to trusted internal callers when an internal API key is configured.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireInternalApiKeyAttribute : Attribute, IAsyncAuthorizationFilter
{
    private const string HeaderName = "X-Internal-Api-Key";

    /// <inheritdoc />
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var configuredKey = configuration["InternalApi:Key"];

        // Key validation is opt-in. If no key is configured, allow access.
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            return Task.CompletedTask;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedValues))
        {
            context.Result = new UnauthorizedObjectResult(new ErrorResponseDto { Message = "Missing internal API key" });
            return Task.CompletedTask;
        }

        var providedKey = providedValues.ToString();
        if (!ConstantTimeEquals(configuredKey, providedKey))
        {
            context.Result = new UnauthorizedObjectResult(new ErrorResponseDto { Message = "Invalid internal API key" });
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Compares two strings using constant-time semantics.
    /// </summary>
    /// <param name="expected">Expected key.</param>
    /// <param name="actual">Provided key.</param>
    /// <returns><see langword="true"/> if values are identical; otherwise <see langword="false"/>.</returns>
    private static bool ConstantTimeEquals(string expected, string actual)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
