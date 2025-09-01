using Core.Identity;

using FluentValidation;

namespace UseCases.Accounts.Phone.Confirm;
public static partial class ConfirmPhoneChange
{
    public sealed record Param(string NewPhone, string Code);
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

            RuleFor(x => x.Code)
                .NotEmpty()
                .WithErrorCode(User.Errors.PhoneConfirmationCodeRequired.Code)
                .WithMessage(User.Errors.PhoneConfirmationCodeRequired.Description);
        }
    }
}
