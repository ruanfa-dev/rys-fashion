using Core.Identity.Users;

using ErrorOr;

using FluentValidation;

using MediatR;

using Microsoft.AspNetCore.Identity;

using SharedKernel.Messaging.Abstracts;

using UseCases.Accounts.Common;

namespace UseCases.Accounts.Password.Reset;
public static partial class ResetPassword
{
    public sealed record Command(Param Param) : ICommand<Result>;
    public sealed record Result(string Message)
    {
        public static Result Default => new("Password has been successfully reset.");
    }
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Param).SetValidator(new ParamValidator());
        }
    }

    public sealed class Handler(
       UserManager<User> userManager)
    : IRequestHandler<Command, ErrorOr<Result>>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            Param param = request.Param;
            // Check: user exists by email
            User? user = await userManager.FindByEmailAsync(param.Email);
            if (user == null)
                return User.Errors.InvalidToken;

            // Decode: reset code
            ErrorOr<string> decodeResult = param.ResetCode.DecodeToken();
            if (decodeResult.IsError)
                return decodeResult.Errors;

            // Reset: password
            IdentityResult result = await userManager.ResetPasswordAsync(user, decodeResult.Value, param.NewPassword);
            if (!result.Succeeded)
                return result.Errors.ToApplicationResult(fallbackCode: "ResetPasswordFailed");

            return Result.Default;
        }
    }
}
