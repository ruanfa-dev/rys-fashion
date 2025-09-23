using Core.Catalogs;

using ErrorOr;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.OptionValues.Delete;
public partial class DeleteOptionValue
{
    public record Command(Guid Id) : ICommand<Deleted>;

    public class Handler(IUnitOfWork unitOfWork) : ICommandHandler<Command, Deleted>
    {
        public async Task<ErrorOr<Deleted>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var dbContext = unitOfWork.Context;
                // Check: OptionValue existing
                var entity = await dbContext.Set<OptionValue>()
                    .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
                if (entity is null)
                    return OptionValue.Errors.NotFound(request.Id);

                // Check: option type in use in variants
                var hasVariantCount = await dbContext.Set<VariantOptionValue>()
                    .CountAsync(pot => pot.OptionValueId == entity.Id, cancellationToken);
                if (hasVariantCount > 0)
                    return OptionValue.Errors.InUseByVariants(hasVariantCount);

                // Raise: domain events before the actual deletion
                entity.AddDomainEvent(new OptionValue.Events.Deleted(entity.Id));

                // Delete: property
                dbContext.Set<OptionValue>().Remove(entity);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Deleted;
            }
            catch (Exception ex)
            {
                return OptionValue.Errors.OptionValueUnexpected(nameof(DeleteOptionValue), ex.Message);
            }

        }
    }
}
