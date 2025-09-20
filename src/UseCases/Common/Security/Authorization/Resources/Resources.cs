namespace UseCases.Common.Security.Authorization.Resources;

/// <summary>
/// Defines application resources for Keycloak authorization
/// Resources represent the entities/objects that can be protected
/// </summary>
public static class Resources
{
    // User Management Resources
    public const string Users = "users";
    public const string UserProfiles = "user-profiles";
    public const string UserSessions = "user-sessions";

    // Role Management Resources
    public const string Roles = "roles";
    public const string Permissions = "permissions";

    // Product Management Resources
    public const string Products = "products";
    public const string ProductCategories = "product-categories";
    public const string ProductInventory = "product-inventory";

    // Order Management Resources
    public const string Orders = "orders";
    public const string OrderItems = "order-items";
    public const string OrderPayments = "order-payments";

    // System Administration Resources
    public const string System = "system";
    public const string Configuration = "configuration";
    public const string AuditLogs = "audit-logs";

    // Analytics and Reporting Resources
    public const string Reports = "reports";
    public const string Analytics = "analytics";
    public const string Dashboard = "dashboard";

    // Customer Service Resources
    public const string CustomerSupport = "customer-support";
    public const string Tickets = "tickets";
    public const string Communications = "communications";

    /// <summary>
    /// Gets all available resources
    /// </summary>
    public static readonly string[] All = 
    [
        Users, UserProfiles, UserSessions,
        Roles, Permissions,
        Products, ProductCategories, ProductInventory,
        Orders, OrderItems, OrderPayments,
        System, Configuration, AuditLogs,
        Reports, Analytics, Dashboard,
        CustomerSupport, Tickets, Communications
    ];

    /// <summary>
    /// Gets admin-level resources
    /// </summary>
    public static readonly string[] AdminResources = 
    [
        Users, Roles, Permissions,
        System, Configuration, AuditLogs,
        Reports, Analytics
    ];

    /// <summary>
    /// Gets manager-level resources
    /// </summary>
    public static readonly string[] ManagerResources = 
    [
        Products, ProductCategories, ProductInventory,
        Orders, OrderPayments,
        Reports, Dashboard,
        CustomerSupport, Tickets
    ];

    /// <summary>
    /// Gets customer-level resources
    /// </summary>
    public static readonly string[] CustomerResources = 
    [
        UserProfiles, Orders, OrderItems
    ];
}