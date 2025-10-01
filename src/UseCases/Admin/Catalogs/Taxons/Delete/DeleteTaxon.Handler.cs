using Core.Catalog.Taxonomies;

using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Taxons.Delete;
public partial class DeleteTaxon
{
    public sealed record Command(Guid Id) : ICommand<Deleted>;

    public sealed class Handler(
        IApplicationDbContext context
    ) : ICommandHandler<Command, Deleted>
    {
        public async Task<ErrorOr<Deleted>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                Taxon? entity = await context.Set<Taxon>()
                    .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
                if (entity is null) return Taxon.Errors.NotFound(request.Id);

                ErrorOr<Deleted> validation = entity.Delete();
                if (validation.IsError) return validation.Errors;

                context.Set<Taxon>().Remove(entity);
                await context.SaveChangesAsync(cancellationToken);

                return Result.Deleted;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting taxon {TaxonId}", request.Id);
                return Taxon.Errors.UnexpectedError(nameof(DeleteTaxon), ex);
            }
        }
    }
}