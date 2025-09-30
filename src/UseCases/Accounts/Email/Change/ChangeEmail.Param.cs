using Core.Identity.Users;

using FluentValidation;

namespace UseCases.Accounts.Email.Change;
public static partial class ChangeEmail
{
    public sealed record Param(
        string CurrentEmail,
        string NewEmail,
        string Password);

    public sealed class ParamValidator : AbstractValidator<Param>
    {
        public ParamValidator()
        {

            RuleFor(x => x.CurrentEmail)
                .NotEmpty()
                .WithErrorCode(User.Errors.EmailRequired.Code)
                .WithMessage(User.Errors.EmailRequired.Description)
                .MaximumLength(User.Constraints.EmailMaxLength)
                .WithErrorCode(User.Errors.EmailTooLong.Code)
                .WithMessage(User.Errors.EmailTooLong.Description)
                .EmailAddress()
                .WithErrorCode(User.Errors.EmailInvalidFormat.Code)
                .WithMessage(User.Errors.EmailInvalidFormat.Description);

            RuleFor(x => x.NewEmail)
                .NotEmpty()
                .WithErrorCode(User.Errors.EmailRequired.Code)
                .WithMessage(User.Errors.EmailRequired.Description)
                .MaximumLength(User.Constraints.EmailMaxLength)
                .WithErrorCode(User.Errors.EmailTooLong.Code)
                .WithMessage(User.Errors.EmailTooLong.Description)
                .EmailAddress()
                .WithErrorCode(User.Errors.EmailInvalidFormat.Code)
                .WithMessage(User.Errors.EmailInvalidFormat.Description)
                .NotEqual(x => x.CurrentEmail)
                .WithErrorCode(User.Errors.SameEmailNotAllowed.Code)
                .WithMessage(User.Errors.SameEmailNotAllowed.Description);

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
        }
    }
}
