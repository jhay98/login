using System.Text;
using System.Text.Json;
using AuthAPI.Interfaces;
using AuthAPI.Models;

namespace AuthAPI.Services;

/// <summary>
/// Handles forwarding AuthAPI requests to downstream APIs and shaping successful responses.
/// </summary>
public sealed class DownstreamProxyService : IDownstreamProxyService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IJwtTokenService _jwtTokenService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DownstreamProxyService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory used to create downstream HTTP clients.</param>
    /// <param name="configuration">Application configuration source.</param>
    /// <param name="jwtTokenService">Service used to generate JWT envelopes.</param>
    public DownstreamProxyService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IJwtTokenService jwtTokenService)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _jwtTokenService = jwtTokenService;
    }

    /// <inheritdoc />
    public async Task<IResult> ProxyAsync(HttpContext httpContext, string downstreamPath, CancellationToken cancellationToken, DownstreamApiTarget target = DownstreamApiTarget.Login)
    {
        var downstream = await ProxyRawAsync(httpContext, downstreamPath, cancellationToken, target);

        if (downstream.BodyBytes.Length == 0)
        {
            return Results.StatusCode(downstream.StatusCode);
        }

        var body = Encoding.UTF8.GetString(downstream.BodyBytes);
        return Results.Content(body, downstream.ContentType ?? "application/json", Encoding.UTF8, downstream.StatusCode);
    }

    /// <inheritdoc />
    public async Task<IResult> ProxyWithRefreshAsync(HttpContext httpContext, string downstreamPath, CancellationToken cancellationToken, DownstreamApiTarget target = DownstreamApiTarget.Login)
    {
        var downstream = await ProxyRawAsync(httpContext, downstreamPath, cancellationToken, target);

        if (downstream.StatusCode >= 400)
        {
            if (downstream.BodyBytes.Length == 0)
            {
                return Results.StatusCode(downstream.StatusCode);
            }

            var errorBody = Encoding.UTF8.GetString(downstream.BodyBytes);
            return Results.Content(errorBody, downstream.ContentType ?? "application/json", Encoding.UTF8, downstream.StatusCode);
        }

        if (downstream.BodyBytes.Length == 0)
        {
            return Results.StatusCode(downstream.StatusCode);
        }

        var refreshedToken = _jwtTokenService.GenerateTokenFromPrincipal(httpContext.User);

        try
        {
            using var jsonDocument = JsonDocument.Parse(downstream.BodyBytes);
            var payload = new RefreshTokenResponseEnvelope<JsonElement>
            {
                Token = refreshedToken,
                Data = jsonDocument.RootElement.Clone()
            };

            return Results.Json(payload, statusCode: downstream.StatusCode);
        }
        catch (JsonException)
        {
            var body = Encoding.UTF8.GetString(downstream.BodyBytes);
            return Results.Content(body, downstream.ContentType ?? "application/json", Encoding.UTF8, downstream.StatusCode);
        }
    }

    /// <inheritdoc />
    public async Task<IResult> ProxyLoginWithTokenAsync(HttpContext httpContext, string downstreamPath, CancellationToken cancellationToken, DownstreamApiTarget target = DownstreamApiTarget.Login)
    {
        var downstream = await ProxyRawAsync(httpContext, downstreamPath, cancellationToken, target);

        if (downstream.StatusCode >= 400)
        {
            if (downstream.BodyBytes.Length == 0)
            {
                return Results.StatusCode(downstream.StatusCode);
            }

            var errorBody = Encoding.UTF8.GetString(downstream.BodyBytes);
            return Results.Content(errorBody, downstream.ContentType ?? "application/json", Encoding.UTF8, downstream.StatusCode);
        }

        if (downstream.BodyBytes.Length == 0)
        {
            return Results.StatusCode(downstream.StatusCode);
        }

        try
        {
            using var jsonDocument = JsonDocument.Parse(downstream.BodyBytes);

            if (!TryReadDownstreamLoginPayload(jsonDocument.RootElement, out var userElement, out var roles))
            {
                return Results.StatusCode(StatusCodes.Status502BadGateway);
            }

            var token = _jwtTokenService.GenerateTokenFromLoginPayload(userElement, roles);
            var payload = new LoginResponseEnvelope<JsonElement>
            {
                Token = token,
                User = userElement.Clone()
            };

            return Results.Json(payload, statusCode: downstream.StatusCode);
        }
        catch (JsonException)
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }
    }

    /// <inheritdoc />
    public async Task<IResult> ProxyRegisterAndRecordActivityAsync(HttpContext httpContext, string downstreamPath, CancellationToken cancellationToken)
    {
        var downstream = await ProxyRawAsync(httpContext, downstreamPath, cancellationToken, DownstreamApiTarget.Login);

        if (downstream.StatusCode < 400)
        {
            await TryRecordAccountCreatedActivityAsync(httpContext, downstream.BodyBytes, cancellationToken);
        }

        if (downstream.BodyBytes.Length == 0)
        {
            return Results.StatusCode(downstream.StatusCode);
        }

        var body = Encoding.UTF8.GetString(downstream.BodyBytes);
        return Results.Content(body, downstream.ContentType ?? "application/json", Encoding.UTF8, downstream.StatusCode);
    }

    /// <summary>
    /// Forwards an HTTP request to the selected downstream API and captures the response body.
    /// </summary>
    /// <param name="httpContext">Current HTTP request context.</param>
    /// <param name="downstreamPath">Relative downstream route path.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Captured downstream response details.</returns>
    private async Task<DownstreamResponse> ProxyRawAsync(
        HttpContext httpContext,
        string downstreamPath,
        CancellationToken cancellationToken,
        DownstreamApiTarget target = DownstreamApiTarget.Login)
    {
        var baseUrl = GetBaseUrl(target);

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return new DownstreamResponse(
                StatusCodes.Status500InternalServerError,
                "application/json",
                Encoding.UTF8.GetBytes("{\"message\":\"Downstream API base URL is not configured.\"}"));
        }

        var targetUri = new Uri($"{baseUrl.TrimEnd('/')}{downstreamPath}{httpContext.Request.QueryString}");

        using var requestMessage = new HttpRequestMessage(new HttpMethod(httpContext.Request.Method), targetUri);

        if (httpContext.Request.ContentLength is > 0 || httpContext.Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            requestMessage.Content = new StreamContent(httpContext.Request.Body);

            if (!string.IsNullOrWhiteSpace(httpContext.Request.ContentType))
            {
                requestMessage.Content.Headers.TryAddWithoutValidation("Content-Type", httpContext.Request.ContentType);
            }
        }

        foreach (var header in httpContext.Request.Headers)
        {
            if (string.Equals(header.Key, "Host", StringComparison.OrdinalIgnoreCase)
                || string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase)
                || string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray())
                && requestMessage.Content != null)
            {
                requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        var internalApiKey = _configuration["DownstreamApi:InternalApiKey"];
        if (!string.IsNullOrWhiteSpace(internalApiKey))
        {
            requestMessage.Headers.Remove("X-Internal-Api-Key");
            requestMessage.Headers.Add("X-Internal-Api-Key", internalApiKey);
        }

        var httpClient = _httpClientFactory.CreateClient("LoginApiProxy");
        using var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        var bodyBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = response.Content.Headers.ContentType?.ToString();

        return new DownstreamResponse((int)response.StatusCode, contentType, bodyBytes);
    }

    /// <summary>
    /// Attempts to record an account-created event in ActivityAPI.
    /// </summary>
    /// <param name="httpContext">Current request context.</param>
    /// <param name="registerResponseBody">Raw registration response payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task TryRecordAccountCreatedActivityAsync(HttpContext httpContext, byte[] registerResponseBody, CancellationToken cancellationToken)
    {
        if (!TryReadUserId(registerResponseBody, out var userId) || userId <= 0)
        {
            return;
        }

        var activityBaseUrl = GetBaseUrl(DownstreamApiTarget.Activity);
        if (string.IsNullOrWhiteSpace(activityBaseUrl))
        {
            return;
        }

        var payload = new
        {
            UserId = userId,
            EventType = "account_created",
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            Metadata = "source=auth-api"
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri($"{activityBaseUrl.TrimEnd('/')}/api/activity"))
        {
            Content = content
        };

        var internalApiKey = _configuration["DownstreamApi:InternalApiKey"];
        if (!string.IsNullOrWhiteSpace(internalApiKey))
        {
            requestMessage.Headers.Remove("X-Internal-Api-Key");
            requestMessage.Headers.Add("X-Internal-Api-Key", internalApiKey);
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient("LoginApiProxy");
            using var response = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            _ = response.StatusCode;
        }
        catch
        {
            // Do not fail registration if activity capture fails.
        }
    }

    /// <summary>
    /// Resolves downstream base URL for target API.
    /// </summary>
    /// <param name="target">Downstream API target.</param>
    /// <returns>Configured base URL.</returns>
    private string? GetBaseUrl(DownstreamApiTarget target)
    {
        return target switch
        {
            DownstreamApiTarget.Activity => _configuration["DownstreamApi:ActivityBaseUrl"],
            _ => _configuration["DownstreamApi:BaseUrl"]
        };
    }

    /// <summary>
    /// Attempts to extract a user identifier from a registration response payload.
    /// </summary>
    /// <param name="responseBody">Raw response body bytes.</param>
    /// <param name="userId">Resolved user id when present.</param>
    /// <returns><c>true</c> when a user id was found; otherwise <c>false</c>.</returns>
    private static bool TryReadUserId(byte[] responseBody, out int userId)
    {
        userId = 0;

        if (responseBody.Length == 0)
        {
            return false;
        }

        try
        {
            using var jsonDocument = JsonDocument.Parse(responseBody);
            if (jsonDocument.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var property in jsonDocument.RootElement.EnumerateObject())
            {
                if (!string.Equals(property.Name, "id", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out userId))
                {
                    return true;
                }

                return false;
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to extract user and role data from a downstream login JSON payload.
    /// </summary>
    /// <param name="root">Root JSON element from downstream response.</param>
    /// <param name="user">Resolved user JSON object when present.</param>
    /// <param name="roles">Resolved role names from payload.</param>
    /// <returns><c>true</c> when payload has the required structure; otherwise, <c>false</c>.</returns>
    private static bool TryReadDownstreamLoginPayload(JsonElement root, out JsonElement user, out IReadOnlyCollection<string> roles)
    {
        user = default;
        roles = Array.Empty<string>();

        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("user", out user) || user.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!root.TryGetProperty("roles", out var rolesElement) || rolesElement.ValueKind != JsonValueKind.Array)
        {
            return true;
        }

        var values = new List<string>();
        foreach (var roleElement in rolesElement.EnumerateArray())
        {
            if (roleElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var roleValue = roleElement.GetString();
            if (!string.IsNullOrWhiteSpace(roleValue))
            {
                values.Add(roleValue);
            }
        }

        roles = values;
        return true;
    }
}
