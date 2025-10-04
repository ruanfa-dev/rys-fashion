using Core.Catalog.Options;

using ErrorOr;

using Mapster;

using Microsoft.EntityFrameworkCore;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.Options.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Options.Get.Id;
public static partial class GetOptionTypeById
{
    public record Result : OptionTypeResult.Details;
    public record Query(Guid Id) : IQuery<Result>;
    public class Handler(
        IApplicationDbContext dbContext
    ) : IQueryHandler<Query, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                // Check: entity exists
                Result? entity = await dbContext.Set<OptionType>()
                    .AsNoTracking()
                    .Where(p => p.Id == request.Id)
                    .ProjectToType<Result>()
                    .FirstOrDefaultAsync(cancellationToken);

                if (entity is null)
                    return OptionType.Errors.NotFound(request.Id);

                return entity;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving option type by ID {OptionTypeId}: {ErrorMessage}", request.Id, ex.Message);
                return OptionType.Errors.UnexpectedError(nameof(GetOptionTypeById), ex);
            }

        }
    }
}
