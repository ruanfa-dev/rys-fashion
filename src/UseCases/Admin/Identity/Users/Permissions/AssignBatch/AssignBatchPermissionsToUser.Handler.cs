using System.Security.Claims;

using Core.Identity.Roles;
using Core.Identity.Users;

using ErrorOr;

using FluentValidation;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Identity.Permissions.Common;
using UseCases.Common.Persistence.Context;
using UseCases.Common.Security.Authentication.Contexts;
using UseCases.Common.Security.Authorization.Claims;

namespace UseCases.Admin.Identity.Users.Permissions.AssignBatch;

public static partial class AssignBatchPermissionsToUser
{
    public sealed record Param : BatchPermissionsParam;
    
    public sealed record Result
    {
        public required Guid UserId { get; init; }
        public required string[] Permissions { get; init; }
        public required string Message { get; init; }
        public required string[] AllUserPermissions { get; init; }
        public DateTimeOffset AssignedAt { get; init; } = DateTimeOffset.UtcNow;
        public string? AssignedBy { get; init; }
        public string? PermissionDescription { get; init; }
        public bool IsSystemPermission { get; init; }
    }
    
    public sealed record Command(Guid UserId, Param Param) : ICommand<Result>;

    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            // UserId 
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithErrorCode(User.Errors.UserIdRequired.Code)
                .WithMessage(User.Errors.UserIdRequired.Description);

            // Param
            RuleFor(x => x.Param)
                .SetValidator(new BatchPermissionsParamValidator());
        }
    }

    public sealed class Handler(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IUnitOfWork unitOfWork,
        IUserContext userContext,
        ILogger<Handler> logger
    ) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            Param param = request.Param;

            try
            {
                // Check: User exists
                User? user = await userManager.FindByIdAsync(request.UserId.ToString());
                if (user == null)
                    return User.Errors.UserNotFound;

                // Begin transaction for all operations
                await unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    // Get user's roles and their permission claims
                    IList<string> userRoles = await userManager.GetRolesAsync(user);
                    HashSet<string> rolePermissionClaims = await GetRolePermissionClaimsAsync(userRoles);

                    // Get current direct user permission claims (not from roles)
                    IList<Claim> currentUserClaims = await userManager.GetClaimsAsync(user);
                    HashSet<string> currentDirectPermissionClaims = currentUserClaims
                        .Where(c => c.Type.Equals(CustomClaim.Permission, StringComparison.OrdinalIgnoreCase))
                        .Select(c => c.Value)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    HashSet<string> targetPermissionSet = param.Permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);

                    // 🔒 SECURITY: Filter out permissions that already exist in roles (prevent duplicates)
                    HashSet<string> permissionsNotInRoles = targetPermissionSet
                        .Except(rolePermissionClaims, StringComparer.OrdinalIgnoreCase)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    // Calculate claims to add and remove from direct user claims
                    List<string> permissionsToAdd = permissionsNotInRoles
                        .Except(currentDirectPermissionClaims, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    List<string> permissionsToRemove = currentDirectPermissionClaims
                        .Except(targetPermissionSet, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    // Log permissions already available through roles
                    List<string> duplicateRolePermissions = targetPermissionSet
                        .Intersect(rolePermissionClaims, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    
                    if (duplicateRolePermissions.Count > 0)
                    {
                        logger.LogInformation("🔒 Security Filter: Skipping {Count} permission(s) for user {UserId} as they already exist in roles: {Permissions}",
                            duplicateRolePermissions.Count, user.Id, string.Join(", ", duplicateRolePermissions));
                    }

                    // Remove direct permission claims that are no longer needed
                    if (permissionsToRemove.Count > 0)
                    {
                        List<Claim> claimsToRemove = currentUserClaims
                            .Where(c => c.Type.Equals(CustomClaim.Permission, StringComparison.OrdinalIgnoreCase) &&
                                       permissionsToRemove.Contains(c.Value))
                            .ToList();

                        foreach (Claim claimToRemove in claimsToRemove)
                        {
                            IdentityResult removeResult = await userManager.RemoveClaimAsync(user, claimToRemove);
                            if (!removeResult.Succeeded)
                            {
                                string errors = string.Join("; ", removeResult.Errors.Select(e => e.Description));
                                logger.LogError("Failed to remove permission claim {Permission} from user {UserId}: {Errors}",
                                    claimToRemove.Value, user.Id, errors);
                                return UserPermission.Errors.RemovalFailed(claimToRemove.Value);
                            }
                        }

                        logger.LogInformation("Successfully removed {RemovedCount} direct permission claim(s) from user {UserId}: {Permissions}",
                            permissionsToRemove.Count, user.Id, string.Join(", ", permissionsToRemove));
                    }

                    // Add new direct permission claims (only those not in roles)
                    if (permissionsToAdd.Count > 0)
                    {
                        List<Claim> claimsToAdd = permissionsToAdd
                            .Select(permission => new Claim(CustomClaim.Permission, permission))
                            .ToList();

                        foreach (Claim claimToAdd in claimsToAdd)
                        {
                            IdentityResult addResult = await userManager.AddClaimAsync(user, claimToAdd);
                            if (!addResult.Succeeded)
                            {
                                string errors = string.Join("; ", addResult.Errors.Select(e => e.Description));
                                logger.LogError("Failed to add permission claim {Permission} to user {UserId}: {Errors}",
                                    claimToAdd.Value, user.Id, errors);
                                return UserPermission.Errors.AssignmentFailed(claimToAdd.Value);
                            }
                        }

                        logger.LogInformation("Successfully added {AddedCount} direct permission claim(s) to user {UserId}: {Permissions}",
                            permissionsToAdd.Count, user.Id, string.Join(", ", permissionsToAdd));
                    }

                    // Get final effective user permissions (roles + direct claims)
                    IList<Claim> finalUserClaims = await userManager.GetClaimsAsync(user);
                    string[] finalDirectPermissions = finalUserClaims
                        .Where(c => c.Type.Equals(CustomClaim.Permission, StringComparison.OrdinalIgnoreCase))
                        .Select(c => c.Value)
                        .ToArray();

                    // Combine role permissions and direct permissions for the result
                    string[] allEffectivePermissions = rolePermissionClaims
                        .Union(finalDirectPermissions, StringComparer.OrdinalIgnoreCase)
                        .OrderBy(p => p)
                        .ToArray();

                    string message = BuildResultMessage(permissionsToAdd.Count, permissionsToRemove.Count, duplicateRolePermissions.Count);

                    // Commit transaction if all operations succeeded
                    await unitOfWork.CommitTransactionAsync(cancellationToken);

                    logger.LogInformation("🔒 Secure permission assignment completed for user {UserId}: {AddedCount} added, {RemovedCount} removed, {SkippedCount} skipped (from roles)",
                        user.Id, permissionsToAdd.Count, permissionsToRemove.Count, duplicateRolePermissions.Count);

                    return new Result
                    {
                        UserId = user.Id,
                        Permissions = param.Permissions,
                        Message = message,
                        AllUserPermissions = allEffectivePermissions,
                        AssignedAt = DateTimeOffset.UtcNow,
                        AssignedBy = userContext.UserName,
                        PermissionDescription = $"Secure batch assignment: {permissionsToAdd.Count} direct claims added, {duplicateRolePermissions.Count} inherited from roles",
                        IsSystemPermission = false
                    };
                }
                catch (Exception)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during secure permission claims assignment for user {UserId}", request.UserId);
                return UserPermission.Errors.UnexpectedError;
            }
        }

        /// <summary>
        /// 🔒 Security Method: Gets all permission claims that exist in the user's roles
        /// This prevents duplicate claim assignment and maintains clean security model
        /// </summary>
        private async Task<HashSet<string>> GetRolePermissionClaimsAsync(IList<string> roleNames)
        {
            HashSet<string> rolePermissionClaims = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (roleNames.Count == 0)
                return rolePermissionClaims;

            // Get all roles and their claims efficiently
            List<Role> roles = await roleManager.Roles
                .Where(r => roleNames.Contains(r.Name!))
                .ToListAsync();

            foreach (Role role in roles)
            {
                IList<Claim> roleClaims = await roleManager.GetClaimsAsync(role);
                IEnumerable<string> permissionClaims = roleClaims
                    .Where(c => c.Type.Equals(CustomClaim.Permission, StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.Value);

                foreach (string claim in permissionClaims)
                {
                    rolePermissionClaims.Add(claim);
                }
            }

            logger.LogDebug("Retrieved {Count} unique permission claims from {RoleCount} roles for security filtering", 
                rolePermissionClaims.Count, roles.Count);

            return rolePermissionClaims;
        }

        private static string BuildResultMessage(int addedCount, int removedCount, int skippedCount)
        {
            return (addedCount, removedCount, skippedCount) switch
            {
                (0, 0, 0) => "User permission claims already match the target configuration",
                (> 0, 0, 0) => $"Successfully assigned {addedCount} permission claim(s)",
                (0, > 0, 0) => $"Successfully removed {removedCount} permission claim(s)",
                (> 0, > 0, 0) => $"Successfully updated permission claims: {addedCount} added, {removedCount} removed",
                (0, 0, > 0) => $"All {skippedCount} permission(s) already exist in user roles - no direct claims needed",
                (> 0, 0, > 0) => $"Assigned {addedCount} direct claim(s), {skippedCount} inherited from roles",
                (0, > 0, > 0) => $"Removed {removedCount} direct claim(s), {skippedCount} inherited from roles",
                (> 0, > 0, > 0) => $"Updated claims: {addedCount} added, {removedCount} removed, {skippedCount} inherited from roles",
                _ => $"Permission claims operation completed: {addedCount} added, {removedCount} removed, {skippedCount} skipped"
            };
        }
    }
}