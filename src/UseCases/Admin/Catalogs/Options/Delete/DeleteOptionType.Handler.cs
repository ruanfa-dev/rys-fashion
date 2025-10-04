using Core.Catalog.Options;

using ErrorOr;

using Microsoft.EntityFrameworkCore;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Options.Delete;
public partial class DeleteOptionType
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
                OptionType? entity = await dbContext.Set<OptionType>()
                    .Include(m => m.ProductOptionTypes)
                    .Include(m => m.OptionTypePrototypes)
                    .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

                if (entity is null)
                    return OptionType.Errors.NotFound(request.Id);

                // Check: if entity is used
                ErrorOr<Deleted> deletedResult = entity.Delete();
                if (deletedResult.IsError)
                    return deletedResult.Errors;

                // Delete: entity
                dbContext.Set<OptionType>().Remove(entity);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Deleted;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while deleting option type with id {OptionTypeId}", request.Id);
                return OptionType.Errors.UnexpectedError(nameof(Options.Delete.DeleteOptionType), ex);
            }

        }
    }
}
