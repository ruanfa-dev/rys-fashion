using Core.Catalogs;

using FluentValidation;

namespace UseCases.Admin.Catalogs.OptionValues.Commons;

public sealed class OptionValueParamValidator : AbstractValidator<OptionValueParam>
{
    public OptionValueParamValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(OptionValue.Errors.NameRequired.Code)
            .WithMessage(OptionValue.Errors.NameRequired.Description)
            .Length(OptionValue.Constraints.NameMinLength, OptionValue.Constraints.NameMaxLength)
            .WithErrorCode(OptionValue.Errors.InvalidNameLength.Code)
            .WithMessage(OptionValue.Errors.InvalidNameLength.Description)
            .Matches(OptionValue.Constraints.NameRegex)
            .WithErrorCode(OptionValue.Errors.InvalidNameFormat.Code)
            .WithMessage(OptionValue.Errors.InvalidNameFormat.Description);

        RuleFor(x => x.Presentation)
            .NotEmpty()
            .WithErrorCode(OptionValue.Errors.PresentationRequired.Code)
            .WithMessage(OptionValue.Errors.PresentationRequired.Description)
            .Length(OptionValue.Constraints.PresentationMinLength, OptionValue.Constraints.PresentationMaxLength)
            .WithErrorCode(OptionValue.Errors.InvalidPresentationLength.Code)
            .WithMessage(OptionValue.Errors.InvalidPresentationLength.Description)
            .Matches(OptionValue.Constraints.PresentationRegex)
            .WithErrorCode(OptionValue.Errors.InvalidPresentationFormat.Code)
            .WithMessage(OptionValue.Errors.InvalidPresentationFormat.Description);

        RuleFor(x => x.Position)
            .GreaterThanOrEqualTo(0)
            .WithErrorCode(OptionValue.Errors.InvalidPosition.Code)
            .WithMessage(OptionValue.Errors.InvalidPosition.Description);

        RuleFor(x => x.OptionTypeId)
            .NotEmpty()
            .WithErrorCode(OptionValue.Errors.OptionTypeRequired.Code)
            .WithMessage(OptionValue.Errors.OptionTypeRequired.Description);
    }
}