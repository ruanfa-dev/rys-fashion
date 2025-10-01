using Core.Catalog.Taxonomies;

using ErrorOr;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Taxonomies.Create;

public static partial class CreateTaxonomy
{
    public sealed class EventHandler(
        IApplicationDbContext context, 
        ILogger<EventHandler> logger)
        : IDomainEventHandler<Taxonomy.Events.Created>
    {
        public async Task Handle(Taxonomy.Events.Created notification, CancellationToken cancellationToken)
        {
            logger.LogInformation("Domain Event: {DomainEvent} for Taxonomy {TaxonomyId}", notification.GetType().Name, notification.TaxonomyId);

            // Load taxonomy with taxons
            Taxonomy? taxonomy = await context.Set<Taxonomy>()
                .Include(t => t.Taxons)
                .FirstOrDefaultAsync(t => t.Id == notification.TaxonomyId, cancellationToken);

            if (taxonomy == null)
            {
                logger.LogWarning("Taxonomy {TaxonomyId} not found when handling Created event.", notification.TaxonomyId);
                return;
            }

            // Ensure root exists
            ErrorOr<Taxon> rootResult = taxonomy.EnsureRoot();
            if (rootResult.IsError)
            {
                logger.LogError("Failed to ensure root taxon for taxonomy {TaxonomyId}: {Errors}", notification.TaxonomyId, string.Join(';', rootResult.Errors.Select(e => e.Code)));
                return;
            }

            // Persist root if it was created
            Taxon root = rootResult.Value;
            bool exists = await context.Set<Taxon>().AnyAsync(t => t.Id == root.Id, cancellationToken);
            if (!exists)
            {
                await context.Set<Taxon>().AddAsync(root, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Created root taxon {TaxonId} for taxonomy {TaxonomyId}", root.Id, taxonomy.Id);
            }
        }
    }

}
