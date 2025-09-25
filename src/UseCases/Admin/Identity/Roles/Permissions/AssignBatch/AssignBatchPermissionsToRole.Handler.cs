using System.Security.Claims;

using Core.Identity;

using ErrorOr;

using FluentValidation;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Identity.Permissions.Common;
using UseCases.Common.Persistence.Context;
using UseCases.Common.Security.Authentication.Contexts;
using UseCases.Common.Security.Authorization.Claims;

namespace UseCases.Admin.Roles.Permissions.AssignBatch;

public static partial class AssignBatchPermissionsToRole
{
    public sealed record Param : BatchPermissionsParam;
    public sealed class ParamValidator : AbstractValidator<Param>
    {
        public ParamValidator()
        {
            RuleFor(x => x.Permissions)
                                .NotNull()
                .WithErrorCode(UserRole.Errors.UserIdsRequired.Code)
                .WithMessage(UserRole.Errors.UserIdsRequired.Description)
                .Must(ids => ids.Length <= UserRole.Constraints.MaxUsersPerRole)
                .WithErrorCode(RolePermission.Errors.MaxPermissionsExceeded.Code)
                .WithMessage(RolePermission.Errors.MaxPermissionsExceeded.Description)
                .Must(permissions => permissions.Distinct().Count() == permissions.Length)
                .WithErrorCode(RolePermission.Errors.DuplicatePermissionsInBatch.Code)
                .WithMessage(RolePermission.Errors.DuplicatePermissionsInBatch.Description);
        }
    }
    public sealed record Result : BatchRolePermissionResult;
    public sealed record Command(Guid RoleId, Param Param) : ICommand<Result>;

    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            // RoleId 
            RuleFor(x => x.RoleId)
                .NotEmpty()
                .WithErrorCode(Role.Errors.RoleIdRequired.Code)
                .WithMessage(Role.Errors.RoleIdRequired.Description);

            // Param
            RuleFor(x => x.Param)
                .SetValidator(new ParamValidator());
        }
    }

    public sealed class Handler(
        RoleManager<Role> roleManager,
        UserManager<User> userManager,
        IUnitOfWork unitOfWork,
        IUserContext userContext,
        ILogger<Handler> logger
    ) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            var param = request.Param;

            try
            {
                // Check: Role exists
                var role = await roleManager.FindByIdAsync(request.RoleId.ToString());
                if (role == null)
                    return Role.Errors.RoleNotFound(request.RoleId.ToString());

                // 🔒 Security: Check if this would affect too many users
                var affectedUsersCount = await GetUsersInRoleCountAsync(role.Name!, cancellationToken);
                if (affectedUsersCount > RolePermission.Constraints.MaxUsersAffectedByPermissionChange)
                {
                    return RolePermission.Errors.WouldAffectTooManyUsers(
                        affectedUsersCount,
                        RolePermission.Constraints.MaxUsersAffectedByPermissionChange);
                }

                // Begin transaction for all operations
                await unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    // Get current role permission claims
                    var currentClaims = await roleManager.GetClaimsAsync(role);
                    var currentPermissionClaims = currentClaims
                        .Where(c => c.Type.Equals(CustomClaim.Permission, StringComparison.OrdinalIgnoreCase))
                        .Select(c => c.Value)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var targetPermissionSet = param.Permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);

                    // Calculate claims to add and remove
                    var permissionsToAdd = targetPermissionSet
                        .Except(currentPermissionClaims, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    var permissionsToRemove = currentPermissionClaims
                        .Except(targetPermissionSet, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    // Remove permission claims not in target list first
                    if (permissionsToRemove.Count > 0)
                    {
                        var claimsToRemove = currentClaims
                            .Where(c => c.Type.Equals(CustomClaim.Permission, StringComparison.OrdinalIgnoreCase) &&
                                       permissionsToRemove.Contains(c.Value))
                            .ToList();

                        foreach (var claimToRemove in claimsToRemove)
                        {
                            var removeResult = await roleManager.RemoveClaimAsync(role, claimToRemove);
                            if (!removeResult.Succeeded)
                            {
                                var errors = string.Join("; ", removeResult.Errors.Select(e => e.Description));
                                logger.LogError("Failed to remove permission claim {Permission} from role {RoleId}: {Errors}",
                                    claimToRemove.Value, role.Id, errors);
                                return RolePermission.Errors.RemovalFailed(claimToRemove.Value);
                            }
                        }

                        logger.LogInformation("Successfully removed {RemovedCount} permission claim(s) from role {RoleId} ({RoleName}): {Permissions}",
                            permissionsToRemove.Count, role.Id, role.Name, string.Join(", ", permissionsToRemove));
                    }

                    // Add new permission claims
                    if (permissionsToAdd.Count > 0)
                    {
                        var claimsToAdd = permissionsToAdd
                            .Select(permission => new Claim(CustomClaim.Permission, permission))
                            .ToList();

                        foreach (var claimToAdd in claimsToAdd)
                        {
                            var addResult = await roleManager.AddClaimAsync(role, claimToAdd);
                            if (!addResult.Succeeded)
                            {
                                var errors = string.Join("; ", addResult.Errors.Select(e => e.Description));
                                logger.LogError("Failed to add permission claim {Permission} to role {RoleId}: {Errors}",
                                    claimToAdd.Value, role.Id, errors);
                                return RolePermission.Errors.AssignmentFailed(claimToAdd.Value);
                            }
                        }

                        logger.LogInformation("Successfully added {AddedCount} permission claim(s) to role {RoleId} ({RoleName}): {Permissions}",
                            permissionsToAdd.Count, role.Id, role.Name, string.Join(", ", permissionsToAdd));
                    }

                    // Get final role permission claims
                    var finalClaims = await roleManager.GetClaimsAsync(role);
                    var finalPermissions = finalClaims
                        .Where(c => c.Type.Equals(CustomClaim.Permission, StringComparison.OrdinalIgnoreCase))
                        .Select(c => c.Value)
                        .OrderBy(p => p)
                        .ToArray();

                    var message = BuildResultMessage(permissionsToAdd.Count, permissionsToRemove.Count, role.Name!);

                    // Commit transaction if all operations succeeded
                    await unitOfWork.CommitTransactionAsync(cancellationToken);

                    logger.LogInformation("🔒 Secure permission assignment completed for role {RoleId} ({RoleName}): {AddedCount} added, {RemovedCount} removed, affecting {AffectedUsersCount} users",
                        role.Id, role.Name, permissionsToAdd.Count, permissionsToRemove.Count, affectedUsersCount);

                    return new Result
                    {
                        RoleId = role.Id,
                        RoleName = role.Name!,
                        Permissions = finalPermissions,
                        Message = message,
                        AssignedAt = DateTimeOffset.UtcNow,
                        AssignedBy = userContext.UserName,
                        AffectedUsersCount = affectedUsersCount
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
                logger.LogError(ex, "Unexpected error during role permission claims assignment for role {RoleId}", request.RoleId);
                return RolePermission.Errors.UnexpectedError;
            }
        }

        /// <summary>
        /// 📊 Gets the count of users who have this role (to show impact of permission changes)
        /// </summary>
        private async Task<int> GetUsersInRoleCountAsync(string roleName, CancellationToken cancellationToken)
        {
            try
            {
                var usersInRole = await userManager.GetUsersInRoleAsync(roleName);
                return usersInRole.Count;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to get users count for role {RoleName}", roleName);
                return 0; // Return 0 if we can't get the count, don't fail the operation
            }
        }

        private static string BuildResultMessage(int addedCount, int removedCount, string roleName)
        {
            return (addedCount, removedCount) switch
            {
                (0, 0) => $"Role '{roleName}' permission claims already match the target configuration",
                ( > 0, 0) => $"Successfully assigned {addedCount} permission claim(s) to role '{roleName}'",
                (0, > 0) => $"Successfully removed {removedCount} permission claim(s) from role '{roleName}'",
                ( > 0, > 0) => $"Successfully updated role '{roleName}' permissions: {addedCount} added, {removedCount} removed",
                _ => $"Role '{roleName}' permission assignment completed with unexpected counts: {addedCount} added, {removedCount} removed"
            };
        }
    }
}