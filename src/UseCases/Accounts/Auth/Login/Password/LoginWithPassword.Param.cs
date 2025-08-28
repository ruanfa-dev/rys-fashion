using Core.Identity;

using FluentValidation;

using UseCases.Common.Security.Authentication.Tokens.Models;

namespace UseCases.Accounts.Auth.Login.Password;
public static partial class LoginWithPassword
{
    public sealed record Param(string Email, string Password, bool RememberMe = false);
    public sealed record Result : AuthenticationResult;
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
