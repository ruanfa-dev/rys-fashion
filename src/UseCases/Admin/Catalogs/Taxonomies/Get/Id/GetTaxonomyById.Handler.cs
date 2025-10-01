using Core.Catalog.Taxonomies;

using ErrorOr;

using Mapster;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.Taxonomies.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Taxonomies.Get.Id;
public partial class GetTaxonomyById
{
    public sealed record Result : TaxonomyResult.Details;
    public sealed record Query(Guid Id) : IQuery<Result>;
    public sealed class Handler(
        IApplicationDbContext context,
        ILogger<Handler> logger
    ) : IQueryHandler<Query, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                // Check: entity exists
                Taxonomy? entity = await context.Set<Taxonomy>()
                    .Include(m => m.Taxons)
                    .AsNoTracking()
                    .Include(t => t.Translations)
                    .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

                if (entity == null)
                    return Taxonomy.Errors.NotFound(request.Id);

                Result details = entity.Adapt<Result>();
                return details;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving taxonomy {TaxonomyId}", request.Id);
                return Taxonomy.Errors.UnexpectedError(nameof(GetTaxonomyById), ex);
            }
        }
    }
}
