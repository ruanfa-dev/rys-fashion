using Core.Identity;

using ErrorOr;

using FluentValidation;

using Mapster;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Security.Authentication.Tokens.Models;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace UseCases.Accounts.Authentication.Login.Password;
public static partial class LoginWithPassword
{
    public sealed record Command(Param Param) : ICommand<Result>;
    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Param).SetValidator(new ParamValidator());
        }
    }

    public sealed class Handler(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        ITokenManagementService tokenManagementService,
        IHttpContextAccessor httpContextAccessor) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {

            // Check: User existence by email
            var param = request.Param;
            var user = await userManager.FindByEmailAsync(param.Email);
            if (user is null)
                return User.Errors.UserNotFound;

            // Check: User is not locked out
            var result = await signInManager.CheckPasswordSignInAsync(
                user,
                param.Password,
                lockoutOnFailure: true);


            // Check: Sign-in result
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    Log.Warning("Login attempt for locked user: {UserId}", user.Id);
                    return User.Errors.LockedOut;
                }
                if (result.IsNotAllowed)
                {
                    Log.Warning("Login attempt for locked user: {UserId}", user.Id);
                    return User.Errors.EmailNotConfirmed;
                }

                Log.Warning("Invalid password for user: {UserId}", user.Id);
                return User.Errors.InvalidCredentials;
            }

            // Get: IP address and user-agent
            var ipAddress = httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = httpContextAccessor.HttpContext?.Request?.Headers.UserAgent.ToString();
            if (string.IsNullOrWhiteSpace(userAgent))
                userAgent = "unknown";

            // Generate: tokens (access + refresh)
            ErrorOr<AuthenticationResult> tokens = await tokenManagementService.AuthenticateAsync(
                user: user,
                ipAddress: ipAddress,
                rememberMe: param.RememberMe,
                cancellationToken);

            if (tokens.IsError)
            {
                return tokens.Errors;
            }


            return tokens.Value.Adapt<Result>();

        }
    }

}
