using Core.Identity;

using ErrorOr;

using FluentValidation;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Accounts.Common;
using UseCases.Common.Notification.Services;
using UseCases.Common.Security.Authentication.Contexts;

namespace UseCases.Accounts.Email.Change;
public static partial class ChangeEmail
{
    public sealed record Command(Param Param) : ICommand<Result>;
    public sealed record Result(string ConfirmMessage);

    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Param).SetValidator(new ParamValidator());
        }
    }

    public sealed class Handler(
        UserManager<User> userManager,
        IUserContext userContext,
        INotificationService notificationService,
        IConfiguration configuration)
        : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Load: user context
            var userId = userContext.UserId;
            var isAuthenticated = userContext.IsAuthenticated;

            // Check: user is authenticated
            if (userId is null || !isAuthenticated)
                return User.Errors.UserUnauthorized;

            // Check: user exists
            var user = await userManager.FindByIdAsync(userId.Value.ToString());
            if (user is null)
                return User.Errors.UserNotFound;

            var param = request.Param;

            // Check: current password is correct (security verification)
            var isCurrentPasswordValid = await userManager.CheckPasswordAsync(user, param.Password);
            if (!isCurrentPasswordValid)
                return User.Errors.InvalidCredentials;

            // Check: new email is not already in use by another user
            var existingUser = await userManager.FindByEmailAsync(param.NewEmail);
            if (existingUser != null && existingUser.Id != user.Id)
                return User.Errors.EmailAlreadyExists(param.NewEmail);

            // Send: email change confirmation to new email address
            var sendEmailResult = await userManager.GenerateAndSendConfirmationEmailAsync(
                notificationService,
                configuration,
                user: user,
                newEmail: param.NewEmail,
                cancellationToken: cancellationToken);

            if (sendEmailResult.IsError)
            {
                // Rollback: clear pending email if email sending fails
                await userManager.UpdateAsync(user);
                Log.Warning("Failed to send email change confirmation to {NewEmail} for user {UserId}: {Errors}",
                    param.NewEmail, userId, string.Join(", ", sendEmailResult.Errors.Select(e => e.Description)));
                return sendEmailResult.Errors;
            }

            Log.Information("Email change initiated for user {UserId} from {CurrentEmail} to {NewEmail}",
                userId, user.Email, param.NewEmail);

            return new Result($"A confirmation email has been sent to {param.NewEmail}. Please check your inbox and click the confirmation link to complete the email address change.");
        }
    }
}