using System.Security.Claims;
using System.Text.Encodings.Web;
using ChildCareLicensing.Api.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ChildCareLicensing.IntegrationTests;

/// <summary>
/// Lets a test present a role and operator without going through a sign-in form. The real
/// authorization policies still decide what that identity is allowed to do.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string RoleHeader = "X-Test-Role";
    public const string OperatorHeader = "X-Test-Operator";
    public const string UserHeader = "X-Test-User";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RoleHeader, out var role) || string.IsNullOrEmpty(role))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var email = Request.Headers.TryGetValue(UserHeader, out var user) && !string.IsNullOrEmpty(user)
            ? user.ToString()
            : "test@example.com";

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role.ToString())
        };

        if (Request.Headers.TryGetValue(OperatorHeader, out var operatorId) && !string.IsNullOrEmpty(operatorId))
        {
            claims.Add(new Claim(AppClaimTypes.OperatorId, operatorId.ToString()));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));

        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(principal, SchemeName)));
    }
}
