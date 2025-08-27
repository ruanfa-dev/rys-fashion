namespace UseCases.Common.Notification.Constants;

public static partial class NotificationPriorities
{
    /// <summary>
    /// Defines the priority levels for notifications.
    /// This helps determine the urgency and delivery order of notifications to users.
    /// </summary>
    public enum NotificationPriority
    {
        /// <summary>
        /// Low priority — used for non-urgent messages such as promotions,
        /// newsletters, or informational updates that do not require immediate attention.
        /// </summary>
        Low,

        /// <summary>
        /// Normal priority — used for standard notifications such as order updates,
        /// account changes, or general system messages that are important but not urgent.
        /// </summary>
        Normal,

        /// <summary>
        /// High priority — used for urgent notifications such as security alerts,
        /// failed payments, or time-sensitive actions that require immediate attention.
        /// </summary>
        High
    }

    public class PriorityDescription
    {
        public NotificationPriority Priority { get; set; }
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string SampleData { get; set; } = default!;
    }

    public static readonly Dictionary<NotificationPriority, PriorityDescription> Description = new()
    {
        [NotificationPriority.Low] = new PriorityDescription
        {
            Priority = NotificationPriority.Low,
            Name = "Low",
            Description = "Non-urgent notifications such as promotions, newsletters, or general updates that do not require immediate attention.",
            SampleData = "Summer sale starts next week! Get ready for up to 50% off."
        },
        [NotificationPriority.Normal] = new PriorityDescription
        {
            Priority = NotificationPriority.Normal,
            Name = "Normal",
            Description = "Standard priority notifications such as order updates, account changes, or system messages that are important but not urgent.",
            SampleData = "Your order #ORD123456 has been shipped."
        },
        [NotificationPriority.High] = new PriorityDescription
        {
            Priority = NotificationPriority.High,
            Name = "High",
            Description = "Urgent notifications such as security alerts, failed payments, or time-sensitive actions that require immediate attention.",
            SampleData = "Suspicious login detected on your account. Please verify your identity immediately."
        }
    };
}

