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
using UseCases.Admin.Catalogs.Taxons.Get.OptionList;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Taxons.Get.PagedList;
public partial class GetTaxonPagedList
{

    public sealed record Param(Guid? TaxonomyId = null, Guid? StoreId = null) : QueryParams;
    public sealed record Result : TaxonResult.ListItem;
    public sealed record Query(Param Param) : IQuery<PagedList<Result>>;
    public sealed class Handler(IApplicationDbContext context, ILogger<Handler> logger)
        : IQueryHandler<Query, PagedList<Result>>
    {
        private readonly IApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly ILogger<Handler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<ErrorOr<PagedList<Result>>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var param = request.Param;
                var query = _context.Set<Taxon>().AsNoTracking();

                // Filter: by TaxonomyId
                if (param.TaxonomyId.HasValue)
                    query = query.Where(t => t.TaxonomyId == param.TaxonomyId.Value);

                // Filter: by StoreId via join to Taxonomy
                if (param.StoreId.HasValue)
                    query = query.Join(_context.Set<Taxonomy>(),
                        t => t.TaxonomyId,
                        tx => tx.Id,
                        (t, tx) => new { Taxon = t, Taxonomy = tx })
                        .Where(x => x.Taxonomy.StoreId == param.StoreId.Value)
                        .Select(x => x.Taxon);

                // Apply: search, sort, projection, and pagination
                var paginatedList = await query
                    .ApplySearch(param.Search)
                    .ApplySort(param.Sort)
                    .ProjectToType<Result>()
                    .ToPagedListOrAllAsync(param.Paging, cancellationToken: cancellationToken);

                _logger.LogDebug("Retrieved {Count} taxons for option list", paginatedList.Items.Count);
                return paginatedList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving taxon option list");
                return Taxon.Errors.UnexpectedError(nameof(GetTaxonOptionList), ex);
            }
        }
    }
}
