using ErrorOr;

namespace UseCases.Common.Notification.Models;

public partial class NotificationData
{
    /// <summary>
    /// Standardized reusable validation errors for NotificationData.
    /// </summary>
    public static class Errors
    {
        public static Error NullData => Error.Validation(
            "Notification.NullData", "NotificationData cannot be null.");

        public static Error MissingUseCase => Error.Validation(
            "Notification.UseCase.Missing", "Notification use case must be specified.");

        public static Error MissingReceiver => Error.Validation(
            "Notification.Receivers.Missing", "At least one valid receiver is required.");

        public static Error EmptyCreatedBy => Error.Validation(
            "Notification.CreatedBy.Missing", "CreatedBy cannot be empty or whitespace.");

        public static Error MissingEmailTitle => Error.Validation(
            "Notification.Email.Title.Missing", "Title is required for Email notifications.");

        public static Error MissingEmailContent => Error.Validation(
            "Notification.Email.Content.Missing", "At least one of Content or HtmlContent is required for Email notifications.");

        public static Error MissingSmsContent => Error.Validation(
            "Notification.SMS.Content.Missing", "Content is required for SMS notifications.");

        public static Error SmsContentTooLong => Error.Validation(
            "Notification.SMS.Content.TooLong", "SMS content exceeds 160 characters and may be truncated.");

        public static Error InvalidEmailSender => Error.Validation(
            "Notification.Email.Sender.Invalid", "Sender must be a valid email address.");

        public static Error InvalidSmsSender => Error.Validation(
            "Notification.SMS.Sender.Invalid", "Sender must be a valid phone number.");

        public static Error NullParameters => Error.Validation(
            "Notification.Parameters.Null", "Parameters dictionary cannot be null.");

        public static Error InvalidReceivers => Error.Validation(
            "Notification.Receivers.Invalid", "At least one valid receiver is required.");

        public static Error InvalidTitle => Error.Validation(
            "Notification.Title.Invalid", "Title cannot be empty or whitespace.");

        public static Error InvalidContent => Error.Validation(
            "Notification.Content.Invalid", "Content cannot be empty or whitespace.");

        public static Error InvalidHtmlContent => Error.Validation(
            "Notification.HtmlContent.Invalid", "HTML content cannot be empty or whitespace.");

        public static Error InvalidCreatedBy => Error.Validation(
            "Notification.CreatedBy.Invalid", "CreatedBy cannot be empty or whitespace.");

        public static Error InvalidLanguage => Error.Validation(
            "Notification.Language.Invalid", "Language cannot be empty or whitespace.");
    }
}