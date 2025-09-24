using Core.Identity;

using FluentValidation;

namespace UseCases.Accounts.Authentication.Sessions.Refresh;
public static partial class RefreshSession
{
    public sealed record Param(string RefreshToken, bool RememberMe = false);
    public sealed class ParamValidator : AbstractValidator<Param>
    {
        public ParamValidator()
        {
            RuleFor(x => x.RefreshToken)
                .NotEmpty()
                .WithErrorCode(RefreshToken.Errors.RefreshTokenRequired.Code)
                .WithMessage(RefreshToken.Errors.RefreshTokenRequired.Description)
                .MaximumLength(RefreshToken.Constraints.TokenLength);
        }
    }


}
