using ErrorOr;

using Serilog;

using UseCases.Common.Notification.Models;
using UseCases.Common.Notification.Services;

namespace Infrastructure.Notification.Services;
public sealed class EmptySmsSenderService : ISmsSenderService
{
    public async Task<ErrorOr<Success>> AddSmsNotificationAsync(
        SmsNotificationData notificationData,
        CancellationToken cancellationToken = default)
    {
        Log.Warning(messageTemplate: "SMS sending is unavailable or disabled. Notification not sent. UseCase: {UseCase}, Receivers: {Receivers}",
            notificationData.UseCase,
            notificationData.Receivers);
        await Task.CompletedTask;
        return Error.Unexpected(
            code: "SmsNotification.Unavailable",
            description: "SMS sending is currently unavailable or disabled.");
    }
}
