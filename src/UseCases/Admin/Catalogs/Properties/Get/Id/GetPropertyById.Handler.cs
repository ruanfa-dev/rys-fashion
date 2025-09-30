using Core.Catalog.Properties;

using ErrorOr;

using Mapster;

using Microsoft.EntityFrameworkCore;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.Properties.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Properties.Get.Id;
public static partial class GetPropertyById
{
    public record Result : PropertyResult.Details;
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
                Result? entity = await dbContext.Set<Property>()
                    .AsNoTracking()
                    .Where(p => p.Id == request.Id)
                    .ProjectToType<Result>()
                    .FirstOrDefaultAsync(cancellationToken);

                if (entity is null)
                    return Property.Errors.NotFound(request.Id);

                return entity;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retrieving property by ID {PropertyId}: {ErrorMessage}", request.Id, ex.Message);
                return Property.Errors.UnexpectedError(nameof(GetPropertyById), ex);
            }

        }
    }
}
