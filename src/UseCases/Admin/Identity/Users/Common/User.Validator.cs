using Core.Identity;
using Core.Identity.Roles;
using Core.Identity.Users;

using FluentValidation;

using UseCases.Admin.Identity.Permissions.Common;
using UseCases.Admin.Identity.Roles.Common;

namespace UseCases.Admin.Identity.Users.Common;

public sealed class UserParamValidator : AbstractValidator<UserParam>
{
    public UserParamValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithErrorCode(User.Errors.EmailRequired.Code)
            .WithMessage(User.Errors.EmailRequired.Description)
            .MaximumLength(User.Constraints.EmailMaxLength)
            .WithErrorCode(User.Errors.EmailTooLong.Code)
            .WithMessage(User.Errors.EmailTooLong.Description)
            .EmailAddress()
            .WithErrorCode(User.Errors.EmailInvalidFormat.Code)
            .WithMessage(User.Errors.EmailInvalidFormat.Description);

        RuleFor(x => x.FirstName)
            .MinimumLength(User.Constraints.NameMinLength)
            .WithErrorCode(User.Errors.FirstNameTooShort.Code)
            .WithMessage(User.Errors.FirstNameTooShort.Description)
            .MaximumLength(User.Constraints.NameMaxLength)
            .WithErrorCode(User.Errors.FirstNameTooLong.Code)
            .WithMessage(User.Errors.FirstNameTooLong.Description)
            .Matches(User.Constraints.NameAllowedPattern)
            .WithErrorCode(User.Errors.FirstNameInvalidFormat.Code)
            .WithMessage(User.Errors.FirstNameInvalidFormat.Description)
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        RuleFor(x => x.LastName)
            .MinimumLength(User.Constraints.NameMinLength)
            .WithErrorCode(User.Errors.LastNameTooShort.Code)
            .WithMessage(User.Errors.LastNameTooShort.Description)
            .MaximumLength(User.Constraints.NameMaxLength)
            .WithErrorCode(User.Errors.LastNameTooLong.Code)
            .WithMessage(User.Errors.LastNameTooLong.Description)
            .Matches(User.Constraints.NameAllowedPattern)
            .WithErrorCode(User.Errors.LastNameInvalidFormat.Code)
            .WithMessage(User.Errors.LastNameInvalidFormat.Description)
            .When(x => !string.IsNullOrEmpty(x.LastName));

        RuleFor(x => x.PhoneNumber)
            .MinimumLength(User.Constraints.PhoneNumberMinLength)
            .WithErrorCode(User.Errors.PhoneNumberTooShort.Code)
            .WithMessage(User.Errors.PhoneNumberTooShort.Description)
            .MaximumLength(User.Constraints.PhoneNumberMaxLength)
            .WithErrorCode(User.Errors.PhoneNumberTooLong.Code)
            .WithMessage(User.Errors.PhoneNumberTooLong.Description)
            .Matches(User.Constraints.PhoneNumberFormat)
            .WithErrorCode(User.Errors.PhoneNumberInvalidFormat.Code)
            .WithMessage(User.Errors.PhoneNumberInvalidFormat.Description)
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.ProfileImagePath)
            .MaximumLength(User.Constraints.ProfileImagePathMaxLength)
            .WithErrorCode(User.Errors.ProfileImagePathTooLong.Code)
            .WithMessage(User.Errors.ProfileImagePathTooLong.Description)
            .Matches(User.Constraints.ProfileImagePathFormat)
            .WithErrorCode(User.Errors.ProfileImagePathInvalidFormat.Code)
            .WithMessage(User.Errors.ProfileImagePathInvalidFormat.Description)
            .When(x => !string.IsNullOrEmpty(x.ProfileImagePath));
    }
}

public sealed class UserCreateParamValidator : AbstractValidator<UserCreateParam>
{
    public UserCreateParamValidator()
    {
        Include(new UserParamValidator());

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithErrorCode(User.Errors.PasswordRequired.Code)
            .WithMessage(User.Errors.PasswordRequired.Description)
            .MinimumLength(User.Constraints.PasswordMinLength)
            .WithErrorCode(User.Errors.PasswordTooShort.Code)
            .WithMessage(User.Errors.PasswordTooShort.Description)
            .MaximumLength(User.Constraints.PasswordMaxLength)
            .WithErrorCode(User.Errors.PasswordTooLong.Code)
            .WithMessage(User.Errors.PasswordTooLong.Description)
            .Matches(User.Constraints.PasswordAllowedPattern)
            .WithErrorCode(User.Errors.PasswordInvalidFormat.Code)
            .WithMessage(User.Errors.PasswordInvalidFormat.Description);

        RuleFor(x => x.Roles)
            .ForEach(m => m.SetValidator(new RoleNameValidator()))
            .When(x => x.Roles is { Length: > 0 });
    }
}

// User-Permission Assignment Validation
public sealed class UserPermissionAssignmentValidator : AbstractValidator<(Guid UserId, string Permission)>
{
    public UserPermissionAssignmentValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithErrorCode(User.Errors.UserIdRequired.Code)
            .WithMessage(User.Errors.UserIdRequired.Description);

        RuleFor(x => x.Permission)
            .SetValidator(new PermissionNameValidator());
    }
}

// Role-Permission Assignment Validation
public sealed class RolePermissionAssignmentValidator : AbstractValidator<(Guid RoleId, string Permission)>
{
    public RolePermissionAssignmentValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty()
            .WithErrorCode(Role.Errors.RoleIdRequired.Code)
            .WithMessage(Role.Errors.RoleIdRequired.Description);

        RuleFor(x => x.Permission)
            .SetValidator(new PermissionNameValidator());
    }
}

