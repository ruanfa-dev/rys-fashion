using Core.Catalogs;

using ErrorOr;

using Mapster;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;
using SharedKernel.Models.Filter;
using SharedKernel.Models.PagedLists;
using SharedKernel.Models.Queries;
using SharedKernel.Models.Search;
using SharedKernel.Models.Sort;

using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Properties.GetOptionList;
public partial class GetPropertyOptionList
{
    public sealed record Param : QueryParams;
    public sealed record Result : PropertySelectItemResult;

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
                var paginatedList = await context.Set<Property>()
                    .AsQueryable()
                    .AsNoTracking()
                    .ApplySearch(param.Search)
                    .ApplyFilters(param.Filter)
                    .ApplySort(param.Sort)
                    .ProjectToType<Result>()
                    .ToPagedListOrAllAsync(
                        param.Paging,
                        cancellationToken: cancellationToken);

                logger.LogDebug("Retrieved {Count} properties for page {Page}",
                    paginatedList.Items.Count,
                    param.Paging.PageSize);

                return paginatedList;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving properties list");
                return Property.Errors.PropertyUnexpected(nameof(GetPropertyOptionList), ex.Message);
            }
        }
    }
}
