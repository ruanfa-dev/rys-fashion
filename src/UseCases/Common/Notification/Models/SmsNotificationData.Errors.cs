using ErrorOr;

namespace UseCases.Common.Notification.Models;

public partial class SmsNotificationData
{
    /// <summary>
    /// Standardized reusable validation errors for SmsNotificationData.
    /// </summary>
    public static class Errors
    {
        public static Error MissingUseCase => Error.Validation(
            "SmsNotification.UseCase.Missing", "Notification use case must be specified.");

        public static Error InvalidSendMethod => Error.Validation(
            "SmsNotification.SendMethod.Invalid", "SendMethod must be SMS for SmsNotificationData.");

        public static Error MissingSenderNumber => Error.Validation(
            "SmsNotification.SenderNumber.Missing", "Sender number is required for SMS notifications.");

        public static Error MissingReceivers => Error.Validation(
            "SmsNotification.Receivers.Missing", "At least one valid phone number is required.");

        public static Error MissingContent => Error.Validation(
            "SmsNotification.Content.Missing", "Content is required for SMS notifications.");

        public static Error ContentTooLong => Error.Validation(
            "SmsNotification.Content.TooLong", "SMS content exceeds 160 characters, which may be truncated by some carriers.");
    }
}