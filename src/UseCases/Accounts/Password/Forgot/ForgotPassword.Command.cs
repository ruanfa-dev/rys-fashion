using Core.Identity.Users;

using ErrorOr;

using FluentValidation;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Accounts.Common;
using UseCases.Common.Notification.Services;

namespace UseCases.Accounts.Password.Forgot;
public static partial class ForgotPassword
{
    public sealed record Command(Param Param) : ICommand<Result>;
    public sealed record Result(string Message)
    {
        public static Result Default => new("If an account with the provided email exists, a password reset link has been sent to that email address.");
    }
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Param).SetValidator(new ParamValidator());
        }
    }
    public sealed class Handler(
       UserManager<User> userManager,
       INotificationService notificationService,
       IConfiguration configuration) : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Check: User existence by email
            Param param = request.Param;
            User? user = await userManager.FindByEmailAsync(param.Email);
            // If user does not exist, return the default message
            if (user is null)
                return Result.Default;

            // Generate: password reset token
            ErrorOr<Success> generatedTokenResult = await userManager.GenerateAndSendPasswordResetCodeAsync(
                notificationService: notificationService,
                configuration: configuration,
                user: user,
                cancellationToken: cancellationToken);

            // Check: if token generation was successful
            if (generatedTokenResult.IsError)
            {
                // Log the error and return the default message
                Log.Error("Failed to send password reset code for user {Email}: {Errors}", param.Email, generatedTokenResult.Errors);
                return Result.Default;
            }

            return Result.Default;
        }
    }
}
