using Core.Catalog.Taxonomies;

using FluentValidation;

using UseCases.Common.Validations.Attributes;

namespace UseCases.Admin.Catalogs.Taxonomies.Commons;
public sealed class TaxonomyParamValidator : AbstractValidator<TaxonomyParam>
{
    public TaxonomyParamValidator()
    {
        this.ApplyMetadataSupportRules(nameof(Taxonomy));
        this.ApplyPositionableRules(nameof(Taxonomy));

        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(Taxonomy.Constraints.NameMinLength)
            .MaximumLength(Taxonomy.Constraints.NameMaxLength);
    }
}
