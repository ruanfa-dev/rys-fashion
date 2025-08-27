using System.Security.Claims;

namespace Infrastructure.Security.Authentication.Contexts;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var idValue = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(idValue, out var guid) ? guid : null;
    }

    public static string? GetUserName(this ClaimsPrincipal user)
    {
        return user?.FindFirst(ClaimTypes.Name)?.Value;
    }

    public static bool IsAuthenticated(this ClaimsPrincipal user)
    {
        return user?.Identity?.IsAuthenticated ?? false;
    }
}
