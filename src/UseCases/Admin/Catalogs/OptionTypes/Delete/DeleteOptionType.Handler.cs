using Core.Catalogs;

using ErrorOr;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.OptionTypes.Delete;
public partial class DeleteOptionType
{
    public record Command(Guid Id) : ICommand<Deleted>;

    public class Handler(IUnitOfWork unitOfWork) : ICommandHandler<Command, Deleted>
    {
        public async Task<ErrorOr<Deleted>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var dbContext = unitOfWork.Context;
                // Check: OptionType existing
                var entity = await dbContext.Set<OptionType>()
                    .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

                if (entity is null)
                    return OptionType.Errors.NotFound(request.Id);

                // Check: option type in use in products
                var hasProducts = await dbContext.Set<ProductOptionType>()
                    .AsNoTracking()
                    .AnyAsync(pot => pot.OptionTypeId == entity.Id, cancellationToken);
                if (hasProducts)
                    return OptionType.Errors.HasProductAssociations(request.Id);

                // Check: option type has prototype associations
                var hasPrototypes = await dbContext.Set<PrototypeOptionType>()
                    .AsNoTracking()
                    .AnyAsync(op => op.OptionTypeId == entity.Id, cancellationToken);
                if (hasPrototypes)
                    return OptionType.Errors.HasPrototypeAssociations(request.Id);

                // Raise: domain events before the actual deletion
                entity.AddDomainEvent(new OptionType.Events.Deleted(entity.Id));

                // Delete: property
                dbContext.Set<OptionType>().Remove(entity);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Deleted;
            }
            catch (Exception ex)
            {
                return OptionType.Errors.OptionTypeUnexpected(nameof(DeleteOptionType), ex.Message);
            }

        }
    }
}
