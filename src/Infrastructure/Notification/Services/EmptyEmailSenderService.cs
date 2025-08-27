using ErrorOr;

using UseCases.Common.Notification.Models;
using UseCases.Common.Notification.Services;

namespace Infrastructure.Notification.Services;

public sealed class EmptyEmailSenderService : IEmailSenderService
{
    public Task<ErrorOr<Success>> AddEmailNotificationAsync(
        EmailNotificationData notificationData,
        CancellationToken cancellationToken = default)
    {
        // Return error indicating email sender is disabled/unavailable
        return Task.FromResult<ErrorOr<Success>>(
            Error.Unexpected(
                "EmailSender.Disabled",
                "Email sender is not available. Email sending is disabled in this environment."));
    }
}
