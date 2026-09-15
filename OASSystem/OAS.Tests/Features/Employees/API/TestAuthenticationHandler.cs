using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OAS.API.Security;
using OAS.Tests.Features.Employees.Integration;

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
    private static readonly Guid TestAdminId =
        Guid.Parse("8F4A57C8-40A6-45B7-8C7B-C2264FA07999");

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(
                "X-Test-Admin",
                out var value) ||
            !string.Equals(
                value.ToString(),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var passwordHash = await PrepareTestAdministratorAsync();

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return AuthenticateResult.Fail(
                "The seeded test administrator could not be found.");
        }

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                TestAdminId.ToString()),

            new Claim(
                ClaimTypes.Name,
                "admin@gmail.com"),

            new Claim(
                ClaimTypes.Email,
                "admin@gmail.com"),

            new Claim(
                ClaimTypes.Role,
                "Administrator"),

            new Claim(
                IdentityClaimTypes.IsSuperAdmin,
                "true"),

            new Claim(
                IdentityClaimTypes.MustChangePassword,
                "false"),

            new Claim(
                IdentityClaimTypes.PasswordVersion,
                IdentityPasswordVersion.Create(passwordHash))
        };

        var identity = new ClaimsIdentity(
            claims,
            "Test");

        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(
            principal,
            "Test");

        return AuthenticateResult.Success(ticket);
    }

    private static async Task<string?> PrepareTestAdministratorAsync()
    {
        await using var connection =
            new SqlConnection(TestDatabase.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                """
                UPDATE [security].[Users]
                SET
                    [IsActive] = 1,
                    [IsSuperAdmin] = 1,
                    [MustChangePassword] = 0
                WHERE [Id] = @UserId;

                SELECT [PasswordHash]
                FROM [security].[Users]
                WHERE [Id] = @UserId;
                """,
                connection);

        command.Parameters.AddWithValue(
            "@UserId",
            TestAdminId);

        var result = await command.ExecuteScalarAsync();

        return result as string;
    }
}