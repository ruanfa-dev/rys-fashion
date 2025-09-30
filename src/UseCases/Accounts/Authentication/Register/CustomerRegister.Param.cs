using Core.Identity.Users;

using FluentValidation;

namespace UseCases.Accounts.Authentication.Register;
public static partial class CustomerRegister
{
    public sealed record Param
    {
        public string? UserName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string ConfirmPassword { get; set; } = default!;
        public string Password { get; set; } = default!;

        // TODO: add Captcha
        // TODOL: add more properties if needed
    }
    public partial class ParamValidator : AbstractValidator<Param>
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

            RuleFor(x => x.UserName)
                .MaximumLength(User.Constraints.UserNameMaxLength)
                .WithErrorCode(User.Errors.UserNameTooLong.Code)
                .WithMessage(User.Errors.UserNameTooLong.Description)
                .MinimumLength(User.Constraints.UserNameMinLength)
                .WithErrorCode(User.Errors.UserNameTooShort.Code)
                .WithMessage(User.Errors.UserNameTooShort.Description)
                .Matches(User.Constraints.UserNameAllowedPattern)
                .WithErrorCode(User.Errors.UserNameInvalidFormat.Code)
                .WithMessage(User.Errors.UserNameInvalidFormat.Description)
                .When(x => !string.IsNullOrEmpty(x.UserName));

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(User.Constraints.PhoneNumberMaxLength)
                .WithErrorCode(User.Errors.PhoneNumberTooLong.Code)
                .WithMessage(User.Errors.PhoneNumberTooLong.Description)
                .MinimumLength(User.Constraints.PhoneNumberMinLength)
                .WithErrorCode(User.Errors.PhoneNumberTooShort.Code)
                .WithMessage(User.Errors.PhoneNumberTooShort.Description)
                .Matches(User.Constraints.PhoneNumberFormat)
                .WithErrorCode(User.Errors.PhoneNumberInvalidFormat.Code)
                .WithMessage(User.Errors.PhoneNumberInvalidFormat.Description)
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

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

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty()
                .WithErrorCode(User.Errors.ConfirmPasswordRequired.Code)
                .WithMessage(User.Errors.ConfirmPasswordRequired.Description)
                .Equal(x => x.Password)
                .WithErrorCode(User.Errors.ConfirmPasswordMismatch.Code)
                .WithMessage(User.Errors.ConfirmPasswordMismatch.Description)
                .MinimumLength(User.Constraints.PasswordMinLength)
                .WithErrorCode(User.Errors.PasswordTooShort.Code)
                .WithMessage(User.Errors.PasswordTooShort.Description)
                .MaximumLength(User.Constraints.PasswordMaxLength)
                .WithErrorCode(User.Errors.PasswordTooLong.Code)
                .WithMessage(User.Errors.PasswordTooLong.Description)
                .Matches(User.Constraints.PasswordAllowedPattern)
                .WithErrorCode(User.Errors.PasswordInvalidFormat.Code)
                .WithMessage(User.Errors.PasswordInvalidFormat.Description);

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithErrorCode(User.Errors.FirstNameRequired.Code)
                .WithMessage(User.Errors.FirstNameRequired.Description)
                .MinimumLength(User.Constraints.NameMinLength)
                .WithErrorCode(User.Errors.FirstNameTooShort.Code)
                .WithMessage(User.Errors.FirstNameTooShort.Description)
                .MaximumLength(User.Constraints.NameMaxLength)
                .WithErrorCode(User.Errors.FirstNameTooLong.Code)
                .WithMessage(User.Errors.FirstNameTooLong.Description)
                .Matches(User.Constraints.NameAllowedPattern)
                .WithErrorCode(User.Errors.FirstNameInvalidFormat.Code)
                .WithMessage(User.Errors.FirstNameInvalidFormat.Description);

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithErrorCode(User.Errors.LastNameRequired.Code)
                .WithMessage(User.Errors.LastNameRequired.Description)
                .MinimumLength(User.Constraints.NameMinLength)
                .WithErrorCode(User.Errors.LastNameTooShort.Code)
                .WithMessage(User.Errors.LastNameTooShort.Description)
                .MaximumLength(User.Constraints.NameMaxLength)
                .WithErrorCode(User.Errors.LastNameTooLong.Code)
                .WithMessage(User.Errors.LastNameTooLong.Description)
                .Matches(User.Constraints.NameAllowedPattern)
                .WithErrorCode(User.Errors.LastNameInvalidFormat.Code)
                .WithMessage(User.Errors.LastNameInvalidFormat.Description);
        }
    }
}
