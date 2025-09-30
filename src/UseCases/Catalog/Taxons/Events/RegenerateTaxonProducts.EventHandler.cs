using Core.Catalog.Taxonomies;
using Core.Catalog.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using UseCases.Common.Persistence.Context;

namespace UseCases.Catalog.Taxons.Events;

internal sealed class RegenerateTaxonProductsEventHandler(IApplicationDbContext context)
    : INotificationHandler<Taxon.Events.RegenerateProducts>
{
    public async Task Handle(Taxon.Events.RegenerateProducts notification, CancellationToken cancellationToken)
    {
        Log.Information("Domain Event: {DomainEvent} for Taxon {TaxonId} (onlyOnce={OnlyOnce})", notification.GetType().Name, notification.TaxonId, notification.OnlyOnce);

        // Load taxon with rules and current classifications
        Taxon? taxon = await context.Set<Taxon>()
            .Include(t => t.TaxonRules)
            .Include(t => t.Classifications)
                .ThenInclude(c => c.Product)
            .FirstOrDefaultAsync(t => t.Id == notification.TaxonId, cancellationToken);

        if (taxon == null)
        {
            Log.Warning("Taxon {TaxonId} not found for regeneration.", notification.TaxonId);
            return;
        }

        // If no rules or manual taxon, nothing to regenerate
        if (!taxon.Automatic || !taxon.TaxonRules.Any())
        {
            Log.Information("Taxon {TaxonId} is manual or has no rules; skipping regeneration.", taxon.Id);
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

        Log.Information("Regeneration completed for Taxon {TaxonId}. Added: {AddedCount}, Removed: {RemovedCount}", taxon.Id, toAdd.Count, toRemove.Count);
    }
}
