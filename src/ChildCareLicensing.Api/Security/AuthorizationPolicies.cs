using System.Security.Claims;
using ChildCareLicensing.Domain.Enums;

namespace ChildCareLicensing.Api.Security;

public static class AuthorizationPolicies
{
    /// <summary>An operator acting on their own centres.</summary>
    public const string Operator = "Operator";

    /// <summary>Ministry staff who decide applications.</summary>
    public const string Reviewer = "Reviewer";

    /// <summary>Any ministry account, whether reviewer or inspector.</summary>
    public const string Ministry = "Ministry";
}

public static class AppClaimTypes
{
    public const string OperatorId = "operator_id";
}

public static class ClaimsPrincipalExtensions
{
    public static bool IsInRole(this ClaimsPrincipal principal, UserRole role)
        => principal.IsInRole(role.ToString());

    public static string DisplayName(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.FindFirstValue(ClaimTypes.Email)
            ?? "Signed in";

    public static string SignInName(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue(ClaimTypes.Name)
            ?? "unknown";

    /// <summary>The operator an account belongs to, or null for ministry accounts.</summary>
    public static Guid? OperatorId(this ClaimsPrincipal principal)
        => Guid.TryParse(principal.FindFirstValue(AppClaimTypes.OperatorId), out var id) ? id : null;
}
