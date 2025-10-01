using FluentValidation;
using SharedKernel.Domain.Attributes.Positionable;

namespace UseCases.Common.Validations.Attributes;

public static class PositionableValidatorExtensions
{
    /// <summary>
    /// Applies shared positioning rules to a concrete validator.
    /// Adjust the generic constraint to match your project's interface/type that exposes `Position`.
    /// </summary>
    /// <param name="validator">The concrete validator.</param>
    /// <param name="prefix">Optional prefix used to namespace error codes/messages.</param>
    public static void ApplyPositionableRules<T>(this AbstractValidator<T> validator, string? prefix = null)
        where T : class, IPositionable 
    {
        string codePrefix = string.IsNullOrWhiteSpace(prefix) ? typeof(T).Name : prefix!;

        validator.RuleFor(x => x.Position)
            .NotNull()
            .WithErrorCode(PositionableErrors.PositionRequired(codePrefix).Code)
            .WithMessage(PositionableErrors.PositionRequired(codePrefix).Description)
            .InclusiveBetween(PositionableConstraints.PositionMin, PositionableConstraints.PositionMax)
            .WithErrorCode(PositionableErrors.PositionOutOfRange(codePrefix).Code)
            .WithMessage(PositionableErrors.PositionOutOfRange(codePrefix).Description);
    }
}
