using Core.Identity.Users;

using FluentValidation;

namespace UseCases.Accounts.Phone.Resend;
public static partial class ResendPhoneVerification
{
    public sealed record Param(string PhoneNumber);
    public sealed record Result(string Message)
    {
        public static Result Default(string phoneNumber) => new($"A new verification SMS has been sent to {phoneNumber}.");
    }
    public sealed class ParamValidator : AbstractValidator<Param>
    {

        public ParamValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithErrorCode(User.Errors.PhoneNumberRequired.Code)
                .WithMessage(User.Errors.PhoneNumberRequired.Description)
                .MinimumLength(User.Constraints.PhoneNumberMinLength)
                .WithErrorCode(User.Errors.PhoneNumberTooShort.Code)
                .WithMessage(User.Errors.PhoneNumberTooShort.Description)
                .MaximumLength(User.Constraints.PhoneNumberMaxLength)
                .WithErrorCode(User.Errors.PhoneNumberTooLong.Code)
                .WithMessage(User.Errors.PhoneNumberTooLong.Description)
                .Matches(User.Constraints.PhoneNumberFormat)
                .WithErrorCode(User.Errors.PhoneNumberInvalidFormat.Code)
                .WithMessage(User.Errors.PhoneNumberInvalidFormat.Description);
        }
    }

}
