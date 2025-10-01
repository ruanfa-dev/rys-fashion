using Core.Catalog.Taxonomies;

using ErrorOr;

using Mapster;

using Microsoft.EntityFrameworkCore;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.Taxons.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Taxons.Reposition;
public partial class RepositionTaxon
{

    public sealed record Param(Guid Id, Guid? ParentId, int Index);
    public sealed record Result : TaxonResult.ListItem;
    public sealed record Command(Param Param) : ICommand<Result>;
    public sealed class Handler(
        IApplicationDbContext context
    ) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                Param param = request.Param;
                Taxon? entity = await context.Set<Taxon>()
                    .Include(t => t.Parent)
                    .Include(t => t.Children)
                    .Include(t => t.Taxonomy)
                    .Include(t => t.Translations)
                    .FirstOrDefaultAsync(t => t.Id == param.Id, cancellationToken);
                if (entity is null) return Taxon.Errors.NotFound(param.Id);

                Taxon? oldParent = null;
                if (entity.ParentId.HasValue)
                {
                    oldParent = await context.Set<Taxon>().FirstOrDefaultAsync(t => t.Id == entity.ParentId.Value, cancellationToken);
                }

                if (param.ParentId.HasValue)
                {
                    entity.ParentId = param.ParentId;
                    // set navigation if available
                    Taxon? newParent = await context.Set<Taxon>().FirstOrDefaultAsync(t => t.Id == param.ParentId.Value, cancellationToken);
                    if (newParent != null)
                    {
                        var setParentResult = entity.SetParent(newParent);
                        if (setParentResult.IsError)
                            return setParentResult.Errors;

                    }
                }

                // setting child index raises moved domain event
                var updateIndexResult = entity.UpdateChildIndex(param.Index);
                if (updateIndexResult.IsError)
                    return updateIndexResult.Errors;

                // regenerate pretty name/permalink after reparenting
                try { entity.RegeneratePrettyNameAndPermalink(); }
                catch (Exception exception)
                {
                    Log.Error(exception, "Error regenerating pretty name and permalink for taxon {TaxonId} after repositioning", entity.Id);
                    throw;
                }

                // Emit moved event explicitly so handlers can react
                entity.AddDomainEvent(new Taxon.Events.Moved(entity.Id, entity.ParentId, param.Index));

                await context.SaveChangesAsync(cancellationToken);

                return entity.Adapt<Result>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error repositioning taxon {TaxonId}", request.Param.Id);
                return Taxon.Errors.UnexpectedError(nameof(RepositionTaxon), ex);
            }
        }
    }
}