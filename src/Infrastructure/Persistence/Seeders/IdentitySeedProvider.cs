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

        Log.Information("[IdentitySeed] Starting role and user seeding");

        try
        {
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
                var newRole = new Role
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                    IsSystemRole = isSystemRole
                };
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
                user = new User
                {
                    UserName = userName,
                    Email = userEmail,
                    EmailConfirmed = true
                };
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

        // Assign all feature permissions as claims to the system admin role
        var existingClaims = await roleManager.GetClaimsAsync(systemAdminRole);
        var allPermissions = UseCases.Common.Security.Authorization.Permissions.Feature.All;

        foreach (var permission in allPermissions)
        {
            if (!existingClaims.Any(c => c.Type == CustomClaim.Permission && c.Value == permission))
            {
                await roleManager.AddClaimAsync(systemAdminRole, new System.Security.Claims.Claim(CustomClaim.Permission, permission));
            }
        }

        Log.Information("[IdentitySeed:Permissions] Assigned all feature permissions to system admin role");
    }
}