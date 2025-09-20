using FluentValidation;

namespace UseCases.Admin.Roles.Create;

public static partial class CreateRoleCommand
{
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Param.Name)
                .NotEmpty()
                .WithMessage("Role name is required")
                .MaximumLength(100)
                .WithMessage("Role name must not exceed 100 characters")
                .Matches("^[a-zA-Z0-9_-]+$")
                .WithMessage("Role name can only contain letters, numbers, hyphens, and underscores");

            RuleFor(x => x.Param.Description)
                .MaximumLength(500)
                .WithMessage("Description must not exceed 500 characters")
                .When(x => !string.IsNullOrEmpty(x.Param.Description));
        }
    }
}