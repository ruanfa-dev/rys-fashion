using UseCases.Common.Security.Authorization.Resources;
using UseCases.Common.Security.Authorization.Scopes;

namespace UseCases.Common.Security.Authorization.Mappings;

/// <summary>
/// Defines the mapping between resources and their allowed scopes
/// This configuration will be used to seed Keycloak with proper resource-scope combinations
/// </summary>
public static class ResourceScopeMapping
{
    /// <summary>
    /// Maps resources to their allowed scopes
    /// </summary>
    public static readonly Dictionary<string, string[]> Mappings = new()
    {
        // User Management
        ["users"] = [
            "create", "read", "update", "delete",
            "list", "search", "view", "edit",
            "enable", "disable", "manage"
        ],
        
        ["user-profiles"] = [
            "read", "update", "view", "edit"
        ],
        
        ["user-sessions"] = [
            "read", "list", "view", "delete", "manage"
        ],

        // Role Management
        ["roles"] = [
            "create", "read", "update", "delete",
            "list", "view", "edit", "manage"
        ],
        
        ["permissions"] = [
            "read", "list", "view", "manage"
        ],

        // Product Management
        ["products"] = [
            "create", "read", "update", "delete",
            "list", "search", "view", "edit",
            "publish", "unpublish", "enable", "disable",
            "import", "export", "manage"
        ],
        
        ["product-categories"] = [
            "create", "read", "update", "delete",
            "list", "view", "edit", "manage"
        ],
        
        ["product-inventory"] = [
            "read", "update", "list", "view",
            "edit", "import", "export", "manage"
        ],

        // Order Management
        ["orders"] = [
            "create", "read", "update", "delete",
            "list", "search", "view", "edit",
            "approve", "reject", "submit",
            "export", "manage"
        ],
        
        ["order-items"] = [
            "read", "update", "list", "view", "edit"
        ],
        
        ["order-payments"] = [
            "read", "update", "list", "view",
            "approve", "reject", "manage"
        ],

        // System Administration
        ["system"] = [
            "read", "configure", "manage", "administer",
            "backup", "restore"
        ],
        
        ["configuration"] = [
            "read", "update", "configure", "manage"
        ],
        
        ["audit-logs"] = [
            "read", "list", "search", "view",
            "export", "manage"
        ],

        // Analytics and Reporting
        ["reports"] = [
            "create", "read", "update", "delete",
            "list", "view", "export", "manage"
        ],
        
        ["analytics"] = [
            "read", "view", "export", "manage"
        ],
        
        ["dashboard"] = [
            "read", "view", "configure", "manage"
        ],

        // Customer Service
        ["customer-support"] = [
            "read", "update", "list", "view",
            "edit", "manage"
        ],
        
        ["tickets"] = [
            "create", "read", "update", "delete",
            "list", "search", "view", "edit",
            "approve", "reject", "submit", "manage"
        ],
        
        ["communications"] = [
            "create", "read", "list", "view",
            "send", "receive", "manage"
        ]
    };

    /// <summary>
    /// Gets all unique resource-scope combinations as permission strings
    /// Format: "resource:scope" (e.g., "users:create", "products:read")
    /// </summary>
    public static IEnumerable<string> GetAllPermissions()
    {
        foreach (var (resource, scopes) in Mappings)
        {
            foreach (var scope in scopes)
            {
                yield return $"{resource}:{scope}";
            }
        }
    }

    /// <summary>
    /// Gets permissions for a specific resource
    /// </summary>
    /// <param name="resource">Resource name</param>
    /// <returns>List of permissions for the resource</returns>
    public static IEnumerable<string> GetResourcePermissions(string resource)
    {
        if (!Mappings.TryGetValue(resource, out var scopes))
            return [];

        return scopes.Select(scope => $"{resource}:{scope}");
    }

    /// <summary>
    /// Gets all resources that have a specific scope
    /// </summary>
    /// <param name="scope">Scope name</param>
    /// <returns>List of resources that have the scope</returns>
    public static IEnumerable<string> GetResourcesWithScope(string scope)
    {
        return Mappings
            .Where(kvp => kvp.Value.Contains(scope))
            .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Validates if a resource-scope combination is valid
    /// </summary>
    /// <param name="resource">Resource name</param>
    /// <param name="scope">Scope name</param>
    /// <returns>True if the combination is valid</returns>
    public static bool IsValidPermission(string resource, string scope)
    {
        return Mappings.TryGetValue(resource, out var scopes) && scopes.Contains(scope);
    }

    /// <summary>
    /// Parses a permission string into resource and scope
    /// </summary>
    /// <param name="permission">Permission string in format "resource:scope"</param>
    /// <returns>Tuple of resource and scope, or null if invalid format</returns>
    public static (string Resource, string Scope)? ParsePermission(string permission)
    {
        var parts = permission.Split(':', 2);
        if (parts.Length != 2)
            return null;

        var resource = parts[0].Trim();
        var scope = parts[1].Trim();

        return IsValidPermission(resource, scope) ? (resource, scope) : null;
    }
}