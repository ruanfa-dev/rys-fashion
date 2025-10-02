using System.Security.Claims;
using System.Text.Json;

using AsyncKeyedLock;

using Core.Identity.Roles;
using Core.Identity.Users;

using Infrastructure.Security.Authorization.Options;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

using Serilog;

using UseCases.Common.Security.Authorization.Claims;
using UseCases.Common.Security.Authorization.Providers;

namespace Infrastructure.Security.Authorization.Providers;

public sealed class UserAuthorizationProvider(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    IDistributedCache cache,
    IOptions<AuthUserCacheOption> authCacheOption)
    : IUserAuthorizationProvider
{
    private static readonly AsyncKeyedLocker<Guid> UserLocks = new();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AuthUserCacheOption _jwtOptions = authCacheOption.Value;
    private const string RoleClaimsCacheKey = "AllRoleClaims";

    public async Task<UserAuthorizationData?> GetUserAuthorizationAsync(Guid userId)
    {
        string cacheKey = $"UserAuth_{userId}";

        if (await TryGetCachedAuthAsync(cacheKey) is { } cached)
            return cached;

        using (await UserLocks.LockAsync(userId))
        {
            if (await TryGetCachedAuthAsync(cacheKey) is { } rechecked)
                return rechecked;

            return await FetchAndCacheAuthData(userId, cacheKey);
        }
    }

    private async ValueTask<UserAuthorizationData?> TryGetCachedAuthAsync(string cacheKey)
    {
        try
        {
            string? cachedData = await cache.GetStringAsync(cacheKey).ConfigureAwait(false);
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

    private async Task<UserAuthorizationData?> FetchAndCacheAuthData(Guid userId, string cacheKey)
    {
        User? user = await userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            Log.Warning("User not found: {UserId}", userId);
            return null;
        }

        (IList<string> roles, List<Claim> roleClaims) = await GetRolesAndClaimsAsync(user);

        UserAuthorizationData authData = new UserAuthorizationData(
            UserId: userId,
            UserName: user.UserName ?? string.Empty,
            Email: user.Email ?? string.Empty,
            Permissions: GetDistinctValues(roleClaims, CustomClaim.Permission),
            Roles: roles.ToList().AsReadOnly(),
            Policies: GetDistinctValues(roleClaims, CustomClaim.Policy));

        await CacheAuthData(cacheKey, authData);
        return authData;
    }

    private async Task<(IList<string> Roles, List<Claim> Claims)> GetRolesAndClaimsAsync(User user)
    {
        IList<string> roleNames = await userManager.GetRolesAsync(user);
        if (roleNames.Count == 0)
            return (roleNames, []);

        // Try to load role claims mapping from cache
        string? cachedJson = await cache.GetStringAsync(RoleClaimsCacheKey);
        Dictionary<string, List<Claim>>? roleClaimsMap = null;

        if (!string.IsNullOrEmpty(cachedJson))
        {
            try
            {
                roleClaimsMap = JsonSerializer.Deserialize<Dictionary<string, List<Claim>>>(cachedJson, JsonOptions);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to deserialize cached role claims, refreshing...");
                await SafeCacheRemoveAsync(RoleClaimsCacheKey);
            }
        }

        // Refresh role claims cache if missing
        if (roleClaimsMap is null)
        {
            List<Role> roles = await roleManager.Roles.AsNoTracking().ToListAsync();
            roleClaimsMap = new Dictionary<string, List<Claim>>(roles.Count);

            foreach (Role role in roles)
            {
                IList<Claim> claimsForRole = await roleManager.GetClaimsAsync(role); // Renamed variable
                roleClaimsMap[role.Name!] = [.. claimsForRole];
            }

            try
            {
                string serialized = JsonSerializer.Serialize(roleClaimsMap, JsonOptions);
                DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_jwtOptions.RoleClaimsCacheExpiryInMinutes),
                    SlidingExpiration = TimeSpan.FromMinutes(_jwtOptions.RoleClaimsCacheExpiryInMinutes / 2)
                };
                await cache.SetStringAsync(RoleClaimsCacheKey, serialized, options);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to cache role claims");
            }
        }

        // Collect all claims from roles
        List<Claim> roleClaims = roleNames
            .Where(roleClaimsMap.ContainsKey)
            .SelectMany(r => roleClaimsMap[r])
            .ToList();

        // Collect claims directly assigned to the user
        IList<Claim> userClaims = await userManager.GetClaimsAsync(user);

        // Combine role claims and user claims
        List<Claim> allClaims = new List<Claim>(roleClaims.Count + userClaims.Count);
        allClaims.AddRange(roleClaims);
        allClaims.AddRange(userClaims);

        return (roleNames, allClaims);
    }

    private static IReadOnlyList<string> GetDistinctValues(IEnumerable<Claim> claims, string claimType) =>
        claims
            .Where(c => c.Type.ToLower() == claimType && !string.IsNullOrEmpty(c.Value))
            .Select(c => c.Value!)
            .Distinct()
            .ToList()
            .AsReadOnly();

    private async Task CacheAuthData(string cacheKey, UserAuthorizationData data)
    {
        try
        {
            string serialized = JsonSerializer.Serialize(data, JsonOptions);
            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_jwtOptions.UserAuthCacheExpiryInMinutes),
                SlidingExpiration = TimeSpan.FromMinutes(_jwtOptions.UserAuthCacheSlidingInMinutes)
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
        string cacheKey = $"UserAuth_{userId}";
        await SafeCacheRemoveAsync(cacheKey);
        Log.Information("Cache invalidated for {UserId}", userId);
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
}
