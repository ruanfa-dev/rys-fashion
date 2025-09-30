using Core.Catalog.Products;
using Core.Catalog.Taxonomies;

using ErrorOr;

using MediatR;

using Microsoft.EntityFrameworkCore;
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
            var eventTaxon = notification.Taxon;
            if (eventTaxon != null)
            {
                await RecomputeNestedSetsForTaxonomy(eventTaxon.TaxonomyId, cancellationToken);
                _logger.LogInformation("Nested set values recalculated (will be saved by outer SaveChanges) after creating taxon {TaxonId}.", notification.TaxonId);
                return;
            }

            // Fallback: attempt to load the taxon from DB (may not exist yet)
            var taxon = await _context.Set<Taxon>()
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
            var eventTaxon = notification.Taxon;
            if (eventTaxon != null)
            {
                await RecomputeNestedSetsForTaxonomy(eventTaxon.TaxonomyId, cancellationToken);
                _logger.LogInformation("Nested set values recalculated (will be saved by outer SaveChanges) after updating taxon {TaxonId}.", notification.TaxonId);
                return;
            }

            var taxon = await _context.Set<Taxon>()
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
            var eventTaxon = notification.Taxon;
            if (eventTaxon != null)
            {
                await RecomputeNestedSetsForTaxonomy(eventTaxon.TaxonomyId, cancellationToken);
                _logger.LogInformation("Nested set values recalculated (will be saved by outer SaveChanges) after deleting taxon {TaxonId}.", notification.TaxonId);
                return;
            }

            var taxon = await _context.Set<Taxon>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == notification.TaxonId, cancellationToken);

            if (taxon != null)
            {
                await RecomputeNestedSetsForTaxonomy(taxon.TaxonomyId, cancellationToken);
                _logger.LogInformation("Nested set values recalculated (will be saved by outer SaveChanges) after deleting taxon {TaxonId}.", notification.TaxonId);
                return;
            }

            var taxonomyIds = await _context.Set<Taxonomy>().Select(tx => tx.Id).ToListAsync(cancellationToken);
            foreach (var taxonomyId in taxonomyIds)
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
            var taxon = await _context.Set<Taxon>()
                .Include(t => t.TaxonRules)
                .Include(t => t.Classifications)
                .FirstOrDefaultAsync(t => t.Id == notification.TaxonId, cancellationToken);

            if (taxon == null)
            {
                _logger.LogWarning("Taxon {TaxonId} not found for product regeneration.", notification.TaxonId);
                return;
            }

            if (!taxon.Automatic)
            {
                _logger.LogInformation("Taxon {TaxonId} is not automatic; skipping product regeneration.", notification.TaxonId);
                return;
            }

            var productsQuery = _context.Set<Product>().AsQueryable();
            foreach (var rule in taxon.TaxonRules)
            {
                productsQuery = rule.Apply(productsQuery);
            }

            var productIds = await productsQuery.Select(p => p.Id).ToListAsync(cancellationToken);

            var toRemove = taxon.Classifications
                .Where(c => !productIds.Contains(c.ProductId))
                .ToList();

            foreach (var classification in toRemove)
            {
                taxon.Classifications.Remove(classification);
                taxon.AddDomainEvent(new Taxon.Events.ProductUnclassified(taxon.Id, classification.ProductId));
            }

            foreach (var productId in productIds)
            {
                if (taxon.Classifications.All(c => c.ProductId != productId))
                {
                    var result = taxon.ClassifyProduct(productId);
                    if (result.IsError)
                    {
                        _logger.LogWarning("Failed to classify product {ProductId} under taxon {TaxonId}: {Errors}", productId, taxon.Id, result.Errors);
                    }
                }
            }

            _logger.LogInformation("Product regeneration prepared for taxon {TaxonId} (changes will be persisted by outer SaveChanges).", notification.TaxonId);
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
            var taxon = await _context.Set<Taxon>()
                .Include(t => t.Taxonomy)
                .Include(t => t.Parent) // Load immediate parent
                .FirstOrDefaultAsync(t => t.Id == notification.TaxonId, cancellationToken);

            if (taxon == null)
            {
                _logger.LogWarning("Taxon {TaxonId} not found for touching featured sections.", notification.TaxonId);
                return;
            }

            var ancestors = new List<Taxon>();
            var current = taxon.Parent;
            int depth = 0;
            const int maxDepth = Taxon.Constraints.DepthMax;

            while (current != null && depth++ < maxDepth)
            {
                ancestors.Add(current);
                current = await _context.Set<Taxon>()
                    .Include(t => t.Parent)
                    .FirstOrDefaultAsync(t => t.Id == current.ParentId, cancellationToken);
            }

            foreach (var ancestor in ancestors)
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
            var taxon = await _context.Set<Taxon>()
                .Include(t => t.Translations)
                .FirstOrDefaultAsync(t => t.Id == notification.TaxonId, cancellationToken);

            if (taxon == null)
            {
                _logger.LogWarning("Taxon {TaxonId} not found for removing featured sections.", notification.TaxonId);
                return;
            }

            taxon.RegeneratePrettyNameAndPermalink();
            foreach (var translation in taxon.Translations)
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
            var eventTaxon = notification.TaxonId != Guid.Empty ? await _context.Set<Taxon>().FirstOrDefaultAsync(t => t.Id == notification.TaxonId, cancellationToken) : null;

            if (eventTaxon == null && notification.ParentId == null)
            {
                _logger.LogWarning("Taxon {TaxonId} not found for handling move event.", notification.TaxonId);
                return;
            }

            var taxonomyId = eventTaxon?.TaxonomyId ?? Guid.Empty;
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
        var persisted = await _context.Set<Taxon>()
            .Where(t => t.TaxonomyId == taxonomyId)
            .ToListAsync(cancellationToken);

        // Include tracked (Added) taxons that aren't yet persisted
        var dbContext = _context as DbContext;
        var trackedAdded = dbContext != null
            ? dbContext.ChangeTracker.Entries<Taxon>().Where(e => e.State == EntityState.Added && e.Entity.TaxonomyId == taxonomyId).Select(e => e.Entity).ToList()
            : new List<Taxon>();

        // Merge lists: prefer tracked instances
        var taxonDict = new Dictionary<Guid, Taxon>();
        foreach (var t in persisted)
        {
            // If tracked, use tracked instance
            var trackedEntry = dbContext?.ChangeTracker.Entries<Taxon>().FirstOrDefault(e => e.Entity.Id == t.Id);
            if (trackedEntry != null)
                taxonDict[t.Id] = trackedEntry.Entity;
            else
                taxonDict[t.Id] = t;
        }

        foreach (var t in trackedAdded)
        {
            taxonDict.TryAdd(t.Id, t);
        }

        var taxons = taxonDict.Values.ToList();

        // Reset children collections and rebuild parent-child graph in-memory
        foreach (var t in taxons)
            t.Children = new List<Taxon>();

        foreach (var t in taxons)
        {
            if (t.ParentId.HasValue && taxonDict.TryGetValue(t.ParentId.Value, out var parent))
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
        var roots = taxons.Where(t => t.ParentId == null).OrderBy(t => t.ChildIndex).ToList();

        int currentLft = 1;
        foreach (var root in roots)
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

        var orderedChildren = taxon.Children.OrderBy(c => c.ChildIndex).ToList();
        foreach (var child in orderedChildren)
        {
            currentLft = await AssignNestedSetValues(child, depth + 1, currentLft, cancellationToken);
        }

        taxon.Rgt = currentLft;
        return currentLft + 1;
    }
}
