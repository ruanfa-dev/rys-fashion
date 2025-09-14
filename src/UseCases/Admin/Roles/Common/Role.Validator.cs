using Core.Identity;

using FluentValidation;

namespace UseCases.Admin.Roles.Common;


/// <summary>
/// Validator for role names
/// </summary>
public sealed class RoleNameValidator : AbstractValidator<string>
{
    public RoleNameValidator()
    {
        RuleFor(x => x)
            .NotEmpty()
            .WithErrorCode(Role.Errors.NameRequired.Code)
            .WithMessage(Role.Errors.NameRequired.Description)
            .MinimumLength(Role.Constraints.MinNameLength)
            .WithErrorCode(Role.Errors.NameTooShort.Code)
            .WithMessage(Role.Errors.NameTooShort.Description)
            .MaximumLength(Role.Constraints.MaxNameLength)
            .WithErrorCode(Role.Errors.NameTooLong.Code)
            .WithMessage(Role.Errors.NameTooLong.Description)
            .Matches(Role.Constraints.NameAllowedPattern)
            .WithErrorCode(Role.Errors.NameInvalidFormat.Code)
            .WithMessage(Role.Errors.NameInvalidFormat.Description);
    }
}

/// <summary>
/// Validator for role parameters
/// </summary>
public sealed class RoleParamValidator : AbstractValidator<RoleParam>
{
    public RoleParamValidator()
    {
        RuleFor(x => x.Name)
            .SetValidator(new RoleNameValidator());

        RuleFor(x => x.DisplayName)
            .MaximumLength(Role.Constraints.MaxDisplayNameLength)
            .WithErrorCode(Role.Errors.DisplayNameTooLong.Code)
            .WithMessage(Role.Errors.DisplayNameTooLong.Description)
            .Matches(Role.Constraints.DisplayNameAllowedPattern)
            .WithErrorCode(Role.Errors.DisplayNameInvalidFormat.Code)
            .WithMessage(Role.Errors.DisplayNameInvalidFormat.Description)
            .When(x => !string.IsNullOrEmpty(x.DisplayName));

        RuleFor(x => x.Description)
            .MaximumLength(Role.Constraints.MaxDescriptionLength)
            .WithErrorCode(Role.Errors.DescriptionTooLong.Code)
            .WithMessage(Role.Errors.DescriptionTooLong.Description)
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(Role.Constraints.MinPriority)
            .WithErrorCode(Role.Errors.PriorityMinExceeded.Code)
            .WithMessage(Role.Errors.PriorityMinExceeded.Description)
            .LessThanOrEqualTo(Role.Constraints.MaxPriority)
            .WithErrorCode(Role.Errors.PriorityMaxExceeded.Code)
            .WithMessage(Role.Errors.PriorityMaxExceeded.Description);
    }
}

