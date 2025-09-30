using System.Security.Claims;

using Core.Identity.Permissions;
using Core.Identity.Roles;
using Core.Identity.Users;

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
        using IServiceScope scope = serviceProvider.CreateScope();
        RoleManager<Role> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        UserManager<User> userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

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
        Permission[] allPermissions = UseCases.Common.Security.Authorization.Permissions.Feature.Permissions ?? Array.Empty<Permission>();

        // Get existing permission names from database (normalize to lower-case for comparison)
        HashSet<string> existingPermissionNames = await dbContext.Permissions
            .Select(p => p.Name.ToLowerInvariant())
            .ToHashSetAsync(cancellationToken);

        // Deduplicate the source permissions by name (case-insensitive) to avoid adding the same logical
        // permission multiple times. Also create fresh Permission instances when inserting so the
        // in-memory provider doesn't see duplicated primary keys from any shared/static instances.
        List<Permission> permissionsToAdd = allPermissions
            .GroupBy(p => (p.Name).ToLowerInvariant())
            .Select(g => g.First())
            .Where(p => !existingPermissionNames.Contains((p.Name ?? string.Empty).ToLowerInvariant()))
            .Select(p => Permission.Create(p.Area, p.Resource, p.Action, p.Description, p.DisplayName))
            .ToList();

        if (permissionsToAdd.Count > 0)
        {
            foreach (Permission permission in permissionsToAdd)
            {
                Log.Information("[IdentitySeed:Permissions] Adding permission: {PermissionName}", permission.Name);
            }

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

        List<string> allRoleNames = DefaultRole.SystemRoles.Concat(DefaultRole.StorefrontRoles).Distinct().ToList();

        foreach (string roleName in allRoleNames)
        {
            Role? existingRole = await roleManager.FindByNameAsync(roleName);
            if (existingRole == null)
            {
                bool isSystemRole = DefaultRole.SystemRoles.Contains(roleName);
                Role newRole = Role.Create(
                    name: roleName,
                    description: $"System role: {roleName}",
                    isSystemRole: isSystemRole
                );
                
                IdentityResult result = await roleManager.CreateAsync(newRole);
                if (!result.Succeeded)
                {
                    string errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    Log.Error("[IdentitySeed:Roles] Failed creating role {RoleName}: {Errors}", roleName, errors);
                }
                else
                {
                    Log.Information("[IdentitySeed:Roles] Created role {RoleName}", roleName);
                }
            }
        }

        Log.Information("[IdentitySeed:Roles] All roles ensured");

        // Deduplicate roles by NormalizedName to avoid multiple entries that break
        // Identity APIs (SingleOrDefault queries). This can happen in in-memory DB
        // scenarios when seeders run multiple times across test host lifecycles.
        try
        {
            List<Role> allRolesList = await roleManager.Roles.ToListAsync(cancellationToken);
            List<IGrouping<string?, Role>> duplicates = allRolesList
                .GroupBy(r => r.NormalizedName)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (IGrouping<string?, Role> dupGroup in duplicates)
            {
                // Keep the first and delete the rest
                Role keep = dupGroup.First();
                foreach (Role remove in dupGroup.Skip(1))
                {
                    IdentityResult delResult = await roleManager.DeleteAsync(remove);
                    if (!delResult.Succeeded)
                    {
                        Log.Warning("[IdentitySeed:Roles] Failed to remove duplicate role {RoleName}: {Errors}", remove.Name, string.Join(";", delResult.Errors.Select(e => e.Description)));
                    }
                    else
                    {
                        Log.Information("[IdentitySeed:Roles] Removed duplicate role entry {RoleName}", remove.Name);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "[IdentitySeed:Roles] Could not deduplicate roles: {Message}", ex.Message);
        }
    }

    private static async Task SeedUsersPerRoleAsync(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        CancellationToken cancellationToken)
    {
        List<Role> allRoles = await roleManager.Roles.ToListAsync(cancellationToken);

        foreach (Role role in allRoles)
        {
            string userEmail = $"{role.Name?.ToLowerInvariant()}@seeder.com";
            string? userName = role.Name?.ToLowerInvariant();
            string password = "Seeder@123"; // Change for production!

            User? user = await userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                user = User.Create(
                    email: userEmail,
                    emailConfirmed: true,
                    userName: userName);
                IdentityResult result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    string errors = string.Join("; ", result.Errors.Select(e => e.Description));
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
        Role? systemAdminRole = await roleManager.FindByNameAsync(systemAdminRoleName);

        if (systemAdminRole == null)
        {
            Log.Warning("[IdentitySeed:Permissions] System admin role '{RoleName}' not found", systemAdminRoleName);
            return;
        }

        // Get all permissions from Feature class
        Permission[] allPermissions = UseCases.Common.Security.Authorization.Permissions.Feature.Permissions;
        IList<Claim> existingClaims = await roleManager.GetClaimsAsync(systemAdminRole);

        int addedCount = 0;
        foreach (Permission permission in allPermissions)
        {
            // Check if permission claim already exists for this role
            if (!existingClaims.Any(c => c.Type == CustomClaim.Permission && c.Value == permission.Name))
            {
                Claim claim = new System.Security.Claims.Claim(CustomClaim.Permission, permission.Name);
                await roleManager.AddClaimAsync(systemAdminRole, claim);
                addedCount++;
            }
        }

        Log.Information("[IdentitySeed:Permissions] Assigned {AddedCount} permissions to system admin role (total: {TotalCount})", 
            addedCount, allPermissions.Length);
    }
}