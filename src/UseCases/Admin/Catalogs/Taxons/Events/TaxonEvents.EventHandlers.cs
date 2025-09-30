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
        _logger.LogInformation("Taxon {TaxonId} created.", notification.TaxonId);

        try
        {
            // Prefer using the Taxon instance carried in the event (tracked/added) to ensure new node is included
            Taxon? eventTaxon = notification.Taxon;
            if (eventTaxon != null)
            {
                await RecomputeNestedSetsForTaxonomy(eventTaxon.TaxonomyId, cancellationToken);
                _logger.LogInformation("Nested set values recalculated (will be saved by outer SaveChanges) after creating taxon {TaxonId}.", notification.TaxonId);
                return;
            }

            // Fallback: attempt to load the taxon from DB (may not exist yet)
            Taxon? taxon = await _context.Set<Taxon>()
                .Include(t => t.Children)
                .Include(t => t.Parent)
                .Include(t => t.Taxonomy)
                .FirstOrDefaultAsync(t => t.Id == notification.TaxonId, cancellationToken);

            if (taxon == null)
            {
                _logger.LogWarning("Taxon {TaxonId} not found to update nested set after creation.", notification.TaxonId);
                return;
            }

            await RecomputeNestedSetsForTaxonomy(taxon.TaxonomyId, cancellationToken);
            _logger.LogInformation("Nested set values recalculated (will be saved by outer SaveChanges) after creating taxon {TaxonId}.", notification.TaxonId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update nested set values after creating taxon {TaxonId}.", notification.TaxonId);
        }
    }

    public async Task Handle(Taxon.Events.Updated notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Taxon {TaxonId} updated.", notification.TaxonId);

        try
        {
            Taxon? eventTaxon = notification.Taxon;
            if (eventTaxon != null)
            {
                await RecomputeNestedSetsForTaxonomy(eventTaxon.TaxonomyId, cancellationToken);
                _logger.LogInformation("Nested set values recalculated (will be saved by outer SaveChanges) after updating taxon {TaxonId}.", notification.TaxonId);
                return;
            }

            Taxon? taxon = await _context.Set<Taxon>()
                .Include(t => t.Children)
                .Include(t => t.Parent)
                .Include(t => t.Taxonomy)
                .FirstOrDefaultAsync(t => t.Id == notification.TaxonId, cancellationToken);

            if (taxon == null)
            {
                _logger.LogWarning("Taxon {TaxonId} not found to update nested set after update.", notification.TaxonId);
                return;
            }

            await RecomputeNestedSetsForTaxonomy(taxon.TaxonomyId, cancellationToken);
            _logger.LogInformation("Nested set values recalculated (will be saved by outer SaveChanges) after updating taxon {TaxonId}.", notification.TaxonId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update nested set values after updating taxon {TaxonId}.", notification.TaxonId);
        }
    }

    public async Task Handle(Taxon.Events.Deleted notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Taxon {TaxonId} deleted.", notification.TaxonId);

        try
        {
            Taxon? eventTaxon = notification.Taxon;
            if (eventTaxon != null)
            {
                await RecomputeNestedSetsForTaxonomy(eventTaxon.TaxonomyId, cancellationToken);
                _logger.LogInformation("Nested set values recalculated (will be saved by outer SaveChanges) after deleting taxon {TaxonId}.", notification.TaxonId);
                return;
            }

            Taxon? taxon = await _context.Set<Taxon>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == notification.TaxonId, cancellationToken);

            if (taxon != null)
            {
                await RecomputeNestedSetsForTaxonomy(taxon.TaxonomyId, cancellationToken);
                _logger.LogInformation("Nested set values recalculated (will be saved by outer SaveChanges) after deleting taxon {TaxonId}.", notification.TaxonId);
                return;
            }

            List<Guid> taxonomyIds = await _context.Set<Taxonomy>().Select(tx => tx.Id).ToListAsync(cancellationToken);
            foreach (Guid taxonomyId in taxonomyIds)
            {
                await RecomputeNestedSetsForTaxonomy(taxonomyId, cancellationToken);
            }

            _logger.LogInformation("Nested set values recalculated (will be saved by outer SaveChanges) for all taxonomies after deleting taxon {TaxonId}.", notification.TaxonId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update nested set values after deleting taxon {TaxonId}.", notification.TaxonId);
        }
    }

    public async Task Handle(Taxon.Events.RuleAdded notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rule {RuleId} added to taxon {TaxonId}.", notification.RuleId, notification.TaxonId);
        await Task.CompletedTask;
    }

    public async Task Handle(Taxon.Events.RuleRemoved notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rule {RuleId} removed from taxon {TaxonId}.", notification.RuleId, notification.TaxonId);
        await Task.CompletedTask;
    }

    public async Task Handle(Taxon.Events.ProductClassified notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Product {ProductId} classified under taxon {TaxonId}.", notification.ProductId, notification.TaxonId);
        await Task.CompletedTask;
    }

    public async Task Handle(Taxon.Events.ProductUnclassified notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Product {ProductId} unclassified from taxon {TaxonId}.", notification.ProductId, notification.TaxonId);
        await Task.CompletedTask;
    }

    public async Task Handle(Taxon.Events.RegenerateProducts notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Domain Event: {DomainEvent} for Taxon {TaxonId} (onlyOnce={OnlyOnce})", notification.GetType().Name, notification.TaxonId, notification.OnlyOnce);

            // Load taxon with rules and current classifications
            Taxon? taxon = await context.Set<Taxon>()
                .Include(t => t.TaxonRules)
                .Include(t => t.Classifications)
                    .ThenInclude(c => c.Product)
                .FirstOrDefaultAsync(t => t.Id == notification.TaxonId, cancellationToken);

            if (taxon == null)
            {
                _logger.LogWarning("Taxon {TaxonId} not found for regeneration.", notification.TaxonId);
                return;
            }

            // If no rules or manual taxon, nothing to regenerate
            if (!taxon.Automatic || !taxon.TaxonRules.Any())
            {
                _logger.LogInformation("Taxon {TaxonId} is manual or has no rules; skipping regeneration.", taxon.Id);
                return;
            }

            // Get all products (use Set<T>() so we don't rely on a dedicated DbSet property)
            IQueryable<Product> allProductsQuery = context.Set<Product>().AsQueryable();

            // Apply each rule and merge results according to rules match policy
            bool anyPolicy = taxon.RulesMatchPolicy == "any";
            IEnumerable<Product> resultProducts = anyPolicy ? Enumerable.Empty<Product>() : allProductsQuery;

            foreach (TaxonRule rule in taxon.TaxonRules)
            {
                IQueryable<Product> matched = rule.Apply(allProductsQuery);
                if (anyPolicy)
                {
                    resultProducts = resultProducts.Concat(matched).Distinct();
                }
                else
                {
                    resultProducts = resultProducts.Intersect(matched);
                }
            }

            List<Product> matchedList = resultProducts.ToList();

            // Update classifications: remove ones not present, add missing ones
            List<Classification> currentClassifications = taxon.Classifications.ToList();

            // Remove classifications for products no longer matched
            List<Classification> toRemove = currentClassifications.Where(c => c.Product == null || !matchedList.Any(p => p.Id == c.Product.Id)).ToList();
            if (toRemove.Any())
            {
                foreach (Classification rem in toRemove)
                {
                    context.Set<Classification>().Remove(rem);
                }
            }

            // Add classifications for products that are matched but not currently classified
            HashSet<Guid> existingProductIds = currentClassifications.Where(c => c.Product != null).Select(c => c.Product!.Id).ToHashSet();
            List<Product> toAdd = matchedList.Where(p => !existingProductIds.Contains(p.Id)).ToList();

            foreach (Product prod in toAdd)
            {
                Classification classification = new Classification
                {
                    Id = Guid.NewGuid(),
                    TaxonId = taxon.Id,
                    Product = prod,
                    Position = 0
                };
                await context.Set<Classification>().AddAsync(classification, cancellationToken);
            }

            // Persist changes
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Regeneration completed for Taxon {TaxonId}. Added: {AddedCount}, Removed: {RemovedCount}", taxon.Id, toAdd.Count, toRemove.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to regenerate products for taxon {TaxonId}.", notification.TaxonId);
        }
    }

    public async Task Handle(Taxon.Events.TouchFeaturedSections notification, CancellationToken cancellationToken)
    {
        try
        {
            Taxon? taxon = await _context.Set<Taxon>()
                .Include(t => t.Taxonomy)
                .Include(t => t.Parent) // Load immediate parent
                .FirstOrDefaultAsync(t => t.Id == notification.TaxonId, cancellationToken);

            if (taxon == null)
            {
                _logger.LogWarning("Taxon {TaxonId} not found for touching featured sections.", notification.TaxonId);
                return;
            }

            List<Taxon> ancestors = new List<Taxon>();
            Taxon? current = taxon.Parent;
            int depth = 0;
            const int maxDepth = Taxon.Constraints.DepthMax;

            while (current != null && depth++ < maxDepth)
            {
                ancestors.Add(current);
                current = await _context.Set<Taxon>()
                    .Include(t => t.Parent)
                    .FirstOrDefaultAsync(t => t.Id == current.ParentId, cancellationToken);
            }

            foreach (Taxon ancestor in ancestors)
            {
                ancestor.MarkAsUpdated();
                ancestor.AddDomainEvent(new Taxon.Events.Updated(ancestor.Id, ancestor));
            }

            if (taxon.Taxonomy != null)
            {
                taxon.Taxonomy.MarkAsUpdated();
                taxon.Taxonomy.AddDomainEvent(new Taxonomy.Events.Updated(taxon.Taxonomy.Id));
            }

            _logger.LogInformation("Touched featured sections for taxon {TaxonId} and its ancestors (changes will be persisted by outer SaveChanges).", notification.TaxonId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to touch featured sections for taxon {TaxonId}.", notification.TaxonId);
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
                _logger.LogWarning("Taxon {TaxonId} not found for removing featured sections.", notification.TaxonId);
                return;
            }

            taxon.RegeneratePrettyNameAndPermalink();
            foreach (TaxonTranslation translation in taxon.Translations)
            {
                translation.UpdatePrettyNameAndPermalink(taxon);
            }

            _logger.LogInformation("Removed featured sections for taxon {TaxonId} (changes will be persisted by outer SaveChanges).", notification.TaxonId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove featured sections for taxon {TaxonId}.", notification.TaxonId);
        }
    }

    public async Task Handle(Taxon.Events.Moved notification, CancellationToken cancellationToken)
    {
        try
        {
            Taxon? eventTaxon = notification.TaxonId != Guid.Empty ? await _context.Set<Taxon>().FirstOrDefaultAsync(t => t.Id == notification.TaxonId, cancellationToken) : null;

            if (eventTaxon == null && notification.ParentId == null)
            {
                _logger.LogWarning("Taxon {TaxonId} not found for handling move event.", notification.TaxonId);
                return;
            }

            Guid taxonomyId = eventTaxon?.TaxonomyId ?? Guid.Empty;
            if (taxonomyId == Guid.Empty && eventTaxon == null)
            {
                _logger.LogWarning("Cannot determine taxonomy for moved taxon {TaxonId}.", notification.TaxonId);
                return;
            }

            await RecomputeNestedSetsForTaxonomy(taxonomyId, cancellationToken);
            _logger.LogInformation("Taxon {TaxonId} moved to ParentId: {ParentId}, Index: {NewIndex}. Nested sets recalculated (will be saved by outer SaveChanges).",
                notification.TaxonId, notification.ParentId, notification.NewIndex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle move event for taxon {TaxonId}.", notification.TaxonId);
        }
    }

    private async Task<ErrorOr<Success>> UpdateNestedSetValues(Taxon taxon, CancellationToken cancellationToken)
    {
        try
        {
            await RecomputeNestedSetsForTaxonomy(taxon.TaxonomyId, cancellationToken);
            return Result.Success;
        }
        catch (Exception ex)
        {
            return Taxon.Errors.UnexpectedError("UpdateNestedSetValues", ex);
        }
    }

    private async Task RecomputeNestedSetsForTaxonomy(Guid taxonomyId, CancellationToken cancellationToken)
    {
        // Load persisted taxons for this taxonomy
        List<Taxon> persisted = await _context.Set<Taxon>()
            .Where(t => t.TaxonomyId == taxonomyId)
            .ToListAsync(cancellationToken);

        // Include tracked (Added) taxons that aren't yet persisted
        DbContext? dbContext = _context as DbContext;
        List<Taxon> trackedAdded = dbContext != null
            ? dbContext.ChangeTracker.Entries<Taxon>().Where(e => e.State == EntityState.Added && e.Entity.TaxonomyId == taxonomyId).Select(e => e.Entity).ToList()
            : new List<Taxon>();

        // Merge lists: prefer tracked instances
        Dictionary<Guid, Taxon> taxonDict = new Dictionary<Guid, Taxon>();
        foreach (Taxon t in persisted)
        {
            // If tracked, use tracked instance
            EntityEntry<Taxon>? trackedEntry = dbContext?.ChangeTracker.Entries<Taxon>().FirstOrDefault(e => e.Entity.Id == t.Id);
            if (trackedEntry != null)
                taxonDict[t.Id] = trackedEntry.Entity;
            else
                taxonDict[t.Id] = t;
        }

        foreach (Taxon t in trackedAdded)
        {
            taxonDict.TryAdd(t.Id, t);
        }

        List<Taxon> taxons = taxonDict.Values.ToList();

        // Reset children collections and rebuild parent-child graph in-memory
        foreach (Taxon t in taxons)
            t.Children = new List<Taxon>();

        foreach (Taxon t in taxons)
        {
            if (t.ParentId.HasValue && taxonDict.TryGetValue(t.ParentId.Value, out Taxon? parent))
            {
                parent.Children.Add(t);
                t.Parent = parent;
            }
            else
            {
                t.Parent = null;
            }
        }

        // Find roots (ParentId == null)
        List<Taxon> roots = taxons.Where(t => t.ParentId == null).OrderBy(t => t.ChildIndex).ToList();

        int currentLft = 1;
        foreach (Taxon root in roots)
        {
            currentLft = await AssignNestedSetValues(root, 0, currentLft, cancellationToken);
        }
    }

    private static async Task<int> AssignNestedSetValues(Taxon taxon, int depth, int currentLft, CancellationToken cancellationToken)
    {
        // Use tracked instances and assign directly so outer SaveChanges persists them
        taxon.Lft = currentLft;
        taxon.Depth = depth;
        currentLft++;

        List<Taxon> orderedChildren = taxon.Children.OrderBy(c => c.ChildIndex).ToList();
        foreach (Taxon child in orderedChildren)
        {
            currentLft = await AssignNestedSetValues(child, depth + 1, currentLft, cancellationToken);
        }

        taxon.Rgt = currentLft;
        return currentLft + 1;
    }
}
