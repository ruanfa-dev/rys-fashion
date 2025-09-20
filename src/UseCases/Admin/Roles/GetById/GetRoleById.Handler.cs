using ErrorOr;

using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;
using SharedKernel.Models.Filter;
using SharedKernel.Models.PagedLists;
using SharedKernel.Models.Queries;
using SharedKernel.Models.Paging;

using UseCases.Admin.Roles.Common;
using UseCases.Common.Security.Authentication.Services;
using UseCases.Common.Security.Authorization.Templates;

namespace UseCases.Admin.Roles.GetById;

public static partial class GetRoleById
{
    public sealed record Param : QueryParams;
    public sealed record Result : RoleDetailedResult;

    public sealed record Query(Guid Id, Param Param) : IQuery<Result>;

    public sealed class Handler(
        IKeycloakAdminService keycloakAdminService,
        ILogger<Handler> logger
    ) : IQueryHandler<Query, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var param = request.Param;

                // Get all roles from Keycloak to find the role by ID
                var allRoles = await keycloakAdminService.GetRolesAsync(cancellationToken);
                var role = allRoles.FirstOrDefault(r => r.Id == request.Id.ToString());
                
                if (role == null)
                {
                    logger.LogWarning("Role {RoleId} not found in Keycloak", request.Id);
                    return Error.NotFound("Role.NotFound", $"Role with ID '{request.Id}' not found");
                }

                // Get role template to get permissions and determine role properties
                var roleTemplate = RolePermissionTemplates.GetRoleTemplate(role.Name);
                var permissions = roleTemplate?.Permissions ?? Array.Empty<string>();
                var isSystemRole = roleTemplate != null;
                var isDefault = IsDefaultRole(role.Name);

                // Get users in role with pagination
                var usersInRole = await GetUsersInRoleAsync(role.Name, param, cancellationToken);

                var result = new Result
                {
                    Id = request.Id,
                    Name = role.Name,
                    Priority = GetRolePriority(role) ?? 0, // Default to 0 if not found
                    Description = role.Description,
                    IsDefault = isDefault,
                    IsSystemRole = isSystemRole,
                    CreatedAt = DateTimeOffset.UtcNow, // Keycloak doesn't provide creation time by default
                    UpdatedAt = DateTimeOffset.UtcNow, // Keycloak doesn't provide update time by default
                    CreatedBy = "System", // Keycloak doesn't track creator
                    UpdatedBy = "System", // Keycloak doesn't track updater
                    UserCount = usersInRole.TotalCount,
                    PermissionCount = permissions.Length,
                    Permissions = permissions,
                    Users = usersInRole
                };

                logger.LogDebug("Retrieved role {RoleId} with {UserCount} users and {PermissionCount} permissions",
                    request.Id, usersInRole.TotalCount, permissions.Length);

                return result;
            }
            catch (NotSupportedException ex)
            {
                logger.LogWarning("Keycloak Admin API not configured: {Message}", ex.Message);
                return Error.Failure("Role.NotConfigured", "Keycloak role management is not properly configured");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving role {RoleId} from Keycloak", request.Id);
                return Error.Failure("Role.RetrievalFailed", "Failed to retrieve role details from Keycloak");
            }
        }

        /// <summary>
        /// Gets users assigned to a specific role with pagination
        /// </summary>
        private async Task<PagedList<UserInRoleListItemResult>> GetUsersInRoleAsync(
            string roleName, 
            Param param, 
            CancellationToken cancellationToken)
        {
            try
            {
                // Get all users and filter those with this role
                var allUsers = await keycloakAdminService.GetUsersAsync(cancellationToken);
                var usersInRole = new List<UserInRoleListItemResult>();

                foreach (var user in allUsers)
                {
                    try
                    {
                        var userRoles = await keycloakAdminService.GetUserRolesAsync(user.Id, cancellationToken);
                        if (userRoles.Any(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase)))
                        {
                            var userItem = new UserInRoleListItemResult
                            {
                                UserId = Guid.Parse(user.Id),
                                Email = user.Email,
                                FirstName = user.FirstName,
                                LastName = user.LastName,
                                AssignedAt = user.CreatedTimestamp > 0 
                                    ? DateTimeOffset.FromUnixTimeMilliseconds(user.CreatedTimestamp) 
                                    : DateTimeOffset.UtcNow,
                                AssignedBy = "System" // Keycloak doesn't track who assigned roles
                            };

                            // Apply search filter
                            if (!string.IsNullOrEmpty(param.Search.SearchTerm))
                            {
                                var searchTerm = param.Search.SearchTerm.ToLowerInvariant();
                                var matchesSearch = user.Email.ToLowerInvariant().Contains(searchTerm) ||
                                                  user.FirstName.ToLowerInvariant().Contains(searchTerm) ||
                                                  user.LastName.ToLowerInvariant().Contains(searchTerm);
                                
                                if (matchesSearch)
                                {
                                    usersInRole.Add(userItem);
                                }
                            }
                            else
                            {
                                usersInRole.Add(userItem);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail the entire operation for individual user role check failures
                        logger.LogWarning(ex, "Failed to get roles for user {UserId}", user.Id);
                    }
                }

                // Apply sorting and pagination
                usersInRole = usersInRole.OrderBy(u => u.Email).ToList();

                var totalCount = usersInRole.Count;
                var pageIndex = param.Paging.EffectivePageIndex();
                var pageSize = param.Paging.PageSize ?? 5;
                var skip = pageIndex * pageSize;
                var paginatedUsers = usersInRole
                    .Skip(skip)
                    .Take(pageSize)
                    .ToList();

                return new PagedList<UserInRoleListItemResult>(
                    paginatedUsers,
                    pageIndex + 1, // PagedList expects 1-based page number
                    pageSize,
                    totalCount);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to get users for role {RoleName}", roleName);
                return new PagedList<UserInRoleListItemResult>(
                    new List<UserInRoleListItemResult>(),
                    1, // Default page number
                    param.Paging.PageSize ?? 5,
                    0);
            }
        }

        /// <summary>
        /// Gets role priority from Keycloak attributes
        /// </summary>
        private static int? GetRolePriority(UseCases.Common.Security.Authentication.Models.KeycloakRole role)
        {
            if (role.Attributes?.TryGetValue("priority", out var priorityValues) == true && 
                priorityValues?.Any() == true && 
                priorityValues[0] is string priorityString &&
                int.TryParse(priorityString, out var priority))
            {
                return priority;
            }

            return null;
        }

        /// <summary>
        /// Determines if a role is considered a "default" role
        /// </summary>
        private static bool IsDefaultRole(string roleName)
        {
            var defaultRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "customer",
                "viewer"
            };

            return defaultRoles.Contains(roleName);
        }
    }
}