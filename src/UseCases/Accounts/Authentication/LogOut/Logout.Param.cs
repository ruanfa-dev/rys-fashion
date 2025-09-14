using Core.Identity;

using FluentValidation;

namespace UseCases.Accounts.Authentication.LogOut;
public static partial class Logout
{
    public sealed record Param(string RefreshToken);
    public sealed class ParamValidator : AbstractValidator<Param>
    {
        public ParamValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty()
                .WithErrorCode(RefreshToken.Errors.RefreshTokenRequired.Code)
                .WithMessage(RefreshToken.Errors.RefreshTokenRequired.Description);
        }
    }
}
