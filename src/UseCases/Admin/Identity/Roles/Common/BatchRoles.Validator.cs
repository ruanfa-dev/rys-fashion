using Core.Identity.Roles;

using FluentValidation;

namespace UseCases.Admin.Identity.Roles.Common;

/// <summary>
/// Validator for batch role parameters
/// </summary>
public sealed class BatchRolesParamValidator : AbstractValidator<BatchRolesParam>
{
    public BatchRolesParamValidator()
    {
        RuleFor(x => x.RoleIds)
            .NotNull()
            .WithErrorCode(Role.Errors.RoleIdsRequired.Code)
            .WithMessage(Role.Errors.RoleIdsRequired.Description);
    }
}

