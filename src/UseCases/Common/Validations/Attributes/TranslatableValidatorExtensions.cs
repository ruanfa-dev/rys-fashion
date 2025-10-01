using FluentValidation;
using SharedKernel.Domain.Attributes.TranslatableResource;

namespace UseCases.Common.Validations.Attributes;

public static class TranslatableValidatorExtensions
{
    /// <summary>
    /// Applies the shared translation validation rules to a concrete validator.
    /// Call this from concrete validators that implement ITranslation.
    /// </summary>
    /// <param name="validator">The concrete validator.</param>
    /// <param name="prefix">
    /// Optional prefix used to namespace error codes and contextualize messages.
    /// If null or whitespace the concrete type name will be used.
    /// </param>
    public static void ApplyTranslatableRules<T>(this AbstractValidator<T> validator, string? prefix = null)
        where T : class, ITranslation
    {
        string codePrefix = string.IsNullOrWhiteSpace(prefix) ? typeof(T).Name : prefix!;

        // Culture required and length
        validator.RuleFor(x => x.Culture)
            .NotEmpty()
            .WithErrorCode(TranslatableErrors.CultureRequired(codePrefix).Code)
            .WithMessage(TranslatableErrors.CultureRequired(codePrefix).Description)
            .MaximumLength(TranslatableConstraints.CultureMaxLength)
            .WithErrorCode(TranslatableErrors.InvalidCultureLength(codePrefix).Code)
            .WithMessage(TranslatableErrors.InvalidCultureLength(codePrefix).Description);

        // Fields count
        validator.RuleFor(x => x.Fields)
            .Must(dict => dict is null || dict.Count <= TranslatableConstraints.MaxFields)
            .WithErrorCode(TranslatableErrors.FieldsTooManyEntries(codePrefix).Code)
            .WithMessage(TranslatableErrors.FieldsTooManyEntries(codePrefix).Description);

        validator.When(x => x.Fields != null, () =>
        {
            validator.RuleForEach(x => x.Fields)
                .ChildRules(kvp =>
                {
                    kvp.RuleFor(p => p.Key)
                        .NotEmpty()
                        .WithErrorCode(TranslatableErrors.FieldKeyInvalidLength(codePrefix).Code) // reuse length code for empty
                        .WithMessage(TranslatableErrors.FieldKeyInvalidLength(codePrefix).Description)
                        .Matches(TranslatableConstraints.FieldKeyAllowedPattern)
                        .WithErrorCode(TranslatableErrors.FieldKeyInvalidChars(codePrefix).Code)
                        .WithMessage(TranslatableErrors.FieldKeyInvalidChars(codePrefix).Description)
                        .Length(TranslatableConstraints.FieldKeyMinLength, TranslatableConstraints.FieldKeyMaxLength)
                        .WithErrorCode(TranslatableErrors.FieldKeyInvalidLength(codePrefix).Code)
                        .WithMessage(TranslatableErrors.FieldKeyInvalidLength(codePrefix).Description);

                    kvp.RuleFor(p => p.Value)
                        .Must(v => v is null || (v.Length <= TranslatableConstraints.FieldValueMaxLength))
                        .WithErrorCode(TranslatableErrors.FieldValueInvalidLength(codePrefix).Code)
                        .WithMessage(TranslatableErrors.FieldValueInvalidLength(codePrefix).Description);
                });
        });
    }
}
