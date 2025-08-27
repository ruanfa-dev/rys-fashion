using ErrorOr;

namespace UseCases.Common.Notification.Models;

public partial class EmailNotificationData
{
    /// <summary>
    /// Standardized reusable validation errors for EmailNotificationData.
    /// </summary>
    public static class Errors
    {
        public static Error MissingUseCase => Error.Validation(
            "EmailNotification.UseCase.Missing", "Notification use case must be specified.");

        public static Error InvalidSendMethod => Error.Validation(
            "EmailNotification.SendMethod.Invalid", "SendMethod must be Email for EmailNotificationData.");

        public static Error MissingReceivers => Error.Validation(
            "EmailNotification.Receivers.Missing", "At least one valid email address is required.");

        public static Error MissingTitle => Error.Validation(
            "EmailNotification.Title.Missing", "Title is required for email notifications.");

        public static Error MissingContent => Error.Validation(
            "EmailNotification.Content.Missing", "At least one of Content or HtmlContent is required for email notifications.");
    }
}