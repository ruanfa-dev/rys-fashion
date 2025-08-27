using System.Security.Claims;
using System.Text.Json;

using AsyncKeyedLock;

using Infrastructure.Identity.Models;
using Infrastructure.Security.Authorization.Options;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

using Serilog;

using UseCases.Common.Security.Authorization.Claims;

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
        var cacheKey = $"UserAuth_{userId}";

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

    private async Task<UserAuthorizationData?> FetchAndCacheAuthData(Guid userId, string cacheKey)
    {
        var user = await userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            Log.Warning("User not found: {UserId}", userId);
            return null;
        }

        var (roles, roleClaims) = await GetRolesAndClaimsAsync(user);

        var authData = new UserAuthorizationData(
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
        var roleNames = await userManager.GetRolesAsync(user);
        if (roleNames.Count == 0)
            return (roleNames, new());

        // Try to load role claims mapping from cache
        var cachedJson = await cache.GetStringAsync(RoleClaimsCacheKey);
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
            var roles = await roleManager.Roles.AsNoTracking().ToListAsync();
            var claimsTasks = roles.ToDictionary(r => r.Name!, r => roleManager.GetClaimsAsync(r));
            await Task.WhenAll(claimsTasks.Values);

            roleClaimsMap = claimsTasks.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Result.ToList()
            );

            try
            {
                var serialized = JsonSerializer.Serialize(roleClaimsMap, JsonOptions);
                var options = new DistributedCacheEntryOptions
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

        var claims = roleNames
            .Where(roleClaimsMap.ContainsKey)
            .SelectMany(r => roleClaimsMap[r])
            .ToList();

        return (roleNames, claims);
    }

    private static IReadOnlyList<string> GetDistinctValues(IEnumerable<Claim> claims, string claimType) =>
        claims
            .Where(c => c.Type == claimType && !string.IsNullOrEmpty(c.Value))
            .Select(c => c.Value!)
            .Distinct()
            .ToList()
            .AsReadOnly();

    private async Task CacheAuthData(string cacheKey, UserAuthorizationData data)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(data, JsonOptions);
            var options = new DistributedCacheEntryOptions
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
        var cacheKey = $"UserAuth_{userId}";
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
