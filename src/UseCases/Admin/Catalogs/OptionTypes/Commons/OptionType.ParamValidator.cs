using Core.Catalogs;

using FluentValidation;

namespace UseCases.Admin.Catalogs.OptionTypes.Commons;

public sealed class OptionTypeParamValidator : AbstractValidator<OptionTypeParam>
{
    public OptionTypeParamValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(OptionType.Errors.NameRequired.Code)
            .WithMessage(OptionType.Errors.NameRequired.Description)
            .Length(OptionType.Constraints.NameMinLength, OptionType.Constraints.NameMaxLength)
            .WithErrorCode(OptionType.Errors.InvalidNameLength.Code)
            .WithMessage(OptionType.Errors.InvalidNameLength.Description)
            .Matches(OptionType.Constraints.NameRegex)
            .WithErrorCode(OptionType.Errors.InvalidNameFormat.Code)
            .WithMessage(OptionType.Errors.InvalidNameFormat.Description);

        RuleFor(x => x.Presentation)
            .NotEmpty()
            .WithErrorCode(OptionType.Errors.PresentationRequired.Code)
            .WithMessage(OptionType.Errors.PresentationRequired.Description)
            .Length(OptionType.Constraints.PresentationMinLength, OptionType.Constraints.PresentationMaxLength)
            .WithErrorCode(OptionType.Errors.InvalidPresentationLength.Code)
            .WithMessage(OptionType.Errors.InvalidPresentationLength.Description)
            .Matches(OptionType.Constraints.PresentationRegex)
            .WithErrorCode(OptionType.Errors.InvalidPresentationFormat.Code)
            .WithMessage(OptionType.Errors.InvalidPresentationFormat.Description);


        RuleFor(x => x.Position)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode(OptionType.Errors.InvalidPosition.Code)
            .WithMessage(OptionType.Errors.InvalidPosition.Description);
    }
}