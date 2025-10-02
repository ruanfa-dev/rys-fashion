using Core.Catalog.Taxonomies;

using ErrorOr;

using Mapster;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.Taxons.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Taxons.Reposition;

public static partial class RepositionTaxon
{
    public sealed record Param(Guid Id, Guid? ParentId, int Index);
    public sealed record Result : TaxonResult.ListItem;
    public sealed record Command(Param Param) : ICommand<Result>;

    public sealed class Handler(IApplicationDbContext context, ILogger<Handler> logger)
        : ICommandHandler<Command, Result>
    {
        private readonly IApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly ILogger<Handler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                Param param = request.Param;

                // Load taxon with its entire subtree to move them together
                Taxon? taxon = await _context.Set<Taxon>()
                    .Include(t => t.Parent)
                        .ThenInclude(p => p!.Children)
                    .Include(t => t.Taxonomy)
                    .Include(t => t.Translations)
                    .FirstOrDefaultAsync(t => t.Id == param.Id, cancellationToken);

                if (taxon == null)
                    return Taxon.Errors.NotFound(param.Id);

                // Load all descendants recursively (the entire subtree)
                await LoadDescendantsRecursively(taxon, cancellationToken);

                // Store old parent for cleanup
                Taxon? oldParent = taxon.Parent;

                // Handle parent change if specified
                if (param.ParentId != taxon.ParentId)
                {
                    if (param.ParentId.HasValue)
                    {
                        // Load new parent
                        Taxon? newParent = await _context.Set<Taxon>()
                            .Include(t => t.Children)
                            .FirstOrDefaultAsync(t => t.Id == param.ParentId.Value, cancellationToken);

                        if (newParent == null)
                            return Taxon.Errors.NotFound(param.ParentId.Value);

                        // Validate not moving to a descendant (circular reference)
                        if (taxon.IsAncestorOf(newParent))
                            return Taxon.Errors.CircularReference;

                        // Set new parent
                        ErrorOr<Taxon> setParentResult = taxon.SetParent(newParent);
                        if (setParentResult.IsError)
                            return setParentResult.Errors;

                        newParent.AddChild(taxon);
                    }
                    else
                    {
                        // Remove parent (make root)
                        if (oldParent != null)
                        {
                            oldParent.RemoveChild(taxon);
                        }
                        taxon.ParentId = null;
                        taxon.Parent = null;
                    }

                    // Regenerate pretty name and permalink for the entire subtree
                    taxon.RegeneratePrettyNameAndPermalink();
                }

                // Update child index
                ErrorOr<Success> updateIndexResult = taxon.UpdateChildIndex(param.Index);
                if (updateIndexResult.IsError)
                    return updateIndexResult.Errors;

                // Emit moved event (triggers nested set recalculation)
                taxon.AddDomainEvent(new Taxon.Events.Moved(taxon.Id, taxon.ParentId, param.Index));

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Repositioned taxon {TaxonId} (with {DescendantCount} descendants) to parent {ParentId} at index {Index}",
                    taxon.Id, taxon.GetAllDescendants().Count(), param.ParentId, param.Index);

                return taxon.Adapt<Result>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error repositioning taxon {TaxonId}", request.Param.Id);
                return Taxon.Errors.UnexpectedError(nameof(RepositionTaxon), ex);
            }
        }

        private async Task LoadDescendantsRecursively(Taxon taxon, CancellationToken cancellationToken)
        {
            // Load immediate children
            await _context.Set<Taxon>()
                .Where(t => t.ParentId == taxon.Id)
                .LoadAsync(cancellationToken);

            // Now that children are loaded, recursively load their descendants
            foreach (Taxon child in taxon.Children)
            {
                await LoadDescendantsRecursively(child, cancellationToken);
            }
        }
    }
}