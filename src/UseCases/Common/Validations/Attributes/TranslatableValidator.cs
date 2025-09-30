using FluentValidation;

using SharedKernel.Domain.Attributes.TranslatableResource;

namespace UseCases.Common.Validations.Attributes;

/// <summary>
/// Generic validator for any DTO/param implementing ITranslation.
/// Include this validator in concrete validators when validating translation objects.
/// </summary>
public sealed class TranslatableValidator : AbstractValidator<TranslationParam>
{
    public TranslatableValidator()
    {
        // Culture required and length
        RuleFor(x => x.Culture)
            .NotEmpty()
            .WithErrorCode(TranslatableErrors.CultureRequired().Code)
            .WithMessage(TranslatableErrors.CultureRequired().Description)
            .MaximumLength(TranslatableConstraints.CultureMaxLength)
            .WithErrorCode(TranslatableErrors.InvalidCultureLength().Code)
            .WithMessage(TranslatableErrors.InvalidCultureLength().Description);

        // Fields count
        RuleFor(x => x.Fields)
            .Must(dict => dict is null || dict.Count <= TranslatableConstraints.MaxFields)
            .WithErrorCode(TranslatableErrors.FieldsTooManyEntries().Code)
            .WithMessage(TranslatableErrors.FieldsTooManyEntries().Description);

        When(x => x.Fields != null, () =>
        {
            RuleForEach(x => x.Fields)
                .ChildRules(kvp =>
                {
                    kvp.RuleFor(p => p.Key)
                        .NotEmpty()
                        .WithErrorCode(TranslatableErrors.FieldKeyInvalidLength().Code) // reuse length code for empty
                        .WithMessage(TranslatableErrors.FieldKeyInvalidLength().Description)
                        .Matches(TranslatableConstraints.FieldKeyAllowedPattern)
                        .WithErrorCode(TranslatableErrors.FieldKeyInvalidChars().Code)
                        .WithMessage(TranslatableErrors.FieldKeyInvalidChars().Description)
                        .Length(TranslatableConstraints.FieldKeyMinLength, TranslatableConstraints.FieldKeyMaxLength)
                        .WithErrorCode(TranslatableErrors.FieldKeyInvalidLength().Code)
                        .WithMessage(TranslatableErrors.FieldKeyInvalidLength().Description);

                    kvp.RuleFor(p => p.Value)
                        .Must(v => v is null || (v.Length >= TranslatableConstraints.FieldValueMinLength && v.Length <= TranslatableConstraints.FieldValueMaxLength))
                        .WithErrorCode(TranslatableErrors.FieldValueInvalidLength().Code)
                        .WithMessage(TranslatableErrors.FieldValueInvalidLength().Description);
                });
        });
    }
}
