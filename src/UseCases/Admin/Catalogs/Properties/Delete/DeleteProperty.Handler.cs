using Core.Catalog.Properties;

using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Properties.Delete;
public partial class DeleteProperty
{
    public record Command(Guid Id) : ICommand<Deleted>;

    public class Handler(IUnitOfWork unitOfWork) : ICommandHandler<Command, Deleted>
    {
        public async Task<ErrorOr<Deleted>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                IApplicationDbContext dbContext = unitOfWork.Context;

                // Check: entity existing
                Property? entity = await dbContext.Set<Property>()
                    .Include(m => m.ProductProperties)
                    .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

                if (entity is null)
                    return Property.Errors.NotFound(request.Id);

                // Check: if entity is used
                ErrorOr<Deleted> deletedResult = entity.Delete();
                if (deletedResult.IsError)
                    return deletedResult.Errors;

                // Delete: entity
                dbContext.Set<Property>().Remove(entity);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Deleted;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while deleting property with id {PropertyId}", request.Id);
                return Property.Errors.UnexpectedError(nameof(DeleteProperty), ex);
            }

        }
    }
}
