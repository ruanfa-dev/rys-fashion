using Core.Catalog.Properties;

using FluentValidation;

using UseCases.Common.Validations.Attributes;

namespace UseCases.Admin.Catalogs.Properties.Commons;

public sealed class PropertyParamValidator : AbstractValidator<PropertyParam>
{
    public PropertyParamValidator()
    {
        this.ApplyParameterizableNameRules(nameof(Property));
        this.ApplyMetadataSupportRules(nameof(Property));
        this.ApplyPositionableRules(nameof(Property));

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