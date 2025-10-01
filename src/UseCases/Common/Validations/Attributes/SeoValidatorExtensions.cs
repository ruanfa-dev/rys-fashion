using FluentValidation;

using SharedKernel.Domain.Attributes.Seo;

namespace UseCases.Common.Validations.Attributes;

public static class SeoValidatorExtensions
{
    /// <summary>
    /// Applies SEO-related validation rules to concrete validators for types implementing ISeoSupport.
    /// </summary>
    public static void ApplySeoRules<T>(this AbstractValidator<T> validator, string? prefix = null)
        where T : class, ISeoSupport
    {
        string codePrefix = string.IsNullOrWhiteSpace(prefix) ? typeof(T).Name : prefix!;

        // MetaTitle: optional, but max length
        validator.RuleFor(x => x.MetaTitle)
            .MaximumLength(SeoSupportConstraints.MetaTitleMaxLength)
            .WithErrorCode(SeoErrors.MetaTitleTooLong(codePrefix).Code)
            .WithMessage(SeoErrors.MetaTitleTooLong(codePrefix).Description)
            .When(x => !string.IsNullOrWhiteSpace(x.MetaTitle));

        // MetaDescription: optional, but max length
        validator.RuleFor(x => x.MetaDescription)
            .MaximumLength(SeoSupportConstraints.MetaDescriptionMaxLength)
            .WithErrorCode(SeoErrors.MetaDescriptionTooLong(codePrefix).Code)
            .WithMessage(SeoErrors.MetaDescriptionTooLong(codePrefix).Description)
            .When(x => !string.IsNullOrWhiteSpace(x.MetaDescription));

        // MetaKeywords: optional, but max length
        validator.RuleFor(x => x.MetaKeywords)
            .MaximumLength(SeoSupportConstraints.MetaFieldMaxLength)
            .WithErrorCode(SeoErrors.MetaKeywordsTooLong(codePrefix).Code)
            .WithMessage(SeoErrors.MetaKeywordsTooLong(codePrefix).Description)
            .When(x => !string.IsNullOrWhiteSpace(x.MetaKeywords));
    }
}