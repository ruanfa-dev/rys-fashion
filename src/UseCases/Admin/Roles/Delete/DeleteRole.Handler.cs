using ErrorOr;

using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Services;
using UseCases.Common.Security.Authorization.Templates;

namespace UseCases.Admin.Roles.Delete;

public static partial class DeleteRole
{
    public sealed record Command(Guid Id) : ICommand<Deleted>;

    public sealed class Handler(
        IKeycloakAdminService keycloakAdminService,
        ILogger<Handler> logger
    ) : ICommandHandler<Command, Deleted>
    {
        public async Task<ErrorOr<Deleted>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                // First, get all roles to find the role by ID
                var allRoles = await keycloakAdminService.GetRolesAsync(cancellationToken);
                var role = allRoles.FirstOrDefault(r => r.Id == request.Id.ToString());
                
                if (role == null)
                {
                    logger.LogWarning("Role {RoleId} not found in Keycloak", request.Id);
                    return Error.NotFound("Role.NotFound", $"Role with ID '{request.Id}' not found");
                }

                var roleName = role.Name;

                // Check if it's a system role that should not be deleted
                var roleTemplate = RolePermissionTemplates.GetRoleTemplate(roleName);
                if (roleTemplate != null)
                {
                    logger.LogWarning("Attempted to delete system role {RoleName}", roleName);
                    return Error.Conflict("Role.CannotDeleteSystemRole", $"Cannot delete system role '{roleName}'");
                }

                // Check if role is in use by getting all users and checking their roles
                var usersWithRole = await GetUsersWithRoleAsync(roleName, cancellationToken);
                if (usersWithRole > 0)
                {
                    logger.LogWarning("Cannot delete role {RoleName} as it is assigned to {UserCount} users", 
                        roleName, usersWithRole);
                    return Error.Conflict("Role.InUse", 
                        $"Cannot delete role '{roleName}' as it is assigned to {usersWithRole} user(s)");
                }

                // Delete the role from Keycloak
                await keycloakAdminService.DeleteRoleAsync(roleName, cancellationToken);

                logger.LogInformation("Successfully deleted role {RoleId} with name {RoleName}", 
                    request.Id, roleName);

                return Result.Deleted;
            }
            catch (NotSupportedException ex)
            {
                logger.LogWarning("Keycloak Admin API not configured: {Message}", ex.Message);
                return Error.Failure("Role.NotConfigured", "Keycloak role management is not properly configured");
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("404"))
            {
                logger.LogWarning("Role {RoleId} not found in Keycloak during deletion", request.Id);
                return Error.NotFound("Role.NotFound", $"Role with ID '{request.Id}' not found");
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "HTTP error deleting role {RoleId} from Keycloak", request.Id);
                return Error.Failure("Role.DeletionFailed", "Failed to delete role from Keycloak due to network error");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error deleting role {RoleId}", request.Id);
                return Error.Failure("Role.UnexpectedError", "An unexpected error occurred while deleting the role");
            }
        }

        /// <summary>
        /// Gets the count of users assigned to a specific role
        /// </summary>
        private async Task<int> GetUsersWithRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            try
            {
                var allUsers = await keycloakAdminService.GetUsersAsync(cancellationToken);
                var usersWithRole = 0;

                foreach (var user in allUsers)
                {
                    try
                    {
                        var userRoles = await keycloakAdminService.GetUserRolesAsync(user.Id, cancellationToken);
                        if (userRoles.Any(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase)))
                        {
                            usersWithRole++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail the entire operation for individual user role check failures
                        logger.LogWarning(ex, "Failed to get roles for user {UserId} while checking role usage", user.Id);
                    }
                }

                return usersWithRole;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to get user count for role {RoleName}, assuming role is in use", roleName);
                return 1; // Return 1 to prevent deletion if we can't check
            }
        }
    }
}