using Core.Identity.Users;

using ErrorOr;

using FluentValidation;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Accounts.Common;
using UseCases.Common.Notification.Services;
using UseCases.Common.Security.Authentication.Contexts;

namespace UseCases.Accounts.Phone.Resend;
public static partial class ResendPhoneVerification
{
    public sealed record Command(Param Param) : ICommand<Result>;
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
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
            Guid? userId = userContext.UserId;
            bool isAuthenticated = userContext.IsAuthenticated;

            // Check: user is authenticated
            if (userId is null || !isAuthenticated)
                return User.Errors.UserUnauthorized;

            // Check: user exists
            User? user = await userManager.FindByIdAsync(userId.Value.ToString());
            if (user is null)
                return User.Errors.UserNotFound;

            Param param = request.Param;

            // Check: phone is not already in use by another user
            IQueryable<User> existingUserQuery = userManager.Users.Where(u => u.PhoneNumber == param.PhoneNumber && u.Id != user.Id);
            User? existingUser = await existingUserQuery.FirstOrDefaultAsync(cancellationToken);
            if (existingUser != null)
                return User.Errors.PhoneNumberAlreadyExists(param.PhoneNumber);

            // Send: phone verification SMS
            ErrorOr<Success> sendSmsResult = await userManager.GenerateAndSendConfirmationSmsAsync(
                notificationService,
                configuration,
                user,
                newPhoneNumber: param.PhoneNumber,
                cancellationToken: cancellationToken);

            if (sendSmsResult.IsError)
            {
                Log.Warning("Failed to resend phone verification to {PhoneNumber} for user {UserId}: {Errors}",
                    param.PhoneNumber, userId, string.Join(", ", sendSmsResult.Errors.Select(e => e.Description)));
                return sendSmsResult.Errors;
            }

            Log.Information("Phone verification resent for user {UserId} to {PhoneNumber}", userId, param.PhoneNumber);

            return Result.Default(param.PhoneNumber);
        }
    }
}
