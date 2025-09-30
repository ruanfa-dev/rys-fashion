using Core.Catalog.Taxonomies;

using ErrorOr;

using FluentValidation;

using Mapster;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.Taxons.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Taxons.Create;

public static partial class CreateTaxon
{
    public sealed record Param : Commons.TaxonParam;
    public sealed record Result : Commons.TaxonResult.Details;
    public sealed record Command(Param Param) : ICommand<Result>;

    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Param).SetValidator(new TaxonParamValidator());
        }
    }

    public sealed class Handler(IApplicationDbContext context, ILogger<Handler> logger)
        : ICommandHandler<Command, Result>
    {
        private readonly IApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly ILogger<Handler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                var param = request.Param;

                // Check uniqueness
                var trimmedName = param.Name!.Trim();
                var exists = await _context.Set<Taxon>()
                    .AnyAsync(t => t.Name == trimmedName && t.TaxonomyId == param.TaxonomyId && t.ParentId == param.ParentId, cancellationToken);
                if (exists)
                    return Taxon.Errors.NameAlreadyExists(trimmedName, param.TaxonomyId);

                var taxonomy = await _context.Set<Taxonomy>()
                    .Include(tx => tx.Taxons)
                    .FirstOrDefaultAsync(tx => tx.Id == param.TaxonomyId, cancellationToken);
                if (taxonomy == null)
                    return Taxon.Errors.UnexpectedError("TaxonomyNotFound", new Exception($"Taxonomy {param.TaxonomyId} not found"));

                // Create taxon
                var createResult = Taxon.Create(
                    trimmedName,
                    param.TaxonomyId,
                    param.ParentId,
                    param.Automatic ?? false,
                    param.RulesMatchPolicy,
                    param.SortOrder,
                    param.HideFromNav ?? false,
                    param.Description,
                    param.MetaTitle,
                    param.MetaDescription,
                    param.MetaKeywords,
                    param.ImageUrl,
                    param.SquareImageUrl,
                    param.PublicMetadata,
                    param.PrivateMetadata);
                if (createResult.IsError) return createResult.Errors;

                var taxon = createResult.Value;

                // Validate root conflict
                var rootValidation = taxon.ValidateForCreateAgainst(taxonomy);
                if (rootValidation.IsError) return rootValidation.Errors;

                // Set parent if provided
                if (param.ParentId.HasValue)
                {
                    var parent = await _context.Set<Taxon>()
                        .Include(t => t.Children)
                        .FirstOrDefaultAsync(t => t.Id == param.ParentId.Value, cancellationToken);
                    if (parent == null)
                        return Taxon.Errors.NotFound(param.ParentId.Value);

                    var parentResult = taxon.SetParent(parent);
                    if (parentResult.IsError) return parentResult.Errors;
                    parent.AddChild(taxon);
                }

                _context.Set<Taxon>().Add(taxon);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created taxon {TaxonId} with name {Name} in taxonomy {TaxonomyId}", taxon.Id, taxon.Name, taxon.TaxonomyId);
                return taxon.Adapt<Result>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating taxon {Name}", request.Param.Name);
                return Taxon.Errors.UnexpectedError(nameof(CreateTaxon), ex);
            }
        }
    }
}
