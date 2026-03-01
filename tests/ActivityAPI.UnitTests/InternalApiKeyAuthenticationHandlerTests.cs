using System.Text.Encodings.Web;
using ActivityAPI.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ActivityAPI.UnitTests;

public class InternalApiKeyAuthenticationHandlerTests
{
    [Fact]
    public async Task AuthenticateAsync_WhenConfiguredKeyMissing_ReturnsFailure()
    {
        var result = await AuthenticateAsync(configuredKey: null, providedKey: "test-key");

        Assert.False(result.Succeeded);
        Assert.Equal("Internal API key is not configured", result.Failure?.Message);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenHeaderMissing_ReturnsFailure()
    {
        var result = await AuthenticateAsync(configuredKey: "expected-key", providedKey: null);

        Assert.False(result.Succeeded);
        Assert.Equal("Missing internal API key", result.Failure?.Message);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenKeyDoesNotMatch_ReturnsFailure()
    {
        var result = await AuthenticateAsync(configuredKey: "expected-key", providedKey: "wrong-key");

        Assert.False(result.Succeeded);
        Assert.Equal("Invalid internal API key", result.Failure?.Message);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenKeyMatches_ReturnsAuthenticatedPrincipal()
    {
        var result = await AuthenticateAsync(configuredKey: "expected-key", providedKey: "expected-key");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.Equal("internal-service", result.Principal!.Identity?.Name);
        Assert.Equal("InternalApiKey", result.Ticket?.AuthenticationScheme);
    }

    private static async Task<AuthenticateResult> AuthenticateAsync(string? configuredKey, string? providedKey)
    {
        var settings = new Dictionary<string, string?>();
        if (configuredKey is not null)
        {
            settings["InternalApi:Key"] = configuredKey;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(configuration)
            .BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = services
        };

        if (providedKey is not null)
        {
            context.Request.Headers["X-Internal-Api-Key"] = providedKey;
        }

        var handler = new InternalApiKeyAuthenticationHandler(
            new StaticOptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions()),
            LoggerFactory.Create(_ => { }),
            UrlEncoder.Default);

        await handler.InitializeAsync(new AuthenticationScheme(
            "InternalApiKey",
            displayName: null,
            typeof(InternalApiKeyAuthenticationHandler)), context);

        return await handler.AuthenticateAsync();
    }

    private sealed class StaticOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
    {
        private readonly TOptions _currentValue;

        public StaticOptionsMonitor(TOptions currentValue)
        {
            _currentValue = currentValue;
        }

        public TOptions CurrentValue => _currentValue;

        public TOptions Get(string? name)
        {
            return _currentValue;
        }

        public IDisposable OnChange(Action<TOptions, string?> listener)
        {
            return NullDisposable.Instance;
        }

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
