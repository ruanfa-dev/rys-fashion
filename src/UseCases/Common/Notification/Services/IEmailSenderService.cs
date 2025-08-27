using ErrorOr;

using UseCases.Common.Notification.Models;

namespace UseCases.Common.Notification.Services;

public interface IEmailSenderService
{
    Task<ErrorOr<Success>> AddEmailNotificationAsync(
        EmailNotificationData notificationData,
        CancellationToken cancellationToken = default);
}
