using Core.Catalog.Options;

using FluentValidation;

using UseCases.Common.Validations.Attributes;

namespace UseCases.Admin.Catalogs.Options.Commons;

public sealed class OptionTypeParamValidator : AbstractValidator<OptionTypeParam>
{
    public OptionTypeParamValidator()
    {
        this.ApplyParameterizableNameRules(nameof(OptionType));
        this.ApplyMetadataSupportRules(nameof(OptionType));
        this.ApplyPositionableRules(nameof(OptionType));
    }
}