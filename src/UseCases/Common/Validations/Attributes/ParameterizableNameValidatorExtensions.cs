using FluentValidation;
using SharedKernel.Domain.Attributes.Parameterizable;

namespace UseCases.Common.Validations.Attributes;

public static class ParameterizableNameValidatorExtensions
{
    /// <summary>
    /// Adds the shared "parameterizable name" rules to a concrete validator.
    /// Call this from concrete validators that implement IParameterizableName.
    /// </summary>
    public static void ApplyParameterizableNameRules<T>(this AbstractValidator<T> validator, string? prefix = null)
        where T : class, IParameterizableName
    {
        string codePrefix = string.IsNullOrWhiteSpace(prefix) ? typeof(T).Name : prefix!;

        // Name: required, length, allowed chars
        validator.RuleFor(x => x.Name!)
            .NotEmpty()
            .WithErrorCode(ParameterizableErrors.NameRequired(codePrefix).Code)
            .WithMessage(ParameterizableErrors.NameRequired(codePrefix).Description)
            .MinimumLength(ParameterizableConstraints.NameMinLength)
            .WithErrorCode(ParameterizableErrors.InvalidNameLength(codePrefix).Code)
            .WithMessage(ParameterizableErrors.InvalidNameLength(codePrefix).Description)
            .MaximumLength(ParameterizableConstraints.NameMaxLength)
            .WithErrorCode(ParameterizableErrors.InvalidNameLength(codePrefix).Code)
            .WithMessage(ParameterizableErrors.InvalidNameLength(codePrefix).Description)
            .Matches(ParameterizableConstraints.NameAllowedPattern)
            .WithErrorCode(ParameterizableErrors.NameInvalidChars(codePrefix).Code)
            .WithMessage(ParameterizableErrors.NameInvalidChars(codePrefix).Description);

        // Presentation: required, length
        validator.RuleFor(x => x.Presentation!)
            .NotEmpty()
            .WithErrorCode(ParameterizableErrors.PresentationRequired(codePrefix).Code)
            .WithMessage(ParameterizableErrors.PresentationRequired(codePrefix).Description)
            .MinimumLength(ParameterizableConstraints.PresentationMinLength)
            .WithErrorCode(ParameterizableErrors.InvalidPresentationLength(codePrefix).Code)
            .WithMessage(ParameterizableErrors.InvalidPresentationLength(codePrefix).Description)
            .MaximumLength(ParameterizableConstraints.PresentationMaxLength)
            .WithErrorCode(ParameterizableErrors.InvalidPresentationLength(codePrefix).Code)
            .WithMessage(ParameterizableErrors.InvalidPresentationLength(codePrefix).Description);
    }
}