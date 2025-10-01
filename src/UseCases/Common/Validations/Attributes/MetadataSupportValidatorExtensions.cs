using FluentValidation;

using SharedKernel.Domain.Attributes.Metadata;

namespace UseCases.Common.Validations.Attributes;

public static class MetadataSupportValidatorExtensions
{
    /// <summary>
    /// Applies the shared metadata validation rules to a concrete validator.
    /// Call this from concrete validators that implement IMetadataSupport.
    /// </summary>
    /// <param name="validator">The concrete validator.</param>
    /// <param name="prefix">
    /// Optional prefix used to namespace error codes and messages.
    /// If null or whitespace the concrete type name will be used.
    /// </param>
    public static void ApplyMetadataSupportRules<T>(this AbstractValidator<T> validator, string? prefix = null)
        where T : class, IMetadataSupport
    {
        string codePrefix = string.IsNullOrWhiteSpace(prefix) ? typeof(T).Name : prefix!;

        // PublicMetadata: optional (nullable). If present, validate entries and total count.
        validator.RuleFor(x => x.PublicMetadata)
            .Must(dict => dict is null || dict.Count <= MetadataConstraints.MaxEntries)
            .WithErrorCode(MetadataErrors.PublicMetadataTooManyEntries(codePrefix).Code)
            .WithMessage(MetadataErrors.PublicMetadataTooManyEntries(codePrefix).Description);

        validator.When(x => x.PublicMetadata != null, () =>
        {
            validator.RuleForEach(x => x.PublicMetadata)
                .ChildRules(kvp =>
                {
                    kvp.RuleFor(p => p.Key)
                        .NotEmpty()
                        .WithErrorCode(MetadataErrors.MetadataKeyRequired(codePrefix).Code)
                        .WithMessage(MetadataErrors.MetadataKeyRequired(codePrefix).Description)
                        .Matches(MetadataConstraints.KeyAllowedPattern)
                        .WithErrorCode(MetadataErrors.MetadataKeyInvalidChars(codePrefix).Code)
                        .WithMessage(MetadataErrors.MetadataKeyInvalidChars(codePrefix).Description)
                        .Length(MetadataConstraints.KeyMinLength, MetadataConstraints.KeyMaxLength)
                        .WithErrorCode(MetadataErrors.MetadataKeyInvalidLength(codePrefix).Code)
                        .WithMessage(MetadataErrors.MetadataKeyInvalidLength(codePrefix).Description);

                    kvp.RuleFor(p => p.Value)
                        .Must(v => v is null || v.Length <= MetadataConstraints.ValueMaxLength)
                        .WithErrorCode(MetadataErrors.MetadataValueInvalidLength(codePrefix).Code)
                        .WithMessage(MetadataErrors.MetadataValueInvalidLength(codePrefix).Description);
                });
        });

        // PrivateMetadata: same rules as public
        validator.RuleFor(x => x.PrivateMetadata)
            .Must(dict => dict is null || dict.Count <= MetadataConstraints.MaxEntries)
            .WithErrorCode(MetadataErrors.PrivateMetadataTooManyEntries(codePrefix).Code)
            .WithMessage(MetadataErrors.PrivateMetadataTooManyEntries(codePrefix).Description);

        validator.When(x => x.PrivateMetadata != null, () =>
        {
            validator.RuleForEach(x => x.PrivateMetadata)
                .ChildRules(kvp =>
                {
                    kvp.RuleFor(p => p.Key)
                        .NotEmpty()
                        .WithErrorCode(MetadataErrors.MetadataKeyRequired(codePrefix).Code)
                        .WithMessage(MetadataErrors.MetadataKeyRequired(codePrefix).Description)
                        .Matches(MetadataConstraints.KeyAllowedPattern)
                        .WithErrorCode(MetadataErrors.MetadataKeyInvalidChars(codePrefix).Code)
                        .WithMessage(MetadataErrors.MetadataKeyInvalidChars(codePrefix).Description)
                        .Length(MetadataConstraints.KeyMinLength, MetadataConstraints.KeyMaxLength)
                        .WithErrorCode(MetadataErrors.MetadataKeyInvalidLength(codePrefix).Code)
                        .WithMessage(MetadataErrors.MetadataKeyInvalidLength(codePrefix).Description);

                    kvp.RuleFor(p => p.Value)
                        .Must(v => v is null || v.Length is >= MetadataConstraints.ValueMinLength and <= MetadataConstraints.ValueMaxLength)
                        .WithErrorCode(MetadataErrors.MetadataValueInvalidLength(codePrefix).Code)
                        .WithMessage(MetadataErrors.MetadataValueInvalidLength(codePrefix).Description);
                });
        });
    }
}