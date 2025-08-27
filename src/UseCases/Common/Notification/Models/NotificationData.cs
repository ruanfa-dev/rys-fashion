using System.Text.RegularExpressions;
using ErrorOr;
using static UseCases.Common.Notification.Constants.NotificationFormats;
using static UseCases.Common.Notification.Constants.NotificationParameters;
using static UseCases.Common.Notification.Constants.NotificationSendMethods;
using static UseCases.Common.Notification.Constants.NotificationUseCases;
using static UseCases.Common.Notification.Constants.NotificationPriorities;

namespace UseCases.Common.Notification.Models;

/// <summary>
/// Represents the base model for sending notifications, regardless of delivery method (Email, SMS, Push, etc.).
/// Contains common metadata, content, and personalization values used in specific notification types.
/// </summary>
public partial class NotificationData
{
    public required NotificationUseCase UseCase { get; set; }
    public NotificationSendMethod SendMethodType { get; set; } = NotificationSendMethod.Email;
    public NotificationFormat TemplateFormatType { get; set; } = NotificationFormat.Default;
    public Dictionary<NotificationParameter, string?> Values { get; set; } = new();
    public List<string> Receivers { get; set; } = new();
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? HtmlContent { get; set; }
    public string CreatedBy { get; set; } = "System";
    public DateTimeOffset? CreatedAt { get; set; }
    public List<string> Attachments { get; set; } = new();
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public string Language { get; set; } = "en-US";
    public string? Sender { get; set; }

    /// <summary>
    /// Validates the notification data and returns either the validated instance or a list of validation errors.
    /// </summary>
    public ErrorOr<NotificationData> Validate()
    {
        var errors = new List<Error>();

        if (UseCase == NotificationUseCase.None)
            errors.Add(Errors.MissingUseCase);

        if (!Receivers.Any(r => !string.IsNullOrWhiteSpace(r)))
            errors.Add(Errors.MissingReceiver);

        if (string.IsNullOrWhiteSpace(CreatedBy))
            errors.Add(Errors.EmptyCreatedBy);

        if (SendMethodType == NotificationSendMethod.Email)
        {
            if (string.IsNullOrWhiteSpace(Title))
                errors.Add(Errors.MissingEmailTitle);

            if (string.IsNullOrWhiteSpace(Content) && string.IsNullOrWhiteSpace(HtmlContent))
                errors.Add(Errors.MissingEmailContent);

            if (!string.IsNullOrWhiteSpace(Sender) && !Regex.IsMatch(Sender, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errors.Add(Errors.InvalidEmailSender);
        }
        else if (SendMethodType == NotificationSendMethod.SMS)
        {
            if (string.IsNullOrWhiteSpace(Content))
                errors.Add(Errors.MissingSmsContent);
            else if (Content.Length > 160)
                errors.Add(Errors.SmsContentTooLong);

            if (!string.IsNullOrWhiteSpace(Sender) && !Regex.IsMatch(Sender, @"^\+?[1-9]\d{7,14}$"))
                errors.Add(Errors.InvalidSmsSender);
        }

        if (errors.Any())
            return errors;

        return this;
    }
}