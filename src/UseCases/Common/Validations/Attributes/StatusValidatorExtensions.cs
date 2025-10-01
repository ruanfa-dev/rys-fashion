using FluentValidation;

using SharedKernel.Domain.Attributes.Status;

namespace UseCases.Common.Validations.Attributes;

public static class StatusValidatorExtensions
{
    /// <summary>
    /// Applies shared status rules to a concrete validator for types implementing IHasStatus&lt;TStatus&gt;.
    /// Validates that the status is not the default enum value and that it maps to a defined enum value.
    /// </summary>
    public static void ApplyStatusRules<T, TStatus>(this AbstractValidator<T> validator, string? prefix = null)
        where T : class, IHasStatus<TStatus>
        where TStatus : struct, Enum
    {
        string codePrefix = string.IsNullOrWhiteSpace(prefix) ? typeof(T).Name : prefix!;

        // Ensure status is not the default enum value (often used as "unspecified")
        validator.RuleFor(x => x.Status)
            .Must(s => !EqualityComparer<TStatus>.Default.Equals(s, default))
            .WithErrorCode(StatusErrors.StatusRequired(codePrefix).Code)
            .WithMessage(StatusErrors.StatusRequired(codePrefix).Description);

        // Ensure status is a defined enum value
        validator.RuleFor(x => x.Status)
            .Must(s => Enum.IsDefined(typeof(TStatus), s))
            .WithErrorCode(StatusErrors.InvalidStatus(codePrefix).Code)
            .WithMessage(StatusErrors.InvalidStatus(codePrefix).Description);
    }
}