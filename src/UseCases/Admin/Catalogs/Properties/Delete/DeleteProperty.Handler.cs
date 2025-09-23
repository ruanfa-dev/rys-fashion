using Core.Catalogs;

using ErrorOr;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;

namespace UseCases.Catalogs.Properties.Delete;
public partial class DeleteProperty
{
    public record Command(Guid Id) : ICommand<Deleted>;

    public class Handler(IUnitOfWork unitOfWork) : ICommandHandler<Command, Deleted>
    {
        public async Task<ErrorOr<Deleted>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var dbContext = unitOfWork.Context;
                // Check: Property existing
                var property = await dbContext.Properties
                    .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

                if (property is null)
                    return Property.Errors.NotFound(request.Id);

                // Check: in use in products
                var isProductInUse = await dbContext.Set<ProductProperty>()
                    .AsNoTracking()
                    .AnyAsync(cp => cp.PropertyId == property.Id, cancellationToken);
                if (isProductInUse)
                    return Property.Errors.PropertyInUse(request.Id);

                // Check: in use in prototypes 
                var isPrototypeInUse = await dbContext.Set<PrototypeProperty>()
                    .AsNoTracking()
                    .AnyAsync(cp => cp.PropertyId == property.Id, cancellationToken);
                if (isPrototypeInUse)
                    return Property.Errors.PrototypeInUse(request.Id);

                // Raise: domain events before the actual deletion
                property.AddDomainEvent(new Property.Events.Deleted(property.Id));

                // Delete: property
                dbContext.Properties.Remove(property);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Deleted;
            }
            catch (Exception ex)
            {
                return Property.Errors.PropertyUnexpected(nameof(DeleteProperty), ex.Message);
            }

        }
    }
}
