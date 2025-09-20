using FluentValidation;

namespace UseCases.Admin.Users.Create;

public static partial class CreateUserCommand
{
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Param.Username)
                .NotEmpty()
                .WithMessage("Username is required")
                .MaximumLength(100)
                .WithMessage("Username must not exceed 100 characters");

            RuleFor(x => x.Param.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Email must be a valid email address")
                .MaximumLength(200)
                .WithMessage("Email must not exceed 200 characters");

            RuleFor(x => x.Param.FirstName)
                .NotEmpty()
                .WithMessage("First name is required")
                .MaximumLength(50)
                .WithMessage("First name must not exceed 50 characters");

            RuleFor(x => x.Param.LastName)
                .NotEmpty()
                .WithMessage("Last name is required")
                .MaximumLength(50)
                .WithMessage("Last name must not exceed 50 characters");

            RuleFor(x => x.Param.Password)
                .MinimumLength(8)
                .WithMessage("Password must be at least 8 characters long")
                .When(x => !string.IsNullOrEmpty(x.Param.Password));
        }
    }
}