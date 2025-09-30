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

namespace UseCases.Admin.Catalogs.Taxonomies.Get.PagedList;
public partial class GetTaxonomyPagedList
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
                PagedList<Result> list = await context.Set<Taxonomy>()
                    .AsNoTracking()
                    .ApplySearch(param.Search)
                    .ApplySort(param.Sort)
                    .ProjectToType<Result>()
                    .ToPagedListAsync(param.Paging, cancellationToken: cancellationToken);

                logger.LogDebug("Retrieved {Count} taxonomies", list.Items.Count);
                return list;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving taxonomies list");
                return Taxonomy.Errors.UnexpectedError(nameof(GetTaxonomyPagedList), ex);
            }
        }
    }
}
