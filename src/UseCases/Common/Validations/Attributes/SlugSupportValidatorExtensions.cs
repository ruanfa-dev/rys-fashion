using FluentValidation;

using SharedKernel.Domain.Attributes.Seo;

namespace UseCases.Common.Validations.Attributes;

public static class SlugSupportValidatorExtensions
{
    /// <summary>
    /// Applies slug validation rules to a concrete validator for types implementing ISlugSupport.
    /// </summary>
    public static void ApplySlugRules<T>(this AbstractValidator<T> validator, string? prefix = null)
        where T : class, ISlugSupport
    {
        string codePrefix = string.IsNullOrWhiteSpace(prefix) ? typeof(T).Name : prefix!;

        validator.RuleFor(x => x.UrlSlug)
            .Matches(SlugSupportConstraints.SlugAllowedPattern)
            .WithErrorCode(SlugSupportErrors.SlugInvalidChars(codePrefix).Code)
            .WithMessage(SlugSupportErrors.SlugInvalidChars(codePrefix).Description)
            .When(x => !string.IsNullOrWhiteSpace(x.UrlSlug));

        validator.RuleFor(x => x.UrlSlug)
            .Length(SlugSupportConstraints.SlugMinLength, SlugSupportConstraints.SlugMaxLength)
            .WithErrorCode(SlugSupportErrors.SlugInvalidLength(codePrefix).Code)
            .WithMessage(SlugSupportErrors.SlugInvalidLength(codePrefix).Description)
            .When(x => !string.IsNullOrWhiteSpace(x.UrlSlug));
    }
}