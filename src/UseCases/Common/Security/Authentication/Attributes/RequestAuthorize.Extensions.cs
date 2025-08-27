using System.Collections.Concurrent;

using Microsoft.AspNetCore.Builder;

using UseCases.Common.Security.Authentication.Attributes;

namespace UseCases.Common.Security.Authentication.Attributes;

public static class AuthorizationExtensions
{
    private static readonly ConcurrentDictionary<string, RequestAuthorizeAttribute> AttributeCache = new();

    // Require a single permission
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission cannot be null or whitespace.", nameof(permission));

        var attribute = AttributeCache.GetOrAdd(permission, _ => new RequestAuthorizeAttribute(permission));
        return builder.RequireAuthorization(attribute);
    }

    // Require multiple permissions (comma separated)
    public static TBuilder RequirePermissions<TBuilder>(this TBuilder builder, params string[] permissions)
        where TBuilder : IEndpointConventionBuilder
    {
        if (permissions is null || permissions.Length == 0)
            throw new ArgumentException("At least one permission must be specified.", nameof(permissions));

        var combinedPermissions = string.Join(",", permissions);
        var attribute = AttributeCache.GetOrAdd(combinedPermissions, _ => new RequestAuthorizeAttribute(combinedPermissions));
        return builder.RequireAuthorization(attribute);
    }

    // Require policy
    public static TBuilder RequirePolicy<TBuilder>(this TBuilder builder, string policy)
        where TBuilder : IEndpointConventionBuilder
    {
        if (string.IsNullOrWhiteSpace(policy))
            throw new ArgumentException("Policy cannot be null or whitespace.", nameof(policy));

        var cacheKey = $"policy:{policy}";
        var attribute = AttributeCache.GetOrAdd(cacheKey, _ => new RequestAuthorizeAttribute(policies: policy));
        return builder.RequireAuthorization(attribute);
    }

    // Require role
    public static TBuilder RequireRole<TBuilder>(this TBuilder builder, string role)
        where TBuilder : IEndpointConventionBuilder
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be null or whitespace.", nameof(role));

        var cacheKey = $"role:{role}";
        var attribute = AttributeCache.GetOrAdd(cacheKey, _ => new RequestAuthorizeAttribute(roles: role));
        return builder.RequireAuthorization(attribute);
    }

    // General method for combining permissions + policies + roles
    public static TBuilder RequireAuthorization<TBuilder>(this TBuilder builder, string? permissions = null, string? policies = null, string? roles = null)
        where TBuilder : IEndpointConventionBuilder
    {
        if (string.IsNullOrWhiteSpace(permissions) && string.IsNullOrWhiteSpace(policies) && string.IsNullOrWhiteSpace(roles))
            throw new ArgumentException("At least one authorization parameter must be specified.");

        var cacheKey = $"p:{permissions ?? ""},pol:{policies ?? ""},r:{roles ?? ""}";
        var attribute = AttributeCache.GetOrAdd(cacheKey,
            _ => new RequestAuthorizeAttribute(permissions, policies, roles));

        return builder.RequireAuthorization(attribute);
    }
}
