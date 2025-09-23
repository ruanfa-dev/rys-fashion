using Core.Catalogs;

using FluentValidation;

namespace UseCases.Admin.Catalogs.Properties.Commons;

public sealed class PropertyParamValidator : AbstractValidator<PropertyParam>
{
    public PropertyParamValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(Property.Errors.NameRequired.Code)
            .WithMessage(Property.Errors.NameRequired.Description)
            .Length(Property.Contrainsts.NameMinLength, Property.Contrainsts.NameMaxLength)
            .WithErrorCode(Property.Errors.InvalidNameLength.Code)
            .WithMessage(Property.Errors.InvalidNameLength.Description)
            .Matches(Property.Contrainsts.NameRegex)
            .WithErrorCode(Property.Errors.InvalidNameFormat.Code)
            .WithMessage(Property.Errors.InvalidNameFormat.Description);

        RuleFor(x => x.Presentation)
            .NotEmpty()
            .WithErrorCode(Property.Errors.PresentationRequired.Code)
            .WithMessage(Property.Errors.PresentationRequired.Description)
            .Length(Property.Contrainsts.PresentationMinLength, Property.Contrainsts.PresentationMaxLength)
            .WithErrorCode(Property.Errors.InvalidPresentationLength.Code)
            .WithMessage(Property.Errors.InvalidPresentationLength.Description)
            .Matches(Property.Contrainsts.PresentationRegex)
            .WithErrorCode(Property.Errors.InvalidPresentationFormat.Code)
            .WithMessage(Property.Errors.InvalidPresentationFormat.Description);

        RuleFor(x => x.DisplayOn)
            .IsInEnum()
            .WithErrorCode(Property.Errors.InvalidDisplayOn.Code)
            .WithMessage(Property.Errors.InvalidDisplayOn.Description);

        RuleFor(x => x.Kind)
            .IsInEnum()
            .WithErrorCode(Property.Errors.InvalidKind.Code)
            .WithMessage(Property.Errors.InvalidKind.Description);

    }
}