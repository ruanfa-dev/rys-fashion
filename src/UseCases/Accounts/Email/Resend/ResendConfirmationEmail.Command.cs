using Core.Identity.Users;

using ErrorOr;

using FluentValidation;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Accounts.Common;
using UseCases.Common.Notification.Services;
using UseCases.Common.Security.Authentication.Contexts;

namespace UseCases.Accounts.Email.Resend;

public static partial class ResendEmailConfirmation
{
    public sealed record Command(Param Param) : ICommand<Result>;

    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Param).SetValidator(new ParamValidator());
        }
    }

    public sealed record Result(
        string ConfirmMessage)
    {
        public static Result NewEmailConfirmation => new(
            ConfirmMessage: "If your email address is registered and not yet confirmed, a confirmation email has been sent.");
    }

    public sealed class Handler(
       UserManager<User> userManager,
       INotificationService notificationService,
       IConfiguration configuration,
       IUserContext userContext)
           : ICommandHandler<Command, Result>
    {
        public async Task<ErrorOr<Result>> Handle(
            Command request, CancellationToken cancellationToken)
        {
            var param = request.Param;
            var isAuthenticated = userContext.IsAuthenticated;

            if (isAuthenticated)
                return await HandleAuthenticatedUserAsync(param, cancellationToken);
            else if (!isAuthenticated && !string.IsNullOrEmpty(param.Email))
                return await HandleAnonymousUserAsync(param, cancellationToken);

            Log.Warning("ResendConfirmEmail: Anonymous request without email.");
            return Error.Validation("ResendConfirmEmail.MissingEmail", "Email is required for anonymous requests.");
        }

        private async Task<ErrorOr<Result>> HandleAuthenticatedUserAsync(Param param, CancellationToken cancellationToken)
        {
            var userId = userContext.UserId!.Value;

            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return User.Errors.UserNotFound;

            // Ensure email belongs to current user
            bool isCurrentEmail = string.Equals(user.Email, param.Email, StringComparison.OrdinalIgnoreCase);
            if (!isCurrentEmail)
            {
                Log.Warning("ResendConfirmEmail: Authenticated user {UserId} attempted to resend confirmation for unauthorized email {Email}", userId, param.Email);
                return Error.Validation("ResendConfirmEmail.UnauthorizedEmail", "You can only resend confirmation emails for your current email address.");
            }

            if (await userManager.IsEmailConfirmedAsync(user))
            {
                Log.Information("ResendConfirmEmail: Authenticated user {UserId} with email {Email} already confirmed.", userId, param.Email);
                return Result.NewEmailConfirmation;
            }

            Log.Information("Resending confirmation email for authenticated user {UserId} to {Email}", userId, user.Email);

            var sendResult = await userManager.GenerateAndSendConfirmationEmailAsync(
                notificationService,
                configuration,
                user,
                cancellationToken: cancellationToken);

            if (sendResult.IsError)
            {
                Log.Error("ResendConfirmEmail: Failed to send confirmation email to {Email} for user {UserId}. Errors: {Errors}", user.Email, userId, sendResult.Errors);
                return sendResult.Errors;
            }

            return Result.NewEmailConfirmation;
        }

        private async Task<ErrorOr<Result>> HandleAnonymousUserAsync(Param param, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByEmailAsync(param.Email ?? string.Empty);

            if (user is null)
            {
                Log.Information("ResendConfirmEmail: Anonymous request for non-existent email {Email}.", param.Email);
                return Result.NewEmailConfirmation;
            }

            if (await userManager.IsEmailConfirmedAsync(user))
            {
                Log.Information("ResendConfirmEmail: Anonymous request for already confirmed email {Email}.", param.Email);
                return Result.NewEmailConfirmation;
            }

            Log.Information("Resending confirmation email for anonymous request to {Email}", user.Email);

            var sendResult = await userManager.GenerateAndSendConfirmationEmailAsync(
                notificationService,
                configuration,
                user,
                cancellationToken: cancellationToken);

            if (sendResult.IsError)
            {
                Log.Error("ResendConfirmEmail: Failed to send confirmation email to {Email}. Errors: {Errors}", user.Email, sendResult.Errors);
                return sendResult.Errors;
            }

            return Result.NewEmailConfirmation;
        }
    }
}
