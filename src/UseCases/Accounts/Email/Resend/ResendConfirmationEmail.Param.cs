using Core.Identity;
using Core.Identity.Users;

using FluentValidation;

namespace UseCases.Accounts.Email.Resend;
public static partial class ResendEmailConfirmation
{
    public sealed record Param(string? Email = null);

    public sealed class ParamValidator : AbstractValidator<Param>
    {
        public ParamValidator()
        {
            RuleFor(x => x.Email)
                .MaximumLength(User.Constraints.EmailMaxLength)
                .WithErrorCode(User.Errors.EmailTooLong.Code)
                .WithMessage(User.Errors.EmailTooLong.Description)
                .EmailAddress()
                .WithErrorCode(User.Errors.EmailInvalidFormat.Code)
                .WithMessage(User.Errors.EmailInvalidFormat.Description)
                .When(x => !string.IsNullOrEmpty(x.Email));
        }
    }
}
