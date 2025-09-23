using Core.Identity;

using ErrorOr;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;
using SharedKernel.Models.Filter;
using SharedKernel.Models.PagedLists;
using SharedKernel.Models.Queries;
using SharedKernel.Models.Search;
using SharedKernel.Models.Sort;

using UseCases.Admin.Roles.Common;
using UseCases.Common.Persistence.Context;
using UseCases.Common.Security.Authorization.Claims;

namespace UseCases.Admin.Roles.GetById;

public static partial class GetRoleById
{
    public sealed record Param : QueryParams;
    public sealed record Result : RoleDetailedResult;

    public sealed record Query(Guid Id, Param Param) : IQuery<Result>;

    public sealed class Handler(
        RoleManager<Role> roleManager,
        IApplicationDbContext dbContext,
        ILogger<Handler> logger
    ) : IQueryHandler<Query, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var param = request.Param;
                // Check: role exists
                var role = await roleManager.FindByIdAsync(request.Id.ToString());
                if (role == null)
                    return Role.Errors.RoleNotFound(Name);

                // Retrieve: role claims/permissions
                var roleClaims = await roleManager.GetClaimsAsync(role);
                var permissions = roleClaims
                    .Where(c => c.Type == CustomClaim.Permission)
                    .Select(c => c.Value!)
                    .ToArray();

                // Retrieve: users in role count
                var usersInRole = await dbContext.Set<UserRole>()
                    .Include(ur => ur.User)
                    .AsNoTracking()
                    .ApplyFilters(param.Filter)
                    .ApplySearch(param.Search)
                    .ApplySort(param.Sort)
                    .Select(m => m.User)
                    .Select(user => new UserInRoleListItemResult()
                    {
                        UserId = user.Id,
                        Email = user.Email!,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        AssignedAt = user.CreatedAt,
                        AssignedBy = user.CreatedBy,
                    }).ToPagedListOrDefaultAsync(
                        pagingParams: param.Paging,
                        defaultPageSize: 5,
                        cancellationToken: cancellationToken);

                var result = new Result()
                {
                    Id = role.Id,
                    Name = role.Name!,
                    Priority = role.Priority,
                    Description = role.Description,
                    IsDefault = role.IsDefault,
                    IsSystemRole = role.IsSystemRole,
                    CreatedAt = role.CreatedAt,
                    UpdatedAt = role.UpdatedAt,
                    CreatedBy = role.CreatedBy,
                    UpdatedBy = role.UpdatedBy,
                    UserCount = usersInRole.TotalCount,
                    PermissionCount = permissions.Length,
                    Permissions = permissions,
                    Users = usersInRole,

                };

                logger.LogDebug("Retrieved role {RoleId} with {UserCount} users and {PermissionCount} permissions",
                    role.Id, usersInRole.Count, permissions.Length);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving role {RoleId}", request.Id);
                return Role.Errors.RoleUnexpected(nameof(GetRoleById), ex.Message);
            }
        }
    }
}