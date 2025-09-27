using FluentValidation;

namespace UseCases.Admin.Identity.Permissions.Common;
public sealed class BatchPermissionsParamValidator : AbstractValidator<BatchPermissionsParam>
{
    public BatchPermissionsParamValidator()
    {
        RuleForEach(x => x.Permissions)
            .SetValidator(new PermissionNameValidator());
    }
}
