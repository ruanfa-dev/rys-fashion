using Core.Identity.Users;

using FluentValidation;

namespace UseCases.Accounts.Phone.Change;

public static partial class ChangePhone
{
    public sealed record Param(string NewPhone);
    public sealed record Result(string ConfirmMessage)
    {
        public static Result PhoneChangeInitiated => new("If the phone number is valid and not already in use, a confirmation code has been sent to the new phone number.");
    }
    public sealed class ParamValidator : AbstractValidator<Param>
    {
        public ParamValidator()
        {
            RuleFor(x => x.NewPhone)
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