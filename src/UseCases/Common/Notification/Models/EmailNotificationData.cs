using ErrorOr;

using static UseCases.Common.Notification.Constants.NotificationPriorities;
using static UseCases.Common.Notification.Constants.NotificationSendMethods;
using static UseCases.Common.Notification.Constants.NotificationUseCases;

namespace UseCases.Common.Notification.Models;

/// <summary>
/// Represents the data required to send an email notification.
/// Includes recipients, content, attachments, and metadata for context.
/// </summary>
public partial class EmailNotificationData
{
    public required NotificationUseCase UseCase { get; set; }
    public List<string> Receivers { get; set; } = new();
    public List<string> Cc { get; set; } = new();
    public List<string> Bcc { get; set; } = new();
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? HtmlContent { get; set; }
    public string CreatedBy { get; set; } = "System";
    public List<string> Attachments { get; set; } = new();
    public DateTimeOffset? CreatedAt { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public NotificationSendMethod SendMethod { get; set; } = NotificationSendMethod.Email;
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// Validates the <see cref="EmailNotificationData"/> instance and returns either the validated instance or a list of validation errors.
    /// </summary>
    public ErrorOr<EmailNotificationData> Validate()
    {
        var errors = new List<Error>();

        if (UseCase == NotificationUseCase.None)
            errors.Add(Errors.MissingUseCase);

        if (SendMethod != NotificationSendMethod.Email)
            errors.Add(Errors.InvalidSendMethod);

        if (!Receivers.Any(r => !string.IsNullOrWhiteSpace(r)))
            errors.Add(Errors.MissingReceivers);

        if (string.IsNullOrWhiteSpace(Title))
            errors.Add(Errors.MissingTitle);

        if (string.IsNullOrWhiteSpace(Content) && string.IsNullOrWhiteSpace(HtmlContent))
            errors.Add(Errors.MissingContent);

        if (errors.Any())
            return errors;

        return this;
    }
}