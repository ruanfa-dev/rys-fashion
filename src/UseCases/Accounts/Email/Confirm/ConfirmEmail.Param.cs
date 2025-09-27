using Core.Identity;
using Core.Identity.Users;

using FluentValidation;

namespace UseCases.Accounts.Email.Confirm;
public static partial class ConfirmEmail
{
    public record Param(string UserId, string Code, string? ChangedEmail);
    public sealed class ParamValidator : AbstractValidator<Param>
    {
        public ParamValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithErrorCode(User.Errors.UserIdRequired.Code)
                .WithMessage(User.Errors.UserIdRequired.Description)
                .Must(BeValidGuid)
                .WithErrorCode(User.Errors.UserIdInvalidFormat.Code)
                .WithMessage(User.Errors.UserIdInvalidFormat.Description);

            RuleFor(x => x.Code)
                .NotEmpty()
                .WithErrorCode(User.Errors.ConfirmationCodeRequired.Code)
                .WithMessage(User.Errors.ConfirmationCodeRequired.Description)
                .Matches(User.Constraints.ConfirmationCodePattern)
                .WithErrorCode(User.Errors.ConfirmationCodeInvalidFormat.Code)
                .WithMessage(User.Errors.ConfirmationCodeInvalidFormat.Description);

            RuleFor(x => x.ChangedEmail)
                .EmailAddress()
                .WithErrorCode(User.Errors.EmailInvalidFormat.Code)
                .WithMessage(User.Errors.EmailInvalidFormat.Description)
                .MaximumLength(User.Constraints.EmailMaxLength)
                .WithErrorCode(User.Errors.EmailTooLong.Code)
                .WithMessage(User.Errors.EmailTooLong.Description)
                .When(x => !string.IsNullOrWhiteSpace(x.ChangedEmail));
        }
        private static bool BeValidGuid(string userId)
        {
            return Guid.TryParse(userId, out _);
        }
    }
}