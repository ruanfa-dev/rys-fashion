using Core.Catalog.Taxonomies;

using ErrorOr;

using Mapster;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.Taxons.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Taxons.Get.Id;
public partial class GetTaxonById
{
    public sealed record Result : TaxonResult.Details;
    public sealed record Query(Guid Id) : IQuery<Result>;
    public sealed class Handler(IApplicationDbContext context, ILogger<Handler> logger) : IQueryHandler<Query, Result>
    {
        private readonly IApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly ILogger<Handler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var taxon = await _context.Set<Taxon>()
                    .AsNoTracking()
                    .Include(t => t.Taxonomy)
                    .Include(t => t.Parent)
                    .Include(t => t.Children)
                    .Include(t => t.Translations)
                    .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

                if (taxon == null)
                    return Taxon.Errors.NotFound(request.Id);

                var result = taxon.Adapt<Result>();
                _logger.LogDebug("Retrieved taxon {TaxonId}", request.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving taxon {TaxonId}", request.Id);
                return Taxon.Errors.UnexpectedError(nameof(GetTaxonById), ex);
            }
        }
    }
}
