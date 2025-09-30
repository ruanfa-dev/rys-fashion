using Core.Catalog.Taxonomies;

using FluentValidation;

using UseCases.Common.Validations.Attributes;

namespace UseCases.Admin.Catalogs.Taxonomies.Commons;
public sealed class TaxonomyParamValidator : AbstractValidator<TaxonomyParam>
{
    public TaxonomyParamValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(Taxonomy.Constraints.NameMinLength)
            .MaximumLength(Taxonomy.Constraints.NameMaxLength);

        RuleFor(x => x.Position)
            .InclusiveBetween(Taxonomy.Constraints.PositionMin, Taxonomy.Constraints.PositionMax)
            .WithErrorCode(Taxonomy.Errors.InvalidPosition.Code)
            .WithMessage(Taxonomy.Errors.InvalidPosition.Description);

        Include(m => new MetadataSupportValidator());
    }
}
