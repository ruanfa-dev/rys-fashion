using Core.Identity.Users;

using FluentValidation;

namespace UseCases.Accounts.Password.Reset;
public static partial class ResetPassword
{
    public sealed record Param(string Email, string ResetCode, string NewPassword);
    public sealed class ParamValidator : AbstractValidator<Param>
    {
        public ParamValidator()
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

            RuleFor(x => x.ResetCode)
                .NotEmpty()
                .WithErrorCode(User.Errors.ResetPasswordCodeRequired.Code)
                .WithMessage(User.Errors.ResetPasswordCodeRequired.Description)
                .Matches(User.Constraints.ResetPasswordCodePattern)
                .WithErrorCode(User.Errors.ResetPasswordCodeInvalid.Code)
                .WithMessage(User.Errors.ResetPasswordCodeInvalid.Description);

            RuleFor(x => x.NewPassword)
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
        }

    }
}
