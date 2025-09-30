using FluentValidation;
using Core.Catalog.Taxonomies;
using UseCases.Common.Validations.Attributes;

namespace UseCases.Admin.Catalogs.Taxons.Commons;
public sealed class TaxonParamValidator : AbstractValidator<TaxonParam>
{
    public TaxonParamValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(Taxon.Errors.NameRequired.Code)
            .WithMessage(Taxon.Errors.NameRequired.Description)
            .Length(Taxon.Constraints.NameMinLength, Taxon.Constraints.NameMaxLength)
            .WithErrorCode(Taxon.Errors.InvalidNameLength.Code)
            .WithMessage(Taxon.Errors.InvalidNameLength.Description);

        RuleFor(x => x.Description)
            .MaximumLength(Taxon.Constraints.DescriptionMaxLength)
            .WithErrorCode(Taxon.Errors.DescriptionTooLong.Code)
            .WithMessage(Taxon.Errors.DescriptionTooLong.Description)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.TaxonomyId)
            .NotEmpty()
            .WithErrorCode(Taxon.Errors.TaxonomyRequired.Code)
            .WithMessage(Taxon.Errors.TaxonomyRequired.Description);

        RuleFor(x => x.RulesMatchPolicy)
            .Must(r => string.IsNullOrWhiteSpace(r) || Taxon.Constraints.RulesMatchPolicies.Contains(r))
            .WithErrorCode(Taxon.Errors.InvalidRulesMatchPolicy.Code)
            .WithMessage(Taxon.Errors.InvalidRulesMatchPolicy.Description);

        RuleFor(x => x.SortOrder)
            .Must(s => string.IsNullOrWhiteSpace(s) || Taxon.Constraints.SortOrders.Contains(s))
            .WithErrorCode(Taxon.Errors.InvalidSortOrder.Code)
            .WithMessage(Taxon.Errors.InvalidSortOrder.Description);

        RuleFor(x => x.MetaTitle)
            .MaximumLength(Taxon.Constraints.MetaFieldMaxLength)
            .WithErrorCode(Taxon.Errors.MetaTitleTooLong.Code)
            .WithMessage(Taxon.Errors.MetaTitleTooLong.Description)
            .When(x => !string.IsNullOrWhiteSpace(x.MetaTitle));

        RuleFor(x => x.MetaDescription)
            .MaximumLength(Taxon.Constraints.MetaFieldMaxLength)
            .WithErrorCode(Taxon.Errors.MetaDescriptionTooLong.Code)
            .WithMessage(Taxon.Errors.MetaDescriptionTooLong.Description)
            .When(x => !string.IsNullOrWhiteSpace(x.MetaDescription));

        RuleFor(x => x.MetaKeywords)
            .MaximumLength(Taxon.Constraints.MetaFieldMaxLength)
            .WithErrorCode(Taxon.Errors.MetaKeywordsTooLong.Code)
            .WithMessage(Taxon.Errors.MetaKeywordsTooLong.Description)
            .When(x => !string.IsNullOrWhiteSpace(x.MetaKeywords));

        RuleFor(x => x.ImageUrl)
            .Must(x => x == null || Uri.TryCreate(x, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
            Taxon.Constraints.ValidImageExtensions.Contains(Path.GetExtension(x).ToLowerInvariant()))
            .WithErrorCode(Taxon.Errors.InvalidImageContentType.Code)
            .WithMessage(Taxon.Errors.InvalidImageContentType.Description)
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl));

        RuleFor(x => x.SquareImageUrl)
            .Must(x => x == null || Uri.TryCreate(x, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
            Taxon.Constraints.ValidImageExtensions.Contains(Path.GetExtension(x).ToLowerInvariant()))
            .WithErrorCode(Taxon.Errors.InvalidSquareImageContentType.Code)
            .WithMessage(Taxon.Errors.InvalidSquareImageContentType.Description)
            .When(x => !string.IsNullOrWhiteSpace(x.SquareImageUrl));

        Include(new MetadataSupportValidator());
    }
}
