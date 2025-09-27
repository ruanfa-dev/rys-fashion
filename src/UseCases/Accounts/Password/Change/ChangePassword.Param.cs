using Core.Identity;
using Core.Identity.Users;

using FluentValidation;

namespace UseCases.Accounts.Password.Change;
public static partial class ChangePassword
{
    public sealed record Param(string CurrentPassword, string NewPassword, string NewPasswordConfirm);
    public sealed class ParamValidator : AbstractValidator<Param>
    {
        public ParamValidator()
        {
            RuleFor(x => x.CurrentPassword)
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

            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .WithErrorCode(User.Errors.NewPasswordRequired.Code)
                .WithMessage(User.Errors.NewPasswordRequired.Description)
                .MinimumLength(User.Constraints.PasswordMinLength)
                .WithErrorCode(User.Errors.PasswordTooShort.Code)
                .WithMessage(User.Errors.PasswordTooShort.Description)
                .MaximumLength(User.Constraints.PasswordMaxLength)
                .WithErrorCode(User.Errors.PasswordTooLong.Code)
                .WithMessage(User.Errors.PasswordTooLong.Description)
                .Matches(User.Constraints.PasswordAllowedPattern)
                .WithErrorCode(User.Errors.PasswordInvalidFormat.Code)
                .WithMessage(User.Errors.PasswordInvalidFormat.Description)
                .NotEqual(m => m.CurrentPassword)
                .WithErrorCode(User.Errors.SamePasswordNotAllowed.Code)
                .WithMessage(User.Errors.SamePasswordNotAllowed.Description);

            RuleFor(x => x.NewPasswordConfirm)
                .NotEmpty()
                .WithErrorCode(User.Errors.ConfirmPasswordRequired.Code)
                .WithMessage(User.Errors.ConfirmPasswordRequired.Description)
                .MinimumLength(User.Constraints.PasswordMinLength)
                .WithErrorCode(User.Errors.PasswordTooShort.Code)
                .WithMessage(User.Errors.PasswordTooShort.Description)
                .MaximumLength(User.Constraints.PasswordMaxLength)
                .WithErrorCode(User.Errors.PasswordTooLong.Code)
                .WithMessage(User.Errors.PasswordTooLong.Description)
                .Matches(User.Constraints.PasswordAllowedPattern)
                .WithErrorCode(User.Errors.PasswordInvalidFormat.Code)
                .WithMessage(User.Errors.PasswordInvalidFormat.Description)
                .Equal(x => x.NewPassword)
                .WithErrorCode(User.Errors.ConfirmPasswordMismatch.Code)
                .WithMessage(User.Errors.ConfirmPasswordMismatch.Description);
        }
    }
}
