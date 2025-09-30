using Core.Catalog.Taxonomies;

using ErrorOr;

using FluentValidation;

using Mapster;

using Microsoft.EntityFrameworkCore;

using Serilog;

using SharedKernel.Messaging.Abstracts;
using MediatR;

using UseCases.Common.Persistence.Context;
using UseCases.Admin.Catalogs.Taxonomies.Commons;

namespace UseCases.Admin.Catalogs.Taxonomies.Update;
public partial class UpdateTaxonomy
{
    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Param).SetValidator(new TaxonomyParamValidator());
        }
    }

    public sealed class Handler(
        IUnitOfWork unitOfWork
    ) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                IApplicationDbContext dbContext = unitOfWork.Context;
                Taxonomy? entity = await dbContext.Set<Taxonomy>()
                    .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
                if (entity is null)
                    return Taxonomy.Errors.NotFound(request.Id);

                Param param = request.Param;
                ErrorOr<Taxonomy> updateResult = entity.Update(param.Name, param.Position);
                if (updateResult.IsError)
                    return updateResult.Errors;

                dbContext.Set<Taxonomy>().Update(updateResult.Value);
                // Persist changes
                await unitOfWork.SaveChangesAsync(cancellationToken);

                Result result = updateResult.Value.Adapt<Result>();
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while updating taxonomy {TaxonomyId}", request.Id);
                return Taxonomy.Errors.UnexpectedError(nameof(UpdateTaxonomy), ex);
            }
        }
    }
}
internal sealed class SyncRootTaxonNameEventHandler(IApplicationDbContext context)
    : INotificationHandler<Taxonomy.Events.Updated>
{
    public async Task Handle(Taxonomy.Events.Updated notification, CancellationToken cancellationToken)
    {
        Log.Information("Domain Event: {DomainEvent} for Taxonomy {TaxonomyId}", notification.GetType().Name, notification.TaxonomyId);

        Taxonomy? taxonomy = await context.Set<Taxonomy>()
            .Include(t => t.Taxons)
            .ThenInclude(x => x.Children)
            .FirstOrDefaultAsync(t => t.Id == notification.TaxonomyId, cancellationToken);

        if (taxonomy == null)
        {
            Log.Warning("Taxonomy {TaxonomyId} not found when handling Updated event.", notification.TaxonomyId);
            return;
        }

        Taxon? root = taxonomy.Root;
            if (root == null)
            {
            // Ensure root exists if missing
            ErrorOr<Taxon> ensure = taxonomy.EnsureRoot();
            if (ensure.IsError)
            {
                Log.Error("Failed to create root for taxonomy {TaxonomyId} during update handler: {Errors}", taxonomy.Id, string.Join(';', ensure.Errors.Select(e => e.Code)));
                return;
            }

            root = ensure.Value;
            bool exists = await context.Set<Taxon>().AnyAsync(t => t.Id == root.Id, cancellationToken);
            if (!exists)
            {
                    await context.Set<Taxon>().AddAsync(root, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
            }
        }

        // If root name differs, update and persist
        if (root.Name != taxonomy.Name)
        {
            ErrorOr<Taxon> upd = root.Update(taxonomy.Name);
            if (upd.IsError)
            {
                Log.Error("Failed to update root taxon name for taxonomy {TaxonomyId}: {Errors}", taxonomy.Id, string.Join(';', upd.Errors.Select(e => e.Code)));
                return;
            }

            context.Set<Taxon>().Update(root);
            await context.SaveChangesAsync(cancellationToken);
            Log.Information("Synchronized root taxon name {TaxonId} -> {Name} for taxonomy {TaxonomyId}", root.Id, root.Name, taxonomy.Id);
        }
    }
}
