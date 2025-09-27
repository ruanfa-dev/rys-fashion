using Core.Identity.Permissions;

using ErrorOr;

using SharedKernel.Messaging.Abstracts;
using SharedKernel.Models.Filter;
using SharedKernel.Models.PagedLists;
using SharedKernel.Models.Queries;
using SharedKernel.Models.Sort;

using UseCases.Admin.Identity.Permissions.Common;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Identity.Permissions.List;

public static partial class ListAvailablePermissions
{
    public sealed record Param : QueryParams;
    public sealed record Result : PermissionResult;
    public sealed record Query(Param Param) : IQuery<PagedList<Result>>;


    public sealed class Handler(IApplicationDbContext dbContext) : IQueryHandler<Query, PagedList<Result>>
    {
        public async Task<ErrorOr<PagedList<Result>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var param = request.Param;
            var permissions = await dbContext.Set<Permission>()
                .Select(p => new Result()
                {
                    Id = p.Id,
                    Name = p.Name,
                    DisplayName = p.DisplayName,
                    Description = p.Description,
                    Action = p.Action,
                    Area = p.Area,
                    Resource = p.Resource,
                    CreatedAt = p.CreatedAt,
                    CreatedBy = p.CreatedBy,
                })
                .ApplyFilters(param.Filter)
                .ApplySort(param.Sort)
                .ToPagedListOrAllAsync(param.Paging, cancellationToken: cancellationToken);

            return permissions;
        }
    }
}