using Core.Identity.Users;

using ErrorOr;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Accounts.Common;
using UseCases.Common.Notification.Services;
using UseCases.Common.Security.Authentication.Contexts;

namespace UseCases.Accounts.Phone.Change;

public static partial class ChangePhone
{
    public sealed record Command(Param Param) : ICommand<Result>;
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

            // Check: new phone is different from current phone
            if (string.Equals(user.PhoneNumber, param.NewPhone, StringComparison.OrdinalIgnoreCase))
                return Error.Validation("ChangePhone.SamePhone", "The new phone number must be different from the current phone number.");

            // Check: new phone is not already in use by another user
            IQueryable<User> existingUserQuery = userManager.Users.Where(u => u.PhoneNumber == param.NewPhone && u.Id != user.Id);
            User? existingUser = await existingUserQuery.FirstOrDefaultAsync(cancellationToken);
            if (existingUser != null)
                return User.Errors.PhoneNumberAlreadyExists(param.NewPhone);

            // Generate verification code for the new phone number
            string code = await userManager.GenerateChangePhoneNumberTokenAsync(user, param.NewPhone);

            // Send SMS verification to new phone number
            ErrorOr<Success> sendSmsResult = await userManager.GenerateAndSendConfirmationSmsAsync(
                notificationService,
                configuration,
                user,
                newPhoneNumber: param.NewPhone,
                cancellationToken: cancellationToken);

            if (sendSmsResult.IsError)
            {
                Log.Warning("Failed to send phone change verification to {NewPhone} for user {UserId}: {Errors}",
                    param.NewPhone, userId, string.Join(", ", sendSmsResult.Errors.Select(e => e.Description)));
                return sendSmsResult.Errors;
            }

            Log.Information("Phone change verification sent for user {UserId} from {CurrentPhone} to {NewPhone}",
                userId, user.PhoneNumber, param.NewPhone);

            return Result.PhoneChangeInitiated;
        }
    }
}