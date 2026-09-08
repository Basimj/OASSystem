using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OAS.Tests.Features.Employees.API;

public sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(
        options,
        logger,
        encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(
                "X-Test-Admin",
                out var value) ||
            !string.Equals(
                value.ToString(),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                "8F4A57C8-40A6-45B7-8C7B-C2264FA07999"),

            new Claim(
                ClaimTypes.Name,
                "test-admin"),

            new Claim(
                ClaimTypes.Role,
                "Administrator")
        };

        var identity = new ClaimsIdentity(
            claims,
            "Test");

        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(
            principal,
            "Test");

        return Task.FromResult(
            AuthenticateResult.Success(ticket));
    }
}