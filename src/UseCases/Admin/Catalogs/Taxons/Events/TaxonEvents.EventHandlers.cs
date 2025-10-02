using Core.Catalog.Products;
using Core.Catalog.Taxonomies;

using ErrorOr;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;

using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Taxons.Events;

public class TaxonEventHandlers(IApplicationDbContext context, ILogger<TaxonEventHandlers> logger)
    :
        INotificationHandler<Taxon.Events.Created>,
        INotificationHandler<Taxon.Events.Updated>,
        INotificationHandler<Taxon.Events.Deleted>,
        INotificationHandler<Taxon.Events.RuleAdded>,
        INotificationHandler<Taxon.Events.RuleRemoved>,
        INotificationHandler<Taxon.Events.ProductClassified>,
        INotificationHandler<Taxon.Events.ProductUnclassified>,
        INotificationHandler<Taxon.Events.RegenerateProducts>,
        INotificationHandler<Taxon.Events.TouchFeaturedSections>,
        INotificationHandler<Taxon.Events.RemoveFeaturedSections>,
        INotificationHandler<Taxon.Events.Moved>
{
    private readonly IApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ILogger<TaxonEventHandlers> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task Handle(Taxon.Events.Created notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling Created event for taxon {TaxonId}", notification.TaxonId);

        try
        {
            Taxon? taxon = notification.Taxon;
            if (taxon == null)
            {
                taxon = await _context.Set<Taxon>()
                    .Include(t => t.Children)
                    .Include(t => t.Parent)
                    .FirstOrDefaultAsync(t => t.Id == notification.TaxonId, cancellationToken);

                if (taxon == null)
                {
                    _logger.LogWarning("Taxon {TaxonId} not found after creation", notification.TaxonId);
                    return;
                }
            }

            await RecomputeNestedSetsForTaxonomy(taxon.TaxonomyId, cancellationToken);
            _logger.LogInformation("Nested set values recalculated for taxonomy {TaxonomyId} after creating taxon {TaxonId}",
                taxon.TaxonomyId, notification.TaxonId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle Created event for taxon {TaxonId}", notification.TaxonId);
        }
    }

    public async Task Handle(Taxon.Events.Updated notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling Updated event for taxon {TaxonId}", notification.TaxonId);

        try
        {
            Taxon? taxon = notification.Taxon;
            if (taxon == null)
            {
                taxon = await _context.Set<Taxon>()
                    .Include(t => t.Children)
                    .Include(t => t.Parent)
                    .FirstOrDefaultAsync(t => t.Id == notification.TaxonId, cancellationToken);

                if (taxon == null)
                {
                    _logger.LogWarning("Taxon {TaxonId} not found for update event", notification.TaxonId);
                    return;
                }
            }

            // Only recompute nested sets if parent changed or this is a structural update
            if (notification.ParentChanged)
            {
                await RecomputeNestedSetsForTaxonomy(taxon.TaxonomyId, cancellationToken);
                _logger.LogInformation("Nested set values recalculated for taxonomy {TaxonomyId} after updating taxon {TaxonId}",
                    taxon.TaxonomyId, notification.TaxonId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle Updated event for taxon {TaxonId}", notification.TaxonId);
        }
    }

    public async Task Handle(Taxon.Events.Deleted notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling Deleted event for taxon {TaxonId}", notification.TaxonId);

        try
        {
            Guid taxonomyId = Guid.Empty;

            if (notification.Taxon != null)
            {
                taxonomyId = notification.Taxon.TaxonomyId;
            }
            else
            {
                Taxon? taxon = await _context.Set<Taxon>()
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Id == notification.TaxonId, cancellationToken);

                if (taxon != null)
                {
                    taxonomyId = taxon.TaxonomyId;
                }
            }

            if (taxonomyId != Guid.Empty)
            {
                await RecomputeNestedSetsForTaxonomy(taxonomyId, cancellationToken);
                _logger.LogInformation("Nested set values recalculated for taxonomy {TaxonomyId} after deleting taxon {TaxonId}",
                    taxonomyId, notification.TaxonId);
            }
            else
            {
                _logger.LogWarning("Could not determine taxonomy for deleted taxon {TaxonId}", notification.TaxonId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle Deleted event for taxon {TaxonId}", notification.TaxonId);
        }
    }

    public async Task Handle(Taxon.Events.RuleAdded notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rule {RuleId} added to taxon {TaxonId}", notification.RuleId, notification.TaxonId);
        await Task.CompletedTask;
    }

    public async Task Handle(Taxon.Events.RuleRemoved notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rule {RuleId} removed from taxon {TaxonId}", notification.RuleId, notification.TaxonId);
        await Task.CompletedTask;
    }

    public async Task Handle(Taxon.Events.ProductClassified notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Product {ProductId} classified under taxon {TaxonId}",
            notification.ProductId, notification.TaxonId);
        await Task.CompletedTask;
    }

    public async Task Handle(Taxon.Events.ProductUnclassified notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Product {ProductId} unclassified from taxon {TaxonId}",
            notification.ProductId, notification.TaxonId);
        await Task.CompletedTask;
    }

    public async Task Handle(Taxon.Events.RegenerateProducts notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Regenerating products for taxon {TaxonId} (onlyOnce={OnlyOnce})",
                notification.TaxonId, notification.OnlyOnce);

            Taxon? taxon = await _context.Set<Taxon>()
                .Include(t => t.TaxonRules)
                .Include(t => t.Classifications)
                    .ThenInclude(c => c.Product)
                .FirstOrDefaultAsync(t => t.Id == notification.TaxonId, cancellationToken);

            if (taxon == null)
            {
                _logger.LogWarning("Taxon {TaxonId} not found for product regeneration", notification.TaxonId);
                return;
            }

            if (!taxon.Automatic || !taxon.TaxonRules.Any())
            {
                _logger.LogInformation("Taxon {TaxonId} is manual or has no rules; skipping regeneration", taxon.Id);
                return;
            }

            IQueryable<Product> allProductsQuery = _context.Set<Product>().AsQueryable();

            // Apply rules based on match policy
            bool anyPolicy = taxon.RulesMatchPolicy == "any";
            IEnumerable<Product> resultProducts = anyPolicy
                ? Enumerable.Empty<Product>()
                : allProductsQuery;

            foreach (TaxonRule rule in taxon.TaxonRules)
            {
                IQueryable<Product> matched = rule.Apply(allProductsQuery);
                resultProducts = anyPolicy
                    ? resultProducts.Concat(matched).Distinct()
                    : resultProducts.Intersect(matched);
            }

            List<Product> matchedList = resultProducts.ToList();

            // Remove classifications for products no longer matched
            List<Classification> currentClassifications = taxon.Classifications.ToList();
            List<Classification> toRemove = currentClassifications
                .Where(c => c.Product == null || !matchedList.Any(p => p.Id == c.Product.Id))
                .ToList();

            foreach (Classification rem in toRemove)
            {
                _context.Set<Classification>().Remove(rem);
            }

            // Add classifications for newly matched products
            HashSet<Guid> existingProductIds = currentClassifications
                .Where(c => c.Product != null)
                .Select(c => c.Product!.Id)
                .ToHashSet();

            List<Product> toAdd = matchedList
                .Where(p => !existingProductIds.Contains(p.Id))
                .ToList();

            foreach (Product prod in toAdd)
            {
                ErrorOr<Classification> classificationResult = Classification.Create(prod.Id, taxon.Id, 0);
                if (!classificationResult.IsError)
                {
                    await _context.Set<Classification>().AddAsync(classificationResult.Value, cancellationToken);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Product regeneration completed for taxon {TaxonId}. Added: {AddedCount}, Removed: {RemovedCount}",
                taxon.Id, toAdd.Count, toRemove.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to regenerate products for taxon {TaxonId}", notification.TaxonId);
        }
    }

    public async Task Handle(Taxon.Events.TouchFeaturedSections notification, CancellationToken cancellationToken)
    {
        try
        {
            Taxon? taxon = await _context.Set<Taxon>()
                .Include(t => t.Taxonomy)
                .FirstOrDefaultAsync(t => t.Id == notification.TaxonId, cancellationToken);

            if (taxon == null)
            {
                _logger.LogWarning("Taxon {TaxonId} not found for touching featured sections", notification.TaxonId);
                return;
            }

            // Load ancestors
            List<Taxon> ancestors = [];
            Guid? currentParentId = taxon.ParentId;
            int depth = 0;
            const int maxDepth = Taxon.Constraints.DepthMax;

            while (currentParentId.HasValue && depth++ < maxDepth)
            {
                Taxon? ancestor = await _context.Set<Taxon>()
                    .FirstOrDefaultAsync(t => t.Id == currentParentId.Value, cancellationToken);

                if (ancestor == null) break;

                ancestors.Add(ancestor);
                currentParentId = ancestor.ParentId;
            }

            foreach (Taxon ancestor in ancestors)
            {
                ancestor.AddDomainEvent(new Taxon.Events.Updated(ancestor.Id, ancestor));
            }

            if (taxon.Taxonomy != null)
            {
                taxon.Taxonomy.AddDomainEvent(new Taxonomy.Events.Updated(taxon.Taxonomy.Id, taxon.Taxonomy));
            }

            _logger.LogInformation("Touched featured sections for taxon {TaxonId} and {AncestorCount} ancestors",
                notification.TaxonId, ancestors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to touch featured sections for taxon {TaxonId}", notification.TaxonId);
        }
    }

    public async Task Handle(Taxon.Events.RemoveFeaturedSections notification, CancellationToken cancellationToken)
    {
        try
        {
            Taxon? taxon = await _context.Set<Taxon>()
                .Include(t => t.Translations)
                .FirstOrDefaultAsync(t => t.Id == notification.TaxonId, cancellationToken);

            if (taxon == null)
            {
                _logger.LogWarning("Taxon {TaxonId} not found for removing featured sections", notification.TaxonId);
                return;
            }

            taxon.RegeneratePrettyNameAndPermalink();
            _logger.LogInformation("Removed featured sections for taxon {TaxonId}", notification.TaxonId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove featured sections for taxon {TaxonId}", notification.TaxonId);
        }
    }

    public async Task Handle(Taxon.Events.Moved notification, CancellationToken cancellationToken)
    {
        try
        {
            Taxon? taxon = await _context.Set<Taxon>()
                .FirstOrDefaultAsync(t => t.Id == notification.TaxonId, cancellationToken);

            if (taxon == null)
            {
                _logger.LogWarning("Taxon {TaxonId} not found for move event", notification.TaxonId);
                return;
            }

            await RecomputeNestedSetsForTaxonomy(taxon.TaxonomyId, cancellationToken);

            _logger.LogInformation("Taxon {TaxonId} moved to parent {ParentId} at index {NewIndex}. Nested sets recalculated.",
                notification.TaxonId, notification.ParentId, notification.NewIndex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle move event for taxon {TaxonId}", notification.TaxonId);
        }
    }

    private async Task RecomputeNestedSetsForTaxonomy(Guid taxonomyId, CancellationToken cancellationToken)
    {
        // Load all persisted taxons for this taxonomy
        List<Taxon> persisted = await _context.Set<Taxon>()
            .Where(t => t.TaxonomyId == taxonomyId)
            .ToListAsync(cancellationToken);

        // Include tracked (Added) taxons not yet persisted
        DbContext? dbContext = _context as DbContext;
        List<Taxon> trackedAdded = dbContext != null
            ? dbContext.ChangeTracker.Entries<Taxon>()
                .Where(e => e.State == EntityState.Added && e.Entity.TaxonomyId == taxonomyId)
                .Select(e => e.Entity)
                .ToList()
            : [];

        // Merge lists: prefer tracked instances over persisted ones
        Dictionary<Guid, Taxon> taxonDict = new Dictionary<Guid, Taxon>();

        foreach (Taxon t in persisted)
        {
            EntityEntry<Taxon>? trackedEntry = dbContext?.ChangeTracker.Entries<Taxon>()
                .FirstOrDefault(e => e.Entity.Id == t.Id);

            taxonDict[t.Id] = trackedEntry != null ? trackedEntry.Entity : t;
        }

        foreach (Taxon t in trackedAdded)
        {
            taxonDict.TryAdd(t.Id, t);
        }

        List<Taxon> taxons = taxonDict.Values.ToList();

        // Rebuild parent-child relationships in memory
        foreach (Taxon t in taxons)
            t.Children = new List<Taxon>();

        foreach (Taxon t in taxons)
        {
            if (t.ParentId.HasValue && taxonDict.TryGetValue(t.ParentId.Value, out Taxon? parent))
            {
                if (!parent.Children.Contains(t))
                    ((List<Taxon>)parent.Children).Add(t);
                t.Parent = parent;
            }
            else
            {
                t.Parent = null;
            }
        }

        // Find roots and assign nested set values
        List<Taxon> roots = taxons
            .Where(t => t.ParentId == null)
            .OrderBy(t => t.ChildIndex)
            .ToList();

        int currentLft = 1;
        foreach (Taxon root in roots)
        {
            currentLft = AssignNestedSetValues(root, 0, currentLft);
        }
    }

    private static int AssignNestedSetValues(Taxon taxon, int depth, int currentLft)
    {
        taxon.Lft = currentLft;
        taxon.Depth = depth;
        currentLft++;

        List<Taxon> orderedChildren = taxon.Children
            .OrderBy(c => c.ChildIndex)
            .ToList();

        foreach (Taxon child in orderedChildren)
        {
            currentLft = AssignNestedSetValues(child, depth + 1, currentLft);
        }

        taxon.Rgt = currentLft;
        return currentLft + 1;
    }
}