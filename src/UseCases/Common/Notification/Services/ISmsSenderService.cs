using ErrorOr;

using UseCases.Common.Notification.Models;

namespace UseCases.Common.Notification.Services;

public interface ISmsSenderService
{
    public Task<ErrorOr<Success>> AddSmsNotificationAsync(
        SmsNotificationData notificationData, 
        CancellationToken cancellationToken = default);
}
