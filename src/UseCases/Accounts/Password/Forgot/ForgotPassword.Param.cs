using Core.Identity;

using FluentValidation;

namespace UseCases.Accounts.Password.Forgot;
public static partial class ForgotPassword
{
    public sealed record Param(string Email);
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
        }
    }
}
