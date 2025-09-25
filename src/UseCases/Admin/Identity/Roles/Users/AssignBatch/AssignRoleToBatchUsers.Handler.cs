using Core.Identity;

using ErrorOr;

using FluentValidation;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Identity.Users.Common;
using UseCases.Common.Persistence.Context;
using UseCases.Common.Security.Authentication.Contexts;

namespace UseCases.Admin.Roles.Users.AssignBatch;

public static partial class AssignRoleToBatchUsers
{
    public sealed record Param : BatchUserParam;

    public sealed class ParamValidator : AbstractValidator<Param>
    {
        public ParamValidator()
        {
            Include(new BatchUserParamValidator());

            RuleFor(x => x.UserIds)
                .NotNull()
                .WithErrorCode(UserRole.Errors.UserIdsRequired.Code)
                .WithMessage(UserRole.Errors.UserIdsRequired.Description)
                .Must(ids => ids.Length <= UserRole.Constraints.MaxUsersPerRole)
                .WithErrorCode(UserRole.Errors.MaxUsersExceeded.Code)
                .WithMessage(UserRole.Errors.MaxUsersExceeded.Description)
                .Must(ids => ids.Distinct().Count() == ids.Length)
                .WithErrorCode(UserRole.Errors.DuplicateUsers.Code)
                .WithMessage(UserRole.Errors.DuplicateUsers.Description);
        }
    }

    public sealed record Result : BatchRoleUserResult;
    public sealed record Command(Guid RoleId, Param Param) : ICommand<Result>;

    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.RoleId)
                .NotEmpty()
                .WithErrorCode(Role.Errors.RoleIdRequired.Code)
                .WithMessage(Role.Errors.RoleIdRequired.Description);

            RuleFor(x => x.Param)
                .SetValidator(new ParamValidator());
        }
    }

    public sealed class Handler(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IUserContext userContext,
        IUnitOfWork unitOfWork,
        ILogger<Handler> logger
    ) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            var param = request.Param;
            var roleId = request.RoleId;

            try
            {
                // Validate role exists upfront
                var role = await roleManager.FindByIdAsync(roleId.ToString());
                if (role == null)
                {
                    logger.LogWarning("Role not found: {RoleId}", roleId);
                    return Role.Errors.RoleNotFound(roleId.ToString());
                }

                // 🚀 Performance: Batch load all users at once instead of one by one
                var users = await BatchLoadUsersAsync(param.UserIds);
                if (users.IsError)
                    return users.Errors;

                var validUsers = users.Value;

                // Begin transaction for all operations
                await unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    // 🔒 Security: Process assignments with detailed tracking
                    var (assignedUsers, skippedUsers) = await ProcessRoleAssignmentsAsync(
                        validUsers, role, cancellationToken);

                    // All operations succeeded - commit transaction
                    await unitOfWork.CommitTransactionAsync(cancellationToken);

                    var message = BuildResultMessage(assignedUsers.Count, skippedUsers.Count, role.Name!);

                    logger.LogInformation("🔒 Batch role assignment completed for role {RoleId} ({RoleName}): {AssignedCount} assigned, {SkippedCount} skipped",
                        roleId, role.Name, assignedUsers.Count, skippedUsers.Count);

                    return new Result
                    {
                        Message = message,
                        RoleId = role.Id,
                        RoleName = role.Name,
                        UserIds = param.UserIds,
                        AssignedAt = DateTimeOffset.UtcNow,
                        AssignedBy = userContext.UserName
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
                logger.LogError(ex, "Unexpected error during batch role assignment for role {RoleId}", roleId);
                return UserRole.Errors.UnexpectedError;
            }
        }

        /// <summary>
        /// 🚀 Performance: Batch loads all users at once for better performance
        /// </summary>
        private async Task<ErrorOr<List<User>>> BatchLoadUsersAsync(Guid[] userIds)
        {
            try
            {
                // Load all users in a single database query
                var users = await userManager.Users
                    .Where(u => userIds.Contains(u.Id))
                    .ToListAsync();

                // Check if all requested users were found
                var foundUserIds = users.Select(u => u.Id).ToHashSet();
                var missingUserIds = userIds.Except(foundUserIds).ToList();

                if (missingUserIds.Count > 0)
                {
                    logger.LogWarning("Users not found during batch load: {MissingUserIds}",
                        string.Join(", ", missingUserIds));
                    return User.Errors.UserNotFound;
                }

                logger.LogDebug("Successfully batch loaded {UserCount} users", users.Count);
                return users;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during batch user loading for IDs: {UserIds}",
                    string.Join(", ", userIds));
                return UserRole.Errors.UnexpectedError;
            }
        }

        /// <summary>
        /// 🔒 Security: Processes role assignments with detailed tracking and prevents duplicates
        /// </summary>
        private async Task<(List<User> AssignedUsers, List<User> SkippedUsers)> ProcessRoleAssignmentsAsync(
            List<User> users, Role role, CancellationToken cancellationToken)
        {
            var assignedUsers = new List<User>();
            var skippedUsers = new List<User>();

            foreach (var user in users)
            {
                // 🔒 Security: Check if user already has the role to prevent unnecessary operations
                var hasRole = await userManager.IsInRoleAsync(user, role.Name!);

                if (hasRole)
                {
                    skippedUsers.Add(user);
                    logger.LogDebug("Skipping role assignment for user {UserId} - already has role {RoleName}",
                        user.Id, role.Name);
                    continue;
                }

                // Assign the role
                var result = await userManager.AddToRoleAsync(user, role.Name!);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    logger.LogError("Failed to assign role {RoleName} to user {UserId}: {Errors}",
                        role.Name, user.Id, errors);

                    // Throw exception to trigger rollback
                    throw new InvalidOperationException($"Failed to assign role '{role.Name}' to user {user.Id}: {errors}");
                }

                assignedUsers.Add(user);
                logger.LogDebug("Successfully assigned role {RoleName} to user {UserId}",
                    role.Name, user.Id);
            }

            if (assignedUsers.Count > 0)
            {
                logger.LogInformation("Successfully assigned role {RoleName} to {AssignedCount} users: {UserIds}",
                    role.Name, assignedUsers.Count,
                    string.Join(", ", assignedUsers.Select(u => u.Id)));
            }

            if (skippedUsers.Count > 0)
            {
                logger.LogInformation("Skipped {SkippedCount} users who already have role {RoleName}: {UserIds}",
                    skippedUsers.Count, role.Name,
                    string.Join(", ", skippedUsers.Select(u => u.Id)));
            }

            return (assignedUsers, skippedUsers);
        }

        /// <summary>
        /// Builds appropriate result message based on assignment outcomes
        /// </summary>
        private static string BuildResultMessage(int assignedCount, int skippedCount, string roleName)
        {
            return (assignedCount, skippedCount) switch
            {
                (0, 0) => $"No users processed for role '{roleName}'",
                (> 0, 0) => $"Successfully assigned role '{roleName}' to {assignedCount} user(s)",
                (0, > 0) => $"All {skippedCount} user(s) already have role '{roleName}' - no changes needed",
                (> 0, > 0) => $"Role '{roleName}' assignment completed: {assignedCount} assigned, {skippedCount} already had role",
                _ => $"Role '{roleName}' assignment completed: {assignedCount} assigned, {skippedCount} skipped"
            };
        }
    }
}