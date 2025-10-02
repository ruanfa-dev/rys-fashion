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

                // Load taxon with necessary relationships
                Taxon? taxon = await _context.Set<Taxon>()
                    .Include(t => t.Taxonomy)
                    .Include(t => t.Parent)
                        .ThenInclude(p => p!.Children)
                    .Include(t => t.Children)
                    .Include(t => t.Translations)
                    .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

                if (taxon == null)
                    return Taxon.Errors.NotFound(request.Id);

                // Check name uniqueness if name is changing
                if (!string.IsNullOrWhiteSpace(param.Name))
                {
                    string trimmedName = param.Name.Trim();
                    bool nameExists = await _context.Set<Taxon>()
                        .AnyAsync(t => t.Name == trimmedName
                            && t.TaxonomyId == param.TaxonomyId
                            && t.ParentId == param.ParentId
                            && t.Id != request.Id,
                            cancellationToken);

                    if (nameExists)
                        return Taxon.Errors.NameAlreadyExists(trimmedName, param.TaxonomyId);
                }

                // Handle parent change if specified
                Taxon? newParent = null;
                if (param.ParentId != taxon.ParentId)
                {
                    if (param.ParentId.HasValue)
                    {
                        newParent = await _context.Set<Taxon>()
                            .Include(t => t.Children)
                            .FirstOrDefaultAsync(t => t.Id == param.ParentId.Value, cancellationToken);

                        if (newParent == null)
                            return Taxon.Errors.NotFound(param.ParentId.Value);
                    }
                }

                // Update taxon properties
                ErrorOr<Taxon> updateResult = taxon.Update(
                    param.Name,
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

                if (updateResult.IsError)
                    return updateResult.Errors;

                // Apply parent change if needed
                if (newParent != null)
                {
                    ErrorOr<Taxon> setParentResult = taxon.SetParent(newParent);
                    if (setParentResult.IsError)
                        return setParentResult.Errors;

                    newParent.AddChild(taxon);
                }
                else if (param.ParentId != taxon.ParentId && !param.ParentId.HasValue)
                {
                    // Remove parent (make root)
                    if (taxon.Parent != null)
                    {
                        taxon.Parent.RemoveChild(taxon);
                    }
                }

                // Regenerate pretty names and permalinks if parent changed or name changed
                if (param.ParentId != taxon.ParentId || (!string.IsNullOrWhiteSpace(param.Name) && param.Name.Trim() != taxon.Name))
                {
                    taxon.RegeneratePrettyNameAndPermalink();
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Updated taxon {TaxonId} with name '{Name}'",
                    taxon.Id, taxon.Name);

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