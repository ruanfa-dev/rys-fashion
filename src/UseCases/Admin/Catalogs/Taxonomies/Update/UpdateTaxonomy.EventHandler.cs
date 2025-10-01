using Core.Catalog.Taxonomies;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Taxonomies.Update;

public static partial class UpdateTaxonomy
{
    public sealed class EventHandler(
        IApplicationDbContext context,
        ILogger<EventHandler> logger)
        : INotificationHandler<Taxonomy.Events.Updated>
    {
        public async Task Handle(Taxonomy.Events.Updated notification, CancellationToken cancellationToken)
        {
            logger.LogInformation("Domain Event: {DomainEvent} for Taxonomy {TaxonomyId}", notification.GetType().Name, notification.TaxonomyId);

            Taxonomy? taxonomy = await context.Set<Taxonomy>()
                .Include(t => t.Taxons)
                .FirstOrDefaultAsync(t => t.Id == notification.TaxonomyId, cancellationToken);

            if (taxonomy == null)
            {
                logger.LogWarning("Taxonomy {TaxonomyId} not found when handling Updated event.", notification.TaxonomyId);
                return;
            }

            // Update root taxon name if taxonomy name changed
            Taxon? root = taxonomy.Root;
            if (root != null && root.Name != taxonomy.Name)
            {
                var updateResult = root.Update(name: taxonomy.Name);
                if (updateResult.IsError)
                {
                    logger.LogError("Failed to update root taxon name for taxonomy {TaxonomyId}: {Errors}", notification.TaxonomyId, string.Join(';', updateResult.Errors.Select(e => e.Code)));
                    return;
                }
                context.Set<Taxon>().Update(root);
                await context.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Updated root taxon {TaxonId} name to '{Name}' for taxonomy {TaxonomyId}", root.Id, taxonomy.Name, taxonomy.Id);
            }
        }
    }
}