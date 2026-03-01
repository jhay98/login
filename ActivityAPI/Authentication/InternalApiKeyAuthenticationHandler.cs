using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ActivityAPI.Authentication;

/// <summary>
/// Authenticates trusted internal callers using the X-Internal-Api-Key header.
/// </summary>
public sealed class InternalApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string HeaderName = "X-Internal-Api-Key";

    public InternalApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var configuration = Context.RequestServices.GetRequiredService<IConfiguration>();
        var configuredKey = configuration["InternalApi:Key"];

        // Key validation is mandatory. Missing configuration is a server misconfiguration.
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Internal API key is not configured"));
        }

        if (!Request.Headers.TryGetValue(HeaderName, out var providedValues))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing internal API key"));
        }

        var providedKey = providedValues.ToString();
        var expectedBytes = Encoding.UTF8.GetBytes(configuredKey);
        var actualBytes = Encoding.UTF8.GetBytes(providedKey);

        if (!CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid internal API key"));
        }

        return Task.FromResult(Success());
    }

    private AuthenticateResult Success()
    {
        var claims = new[] { new Claim(ClaimTypes.Name, "internal-service") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
