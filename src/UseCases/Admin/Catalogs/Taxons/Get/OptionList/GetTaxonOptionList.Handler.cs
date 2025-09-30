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

using UseCases.Admin.Catalogs.Taxons.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Taxons.Get.OptionList;
public partial class GetTaxonOptionList
{
    public sealed record Param : QueryParams;
    public sealed record Result : TaxonResult.ComboItem;
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
                PagedList<Result> paginatedList = await context.Set<Taxon>()
                    .AsQueryable()
                    .AsNoTracking()
                    .ApplySearch(param.Search)
                    .ApplySort(param.Sort)
                    .ProjectToType<Result>()
                    .ToPagedListOrAllAsync(param.Paging, cancellationToken: cancellationToken);

                logger.LogDebug("Retrieved {Count} taxons for page", paginatedList.Items.Count);
                return paginatedList;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving taxon option list");
                return Taxon.Errors.UnexpectedError(nameof(GetTaxonOptionList), ex);
            }
        }
    }
}
