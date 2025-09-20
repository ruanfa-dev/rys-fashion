using UseCases.Common.Security.Authorization.Mappings;

namespace UseCases.Common.Security.Authorization.Templates;

/// <summary>
/// Defines role-based permission templates for seeding Keycloak
/// These templates define what permissions each role should have by default
/// </summary>
public static class RolePermissionTemplates
{
    /// <summary>
    /// Super Administrator - Full system access
    /// </summary>
    public static readonly RoleTemplate SuperAdmin = new(
        Name: "super-admin",
        DisplayName: "Super Administrator",
        Description: "Full system access with all permissions",
        Permissions: ResourceScopeMapping.GetAllPermissions().ToArray()
    );

    /// <summary>
    /// Administrator - System management access
    /// </summary>
    public static readonly RoleTemplate Admin = new(
        Name: "admin",
        DisplayName: "Administrator",
        Description: "System administrator with user and role management permissions",
        Permissions: [
            // User Management
            ..ResourceScopeMapping.GetResourcePermissions("users"),
            ..ResourceScopeMapping.GetResourcePermissions("user-sessions"),
            
            // Role Management
            ..ResourceScopeMapping.GetResourcePermissions("roles"),
            ..ResourceScopeMapping.GetResourcePermissions("permissions"),
            
            // System Administration
            ..ResourceScopeMapping.GetResourcePermissions("system"),
            ..ResourceScopeMapping.GetResourcePermissions("configuration"),
            ..ResourceScopeMapping.GetResourcePermissions("audit-logs"),
            
            // Reports and Analytics
            ..ResourceScopeMapping.GetResourcePermissions("reports"),
            ..ResourceScopeMapping.GetResourcePermissions("analytics")
        ]
    );

    /// <summary>
    /// Manager - Business operations management
    /// </summary>
    public static readonly RoleTemplate Manager = new(
        Name: "manager",
        DisplayName: "Manager",
        Description: "Business manager with product and order management permissions",
        Permissions: [
            // Product Management
            ..ResourceScopeMapping.GetResourcePermissions("products"),
            ..ResourceScopeMapping.GetResourcePermissions("product-categories"),
            ..ResourceScopeMapping.GetResourcePermissions("product-inventory"),
            
            // Order Management
            ..ResourceScopeMapping.GetResourcePermissions("orders"),
            ..ResourceScopeMapping.GetResourcePermissions("order-items"),
            ..ResourceScopeMapping.GetResourcePermissions("order-payments"),
            
            // Customer Service
            ..ResourceScopeMapping.GetResourcePermissions("customer-support"),
            ..ResourceScopeMapping.GetResourcePermissions("tickets"),
            ..ResourceScopeMapping.GetResourcePermissions("communications"),
            
            // Dashboard and Reports (read-only)
            "dashboard:read",
            "dashboard:view",
            "reports:read",
            "reports:view",
            "reports:export"
        ]
    );

    /// <summary>
    /// Staff - General staff operations
    /// </summary>
    public static readonly RoleTemplate Staff = new(
        Name: "staff",
        DisplayName: "Staff",
        Description: "Staff member with limited operational permissions",
        Permissions: [
            // Products (read-only and basic editing)
            "products:read",
            "products:list",
            "products:search",
            "products:view",
            "products:update",
            "product-categories:read",
            "product-categories:list",
            "product-categories:view",
            "product-inventory:read",
            "product-inventory:update",
            "product-inventory:view",
            
            // Orders (read and basic operations)
            "orders:read",
            "orders:list",
            "orders:search",
            "orders:view",
            "orders:update",
            "order-items:read",
            "order-items:view",
            
            // Customer Service
            "customer-support:read",
            "customer-support:view",
            "tickets:create",
            "tickets:read",
            "tickets:update",
            "tickets:view",
            "communications:create",
            "communications:read",
            "communications:send"
        ]
    );

    /// <summary>
    /// Customer - End user operations
    /// </summary>
    public static readonly RoleTemplate Customer = new(
        Name: "customer",
        DisplayName: "Customer",
        Description: "Customer with personal account and order management permissions",
        Permissions: [
            // User Profile (own profile only)
            "user-profiles:read",
            "user-profiles:update",
            "user-profiles:view",
            "user-profiles:edit",
            
            // Orders (own orders only)
            "orders:create",
            "orders:read",
            "orders:list",
            "orders:view",
            "order-items:read",
            "order-items:view",
            
            // Products (read-only for browsing)
            "products:read",
            "products:list",
            "products:search",
            "products:view",
            "product-categories:read",
            "product-categories:list",
            "product-categories:view",
            
            // Customer Support
            "tickets:create",
            "tickets:read",
            "tickets:view",
            "communications:create",
            "communications:read",
            "communications:send",
            "communications:receive"
        ]
    );

    /// <summary>
    /// Viewer - Read-only access for auditing or reporting
    /// </summary>
    public static readonly RoleTemplate Viewer = new(
        Name: "viewer",
        DisplayName: "Viewer",
        Description: "Read-only access for viewing and reporting purposes",
        Permissions: ResourceScopeMapping.Mappings
            .SelectMany(kvp => kvp.Value
                .Where(scope => new[] { "read", "list", "search", "view" }.Contains(scope))
                .Select(scope => $"{kvp.Key}:{scope}"))
            .ToArray()
    );

    /// <summary>
    /// Gets all role templates
    /// </summary>
    public static readonly RoleTemplate[] All = [
        SuperAdmin, Admin, Manager, Staff, Customer, Viewer
    ];

    /// <summary>
    /// Gets a role template by name
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <returns>Role template or null if not found</returns>
    public static RoleTemplate? GetRoleTemplate(string roleName)
    {
        return All.FirstOrDefault(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets role templates for hierarchical roles (roles that inherit from others)
    /// </summary>
    public static readonly Dictionary<string, string[]> RoleHierarchy = new()
    {
        [SuperAdmin.Name] = [Admin.Name, Manager.Name, Staff.Name, Customer.Name, Viewer.Name],
        [Admin.Name] = [Manager.Name, Staff.Name, Viewer.Name],
        [Manager.Name] = [Staff.Name, Viewer.Name],
        [Staff.Name] = [Viewer.Name]
    };
}

/// <summary>
/// Represents a role template with its permissions
/// </summary>
/// <param name="Name">Role name (used as identifier)</param>
/// <param name="DisplayName">Display name for UI</param>
/// <param name="Description">Role description</param>
/// <param name="Permissions">Array of permissions in "resource:scope" format</param>
public record RoleTemplate(
    string Name,
    string DisplayName,
    string Description,
    string[] Permissions
);