using Core.Identity;

using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using UseCases.Common.Notification.Models;
using UseCases.Common.Notification.Services;
using UseCases.Common.Persistence.Context;

using static UseCases.Common.Notification.Constants.NotificationSendMethods;
using static UseCases.Common.Notification.Constants.NotificationUseCases;

namespace Infrastructure.Notification.Services;

internal sealed class NotificationService(
    IEmailSenderService emailSenderService,
    ISmsSenderService smsSenderService,
    IServiceScopeFactory serviceScopeFactory)
    : INotificationService
{
    public async Task<ErrorOr<Success>> AddNotificationAsync(
        NotificationData notificationData,
        CancellationToken cancellationToken = default)
    {
        if (notificationData.UseCase == NotificationUseCase.None)
            return Errors.InvalidUseCase;

        if (notificationData.Receivers?.Any(r => !string.IsNullOrWhiteSpace(r)) != true)
            return Errors.EmptyReceivers;

        if (notificationData.SendMethodType is not (NotificationSendMethod.Email or NotificationSendMethod.SMS))
            return Errors.NotSupportedSendMethod;

        using var scope = serviceScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var validContacts = await GetValidContactsAsync(unitOfWork, notificationData, notificationData.SendMethodType, cancellationToken);
        if (validContacts.Count == 0)
            return Errors.ContactNotFound;

        notificationData.Receivers = validContacts;

        var validationResult = notificationData.Validate();
        if (validationResult.IsError)
            return validationResult.Errors;

        return await DispatchNotificationAsync(notificationData, cancellationToken);
    }

    private static async Task<List<string>> GetValidContactsAsync(
        IUnitOfWork unitOfWork,
        NotificationData notificationData,
        NotificationSendMethod sendMethod,
        CancellationToken cancellationToken)
    {
        var contactSet = new HashSet<string>(
            notificationData.Receivers!,
            StringComparer.OrdinalIgnoreCase
        );

        return sendMethod switch
        {
            NotificationSendMethod.Email => await unitOfWork.Context.Set<User>()
                .Where(u => !string.IsNullOrWhiteSpace(u.Email)
                            && contactSet.Contains(u.Email))
                .Select(u => u.Email!)
                .Distinct()
                .ToListAsync(cancellationToken),

            NotificationSendMethod.SMS => await unitOfWork.Context.Set<User>()
                .Where(u => !string.IsNullOrWhiteSpace(u.PhoneNumber)
                            && contactSet.Contains(u.PhoneNumber))
                .Select(u => u.PhoneNumber!)
                .Distinct()
                .ToListAsync(cancellationToken),

            _ => new List<string>()
        };
    }

    private async Task<ErrorOr<Success>> DispatchNotificationAsync(
        NotificationData notificationData,
        CancellationToken cancellationToken)
    {
        return notificationData.SendMethodType switch
        {
            NotificationSendMethod.Email => await emailSenderService.AddEmailNotificationAsync(
                notificationData.ToEmailNotificationData(),
                cancellationToken
            ),

            NotificationSendMethod.SMS => await smsSenderService.AddSmsNotificationAsync(
                notificationData.ToSmsNotificationData(),
                cancellationToken
            ),

            _ => Errors.NotSupportedSendMethod
        };
    }

    public static class Errors
    {
        public static Error InvalidUseCase => Error.Validation(
            "NotificationService.InvalidUseCase", "Use case must be specified.");

        public static Error EmptyReceivers => Error.Validation(
            "NotificationService.EmptyReceivers", "At least one valid receiver required.");

        public static Error NotSupportedSendMethod => Error.Validation(
            "NotificationService.NotSupportedSendMethod", "The specified send method type is not supported.");

        public static Error ContactNotFound => Error.NotFound(
            "NotificationService.ContactNotFound", "No valid contacts found.");

        public static Error DatabaseError => Error.Unexpected(
            "NotificationService.DatabaseError", "Database operation failed.");
    }
}