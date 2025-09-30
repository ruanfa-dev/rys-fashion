using Core.Catalog.Taxonomies;

using ErrorOr;

using Mapster;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;
using SharedKernel.Models.PagedLists;
using SharedKernel.Models.Search;
using SharedKernel.Models.Sort;

using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Taxonomies.Get.OptionList;
public partial class GetTaxonomyOptionList
{
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
