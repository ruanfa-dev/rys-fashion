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
        var taxon = await context.Set<Taxon>()
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
        var allProductsQuery = context.Set<Product>().AsQueryable();

        // Apply each rule and merge results according to rules match policy
        var anyPolicy = taxon.RulesMatchPolicy == "any";
        IEnumerable<Product> resultProducts = anyPolicy ? Enumerable.Empty<Product>() : allProductsQuery;

        foreach (var rule in taxon.TaxonRules)
        {
            var matched = rule.Apply(allProductsQuery);
            if (anyPolicy)
            {
                resultProducts = resultProducts.Concat(matched).Distinct();
            }
            else
            {
                resultProducts = resultProducts.Intersect(matched);
            }
        }

        var matchedList = resultProducts.ToList();

        // Update classifications: remove ones not present, add missing ones
        var currentClassifications = taxon.Classifications.ToList();

        // Remove classifications for products no longer matched
        var toRemove = currentClassifications.Where(c => c.Product == null || !matchedList.Any(p => p.Id == c.Product.Id)).ToList();
        if (toRemove.Any())
        {
            foreach (var rem in toRemove)
            {
                context.Set<Classification>().Remove(rem);
            }
        }

        // Add classifications for products that are matched but not currently classified
        var existingProductIds = currentClassifications.Where(c => c.Product != null).Select(c => c.Product!.Id).ToHashSet();
        var toAdd = matchedList.Where(p => !existingProductIds.Contains(p.Id)).ToList();

        foreach (var prod in toAdd)
        {
            var classification = new Classification
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
