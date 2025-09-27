using Core.Identity;
using Core.Identity.Users;

using FluentValidation;

namespace UseCases.Accounts.Profile.Common;
public record AccountProfileParam
{
    public string UserName { get; init; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfileImagePath { get; set; }
}

public sealed class AccountProfileParamValidator : AbstractValidator<AccountProfileParam>
{
    public AccountProfileParamValidator()
    {
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