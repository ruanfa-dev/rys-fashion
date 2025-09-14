using Core.Identity;

using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;
using SharedKernel.Models.Filter;
using SharedKernel.Models.PagedLists;
using SharedKernel.Models.Queries;
using SharedKernel.Models.Search;

using UseCases.Admin.Roles.Common;
using UseCases.Common.Persistence.Context;
using UseCases.Common.Security.Authorization.Claims;

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
        IApplicationDbContext context,
        ILogger<Handler> logger
    ) : IQueryHandler<Query, PagedList<Result>>
    {
        public async Task<ErrorOr<PagedList<Result>>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var param = request.Param;
                var query = context.Set<Role>()
                    .AsQueryable()
                    .AsNoTracking()
                    .Where(r => !param.IsSystemRole.HasValue
                                || r.IsSystemRole == param.IsSystemRole.Value)
                    .Where(r => !param.IsDefault.HasValue
                                || r.IsDefault == param.IsDefault.Value)
                    .ApplySearch(param.Search)
                    .ApplyFilters(param.Filter);

                // Project to result with user count
                var projectedQuery = query
                    .OrderBy(r => r.Name)
                    .Select(r => new Result
                    {
                        Id = r.Id,
                        Name = r.Name!,
                        Description = r.Description,
                        IsDefault = r.IsDefault,
                        IsSystemRole = r.IsSystemRole,
                        CreatedAt = r.CreatedAt,
                        CreatedBy = r.CreatedBy,
                        PermissionCount = r.RoleClaims.Count(rc => rc.ClaimType == CustomClaim.Permission),
                        UserCount = context.UserRoles.Count(ur => ur.RoleId == r.Id)
                    });

                var paginatedList = await projectedQuery
                    .ToPagedListAsync(
                        param.Paging,
                        cancellationToken: cancellationToken);

                logger.LogDebug("Retrieved {Count} roles for page {Page}", paginatedList.Items.Count, param.Paging.PageSize);

                return paginatedList;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving roles list");
                return Error.Failure("Roles.RetrievalFailed", "Failed to retrieve roles list");
            }
        }
    }
}
