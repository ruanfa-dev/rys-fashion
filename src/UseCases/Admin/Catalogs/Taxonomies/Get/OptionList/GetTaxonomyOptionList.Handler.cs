using Core.Catalog.Taxonomies;

using ErrorOr;

using Mapster;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;
using SharedKernel.Models.PagedLists;
using SharedKernel.Models.Queries;
using SharedKernel.Models.Search;
using SharedKernel.Models.Sort;

using UseCases.Admin.Catalogs.Taxonomies.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Taxonomies.Get.OptionList;
public partial class GetTaxonomyOptionList
{
    public sealed record Param : QueryParams;
    public sealed record Result : TaxonomyResult.ListItem;
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
                PagedList<Result> paginatedList = await context.Set<Taxonomy>()
                    .Include(m=> m.Taxons)
                    .AsQueryable()
                    .AsNoTracking()
                    .ApplySearch(param.Search)
                    .ApplySort(param.Sort)
                    .ProjectToType<Result>()
                    .ToPagedListOrAllAsync(param.Paging, cancellationToken: cancellationToken);

                logger.LogDebug("Retrieved {Count} taxonomies for page", paginatedList.Items.Count);
                return paginatedList;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving taxonomy option list");
                return Taxonomy.Errors.UnexpectedError(nameof(GetTaxonomyOptionList), ex);
            }
        }
    }
}
