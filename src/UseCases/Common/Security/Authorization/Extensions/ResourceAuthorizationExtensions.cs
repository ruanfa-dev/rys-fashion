using Microsoft.AspNetCore.Builder;

using UseCases.Common.Security.Authorization.Attributes;

namespace UseCases.Common.Security.Authorization.Extensions;

/// <summary>
/// Extension methods for resource and scope-based authorization on endpoints
/// </summary>
public static class ResourceAuthorizationExtensions
{
    /// <summary>
    /// Requires a specific resource and scope permission
    /// </summary>
    /// <typeparam name="TBuilder">Type of endpoint convention builder</typeparam>
    /// <param name="builder">The endpoint convention builder</param>
    /// <param name="resource">Required resource</param>
    /// <param name="scope">Required scope</param>
    /// <returns>The builder for method chaining</returns>
    public static TBuilder RequireResourcePermission<TBuilder>(this TBuilder builder, string resource, string scope)
        where TBuilder : IEndpointConventionBuilder
    {
        var permission = $"{resource}:{scope}";
        return builder.RequirePermission(permission);
    }

    /// <summary>
    /// Requires multiple resource-scope permissions (all must be present)
    /// </summary>
    /// <typeparam name="TBuilder">Type of endpoint convention builder</typeparam>
    /// <param name="builder">The endpoint convention builder</param>
    /// <param name="permissions">Array of resource:scope permissions</param>
    /// <returns>The builder for method chaining</returns>
    public static TBuilder RequireResourcePermissions<TBuilder>(this TBuilder builder, params string[] permissions)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequirePermissions(permissions);
    }

    // User Management Extensions
    public static TBuilder RequireUserManagement<TBuilder>(this TBuilder builder, string scope)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("users", scope);

    public static TBuilder RequireUserCreate<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("users", "create");

    public static TBuilder RequireUserRead<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("users", "read");

    public static TBuilder RequireUserUpdate<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("users", "update");

    public static TBuilder RequireUserDelete<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("users", "delete");

    public static TBuilder RequireUserManage<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("users", "manage");

    // Product Management Extensions
    public static TBuilder RequireProductManagement<TBuilder>(this TBuilder builder, string scope)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("products", scope);

    public static TBuilder RequireProductCreate<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("products", "create");

    public static TBuilder RequireProductRead<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("products", "read");

    public static TBuilder RequireProductUpdate<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("products", "update");

    public static TBuilder RequireProductDelete<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("products", "delete");

    public static TBuilder RequireProductManage<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("products", "manage");

    // Order Management Extensions
    public static TBuilder RequireOrderManagement<TBuilder>(this TBuilder builder, string scope)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("orders", scope);

    public static TBuilder RequireOrderCreate<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("orders", "create");

    public static TBuilder RequireOrderRead<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("orders", "read");

    public static TBuilder RequireOrderUpdate<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("orders", "update");

    public static TBuilder RequireOrderApprove<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("orders", "approve");

    public static TBuilder RequireOrderManage<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("orders", "manage");

    // Role Management Extensions
    public static TBuilder RequireRoleManagement<TBuilder>(this TBuilder builder, string scope)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("roles", scope);

    public static TBuilder RequireRoleCreate<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("roles", "create");

    public static TBuilder RequireRoleRead<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("roles", "read");

    public static TBuilder RequireRoleUpdate<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("roles", "update");

    public static TBuilder RequireRoleDelete<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("roles", "delete");

    public static TBuilder RequireRoleManage<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("roles", "manage");

    // System Administration Extensions
    public static TBuilder RequireSystemAccess<TBuilder>(this TBuilder builder, string scope)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("system", scope);

    public static TBuilder RequireSystemRead<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("system", "read");

    public static TBuilder RequireSystemConfigure<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("system", "configure");

    public static TBuilder RequireSystemManage<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("system", "manage");

    public static TBuilder RequireSystemAdminister<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("system", "administer");

    // Reports and Analytics Extensions
    public static TBuilder RequireReportAccess<TBuilder>(this TBuilder builder, string scope)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("reports", scope);

    public static TBuilder RequireReportRead<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("reports", "read");

    public static TBuilder RequireReportCreate<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("reports", "create");

    public static TBuilder RequireReportExport<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.RequireResourcePermission("reports", "export");
}