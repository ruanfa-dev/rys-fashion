using Core.Catalogs;

using ErrorOr;

using Mapster;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.Properties.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Catalogs.Properties.GetById;
public partial class GetPropertyById
{
    public record Result : PropertyResult;
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
                var entity = await dbContext.Properties.AsNoTracking()
                    .Where(p => p.Id == request.Id)
                    .ProjectToType<Result>()
                    .FirstOrDefaultAsync(cancellationToken);

                if (entity is null)
                    return Property.Errors.NotFound(request.Id);

                return entity;
            }
            catch (Exception ex)
            {
                return Property.Errors.PropertyUnexpected(nameof(GetPropertyById), ex.Message);
            }

        }
    }
}
