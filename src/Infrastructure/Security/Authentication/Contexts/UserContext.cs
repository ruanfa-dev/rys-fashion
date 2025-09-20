using System.Security.Claims;

using Microsoft.AspNetCore.Http;

using UseCases.Common.Security.Authentication.Contexts;

namespace Infrastructure.Security.Authentication.Contexts;

public sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private readonly ClaimsPrincipal? _user = httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var userIdClaim = _user?.FindFirst("sub") ?? _user?.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId) ? userId : null;
        }
    }

    public string? UserName
    {
        get
        {
            // Try Keycloak preferred_username first, fallback to standard claims
            return _user?.FindFirst("preferred_username")?.Value ??
                   _user?.FindFirst(ClaimTypes.Name)?.Value ??
                   _user?.FindFirst("name")?.Value;
        }
    }

    public string? Email
    {
        get
        {
            return _user?.FindFirst("email")?.Value ??
                   _user?.FindFirst(ClaimTypes.Email)?.Value;
        }
    }

    public bool IsAuthenticated => _user?.Identity?.IsAuthenticated == true;

    public IEnumerable<string> Roles
    {
        get
        {
            // Get roles from Keycloak realm_access or resource_access claims
            var roles = new List<string>();

            // Standard role claims
            roles.AddRange(_user?.FindAll("roles")?.Select(c => c.Value) ?? []);
            roles.AddRange(_user?.FindAll(ClaimTypes.Role)?.Select(c => c.Value) ?? []);

            // Keycloak realm_access roles
            var realmAccessClaim = _user?.FindFirst("realm_access");
            if (realmAccessClaim?.Value != null)
            {
                try
                {
                    var realmAccess = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(realmAccessClaim.Value);
                    if (realmAccess?.TryGetValue("roles", out var realmRoles) == true && realmRoles is System.Text.Json.JsonElement rolesElement)
                    {
                        if (rolesElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            roles.AddRange(rolesElement.EnumerateArray().Select(r => r.GetString()).Where(r => !string.IsNullOrEmpty(r))!);
                        }
                    }
                }
                catch
                {
                    // Ignore JSON parsing errors
                }
            }

            return roles.Distinct();
        }
    }

    public IEnumerable<string> Permissions
    {
        get
        {
            return _user?.FindAll("permissions")?.Select(c => c.Value) ?? [];
        }
    }

    public bool HasRole(string role)
    {
        return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    public bool HasPermission(string permission)
    {
        return Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    public bool HasAnyRole(params string[] roles)
    {
        var userRoles = Roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return roles.Any(role => userRoles.Contains(role));
    }

    public bool HasAllRoles(params string[] roles)
    {
        var userRoles = Roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return roles.All(role => userRoles.Contains(role));
    }

    public string? GetClaimValue(string claimType)
    {
        return _user?.FindFirst(claimType)?.Value;
    }

    public IEnumerable<string> GetClaimValues(string claimType)
    {
        return _user?.FindAll(claimType)?.Select(c => c.Value) ?? [];
    }
}
