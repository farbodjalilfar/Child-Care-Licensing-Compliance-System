using System.Security.Claims;
using ChildCareLicensing.Application.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace ChildCareLicensing.Api.Security;

/// <summary>
/// Sign-in and sign-out run as plain form posts rather than through a Blazor circuit,
/// because the response has to carry a Set-Cookie header.
/// </summary>
public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/account");

        group.MapPost("/login", async (
            HttpContext context,
            IUserAccountService accounts,
            [FromForm] string email,
            [FromForm] string password,
            [FromForm] string? returnUrl,
            CancellationToken cancellationToken) =>
        {
            var account = await accounts.ValidateCredentialsAsync(email, password, cancellationToken);

            if (account is null)
            {
                var retry = QueryString.Create("error", "invalid");
                if (!string.IsNullOrWhiteSpace(returnUrl))
                {
                    retry = retry.Add("returnUrl", returnUrl);
                }

                return Results.Redirect("/account/login" + retry);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new(ClaimTypes.Name, account.DisplayName),
                new(ClaimTypes.Email, account.Email),
                new(ClaimTypes.Role, account.Role.ToString())
            };

            if (account.OperatorId is { } operatorId)
            {
                claims.Add(new Claim(AppClaimTypes.OperatorId, operatorId.ToString()));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = false });

            return Results.Redirect(SafeReturnUrl(returnUrl));
        });

        group.MapPost("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/");
        });

        return endpoints;
    }

    /// <summary>Only same-site relative paths are honoured, to avoid an open redirect.</summary>
    private static string SafeReturnUrl(string? returnUrl)
        => !string.IsNullOrWhiteSpace(returnUrl)
            && returnUrl.StartsWith('/')
            && !returnUrl.StartsWith("//", StringComparison.Ordinal)
                ? returnUrl
                : "/";
}
