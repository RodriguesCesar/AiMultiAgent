using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AiMultiAgent.API.Security;

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string HeaderName = "X-API-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var configuredApiKey = configuration["Security:ApiKey"] ?? Environment.GetEnvironmentVariable("AI_API_KEY");

        if (string.IsNullOrWhiteSpace(configuredApiKey))
            return Task.FromResult(AuthenticateResult.Fail("ApiKey não configurada."));

        if (!Request.Headers.TryGetValue(HeaderName, out var receivedApiKey))
            return Task.FromResult(AuthenticateResult.Fail($"Header {HeaderName} ausente."));

        if (!string.Equals(receivedApiKey, configuredApiKey, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.Fail("ApiKey inválida."));

        var claims = new[] { new Claim(ClaimTypes.Name, "api-client") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
