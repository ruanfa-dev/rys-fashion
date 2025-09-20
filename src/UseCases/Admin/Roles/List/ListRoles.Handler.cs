using ErrorOr;

using Microsoft.Extensions.Logging;

using SharedKernel.Models.Filter;
using SharedKernel.Models.PagedLists;
using SharedKernel.Models.Queries;
using SharedKernel.Models.Paging;
using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Roles.Common;
using UseCases.Common.Security.Authentication.Services;
using UseCases.Common.Security.Authorization.Templates;

namespace UseCases.Admin.Roles.List;

public static partial class ListRoles
{
    public record Param : QueryParams
    {
        public bool? IsSystemRole { get; init; }
        public bool? IsDefault { get; init; }
    }
    
    public sealed record Result : RoleResult;
    
    public sealed record Query(Param Param) : IQuery<PagedList<Result>>;

    public sealed class Handler(
        IKeycloakAdminService keycloakAdminService,
        ILogger<Handler> logger
    ) : IQueryHandler<Query, PagedList<Result>>
    {
        public async Task<ErrorOr<PagedList<Result>>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var param = request.Param;

                // Get all roles from Keycloak
                var keycloakRoles = await keycloakAdminService.GetRolesAsync(cancellationToken);
                
                // Convert Keycloak roles to our result format and apply filtering
                var roleResults = new List<Result>();
                
                foreach (var keycloakRole in keycloakRoles)
                {
                    // Get role template to determine if it's a system role and get permissions
                    var roleTemplate = RolePermissionTemplates.GetRoleTemplate(keycloakRole.Name);
                    var isSystemRole = roleTemplate != null;
                    var isDefault = IsDefaultRole(keycloakRole.Name);
                    
                    // Apply filters
                    if (param.IsSystemRole.HasValue && isSystemRole != param.IsSystemRole.Value)
                        continue;
                        
                    if (param.IsDefault.HasValue && isDefault != param.IsDefault.Value)
                        continue;

                    // Apply search filter
                    if (!string.IsNullOrEmpty(param.Search.SearchTerm))
                    {
                        var searchTerm = param.Search.SearchTerm.ToLowerInvariant();
                        var matchesSearch = keycloakRole.Name.ToLowerInvariant().Contains(searchTerm) ||
                                          (keycloakRole.Description?.ToLowerInvariant().Contains(searchTerm) == true);
                        
                        if (!matchesSearch)
                            continue;
                    }

                    // Get user count for this role (this might be expensive - consider caching)
                    var userCount = await GetUserCountForRoleAsync(keycloakRole.Name, cancellationToken);
                    
                    // Get permission count from role template
                    var permissionCount = roleTemplate?.Permissions.Length ?? 0;

                    roleResults.Add(new Result
                    {
                        Id = Guid.Parse(keycloakRole.Id), // Convert Keycloak ID to Guid
                        Name = keycloakRole.Name,
                        Description = keycloakRole.Description,
                        IsDefault = isDefault,
                        IsSystemRole = isSystemRole,
                        CreatedAt = DateTimeOffset.UtcNow, // Keycloak doesn't provide creation time by default
                        CreatedBy = "System", // Keycloak doesn't track creator
                        PermissionCount = permissionCount,
                        UserCount = userCount
                    });
                }

                // Apply sorting
                roleResults = roleResults.OrderBy(r => r.Name).ToList();

                // Apply pagination
                var totalCount = roleResults.Count;
                var pageIndex = param.Paging.EffectivePageIndex();
                var pageSize = param.Paging.PageSize ?? 10;
                var skip = pageIndex * pageSize;
                var paginatedResults = roleResults
                    .Skip(skip)
                    .Take(pageSize)
                    .ToList();

                var pagedList = new PagedList<Result>(
                    paginatedResults,
                    pageIndex + 1, // PagedList expects 1-based page number
                    pageSize,
                    totalCount);

                logger.LogDebug("Retrieved {Count} roles for page {Page}", pagedList.Items.Count, pageIndex + 1);

                return pagedList;
            }
            catch (NotSupportedException ex)
            {
                logger.LogWarning("Keycloak Admin API not configured: {Message}", ex.Message);
                return Error.Failure("Roles.NotConfigured", "Keycloak role management is not properly configured");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving roles list from Keycloak");
                return Error.Failure("Roles.RetrievalFailed", "Failed to retrieve roles list from Keycloak");
            }
        }

        /// <summary>
        /// Gets the count of users assigned to a specific role
        /// Note: This is a simplified implementation. For better performance,
        /// consider implementing batch user queries or caching role assignments.
        /// </summary>
        private async Task<int> GetUserCountForRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            try
            {
                // Get all users and count those with this role
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
                        logger.LogWarning(ex, "Failed to get roles for user {UserId}", user.Id);
                    }
                }

                return usersWithRole;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to get user count for role {RoleName}", roleName);
                return 0; // Return 0 on error rather than failing the entire operation
            }
        }

        /// <summary>
        /// Determines if a role is considered a "default" role
        /// </summary>
        private static bool IsDefaultRole(string roleName)
        {
            // Define which roles are considered default roles
            var defaultRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "customer",
                "viewer"
            };

            return defaultRoles.Contains(roleName);
        }
    }
}
