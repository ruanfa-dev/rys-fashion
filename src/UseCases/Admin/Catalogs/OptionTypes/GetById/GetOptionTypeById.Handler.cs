using Core.Catalogs;

using ErrorOr;

using Mapster;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.OptionTypes.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.OptionTypes.GetById;
public partial class GetOptionTypeById
{
    public record Result : OptionTypeResult;
    public record Query(Guid Id) : IQuery<Result>;
    public class Handler(
        IApplicationDbContext dbContext
    ) : IQueryHandler<Query, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Check: property exists
            var entity = await dbContext.Set<OptionType>()
                .AsNoTracking()
                .Where(p => p.Id == request.Id)
                .ProjectToType<Result>()
                .FirstOrDefaultAsync(cancellationToken);

            if (entity is null)
                return OptionType.Errors.NotFound(request.Id);

            return entity;
        }
    }
}
