namespace UseCases.Common.Notification.Constants;

public static partial class NotificationSendMethods
{
    public enum NotificationSendMethod
    {
        None = 0,
        Email = 1,
        SMS = 2,
        PushNotification = 3, // For mobile app notifications (e.g., new arrivals, order updates)
        WhatsApp = 4         // Popular for fashion brands in certain regions
    }

    public class SendMethodDescription
    {
        public NotificationSendMethod Method { get; set; }
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string SampleData { get; set; } = default!;
    }

    public static readonly Dictionary<NotificationSendMethod, SendMethodDescription> Description = new()
    {
        [NotificationSendMethod.None] = new SendMethodDescription
        {
            Method = NotificationSendMethod.None,
            Name = "None",
            Description = "No notification will be sent. Used for draft or inactive templates.",
            SampleData = string.Empty
        },
        [NotificationSendMethod.Email] = new SendMethodDescription
        {
            Method = NotificationSendMethod.Email,
            Name = "Email",
            Description = "Sends notifications via email. Ideal for order confirmations, promotions, and account updates.",
            SampleData = "Subject: Your Order #ORD123456 Has Shipped!"
        },
        [NotificationSendMethod.SMS] = new SendMethodDescription
        {
            Method = NotificationSendMethod.SMS,
            Name = "SMS",
            Description = "Sends notifications via text message. Best for short, time-sensitive alerts like delivery updates or OTP codes.",
            SampleData = "Your OTP code is 123456."
        },
        [NotificationSendMethod.PushNotification] = new SendMethodDescription
        {
            Method = NotificationSendMethod.PushNotification,
            Name = "Push Notification",
            Description = "Sends push notifications through a mobile app. Commonly used for promotions, flash sales, and real-time order status updates.",
            SampleData = "Flash Sale! Get 20% off for the next 2 hours."
        },
        [NotificationSendMethod.WhatsApp] = new SendMethodDescription
        {
            Method = NotificationSendMethod.WhatsApp,
            Name = "WhatsApp",
            Description = "Sends notifications via WhatsApp. Popular in some regions for direct customer engagement and order support.",
            SampleData = "Hi Jane, your order #ORD123456 has been delivered. Thank you for shopping with us!"
        }
    };
}
