using Core.Identity;

using ErrorOr;

using FluentValidation;

using Mapster;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Common.Persistence.Context;
using UseCases.Common.Security.Authentication.Tokens.Models;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace UseCases.Accounts.Auth.Login.Password;
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
        RoleManager<Role> roleManager,
        ITokenManagementService tokenManagementService,
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            try
            {
                // Begin: transaction
                await unitOfWork.BeginTransactionAsync(cancellationToken);

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
                var userAgent = httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();
                if (string.IsNullOrWhiteSpace(userAgent))
                    userAgent = "unknown";

                // Check: user belongs to any system role
                var roles = await userManager.GetRolesAsync(user);
                var isSystemUser = await roleManager.Roles
                    .Where(r => !string.IsNullOrWhiteSpace(r.Name) && roles.Contains(r.Name))
                    .AnyAsync(r => r.IsSystemRole, cancellationToken);

                // Generate: tokens (access + refresh)
                ErrorOr<AuthenticationResult> tokens = await tokenManagementService.AuthenticateAsync(
                    user: user,
                    ipAddress: ipAddress,
                    rememberMe: param.RememberMe,
                    isSystemUser: isSystemUser,
                    cancellationToken);

                if (tokens.IsError)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return tokens.Errors;
                }

                await unitOfWork.CommitTransactionAsync(cancellationToken);

                return tokens.Value.Adapt<Result>();
            }
            catch
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }

}
