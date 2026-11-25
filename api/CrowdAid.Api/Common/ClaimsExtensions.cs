using System.Security.Claims;

namespace CrowdAid.Api.Common;

public static class ClaimsExtensions
{
    /// <summary>The signed-in user's id from the "sub" claim. For /me/* routes, always load data from this.</summary>
    public static Guid UserId(this ClaimsPrincipal user)
        => Guid.Parse(user.FindFirstValue("sub") ?? throw new UnauthorizedAccessException());

    public static string DisplayName(this ClaimsPrincipal user)
        => user.FindFirstValue("name") ?? "Admin";
}
