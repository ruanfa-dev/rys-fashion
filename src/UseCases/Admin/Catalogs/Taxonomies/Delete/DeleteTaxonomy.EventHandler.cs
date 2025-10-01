using Core.Catalog.Taxonomies;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Taxonomies.Delete;

public static partial class DeleteTaxonomy
{
    public sealed class EventHandler(
        IApplicationDbContext context,
        ILogger<EventHandler> logger)
        : IDomainEventHandler<Taxonomy.Events.Deleted>
    {
        public async Task Handle(Taxonomy.Events.Deleted notification, CancellationToken cancellationToken)
        {
            logger.LogInformation("Domain Event: {DomainEvent} for Taxonomy {TaxonomyId}", notification.GetType().Name, notification.TaxonomyId);

            var taxons = await context.Set<Taxon>()
                .Where(t => t.TaxonomyId == notification.TaxonomyId)
                .ToListAsync(cancellationToken);

            if (taxons.Count > 0)
            {
                context.Set<Taxon>().RemoveRange(taxons);
                await context.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Removed {Count} taxons for deleted taxonomy {TaxonomyId}", taxons.Count, notification.TaxonomyId);
            }
        }
    }
}