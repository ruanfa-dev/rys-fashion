using Core.Identity.Roles;
using Core.Identity.Users;

using ErrorOr;

using FluentValidation;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Identity.Roles.Common;
using UseCases.Common.Persistence.Context;
using UseCases.Common.Security.Authentication.Contexts;

namespace UseCases.Admin.Identity.Users.Roles.AssignBatch;

public static partial class AssignBatchRolesToUser
{
    public sealed record Param : BatchRolesParam;
    public sealed record Result : BatchUserRoleResult;
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
                .SetValidator(new BatchRolesParamValidator());
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
            try
            {
                // Check: User exists
                var user = await userManager.FindByIdAsync(request.UserId.ToString());
                if (user == null)
                    return User.Errors.UserNotFound;

                // Validate all roles exist upfront and get their names
                var roleNames = new List<string>();
                var nonExistentRoles = new List<Error>();

                foreach (var roleId in param.RoleIds)
                {
                    var role = await roleManager.FindByIdAsync(roleId);
                    if (role == null)
                    {
                        nonExistentRoles.Add(Role.Errors.RoleNotFound(roleId));
                    }
                    else
                    {
                        roleNames.Add(role.Name!);
                    }
                }

                if (nonExistentRoles.Count > 0)
                {
                    logger.LogWarning("Roles not found when processing assignment for user {UserId}: {RoleIds}",
                        request.UserId, string.Join(", ", param.RoleIds));
                    return nonExistentRoles;
                }

                // Begin: transaction for all operations
                await unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    // Get current user roles
                    var currentUserRoles = await userManager.GetRolesAsync(user);
                    var currentRoleSet = currentUserRoles.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var targetRoleSet = roleNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

                    // Calculate roles to add and remove
                    var rolesToAdd = targetRoleSet.Except(currentRoleSet, StringComparer.OrdinalIgnoreCase).ToList();
                    var rolesToRemove = currentRoleSet.Except(targetRoleSet, StringComparer.OrdinalIgnoreCase).ToList();

                    // Remove roles not in target list first
                    if (rolesToRemove.Count > 0)
                    {
                        var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
                        if (!removeResult.Succeeded)
                        {
                            var errors = string.Join("; ", removeResult.Errors.Select(e => e.Description));
                            var roleList = string.Join(", ", rolesToRemove);
                            logger.LogError("Failed to remove roles {RoleNames} from user {UserId}: {Errors}",
                                roleList, user.Id, errors);
                            return UserRole.Errors.RemovalFailed(roleList);
                        }

                        logger.LogInformation("Successfully removed {RemovedCount} role(s) from user {UserId}: {RoleNames}",
                            rolesToRemove.Count, user.Id, string.Join(", ", rolesToRemove));
                    }

                    // Add new roles
                    if (rolesToAdd.Count > 0)
                    {
                        var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
                        if (!addResult.Succeeded)
                        {
                            var errors = string.Join("; ", addResult.Errors.Select(e => e.Description));
                            var roleList = string.Join(", ", rolesToAdd);
                            logger.LogError("Failed to assign roles {RoleNames} to user {UserId}: {Errors}",
                                roleList, user.Id, errors);
                            return UserRole.Errors.AssignmentFailed(roleList);
                        }

                        logger.LogInformation("Successfully assigned {AddedCount} role(s) to user {UserId}: {RoleNames}",
                            rolesToAdd.Count, user.Id, string.Join(", ", rolesToAdd));
                    }

                    var finalUserRoles = await userManager.GetRolesAsync(user);
                    var message = BuildResultMessage(rolesToAdd.Count, rolesToRemove.Count);

                    // Commit transaction if all operations succeeded
                    await unitOfWork.CommitTransactionAsync(cancellationToken);

                    logger.LogInformation("Role replacement operation completed for user {UserId}: {AddedCount} added, {RemovedCount} removed",
                        user.Id, rolesToAdd.Count, rolesToRemove.Count);

                    return new Result
                    {
                        Message = message,
                        UserId = user.Id,
                        UserName = user.UserName,
                        RoleIds = [.. finalUserRoles],
                        AssignedAt = DateTimeOffset.UtcNow,
                        AssignedBy = userContext.UserName,
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
                logger.LogError(ex, "Unexpected error during role replacement operation for user {UserId}", request.UserId);
                return UserRole.Errors.UnexpectedError;
            }
        }

        private static string BuildResultMessage(int addedCount, int removedCount)
        {
            return (addedCount, removedCount) switch
            {
                (0, 0) => "User roles already match the target configuration",
                ( > 0, 0) => $"Successfully assigned {addedCount} role(s)",
                (0, > 0) => $"Successfully removed {removedCount} role(s)",
                ( > 0, > 0) => $"Successfully replaced user roles: {addedCount} added, {removedCount} removed",
                _ => throw new NotImplementedException()
            };
        }
    }
}