namespace UseCases.Common.Notification.Constants;
public static partial class NotificationFormats
{
    public enum NotificationFormat
    {
        Default,
        Html
    }

    public class FormatDescription
    {
        public NotificationFormat Format { get; set; }
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string SampleData { get; set; } = default!;
    }

    public static readonly Dictionary<NotificationFormat, FormatDescription> Description = new()
    {
        [NotificationFormat.Default] = new FormatDescription
        {
            Format = NotificationFormat.Default,
            Name = "Default (Plain Text)",
            Description = "A plain text template without advanced styling. Suitable for SMS, WhatsApp, and basic email notifications where formatting is minimal.",
            SampleData = "Your order #ORD123456 has been shipped."
        },
        [NotificationFormat.Html] = new FormatDescription
        {
            Format = NotificationFormat.Html,
            Name = "HTML",
            Description = "A rich HTML template with styling, images, and layout control. Commonly used for marketing emails and branded notifications.",
            SampleData = "<html><body><h1>Order Shipped</h1><p>Your order #ORD123456 is on the way!</p></body></html>"
        }
    };
}
