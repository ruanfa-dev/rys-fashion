using Core.Catalog.Properties;

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
            .Length(Property.Constraints.NameMinLength, Property.Constraints.NameMaxLength)
            .WithErrorCode(Property.Errors.InvalidNameLength.Code)
            .WithMessage(Property.Errors.InvalidNameLength.Description);

        RuleFor(x => x.Presentation)
            .NotEmpty()
            .WithErrorCode(Property.Errors.PresentationRequired.Code)
            .WithMessage(Property.Errors.PresentationRequired.Description)
            .Length(Property.Constraints.PresentationMinLength, Property.Constraints.PresentationMaxLength)
            .WithErrorCode(Property.Errors.InvalidPresentationLength.Code)
            .WithMessage(Property.Errors.InvalidPresentationLength.Description);

        RuleFor(x => x.DisplayOn)
            .IsInEnum()
            .WithErrorCode(Property.Errors.InvalidDisplayOn.Code)
            .WithMessage(Property.Errors.InvalidDisplayOn.Description);

        RuleFor(x => x.Kind)
            .IsInEnum()
            .WithErrorCode(Property.Errors.InvalidKind.Code)
            .WithMessage(Property.Errors.InvalidKind.Description);

        RuleFor(x => x.Position)
            .InclusiveBetween(Property.Constraints.PositionMin, Property.Constraints.PositionMax)
            .WithErrorCode(Property.Errors.InvalidPosition.Code)
            .WithMessage(Property.Errors.InvalidPosition.Description);

    }
}