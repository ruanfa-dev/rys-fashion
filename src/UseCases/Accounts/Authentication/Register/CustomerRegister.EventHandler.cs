using Core.Identity.Users;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

using Serilog;

using SharedKernel.Messaging.Abstracts;

using UseCases.Accounts.Common;
using UseCases.Common.Notification.Services;

using Success = ErrorOr.Success;

namespace UseCases.Accounts.Authentication.Register;
public static partial class CustomerRegister
{
    public sealed class EventHandler(UserManager<User> userManager,
        INotificationService notificationService,
        IConfiguration configuration) : IDomainEventHandler<User.Events.UserRegistered>
    {
        public async Task Handle(User.Events.UserRegistered notification, CancellationToken cancellationToken)
        {
            try
            {
                User? user = await userManager.FindByIdAsync(notification.UserId.ToString());
                if (user == null)
                {
                    Log.Warning("User with ID {UserId} not found for sending confirmation email", notification.UserId);
                    return;
                }

                bool emailSuccess = false;
                bool phoneSuccess = false;
                bool hasPhone = !string.IsNullOrWhiteSpace(user.PhoneNumber);

                // Send: confirmation email
                ErrorOr.ErrorOr<Success> emailResult = await userManager.GenerateAndSendConfirmationEmailAsync(
                    notificationService: notificationService,
                    configuration: configuration,
                    user: user,
                    cancellationToken: cancellationToken);

                if (emailResult.IsError)
                {
                    Log.Error("Failed to send confirmation email to {Email}: {Errors}", user.Email, emailResult.Errors);
                }
                else
                {
                    emailSuccess = true;
                    Log.Information("Confirmation email sent successfully to {Email} for user {UserId}", user.Email, notification.UserId);
                }

                // Send: phone confirmation if phone number exists
                if (hasPhone)
                {
                    ErrorOr.ErrorOr<Success> phoneResult = await userManager.GenerateAndSendConfirmationSmsAsync(
                        notificationService: notificationService,
                        configuration: configuration,
                        user: user,
                        cancellationToken: cancellationToken);

                    if (phoneResult.IsError)
                    {
                        Log.Error("Failed to send confirmation SMS to {PhoneNumber}: {Errors}", user.PhoneNumber, phoneResult.Errors);
                    }
                    else
                    {
                        phoneSuccess = true;
                        Log.Information("Confirmation SMS sent successfully to {PhoneNumber} for user {UserId}", user.PhoneNumber, notification.UserId);
                    }
                }
                else
                {
                    phoneSuccess = true; // No phone to confirm, consider as success
                }

                // Rollback: user creation if both confirmations fail
                if (!emailSuccess && !phoneSuccess)
                {
                    IdentityResult deleteResult = await userManager.DeleteAsync(user);
                    if (deleteResult.Succeeded)
                    {
                        Log.Warning("User {UserId} deleted due to failed confirmation send", notification.UserId);
                    }
                    else
                    {
                        Log.Error("Failed to delete user {UserId} after confirmation send failure. Manual cleanup required", notification.UserId);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error occurred while handling user registration event for user {UserId}", notification.UserId);
                throw;
            }
        }
    }
}
