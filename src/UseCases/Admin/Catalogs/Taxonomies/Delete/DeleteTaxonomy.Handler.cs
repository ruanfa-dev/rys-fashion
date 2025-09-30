using Core.Catalog.Taxonomies;

using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Taxonomies.Delete;
public partial class DeleteTaxonomy
{
    public sealed class Handler(
        IApplicationDbContext context
    ) : ICommandHandler<Command, Deleted>
    {
        public async Task<ErrorOr<Deleted>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                Taxonomy? entity = await context.Set<Taxonomy>()
                    .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
                if (entity is null) return Taxonomy.Errors.NotFound(request.Id);

                // business validation before delete
                ErrorOr<ErrorOr.Deleted> validation = entity.Delete();
                if (validation.IsError) return validation.Errors;

                context.Set<Taxonomy>().Remove(entity);
                await context.SaveChangesAsync(cancellationToken);

                return new Deleted(request.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting taxonomy {TaxonomyId}", request.Id);
                return Taxonomy.Errors.UnexpectedError(nameof(DeleteTaxonomy), ex);
            }
        }
    }
}
