using System.Collections.Concurrent;

using Core.Identity.Permissions;

using Microsoft.AspNetCore.Builder;

namespace UseCases.Common.Security.Authorization.Attributes;

/// <summary>
/// Extension methods for easier authorization configuration on endpoints.
/// Provides a fluent API for configuring authorization requirements.
/// </summary>
public static class AuthorizationExtensions
{
    private static readonly ConcurrentDictionary<string, RequestAuthorizeAttribute> AttributeCache = new();

    /// <summary>
    /// Requires a single permission for the endpoint.
    /// </summary>
    /// <typeparam name="TBuilder">Type of endpoint convention builder</typeparam>
    /// <param name="builder">The endpoint convention builder</param>
    /// <param name="permission">Required permission</param>
    /// <returns>The builder for method chaining</returns>
    /// <exception cref="ArgumentException">Thrown when permission is null or whitespace</exception>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission cannot be null or whitespace.", nameof(permission));

        string cacheKey = $"perm:{permission}";
        RequestAuthorizeAttribute attribute = AttributeCache.GetOrAdd(cacheKey, _ => new RequestAuthorizeAttribute(permissions: permission));
        return builder.RequireAuthorization(attribute);
    }

    /// <summary>
    /// Requires a permission from a Permission entity for the endpoint.
    /// </summary>
    /// <typeparam name="TBuilder">Type of endpoint convention builder</typeparam>
    /// <param name="builder">The endpoint convention builder</param>
    /// <param name="permission">Required permission entity</param>
    /// <returns>The builder for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when permission is null</exception>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, Permission permission)
        where TBuilder : IEndpointConventionBuilder
    {
        if (permission == null)
            throw new ArgumentNullException(nameof(permission));

        return builder.RequirePermission(permission.Name);
    }

    /// <summary>
    /// Requires multiple permissions for the endpoint (AND logic - all must be present).
    /// </summary>
    /// <typeparam name="TBuilder">Type of endpoint convention builder</typeparam>
    /// <param name="builder">The endpoint convention builder</param>
    /// <param name="permissions">Required permissions</param>
    /// <returns>The builder for method chaining</returns>
    /// <exception cref="ArgumentException">Thrown when no permissions are specified</exception>
    public static TBuilder RequirePermissions<TBuilder>(this TBuilder builder, params string[] permissions)
        where TBuilder : IEndpointConventionBuilder
    {
        if (permissions is null || permissions.Length == 0)
            throw new ArgumentException("At least one permission must be specified.", nameof(permissions));

        string combinedPermissions = string.Join(",", permissions.Where(p => !string.IsNullOrWhiteSpace(p)));
        if (string.IsNullOrEmpty(combinedPermissions))
            throw new ArgumentException("At least one valid permission must be specified.", nameof(permissions));

        string cacheKey = $"perms:{combinedPermissions}";
        RequestAuthorizeAttribute attribute = AttributeCache.GetOrAdd(cacheKey, _ => new RequestAuthorizeAttribute(permissions: combinedPermissions));
        return builder.RequireAuthorization(attribute);
    }

    /// <summary>
    /// Requires multiple permissions from Permission entities for the endpoint.
    /// </summary>
    /// <typeparam name="TBuilder">Type of endpoint convention builder</typeparam>
    /// <param name="builder">The endpoint convention builder</param>
    /// <param name="permissions">Required permission entities</param>
    /// <returns>The builder for method chaining</returns>
    /// <exception cref="ArgumentException">Thrown when no permissions are specified</exception>
    public static TBuilder RequirePermissions<TBuilder>(this TBuilder builder, params Permission[] permissions)
        where TBuilder : IEndpointConventionBuilder
    {
        if (permissions is null || permissions.Length == 0)
            throw new ArgumentException("At least one permission must be specified.", nameof(permissions));

        string[] permissionNames = permissions.Where(p => p != null).Select(p => p.Name).ToArray();
        return builder.RequirePermissions(permissionNames);
    }

    /// <summary>
    /// Requires a specific policy for the endpoint.
    /// </summary>
    /// <typeparam name="TBuilder">Type of endpoint convention builder</typeparam>
    /// <param name="builder">The endpoint convention builder</param>
    /// <param name="policy">Required policy</param>
    /// <returns>The builder for method chaining</returns>
    /// <exception cref="ArgumentException">Thrown when policy is null or whitespace</exception>
    public static TBuilder RequirePolicy<TBuilder>(this TBuilder builder, string policy)
        where TBuilder : IEndpointConventionBuilder
    {
        if (string.IsNullOrWhiteSpace(policy))
            throw new ArgumentException("Policy cannot be null or whitespace.", nameof(policy));

        string cacheKey = $"policy:{policy}";
        RequestAuthorizeAttribute attribute = AttributeCache.GetOrAdd(cacheKey, _ => new RequestAuthorizeAttribute(policies: policy));
        return builder.RequireAuthorization(attribute);
    }

    /// <summary>
    /// Requires multiple policies for the endpoint.
    /// </summary>
    /// <typeparam name="TBuilder">Type of endpoint convention builder</typeparam>
    /// <param name="builder">The endpoint convention builder</param>
    /// <param name="policies">Required policies</param>
    /// <returns>The builder for method chaining</returns>
    /// <exception cref="ArgumentException">Thrown when no policies are specified</exception>
    public static TBuilder RequirePolicies<TBuilder>(this TBuilder builder, params string[] policies)
        where TBuilder : IEndpointConventionBuilder
    {
        if (policies is null || policies.Length == 0)
            throw new ArgumentException("At least one policy must be specified.", nameof(policies));

        string combinedPolicies = string.Join(",", policies.Where(p => !string.IsNullOrWhiteSpace(p)));
        if (string.IsNullOrEmpty(combinedPolicies))
            throw new ArgumentException("At least one valid policy must be specified.", nameof(policies));

        string cacheKey = $"policies:{combinedPolicies}";
        RequestAuthorizeAttribute attribute = AttributeCache.GetOrAdd(cacheKey, _ => new RequestAuthorizeAttribute(policies: combinedPolicies));
        return builder.RequireAuthorization(attribute);
    }

    /// <summary>
    /// Requires a specific role for the endpoint.
    /// </summary>
    /// <typeparam name="TBuilder">Type of endpoint convention builder</typeparam>
    /// <param name="builder">The endpoint convention builder</param>
    /// <param name="role">Required role</param>
    /// <returns>The builder for method chaining</returns>
    /// <exception cref="ArgumentException">Thrown when role is null or whitespace</exception>
    public static TBuilder RequireRole<TBuilder>(this TBuilder builder, string role)
        where TBuilder : IEndpointConventionBuilder
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be null or whitespace.", nameof(role));

        string cacheKey = $"role:{role}";
        RequestAuthorizeAttribute attribute = AttributeCache.GetOrAdd(cacheKey, _ => new RequestAuthorizeAttribute(roles: role));
        return builder.RequireAuthorization(attribute);
    }

    /// <summary>
    /// Requires multiple roles for the endpoint.
    /// </summary>
    /// <typeparam name="TBuilder">Type of endpoint convention builder</typeparam>
    /// <param name="builder">The endpoint convention builder</param>
    /// <param name="roles">Required roles</param>
    /// <returns>The builder for method chaining</returns>
    /// <exception cref="ArgumentException">Thrown when no roles are specified</exception>
    public static TBuilder RequireRoles<TBuilder>(this TBuilder builder, params string[] roles)
        where TBuilder : IEndpointConventionBuilder
    {
        if (roles is null || roles.Length == 0)
            throw new ArgumentException("At least one role must be specified.", nameof(roles));

        string combinedRoles = string.Join(",", roles.Where(r => !string.IsNullOrWhiteSpace(r)));
        if (string.IsNullOrEmpty(combinedRoles))
            throw new ArgumentException("At least one valid role must be specified.", nameof(roles));

        string cacheKey = $"roles:{combinedRoles}";
        RequestAuthorizeAttribute attribute = AttributeCache.GetOrAdd(cacheKey, _ => new RequestAuthorizeAttribute(roles: combinedRoles));
        return builder.RequireAuthorization(attribute);
    }

    /// <summary>
    /// General method for combining permissions, policies, and roles with custom configuration.
    /// </summary>
    /// <typeparam name="TBuilder">Type of endpoint convention builder</typeparam>
    /// <param name="builder">The endpoint convention builder</param>
    /// <param name="permissions">Required permissions (comma-separated or array)</param>
    /// <param name="policies">Required policies (comma-separated or array)</param>
    /// <param name="roles">Required roles (comma-separated or array)</param>
    /// <returns>The builder for method chaining</returns>
    /// <exception cref="ArgumentException">Thrown when no authorization parameters are specified</exception>
    public static TBuilder RequireCustomAuthorization<TBuilder>(this TBuilder builder,
        string? permissions = null,
        string? policies = null,
        string? roles = null)
        where TBuilder : IEndpointConventionBuilder
    {
        if (string.IsNullOrWhiteSpace(permissions) &&
            string.IsNullOrWhiteSpace(policies) &&
            string.IsNullOrWhiteSpace(roles))
        {
            throw new ArgumentException("At least one authorization parameter must be specified.");
        }

        string cacheKey = $"custom:p:{permissions ?? ""},pol:{policies ?? ""},r:{roles ?? ""}";
        RequestAuthorizeAttribute attribute = AttributeCache.GetOrAdd(cacheKey,
            _ => new RequestAuthorizeAttribute(permissions, roles, policies));

        return builder.RequireAuthorization(attribute);
    }

    /// <summary>
    /// Requires administrative access (combines common admin permissions).
    /// </summary>
    /// <typeparam name="TBuilder">Type of endpoint convention builder</typeparam>
    /// <param name="builder">The endpoint convention builder</param>
    /// <param name="resource">Optional specific resource for granular admin permissions</param>
    /// <returns>The builder for method chaining</returns>
    public static TBuilder RequireAdminAccess<TBuilder>(this TBuilder builder, string? resource = null)
        where TBuilder : IEndpointConventionBuilder
    {
        string adminRole = "Administrator";
        string adminPermissions = resource != null ? $"Admin.{resource}.Manage" : "Admin.Manage";

        return builder.RequireCustomAuthorization(permissions: adminPermissions, roles: adminRole);
    }

    /// <summary>
    /// Clears the internal attribute cache. Useful for testing or when you need to refresh cached attributes.
    /// </summary>
    public static void ClearCache()
    {
        AttributeCache.Clear();
    }

    /// <summary>
    /// Gets the current cache size for monitoring purposes.
    /// </summary>
    /// <returns>Number of cached authorization attributes</returns>
    public static int GetCacheSize() => AttributeCache.Count;
}
