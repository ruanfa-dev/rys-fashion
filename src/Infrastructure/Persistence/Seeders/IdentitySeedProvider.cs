using Core.Identity;

using Infrastructure.Persistence.Contexts;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

using UseCases.Common.Security.Authorization.Claims;
using UseCases.Common.Security.Authorization.Roles;

namespace Infrastructure.Persistence.Seeders;

public sealed class IdentitySeedProvider(IServiceProvider serviceProvider) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Log.Information("[IdentitySeed] Starting identity seeding");

        try
        {
            await EnsureAllPermissionsExistAsync(dbContext, cancellationToken);
            await EnsureAllRolesExistAsync(roleManager, cancellationToken);
            await SeedUsersPerRoleAsync(userManager, roleManager, cancellationToken);
            await AssignAllPermissionsToSystemAdminAsync(roleManager, cancellationToken);
            Log.Information("[IdentitySeed] Seeding completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[IdentitySeed] Seeding failed");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureAllPermissionsExistAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        Log.Information("[IdentitySeed:Permissions] Ensuring all permissions exist in database");

        // Get all predefined permissions from the Feature class
        var allPermissions = UseCases.Common.Security.Authorization.Permissions.Feature.Permissions;
        
        // Get existing permissions from database
        var existingPermissionNames = await dbContext.Permissions
            .Select(p => p.Name)
            .ToHashSetAsync(cancellationToken);

        var permissionsToAdd = new List<Permission>();

        foreach (var permission in allPermissions)
        {
            if (!existingPermissionNames.Contains(permission.Name))
            {
                permissionsToAdd.Add(permission);
                Log.Information("[IdentitySeed:Permissions] Adding permission: {PermissionName}", permission.Name);
            }
        }

        if (permissionsToAdd.Count > 0)
        {
            await dbContext.Permissions.AddRangeAsync(permissionsToAdd, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            Log.Information("[IdentitySeed:Permissions] Added {Count} new permissions", permissionsToAdd.Count);
        }
        else
        {
            Log.Information("[IdentitySeed:Permissions] All permissions already exist");
        }
    }

    private static async Task EnsureAllRolesExistAsync(RoleManager<Role> roleManager, CancellationToken cancellationToken)
    {
        Log.Information("[IdentitySeed:Roles] Ensuring all roles exist");

        var allRoleNames = DefaultRole.SystemRoles.Concat(DefaultRole.StorefrontRoles).Distinct().ToList();

        foreach (var roleName in allRoleNames)
        {
            var existingRole = await roleManager.FindByNameAsync(roleName);
            if (existingRole == null)
            {
                var isSystemRole = DefaultRole.SystemRoles.Contains(roleName);
                var newRole = Role.Create(
                    name: roleName,
                    description: $"System role: {roleName}",
                    isSystemRole: isSystemRole
                );
                
                var result = await roleManager.CreateAsync(newRole);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    Log.Error("[IdentitySeed:Roles] Failed creating role {RoleName}: {Errors}", roleName, errors);
                }
                else
                {
                    Log.Information("[IdentitySeed:Roles] Created role {RoleName}", roleName);
                }
            }
        }

        Log.Information("[IdentitySeed:Roles] All roles ensured");
    }

    private static async Task SeedUsersPerRoleAsync(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        CancellationToken cancellationToken)
    {
        var allRoles = await roleManager.Roles.ToListAsync(cancellationToken);

        foreach (var role in allRoles)
        {
            var userEmail = $"{role.Name?.ToLowerInvariant()}@seeder.com";
            var userName = role.Name?.ToLowerInvariant();
            var password = "Seeder@123"; // Change for production!

            var user = await userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                user = User.Create(
                    email: userEmail,
                    emailConfirmed: true,
                    userName: userName);
                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    Log.Error("[IdentitySeed:Users] Failed creating user for role {RoleName}: {Errors}", role.Name, errors);
                    continue;
                }
                Log.Information("[IdentitySeed:Users] Created user {UserName} for role {RoleName}", userName, role.Name);
            }

            // Assign role to user if not already assigned
            if (!await userManager.IsInRoleAsync(user, role.Name!))
            {
                await userManager.AddToRoleAsync(user, role.Name!);
                Log.Information("[IdentitySeed:Users] Assigned role {RoleName} to user {UserName}", role.Name, userName);
            }
        }
    }

    private static async Task AssignAllPermissionsToSystemAdminAsync(
        RoleManager<Role> roleManager,
        CancellationToken cancellationToken)
    {
        const string systemAdminRoleName = DefaultRole.Admin;
        var systemAdminRole = await roleManager.FindByNameAsync(systemAdminRoleName);

        if (systemAdminRole == null)
        {
            Log.Warning("[IdentitySeed:Permissions] System admin role '{RoleName}' not found", systemAdminRoleName);
            return;
        }

        // Get all permissions from Feature class
        var allPermissions = UseCases.Common.Security.Authorization.Permissions.Feature.Permissions;
        var existingClaims = await roleManager.GetClaimsAsync(systemAdminRole);

        var addedCount = 0;
        foreach (var permission in allPermissions)
        {
            // Check if permission claim already exists for this role
            if (!existingClaims.Any(c => c.Type == CustomClaim.Permission && c.Value == permission.Name))
            {
                var claim = new System.Security.Claims.Claim(CustomClaim.Permission, permission.Name);
                await roleManager.AddClaimAsync(systemAdminRole, claim);
                addedCount++;
            }
        }

        Log.Information("[IdentitySeed:Permissions] Assigned {AddedCount} permissions to system admin role (total: {TotalCount})", 
            addedCount, allPermissions.Length);
    }
}