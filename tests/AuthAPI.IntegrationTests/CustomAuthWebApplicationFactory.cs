using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AuthAPI.IntegrationTests;

public class CustomAuthWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly Dictionary<string, string?> _originalEnvironmentValues = new();
    public StubDownstreamHandler DownstreamHandler { get; } = new();

    public CustomAuthWebApplicationFactory()
    {
        SetEnvironmentVariable("JwtSettings__SecretKey", "authapi-integration-secret-key-with-at-least-32-characters");
        SetEnvironmentVariable("JwtSettings__Issuer", "IntegrationTests");
        SetEnvironmentVariable("JwtSettings__Audience", "IntegrationTestsClient");
        SetEnvironmentVariable("JwtSettings__ExpirationMinutes", "60");

        SetEnvironmentVariable("DownstreamApi__BaseUrl", "http://downstream.local");
        SetEnvironmentVariable("DownstreamApi__InternalApiKey", "integration-internal-key");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.AddSingleton(DownstreamHandler);
            services.AddHttpClient("LoginApiProxy")
                .ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<StubDownstreamHandler>());
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            foreach (var keyValuePair in _originalEnvironmentValues)
            {
                Environment.SetEnvironmentVariable(keyValuePair.Key, keyValuePair.Value);
            }
        }
    }

    private void SetEnvironmentVariable(string key, string value)
    {
        _originalEnvironmentValues[key] = Environment.GetEnvironmentVariable(key);
        Environment.SetEnvironmentVariable(key, value);
    }
}

public class StubDownstreamHandler : HttpMessageHandler
{
    private int _requestCount;

    public int RequestCount => _requestCount;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _requestCount);

        if (!request.Headers.TryGetValues("X-Internal-Api-Key", out var values)
            || values.FirstOrDefault() != "integration-internal-key")
        {
            return Json(HttpStatusCode.Unauthorized, "{\"message\":\"Missing internal API key\"}");
        }

        if (request.Headers.Authorization is not null)
        {
            return Json(HttpStatusCode.BadRequest, "{\"message\":\"Authorization header should not be forwarded\"}");
        }

        var path = request.RequestUri?.AbsolutePath ?? string.Empty;

        if (request.Method == HttpMethod.Post && path == "/api/auth/register")
        {
            return Json(HttpStatusCode.Created, "{\"id\":15,\"email\":\"new.user@example.com\",\"firstName\":\"New\",\"lastName\":\"User\",\"createdAt\":\"2026-03-01T00:00:00Z\"}");
        }

        if (request.Method == HttpMethod.Post && path == "/api/auth/login")
        {
            return Json(HttpStatusCode.OK, "{\"user\":{\"id\":7,\"email\":\"user@example.com\",\"firstName\":\"Test\",\"lastName\":\"User\",\"createdAt\":\"2026-03-01T00:00:00Z\"},\"roles\":[\"User\"]}");
        }

        if (request.Method == HttpMethod.Get && path == "/api/auth/me/7")
        {
            return Json(HttpStatusCode.OK, "{\"id\":7,\"email\":\"user@example.com\",\"firstName\":\"Test\",\"lastName\":\"User\",\"createdAt\":\"2026-03-01T00:00:00Z\"}");
        }

        if (request.Method == HttpMethod.Get && path == "/api/auth/users/internal")
        {
            return Json(HttpStatusCode.OK, "[{\"id\":7,\"email\":\"user@example.com\",\"firstName\":\"Test\",\"lastName\":\"User\",\"createdAt\":\"2026-03-01T00:00:00Z\"}]");
        }

        return Json(HttpStatusCode.NotFound, "{\"message\":\"Not found\"}");
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string body)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }
}
