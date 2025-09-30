using Core.Catalog.Taxonomies;

using ErrorOr;

using Mapster;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.Taxons.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Taxons.Get.TreeList;

public static partial class GetTaxonTree
{
    public record Param(Guid? TaxonomyId = null, Guid? StoreId = null, bool IncludeLeavesOnly = false);
    public record Result : TaxonResult.TreeItem;

    public sealed record Query(Param Param) : IQuery<List<Result>>;

    public sealed class Handler(IApplicationDbContext context, ILogger<Handler> logger)
        : IQueryHandler<Query, List<Result>>
    {
        private readonly IApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly ILogger<Handler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<ErrorOr<List<Result>>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var param = request.Param;

                _logger.LogDebug("GetTaxonTree called with TaxonomyId={TaxonomyId}, StoreId={StoreId}, IncludeLeavesOnly={IncludeLeavesOnly}",
                    param.TaxonomyId, param.StoreId, param.IncludeLeavesOnly);

                // Build base query
                IQueryable<Taxon> query = _context.Set<Taxon>().AsNoTracking();

                if (param.TaxonomyId.HasValue)
                    query = query.Where(t => t.TaxonomyId == param.TaxonomyId.Value);

                if (param.StoreId.HasValue)
                {
                    query = query.Join(
                        _context.Set<Taxonomy>(),
                        t => t.TaxonomyId,
                        tx => tx.Id,
                        (t, tx) => new { Taxon = t, Taxonomy = tx })
                        .Where(x => x.Taxonomy.StoreId == param.StoreId.Value)
                        .Select(x => x.Taxon);
                }

                // Single query to fetch all taxons
                var allTaxons = await query.ToListAsync(cancellationToken);
                _logger.LogDebug("Fetched {Count} taxons from database", allTaxons.Count);

                // Filter to leaves in-memory if needed
                List<Taxon> taxonsToProcess;
                if (param.IncludeLeavesOnly)
                {
                    var parentIds = new HashSet<Guid>(
                        allTaxons.Where(t => t.ParentId.HasValue)
                                 .Select(t => t.ParentId!.Value));

                    taxonsToProcess = allTaxons
                        .Where(t => !parentIds.Contains(t.Id))
                        .ToList();

                    _logger.LogDebug("Filtered to {Count} leaf taxons", taxonsToProcess.Count);
                }
                else
                {
                    taxonsToProcess = allTaxons;
                }

                // Build parent-child lookup for O(1) access
                var parentChildMap = taxonsToProcess
                    .Where(t => t.ParentId.HasValue)
                    .GroupBy(t => t.ParentId!.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderBy(t => t.ChildIndex).ToList());

                // Get root taxons
                var rootTaxons = taxonsToProcess
                    .Where(t => t.ParentId == null)
                    .OrderBy(t => t.Lft)
                    .ToList();

                // Build tree in-memory recursively
                var result = rootTaxons
                    .Select(root => BuildTreeItem(root, parentChildMap))
                    .ToList();

                _logger.LogDebug("Built taxon tree with {Count} root nodes", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving taxon tree");
                return Taxon.Errors.UnexpectedError(nameof(GetTaxonTree), ex);
            }
        }

        private static Result BuildTreeItem(Taxon taxon, Dictionary<Guid, List<Taxon>> parentChildMap)
        {
            var treeItem = taxon.Adapt<Result>();

            // Get children from pre-built dictionary (O(1) lookup)
            if (parentChildMap.TryGetValue(taxon.Id, out var children))
            {
                treeItem.Children.AddRange(
                    children.Select(child => BuildTreeItem(child, parentChildMap)));
            }

            return treeItem;
        }
    }
}