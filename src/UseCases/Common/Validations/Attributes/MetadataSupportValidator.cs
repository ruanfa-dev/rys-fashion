using FluentValidation;

using SharedKernel.Domain.Attributes.Metadata;

namespace UseCases.Common.Validations.Attributes;

// Generic validator for any param/DTO implementing IMetadataSupport.
// Include this validator in concrete validators:
//   Include(new MetadataSupportValidator<PropertyParam>());
public sealed class MetadataSupportValidator : AbstractValidator<MetadataParam> 
{
    public MetadataSupportValidator()
    {
        // PublicMetadata: optional (nullable). If present, validate entries and total count.
        RuleFor(x => x.PublicMetadata)
            .Must(dict => dict is null || dict.Count <= MetadataConstraints.MaxEntries)
            .WithErrorCode(MetadataErrors.PublicMetadataTooManyEntries.Code)
            .WithMessage(MetadataErrors.PublicMetadataTooManyEntries.Description);

        When(x => x.PublicMetadata != null, () =>
        {
            RuleForEach(x => x.PublicMetadata)
                .ChildRules(kvp =>
                {
                    kvp.RuleFor(p => p.Key)
                        .NotEmpty()
                        .WithErrorCode(MetadataErrors.MetadataKeyRequired.Code)
                        .WithMessage(MetadataErrors.MetadataKeyRequired.Description)
                        .Matches(MetadataConstraints.KeyAllowedPattern)
                        .WithErrorCode(MetadataErrors.MetadataKeyInvalidChars.Code)
                        .WithMessage(MetadataErrors.MetadataKeyInvalidChars.Description)
                        .Length(MetadataConstraints.KeyMinLength, MetadataConstraints.KeyMaxLength)
                        .WithErrorCode(MetadataErrors.MetadataKeyInvalidLength.Code)
                        .WithMessage(MetadataErrors.MetadataKeyInvalidLength.Description);

                    kvp.RuleFor(p => p.Value)
                        .Must(v => v is null || v.Length >= MetadataConstraints.ValueMinLength && v.Length <= MetadataConstraints.ValueMaxLength)
                        .WithErrorCode(MetadataErrors.MetadataValueInvalidLength.Code)
                        .WithMessage(MetadataErrors.MetadataValueInvalidLength.Description);
                });
        });

        // PrivateMetadata: same rules as public
        RuleFor(x => x.PrivateMetadata)
            .Must(dict => dict is null || dict.Count <= MetadataConstraints.MaxEntries)
            .WithErrorCode(MetadataErrors.PrivateMetadataTooManyEntries.Code)
            .WithMessage(MetadataErrors.PrivateMetadataTooManyEntries.Description);

        When(x => x.PrivateMetadata != null, () =>
        {
            RuleForEach(x => x.PrivateMetadata)
                .ChildRules(kvp =>
                {
                    kvp.RuleFor(p => p.Key)
                        .NotEmpty()
                        .WithErrorCode(MetadataErrors.MetadataKeyRequired.Code)
                        .WithMessage(MetadataErrors.MetadataKeyRequired.Description)
                        .Matches(MetadataConstraints.KeyAllowedPattern)
                        .WithErrorCode(MetadataErrors.MetadataKeyInvalidChars.Code)
                        .WithMessage(MetadataErrors.MetadataKeyInvalidChars.Description)
                        .Length(MetadataConstraints.KeyMinLength, MetadataConstraints.KeyMaxLength)
                        .WithErrorCode(MetadataErrors.MetadataKeyInvalidLength.Code)
                        .WithMessage(MetadataErrors.MetadataKeyInvalidLength.Description);

                    kvp.RuleFor(p => p.Value)
                        .Must(v => v is null || v.Length >= MetadataConstraints.ValueMinLength && v.Length <= MetadataConstraints.ValueMaxLength)
                        .WithErrorCode(MetadataErrors.MetadataValueInvalidLength.Code)
                        .WithMessage(MetadataErrors.MetadataValueInvalidLength.Description);
                });
        });
    }
}