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
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
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
            user.RecordSignIn(ipAddress);
            await userManager.UpdateAsync(user);

            ErrorOr<AccessTokenResult> accessResult = await jwtTokenService.GenerateAccessTokenAsync(user!, cancellationToken);
            if (accessResult.IsError)
            {
                Log.Error("Access token generation failed for user {UserId}", user!.Id);
                return accessResult.Errors;
            }

            ErrorOr<RefreshTokenResult> refreshResult = await refreshTokenService.GenerateRefreshTokenAsync(
                user!.Id, ipAddress, param.RememberMe, cancellationToken);
            if (refreshResult.IsError)
            {
                Log.Error("Refresh token generation failed for user {UserId}", user.Id);
                return refreshResult.Errors;
            }

            var tokens = new AuthenticationResult
            {
                AccessToken = accessResult.Value.Token,
                AccessTokenExpiresAt = accessResult.Value.ExpiresAt,
                RefreshToken = refreshResult.Value.Token,
                RefreshTokenExpiresAt = refreshResult.Value.ExpiresAt,
                TokenType = "Bearer"
            };

            return tokens.Adapt<Result>();
        }
    }

}
