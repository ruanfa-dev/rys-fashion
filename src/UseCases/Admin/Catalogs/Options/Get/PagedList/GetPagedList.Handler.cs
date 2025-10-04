using Core.Catalog.Options;

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

using UseCases.Admin.Catalogs.Options.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Options.Get.PagedList;
public static partial class GetOptionTypePagedList
{
    public sealed record Param : QueryParams;
    public sealed record Result : OptionTypeResult.ListItem;

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
                Param param = request.Param;
                PagedList<Result> paginatedList = await context.Set<OptionType>()
                    .AsQueryable()
                    .AsNoTracking()
                    .ApplySearch(param.Search)
                    .ApplyFilters(param.Filter)
                    .ApplySort(param.Sort)
                    .ProjectToType<Result>()
                    .ToPagedListAsync(
                        param.Paging,
                        cancellationToken: cancellationToken);

                logger.LogDebug("Retrieved {Count} properties for page {Page}",
                    paginatedList.Items.Count,
                    param.Paging.PageSize);

                return paginatedList;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving option types list");
                return OptionType.Errors.UnexpectedError(nameof(GetOptionTypePagedList), ex);
            }
        }
    }
}
