using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AuthAPI.Interfaces;
using AuthAPI.Models;
using AuthAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthAPI.UnitTests;

public class DownstreamProxyServiceTests
{
    [Fact]
    public async Task ProxyWithRefreshAsync_OnSuccess_ReturnsWrappedPayloadWithRefreshedToken()
    {
        var handler = new RecordingHttpMessageHandler((request, _) =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"id":7,"email":"user@example.com"}
                    """, Encoding.UTF8, "application/json")
            };
        });

        var service = CreateService(
            handler,
            new StubJwtTokenService { TokenFromPrincipal = "refreshed-token" },
            new Dictionary<string, string?>
            {
                ["DownstreamApi:BaseUrl"] = "http://login.local",
                ["DownstreamApi:InternalApiKey"] = "internal-key"
            });

        var httpContext = CreateHttpContext("GET");
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("userId", "7")], "test"));

        var result = await service.ProxyWithRefreshAsync(httpContext, "/api/auth/me/7", CancellationToken.None);
        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        using var payload = JsonDocument.Parse(response.Body);
        Assert.Equal("refreshed-token", payload.RootElement.GetProperty("token").GetString());
        Assert.Equal(7, payload.RootElement.GetProperty("data").GetProperty("id").GetInt32());

        var forwarded = Assert.Single(handler.RecordedRequests);
        Assert.Equal("/api/auth/me/7", forwarded.Path);
        Assert.True(forwarded.Headers.ContainsKey("X-Internal-Api-Key"));
        Assert.False(forwarded.Headers.ContainsKey("Authorization"));
    }

    [Fact]
    public async Task ProxyLoginWithTokenAsync_WhenDownstreamPayloadInvalid_ReturnsBadGateway()
    {
        var handler = new RecordingHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        var service = CreateService(
            handler,
            new StubJwtTokenService { TokenFromLoginPayload = "login-token" },
            new Dictionary<string, string?>
            {
                ["DownstreamApi:BaseUrl"] = "http://login.local"
            });

        var result = await service.ProxyLoginWithTokenAsync(CreateHttpContext("POST"), "/api/auth/login", CancellationToken.None);
        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status502BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task ProxyAsync_WhenBaseUrlMissing_ReturnsInternalServerErrorWithMessage()
    {
        var handler = new RecordingHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(handler, new StubJwtTokenService(), new Dictionary<string, string?>());

        var result = await service.ProxyAsync(CreateHttpContext("GET"), "/api/auth/users/internal", CancellationToken.None);
        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status500InternalServerError, response.StatusCode);
        Assert.Contains("not configured", response.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProxyRegisterAndRecordActivityAsync_OnSuccessfulRegister_SendsActivityRequest()
    {
        var handler = new RecordingHttpMessageHandler((request, _) =>
        {
            if (request.RequestUri?.AbsolutePath == "/api/auth/register")
            {
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("""
                        {"id":99,"email":"new.user@example.com"}
                        """, Encoding.UTF8, "application/json")
                };
            }

            if (request.RequestUri?.AbsolutePath == "/api/activity")
            {
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var service = CreateService(
            handler,
            new StubJwtTokenService(),
            new Dictionary<string, string?>
            {
                ["DownstreamApi:BaseUrl"] = "http://login.local",
                ["DownstreamApi:ActivityBaseUrl"] = "http://activity.local",
                ["DownstreamApi:InternalApiKey"] = "internal-key"
            });

        var httpContext = CreateHttpContext("POST", """
            {"email":"new.user@example.com"}
            """);
        var result = await service.ProxyRegisterAndRecordActivityAsync(httpContext, "/api/auth/register", CancellationToken.None);
        var response = await ExecuteResultAsync(result);

        Assert.Equal(StatusCodes.Status201Created, response.StatusCode);
        Assert.Equal(2, handler.RecordedRequests.Count);
        Assert.Equal("/api/auth/register", handler.RecordedRequests[0].Path);
        Assert.Equal("/api/activity", handler.RecordedRequests[1].Path);
    }

    private static DownstreamProxyService CreateService(
        RecordingHttpMessageHandler handler,
        IJwtTokenService jwtTokenService,
        IDictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var client = new HttpClient(handler);
        var clientFactory = new StubHttpClientFactory(client);

        return new DownstreamProxyService(clientFactory, configuration, jwtTokenService);
    }

    private static DefaultHttpContext CreateHttpContext(string method, string? body = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;

        if (body is not null)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            context.Request.Body = new MemoryStream(bytes);
            context.Request.ContentLength = bytes.Length;
            context.Request.ContentType = "application/json";
        }

        context.Request.Headers.Authorization = "Bearer upstream-token";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<(int StatusCode, string Body)> ExecuteResultAsync(IResult result)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();

        var context = new DefaultHttpContext();
        context.RequestServices = services.BuildServiceProvider();
        context.Response.Body = new MemoryStream();

        await result.ExecuteAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        return (context.Response.StatusCode, body);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public StubHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name)
        {
            return _client;
        }
    }

    private sealed class StubJwtTokenService : IJwtTokenService
    {
        public string TokenFromPrincipal { get; set; } = "token-from-principal";
        public string TokenFromLoginPayload { get; set; } = "token-from-login";

        public string GenerateTokenFromPrincipal(ClaimsPrincipal principal)
        {
            return TokenFromPrincipal;
        }

        public string GenerateTokenFromLoginPayload(JsonElement userElement, IReadOnlyCollection<string> roles)
        {
            return TokenFromLoginPayload;
        }

        public bool TryGetUserId(ClaimsPrincipal principal, out int userId)
        {
            userId = 0;
            return false;
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, int, HttpResponseMessage> _responder;
        private int _count;

        public List<RecordedRequest> RecordedRequests { get; } = [];

        public RecordingHttpMessageHandler(Func<HttpRequestMessage, int, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var currentCount = Interlocked.Increment(ref _count);
            var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);

            RecordedRequests.Add(new RecordedRequest(
                request.Method.Method,
                request.RequestUri?.AbsolutePath ?? string.Empty,
                headers));

            return Task.FromResult(_responder(request, currentCount));
        }
    }

    private sealed record RecordedRequest(string Method, string Path, Dictionary<string, string> Headers);
}
