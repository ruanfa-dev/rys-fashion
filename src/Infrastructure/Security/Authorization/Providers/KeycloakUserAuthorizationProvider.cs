using System.Text.Json;

using Infrastructure.Security.Authorization.Options;
using Infrastructure.Security.Authorization.Seeders;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

using Serilog;

using UseCases.Common.Security.Authentication.Contexts;
using UseCases.Common.Security.Authorization.Providers;
using UseCases.Common.Security.Authentication.Services;
using UseCases.Common.Security.Authorization.Templates;

namespace Infrastructure.Security.Authorization.Providers;

/// <summary>
/// Keycloak-based user authorization provider that retrieves user permissions from Keycloak
/// using resource-scope based authorization model
/// </summary>
public sealed class KeycloakUserAuthorizationProvider(
    IKeycloakAdminService keycloakAdminService,
    IDistributedCache cache,
    IOptions<AuthUserCacheOption> authCacheOption)
    : IUserAuthorizationProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AuthUserCacheOption _cacheOptions = authCacheOption.Value;

    public async Task<UserAuthorizationData?> GetUserAuthorizationAsync(Guid userId)
    {
        var cacheKey = $"KeycloakUserAuth_{userId}";

        // Try to get from cache first
        if (await TryGetCachedAuthAsync(cacheKey) is { } cached)
            return cached;

        return await FetchAndCacheAuthDataFromKeycloak(userId, cacheKey);
    }

    private async ValueTask<UserAuthorizationData?> TryGetCachedAuthAsync(string cacheKey)
    {
        try
        {
            var cachedData = await cache.GetStringAsync(cacheKey).ConfigureAwait(false);
            return string.IsNullOrEmpty(cachedData)
                ? null
                : JsonSerializer.Deserialize<UserAuthorizationData>(cachedData, JsonOptions);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Cache retrieval failed for {Key}", cacheKey);
            _ = SafeCacheRemoveAsync(cacheKey); // fire-and-forget cleanup
            return null;
        }
    }

    private async Task<UserAuthorizationData?> FetchAndCacheAuthDataFromKeycloak(Guid userId, string cacheKey)
    {
        try
        {
            // Get user from Keycloak
            var keycloakUser = await keycloakAdminService.GetUserByIdAsync(userId.ToString());
            if (keycloakUser == null)
            {
                Log.Warning("User not found in Keycloak: {UserId}", userId);
                return null;
            }

            // Get user roles from Keycloak
            var userRoles = await keycloakAdminService.GetUserRolesAsync(userId.ToString());
            var roleNames = userRoles.Select(r => r.Name).ToList();

            // Get permissions based on role templates and resource-scope mappings
            var permissions = GetPermissionsFromRoles(roleNames);

            // For policies, we can derive them from roles or use custom logic
            var policies = GetPoliciesFromRoles(roleNames);

            var authData = new UserAuthorizationData(
                UserId: userId,
                UserName: keycloakUser.Username,
                Email: keycloakUser.Email,
                Permissions: permissions.ToList().AsReadOnly(),
                Roles: roleNames.AsReadOnly(),
                Policies: policies.ToList().AsReadOnly()
            );

            await CacheAuthData(cacheKey, authData);
            return authData;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch user authorization data from Keycloak for user {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Gets permissions from user roles using the role permission templates
    /// </summary>
    private static IEnumerable<string> GetPermissionsFromRoles(IList<string> roleNames)
    {
        var allPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in roleNames)
        {
            var rolePermissions = KeycloakAuthorizationSeeder.GetRolePermissions(roleName);
            foreach (var permission in rolePermissions)
            {
                allPermissions.Add(permission);
            }
        }

        return allPermissions;
    }

    /// <summary>
    /// Gets policies from user roles (can be customized based on business logic)
    /// </summary>
    private static IEnumerable<string> GetPoliciesFromRoles(IList<string> roleNames)
    {
        var policies = new List<string>();

        foreach (var roleName in roleNames)
        {
            // Add role-based policies
            policies.Add($"role:{roleName}");

            // Add hierarchical policies based on role hierarchy
            if (RolePermissionTemplates.RoleHierarchy.ContainsKey(roleName))
            {
                policies.Add($"hierarchy:{roleName}");
            }

            // Add resource-based policies based on role capabilities
            switch (roleName.ToLowerInvariant())
            {
                case "super-admin":
                    policies.AddRange(["admin:full", "system:manage", "data:export"]);
                    break;
                case "admin":
                    policies.AddRange(["admin:limited", "user:manage", "role:manage"]);
                    break;
                case "manager":
                    policies.AddRange(["business:manage", "order:manage", "product:manage"]);
                    break;
                case "staff":
                    policies.AddRange(["business:operate", "customer:support"]);
                    break;
                case "customer":
                    policies.AddRange(["self:manage", "order:create"]);
                    break;
                case "viewer":
                    policies.Add("read:only");
                    break;
            }
        }

        return policies.Distinct();
    }

    private async Task CacheAuthData(string cacheKey, UserAuthorizationData data)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(data, JsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheOptions.UserAuthCacheExpiryInMinutes),
                SlidingExpiration = TimeSpan.FromMinutes(_cacheOptions.UserAuthCacheSlidingInMinutes)
            };

            await cache.SetStringAsync(cacheKey, serialized, options);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Caching failed for {Key}", cacheKey);
        }
    }

    public async Task InvalidateUserAuthorizationAsync(Guid userId)
    {
        var cacheKey = $"KeycloakUserAuth_{userId}";
        await SafeCacheRemoveAsync(cacheKey);
        Log.Information("Keycloak user auth cache invalidated for {UserId}", userId);
    }

    private async Task SafeCacheRemoveAsync(string key)
    {
        try
        {
            await cache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Cache removal failed for {Key}", key);
        }
    }

    /// <summary>
    /// Checks if a user has a specific resource-scope permission
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="resource">Resource name</param>
    /// <param name="scope">Scope name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if user has the permission</returns>
    public async Task<bool> HasResourcePermissionAsync(Guid userId, string resource, string scope, CancellationToken cancellationToken = default)
    {
        var userAuth = await GetUserAuthorizationAsync(userId);
        if (userAuth == null) return false;

        var permission = $"{resource}:{scope}";
        return userAuth.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets all resources that a user can access with a specific scope
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="scope">Scope name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of accessible resources</returns>
    public async Task<IEnumerable<string>> GetUserResourcesForScopeAsync(Guid userId, string scope, CancellationToken cancellationToken = default)
    {
        var userAuth = await GetUserAuthorizationAsync(userId);
        if (userAuth == null) return [];

        return userAuth.Permissions
            .Where(p => p.EndsWith($":{scope}", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Split(':')[0])
            .Distinct();
    }

    /// <summary>
    /// Gets all scopes that a user has for a specific resource
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="resource">Resource name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available scopes</returns>
    public async Task<IEnumerable<string>> GetUserScopesForResourceAsync(Guid userId, string resource, CancellationToken cancellationToken = default)
    {
        var userAuth = await GetUserAuthorizationAsync(userId);
        if (userAuth == null) return [];

        var resourcePrefix = $"{resource}:";
        return userAuth.Permissions
            .Where(p => p.StartsWith(resourcePrefix, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Substring(resourcePrefix.Length))
            .Distinct();
    }
}