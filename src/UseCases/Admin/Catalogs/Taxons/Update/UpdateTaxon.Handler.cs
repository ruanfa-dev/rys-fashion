using Core.Catalog.Taxonomies;

using ErrorOr;

using FluentValidation;

using Mapster;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

using UseCases.Admin.Catalogs.Taxons.Commons;
using UseCases.Common.Persistence.Context;

namespace UseCases.Admin.Catalogs.Taxons.Update;

public static partial class UpdateTaxon
{
    public sealed record Param : TaxonParam;
    public sealed record Result : TaxonResult.ListItem;
    public sealed record Command(Guid Id, Param Param) : ICommand<Result>;

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
                Param param = request.Param;
                // Check: entity existing
                Taxon? taxon = await _context.Set<Taxon>()
                    .Include(t => t.Taxonomy)
                    .Include(t => t.Parent).ThenInclude(p => p!.Children)
                    .Include(t => t.Children)
                    .Include(t => t.Translations)
                    .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

                if (taxon == null)
                    return Taxon.Errors.NotFound(request.Id);

                // Check: uniqueness for name
                string trimmedName = param.Name?.Trim() ?? "";
                bool exists = await _context.Set<Taxon>()
                    .AnyAsync(t => t.Name == trimmedName && t.TaxonomyId == taxon.TaxonomyId && t.ParentId == param.ParentId && t.Id != request.Id, cancellationToken);
                if (exists)
                    return Taxon.Errors.NameAlreadyExists(trimmedName, taxon.TaxonomyId);

                // Store original ParentId to detect change
                Guid? originalParentId = taxon.ParentId;

                ErrorOr<Taxon> updateResult = taxon.Update(
                    trimmedName,
                    param.ParentId,
                    param.Description,
                    param.Automatic,
                    param.RulesMatchPolicy,
                    param.SortOrder,
                    param.HideFromNav,
                    param.MetaTitle,
                    param.MetaDescription,
                    param.MetaKeywords,
                    param.ImageUrl,
                    param.SquareImageUrl,
                    param.PublicMetadata,
                    param.PrivateMetadata);
                if (updateResult.IsError) return updateResult.Errors;

                // If parent changed, load new parent and call SetParent to update navigation's
                if (param.ParentId != originalParentId)
                {
                    Taxon? newParent = await _context.Set<Taxon>()
                        .Include(t => t.Children)
                        .FirstOrDefaultAsync(t => t.Id == param.ParentId, cancellationToken);
                    if (newParent == null)
                        return Taxon.Errors.NotFound(param.ParentId ?? Guid.Empty);

                    ErrorOr<Taxon> setParentResult = taxon.SetParent(newParent);
                    if (setParentResult.IsError) return setParentResult.Errors;
                }

                // Regenerate pretty name and permalink (recursively for descendants)
                taxon.RegeneratePrettyNameAndPermalink();

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated taxon {TaxonId} with name {Name}", taxon.Id, taxon.Name);
                return taxon.Adapt<Result>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating taxon {TaxonId}", request.Id);
                return Taxon.Errors.UnexpectedError(nameof(UpdateTaxon), ex);
            }
        }
    }
}