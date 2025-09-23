using Core.Catalogs;

using ErrorOr;

using Mapster;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.OptionValues.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.OptionValues.GetById;
public partial class GetOptionValueById
{
    public record Result : OptionValueResult;
    public record Query(Guid Id) : IQuery<Result>;
    public class Handler(
        IApplicationDbContext dbContext
    ) : IQueryHandler<Query, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Check: property exists
            var entity = await dbContext.Set<OptionValue>().AsNoTracking()
                .Where(p => p.Id == request.Id)
                .ProjectToType<Result>()
                .FirstOrDefaultAsync(cancellationToken);

            if (entity is null)
                return OptionValue.Errors.NotFound(request.Id);

            return entity;
        }
    }
}
