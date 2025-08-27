using ErrorOr;

using UseCases.Common.Notification.Models;

namespace UseCases.Common.Notification.Services;

public interface INotificationService
{
    Task<ErrorOr<Success>> AddNotificationAsync(NotificationData notification, CancellationToken cancellationToken);
}
