using Microsoft.Extensions.Logging;

using UseCases.Common.Security.Authentication.Services;
using UseCases.Common.Security.Authorization.Mappings;
using UseCases.Common.Security.Authorization.Templates;

namespace Infrastructure.Security.Authorization.Seeders;

/// <summary>
/// Service responsible for seeding Keycloak with resources, scopes, and role-based permissions
/// </summary>
public class KeycloakAuthorizationSeeder(
    IKeycloakAdminService keycloakAdminService,
    ILogger<KeycloakAuthorizationSeeder> logger)
{
    public async Task SeedAuthorizationDataAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting Keycloak authorization data seeding...");

        try
        {
            // Step 1: Seed Roles
            await SeedRolesAsync(cancellationToken);

            // Step 2: Seed Role Hierarchies (if supported by your Keycloak setup)
            SeedRoleHierarchies();

            logger.LogInformation("Keycloak authorization data seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed Keycloak authorization data");
            throw;
        }
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding roles and permissions...");

        foreach (var roleTemplate in RolePermissionTemplates.All)
        {
            try
            {
                // Check if role exists
                var existingRole = await keycloakAdminService.GetRoleByNameAsync(roleTemplate.Name, cancellationToken);
                
                if (existingRole == null)
                {
                    // Create role
                    await keycloakAdminService.CreateRoleAsync(new()
                    {
                        Name = roleTemplate.Name,
                        Description = roleTemplate.Description
                    }, cancellationToken);

                    logger.LogInformation("Created role: {RoleName}", roleTemplate.Name);
                }
                else
                {
                    logger.LogDebug("Role already exists: {RoleName}", roleTemplate.Name);
                }

                // Note: In a full implementation, you would also need to:
                // 1. Create client resources in Keycloak
                // 2. Create client scopes in Keycloak  
                // 3. Create client resource permissions
                // 4. Associate permissions with roles
                // 
                // This requires additional Keycloak Admin API endpoints for resource-based authorization
                // For now, we're creating the basic role structure

                logger.LogDebug("Role {RoleName} has {PermissionCount} permissions", 
                    roleTemplate.Name, roleTemplate.Permissions.Length);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create role: {RoleName}", roleTemplate.Name);
                throw;
            }
        }
    }

    private void SeedRoleHierarchies()
    {
        logger.LogInformation("Setting up role hierarchies...");

        foreach (var (parentRole, childRoles) in RolePermissionTemplates.RoleHierarchy)
        {
            try
            {
                // Note: Role hierarchy implementation depends on your Keycloak setup
                // This might involve composite roles or role mappings
                // The exact implementation would depend on your specific Keycloak configuration

                logger.LogDebug("Role hierarchy: {ParentRole} -> [{ChildRoles}]", 
                    parentRole, string.Join(", ", childRoles));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to set up role hierarchy for: {ParentRole}", parentRole);
                // Don't throw here as role hierarchies are not critical for basic functionality
            }
        }
    }

    /// <summary>
    /// Gets all permissions for a specific role
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <returns>List of permissions</returns>
    public static IEnumerable<string> GetRolePermissions(string roleName)
    {
        var roleTemplate = RolePermissionTemplates.GetRoleTemplate(roleName);
        return roleTemplate?.Permissions ?? [];
    }

    /// <summary>
    /// Validates if a role has a specific permission
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <param name="permission">Permission in "resource:scope" format</param>
    /// <returns>True if role has the permission</returns>
    public static bool RoleHasPermission(string roleName, string permission)
    {
        var rolePermissions = GetRolePermissions(roleName);
        return rolePermissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets all resources that a role can access with a specific scope
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <param name="scope">Scope name</param>
    /// <returns>List of accessible resources</returns>
    public static IEnumerable<string> GetRoleResourcesForScope(string roleName, string scope)
    {
        var rolePermissions = GetRolePermissions(roleName);
        return rolePermissions
            .Where(p => p.EndsWith($":{scope}", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Split(':')[0])
            .Distinct();
    }

    /// <summary>
    /// Gets all scopes that a role has for a specific resource
    /// </summary>
    /// <param name="roleName">Role name</param>
    /// <param name="resource">Resource name</param>
    /// <returns>List of available scopes</returns>
    public static IEnumerable<string> GetRoleScopesForResource(string roleName, string resource)
    {
        var rolePermissions = GetRolePermissions(roleName);
        var resourcePrefix = $"{resource}:";
        
        return rolePermissions
            .Where(p => p.StartsWith(resourcePrefix, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Substring(resourcePrefix.Length))
            .Distinct();
    }
}