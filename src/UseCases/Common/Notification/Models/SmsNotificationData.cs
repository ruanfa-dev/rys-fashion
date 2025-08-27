using ErrorOr;

using static UseCases.Common.Notification.Constants.NotificationPriorities;
using static UseCases.Common.Notification.Constants.NotificationSendMethods;
using static UseCases.Common.Notification.Constants.NotificationUseCases;

namespace UseCases.Common.Notification.Models;

/// <summary>
/// Represents the data required to send an SMS notification.
/// Includes recipients, content, metadata, and parameters for template replacement.
/// </summary>
public partial class SmsNotificationData
{
    public required NotificationUseCase UseCase { get; set; }
    public List<string> Receivers { get; set; } = new();
    public string Content { get; set; } = string.Empty;
    public string SenderNumber { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = "System";
    public DateTimeOffset? CreatedAt { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public NotificationSendMethod SendMethod { get; set; } = NotificationSendMethod.SMS;
    public string Language { get; set; } = "en-US";
    public bool IsUnicode { get; set; } = false;
    public string? TrackingId { get; set; }

    /// <summary>
    /// Validates the <see cref="SmsNotificationData"/> instance and returns either the validated instance or a list of validation errors.
    /// </summary>
    public ErrorOr<SmsNotificationData> Validate()
    {
        var errors = new List<Error>();

        if (UseCase == NotificationUseCase.None)
            errors.Add(Errors.MissingUseCase);

        if (SendMethod != NotificationSendMethod.SMS)
            errors.Add(Errors.InvalidSendMethod);

        if (string.IsNullOrWhiteSpace(SenderNumber))
            errors.Add(Errors.MissingSenderNumber);

        if (!Receivers.Any(r => !string.IsNullOrWhiteSpace(r)))
            errors.Add(Errors.MissingReceivers);

        if (string.IsNullOrWhiteSpace(Content))
            errors.Add(Errors.MissingContent);
        else if (Content.Length > 160 && !IsUnicode)
            errors.Add(Errors.ContentTooLong);

        if (errors.Any())
            return errors;

        return this;
    }
}