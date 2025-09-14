using Core.Identity;

using ErrorOr;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Accounts.Common;

namespace UseCases.Admin.Roles.Delete;

public static partial class DeleteRole
{
    public sealed record Command(Guid Id) : ICommand<Deleted>;

    public sealed class Handler(
        RoleManager<Role> roleManager,
        UserManager<User> userManager,
        ILogger<Handler> logger
    ) : ICommandHandler<Command, Deleted>
    {
        public async Task<ErrorOr<Deleted>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var role = await roleManager.FindByIdAsync(request.Id.ToString());
                if (role == null)
                    return Role.Errors.RoleNotFound(Name);

                // Check: if it's a default role
                if (role.IsDefault)
                    return Role.Errors.CannotDeleteDefaultRole(role.Name!);

                // Check: if role is in use
                var usersInRole = await userManager.GetUsersInRoleAsync(role.Name!);
                if (usersInRole.Count > 0)
                    return Role.Errors.RoleInUse(role.Name!);

                // Store role info for response before deletion
                var roleName = role.Name!;
                var roleId = role.Id;

                // Delete the role
                var result = await roleManager.DeleteAsync(role);
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    logger.LogError("Failed to delete role {RoleId}: {Errors}", request.Id, errors);
                    return result.Errors.ToApplicationResult(prefix: "Role", fallbackCode: "DeletionFailed");
                }

                logger.LogInformation("Successfully deleted role {RoleId} with name {RoleName}", roleId, roleName);

                return Result.Deleted;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error deleting role {RoleId}", request.Id);
                return Role.Errors.UnexpectedError("deleting");
            }
        }
    }
}