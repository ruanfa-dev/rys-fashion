namespace UseCases.Common.Notification.Constants;

public static partial class NotificationParameters
{
    public enum NotificationParameter
    {
        // System-related parameters
        SystemName,                // Name of the fashion e-shop or application (e.g., "TrendyThreads")
        SupportEmail,              // Support email address for customer service (e.g., "support@trendythreads.com")
        SupportPhone,              // Support phone number for customer service (e.g., "+1-800-555-1234")
        CustomerSupportLink,       // URL to the customer support page (e.g., "https://trendythreads.com/support")

        // User-related parameters
        UserName,                  // Username of the user (e.g., "fashionista123")
        UserEmail,                 // Email address of the user (e.g., "user@domain.com")
        UserFullName,              // Full name of the user (e.g., "Jane Doe")
        UserFirstName,             // First name of the user (e.g., "Jane")
        UserLastName,              // Last name of the user (e.g., "Doe")
        UserProfileUrl,            // URL to the user's profile page (e.g., "https://trendythreads.com/profile")
        UserFavoriteCategory,      // User's favorite fashion category (e.g., "Dresses", "Streetwear")
        OtpCode,                   // One-time password for two-factor authentication (e.g., "123456")

        // Order-related parameters
        OrderId,                   // Unique identifier for the order (e.g., "ORD123456")
        OrderDate,                 // Date the order was placed (e.g., "2025-05-31")
        OrderTotal,                // Total cost of the order (e.g., "$150.00")
        OrderStatus,               // Status of the order (e.g., "Processing", "Shipped")
        OrderTrackingNumber,       // Tracking number for the order shipment (e.g., "1Z9999W999999999")
        OrderTrackingUrl,          // URL to track the shipment (e.g., "https://carrier.com/track/1Z9999W999999999")
        OrderItems,                // List of items in the order (e.g., "Red Dress, Black Sneakers")

        // Payment-related parameters
        PaymentStatus,             // Status of the payment (e.g., "Successful", "Failed")
        PaymentAmount,             // Amount paid for the order (e.g., "$150.00")
        PaymentMethod,             // Payment method used (e.g., "Credit Card", "PayPal")

        // Link-related parameters
        ActiveUrl,                 // URL for activating the user's account (e.g., "https://trendythreads.com/activate?token=xyz")
        ResetPasswordUrl,          // URL for resetting the user's password (e.g., "https://trendythreads.com/reset?token=xyz")
        UnsubscribeUrl,            // URL for unsubscribing from marketing emails (e.g., "https://trendythreads.com/unsubscribe")
        SurveyUrl,                 // URL for a marketing survey (e.g., "https://trendythreads.com/survey")
        SiteUrl,                   // Base URL of the e-shop (e.g., "https://trendythreads.com")

        // Time-related parameters
        CreatedDateTimeOffset,           // Date and time when the user or order was created (e.g., "2025-05-31 09:46:00")
        ExpiryDateTimeOffset,            // Expiry date and time for a promotion or offer (e.g., "2025-06-07 23:59:59")
        DeliveryDate,              // Expected or actual delivery date for the order (e.g., "2025-06-05")

        // Promotional parameters
        PromoCode,                 // Promotional code applied to the order (e.g., "SUMMER25")
        PromoDiscount,             // Discount amount applied via promo code (e.g., "25% off")
        PromoUrl,                  // URL for the promotional offer or sale page (e.g., "https://trendythreads.com/summer-sale")

        // Fashion-specific parameters
        CollectionName,            // Name of a new or featured collection (e.g., "Summer Chic Collection")
        CollectionUrl,             // URL to a specific collection (e.g., "https://trendythreads.com/collections/summer-chic")
        RecentProductView,         // Name or link of a recently viewed product (e.g., "Floral Midi Dress")
        RecommendedProductUrl,     // URL to a recommended product based on user behavior (e.g., "https://trendythreads.com/products/floral-dress")
        LoyaltyPoints,             // User's current loyalty points balance (e.g., "150 points")
        LoyaltyRewardUrl           // URL to redeem loyalty rewards (e.g., "https://trendythreads.com/rewards")
    }
    public class ParameterDescription
    {
        public NotificationParameter Parameter { get; init; }
        public required string Name { get; init; }
        public required string Description { get; init; }
        public required string SampleData { get; init; }
    }

    public static readonly Dictionary<NotificationParameter, ParameterDescription> Description = new()
    {
        #region System-related parameters
        [NotificationParameter.SystemName] = new ParameterDescription
        {
            Parameter = NotificationParameter.SystemName,
            Name = "System Name",
            Description = "The name of the fashion e-shop or application, used for branding in notifications.",
            SampleData = "TrendyThreads"
        },
        [NotificationParameter.SupportEmail] = new ParameterDescription
        {
            Parameter = NotificationParameter.SupportEmail,
            Name = "Support Email",
            Description = "The email address for customer service, provided for user support inquiries.",
            SampleData = "support@trendythreads.com"
        },
        [NotificationParameter.SupportPhone] = new ParameterDescription
        {
            Parameter = NotificationParameter.SupportPhone,
            Name = "Support Phone",
            Description = "The phone number for customer service, used for urgent or direct support contact.",
            SampleData = "+1-800-555-1234"
        },
        [NotificationParameter.CustomerSupportLink] = new ParameterDescription
        {
            Parameter = NotificationParameter.CustomerSupportLink,
            Name = "Customer Support Link",
            Description = "URL to the customer support page, directing users to help resources.",
            SampleData = "https://trendythreads.com/support"
        },
        #endregion

        #region User-related parameters
        [NotificationParameter.UserName] = new ParameterDescription
        {
            Parameter = NotificationParameter.UserName,
            Name = "User Name",
            Description = "The username of the user, used for login or identification purposes.",
            SampleData = "fashionista123"
        },
        [NotificationParameter.UserEmail] = new ParameterDescription
        {
            Parameter = NotificationParameter.UserEmail,
            Name = "User Email",
            Description = "The email address of the user, used for communication and notifications.",
            SampleData = "jane.doe@domain.com"
        },
        [NotificationParameter.UserFullName] = new ParameterDescription
        {
            Parameter = NotificationParameter.UserFullName,
            Name = "User Full Name",
            Description = "The full name of the user, used for personalized greetings in notifications.",
            SampleData = "Jane Doe"
        },
        [NotificationParameter.UserFirstName] = new ParameterDescription
        {
            Parameter = NotificationParameter.UserFirstName,
            Name = "User First Name",
            Description = "The first name of the user, used for a friendly and personal tone.",
            SampleData = "Jane"
        },
        [NotificationParameter.UserLastName] = new ParameterDescription
        {
            Parameter = NotificationParameter.UserLastName,
            Name = "User Last Name",
            Description = "The last name of the user, used in formal or full-name contexts.",
            SampleData = "Doe"
        },
        [NotificationParameter.UserProfileUrl] = new ParameterDescription
        {
            Parameter = NotificationParameter.UserProfileUrl,
            Name = "User Profile URL",
            Description = "URL to the user's profile page, allowing quick access to account settings.",
            SampleData = "https://trendythreads.com/profile"
        },
        [NotificationParameter.UserFavoriteCategory] = new ParameterDescription
        {
            Parameter = NotificationParameter.UserFavoriteCategory,
            Name = "User Favorite Category",
            Description = "The user's preferred fashion category, used for personalized recommendations.",
            SampleData = "Dresses"
        },
        [NotificationParameter.OtpCode] = new ParameterDescription
        {
            Parameter = NotificationParameter.OtpCode,
            Name = "OTP Code",
            Description = "One-time password for two-factor authentication, used for secure login or actions.",
            SampleData = "123456"
        },
        #endregion

        #region Order-related parameters
        [NotificationParameter.OrderId] = new ParameterDescription
        {
            Parameter = NotificationParameter.OrderId,
            Name = "Order ID",
            Description = "Unique identifier for the order, used to reference specific purchases.",
            SampleData = "ORD123456"
        },
        [NotificationParameter.OrderDate] = new ParameterDescription
        {
            Parameter = NotificationParameter.OrderDate,
            Name = "Order Date",
            Description = "The date the order was placed, providing context for the purchase timeline.",
            SampleData = "2025-05-31"
        },
        [NotificationParameter.OrderTotal] = new ParameterDescription
        {
            Parameter = NotificationParameter.OrderTotal,
            Name = "Order Total",
            Description = "The total cost of the order, including all items and fees.",
            SampleData = "$150.00"
        },
        [NotificationParameter.OrderStatus] = new ParameterDescription
        {
            Parameter = NotificationParameter.OrderStatus,
            Name = "Order Status",
            Description = "The current status of the order, such as Processing, Shipped, or Delivered.",
            SampleData = "Shipped"
        },
        [NotificationParameter.OrderTrackingNumber] = new ParameterDescription
        {
            Parameter = NotificationParameter.OrderTrackingNumber,
            Name = "Order Tracking Number",
            Description = "The tracking number for the order shipment, used for tracking delivery.",
            SampleData = "1Z9999W999999999"
        },
        [NotificationParameter.OrderTrackingUrl] = new ParameterDescription
        {
            Parameter = NotificationParameter.OrderTrackingUrl,
            Name = "Order Tracking URL",
            Description = "URL to track the shipment, linking to the carrier’s tracking page.",
            SampleData = "https://carrier.com/track/1Z9999W999999999"
        },
        [NotificationParameter.OrderItems] = new ParameterDescription
        {
            Parameter = NotificationParameter.OrderItems,
            Name = "Order Items",
            Description = "List of items in the order, providing details of purchased products.",
            SampleData = "Red Dress, Black Sneakers"
        },
        #endregion

        #region Payment-related parameters
        [NotificationParameter.PaymentStatus] = new ParameterDescription
        {
            Parameter = NotificationParameter.PaymentStatus,
            Name = "Payment Status",
            Description = "The status of the payment, such as Successful or Failed.",
            SampleData = "Successful"
        },
        [NotificationParameter.PaymentAmount] = new ParameterDescription
        {
            Parameter = NotificationParameter.PaymentAmount,
            Name = "Payment Amount",
            Description = "The amount paid for the order, reflecting the transaction value.",
            SampleData = "$150.00"
        },
        [NotificationParameter.PaymentMethod] = new ParameterDescription
        {
            Parameter = NotificationParameter.PaymentMethod,
            Name = "Payment Method",
            Description = "The payment method used for the order, such as Credit Card or PayPal.",
            SampleData = "Credit Card"
        },
        #endregion

        #region Link-related parameters
        [NotificationParameter.ActiveUrl] = new ParameterDescription
        {
            Parameter = NotificationParameter.ActiveUrl,
            Name = "Activation URL",
            Description = "URL for activating the user’s account, used during registration.",
            SampleData = "https://trendythreads.com/activate?token=xyz"
        },
        [NotificationParameter.ResetPasswordUrl] = new ParameterDescription
        {
            Parameter = NotificationParameter.ResetPasswordUrl,
            Name = "Reset Password URL",
            Description = "URL for resetting the user’s password, used in password recovery.",
            SampleData = "https://trendythreads.com/reset?token=xyz"
        },
        [NotificationParameter.UnsubscribeUrl] = new ParameterDescription
        {
            Parameter = NotificationParameter.UnsubscribeUrl,
            Name = "Unsubscribe URL",
            Description = "URL for unsubscribing from marketing emails, ensuring compliance with regulations.",
            SampleData = "https://trendythreads.com/unsubscribe"
        },
        [NotificationParameter.SurveyUrl] = new ParameterDescription
        {
            Parameter = NotificationParameter.SurveyUrl,
            Name = "Survey URL",
            Description = "URL for a marketing survey, used to collect user feedback.",
            SampleData = "https://trendythreads.com/survey"
        },
        [NotificationParameter.SiteUrl] = new ParameterDescription
        {
            Parameter = NotificationParameter.SiteUrl,
            Name = "Site URL",
            Description = "Base URL of the e-shop, used for general navigation links.",
            SampleData = "https://trendythreads.com"
        },
        #endregion

        #region Time-related parameters
        [NotificationParameter.CreatedDateTimeOffset] = new ParameterDescription
        {
            Parameter = NotificationParameter.CreatedDateTimeOffset,
            Name = "Created DateTimeOffset",
            Description = "Date and time when the user or order was created, for record-keeping.",
            SampleData = "2025-05-31 09:46:00"
        },
        [NotificationParameter.ExpiryDateTimeOffset] = new ParameterDescription
        {
            Parameter = NotificationParameter.ExpiryDateTimeOffset,
            Name = "Expiry DateTimeOffset",
            Description = "Expiry date and time for a promotion or offer, creating urgency.",
            SampleData = "2025-06-07 23:59:59"
        },
        [NotificationParameter.DeliveryDate] = new ParameterDescription
        {
            Parameter = NotificationParameter.DeliveryDate,
            Name = "Delivery Date",
            Description = "Expected or actual delivery date for the order, informing users of timelines.",
            SampleData = "2025-06-05"
        },
        #endregion

        #region Promotional parameters
        [NotificationParameter.PromoCode] = new ParameterDescription
        {
            Parameter = NotificationParameter.PromoCode,
            Name = "Promo Code",
            Description = "Promotional code applied to the order, offering discounts or incentives.",
            SampleData = "SUMMER25"
        },
        [NotificationParameter.PromoDiscount] = new ParameterDescription
        {
            Parameter = NotificationParameter.PromoDiscount,
            Name = "Promo Discount",
            Description = "Discount amount applied via a promo code, such as a percentage or fixed amount.",
            SampleData = "25% off"
        },
        [NotificationParameter.PromoUrl] = new ParameterDescription
        {
            Parameter = NotificationParameter.PromoUrl,
            Name = "Promo URL",
            Description = "URL for the promotional offer or sale page, directing users to specific campaigns.",
            SampleData = "https://trendythreads.com/summer-sale"
        },
        #endregion

        #region Fashion-specific parameters
        [NotificationParameter.CollectionName] = new ParameterDescription
        {
            Parameter = NotificationParameter.CollectionName,
            Name = "Collection Name",
            Description = "Name of a new or featured fashion collection, used in promotional notifications.",
            SampleData = "Summer Chic Collection"
        },
        [NotificationParameter.CollectionUrl] = new ParameterDescription
        {
            Parameter = NotificationParameter.CollectionUrl,
            Name = "Collection URL",
            Description = "URL to a specific fashion collection, linking to curated product pages.",
            SampleData = "https://trendythreads.com/collections/summer-chic"
        },
        [NotificationParameter.RecentProductView] = new ParameterDescription
        {
            Parameter = NotificationParameter.RecentProductView,
            Name = "Recent Product View",
            Description = "Name or link of a recently viewed product, used for re-engagement.",
            SampleData = "Floral Midi Dress"
        },
        [NotificationParameter.RecommendedProductUrl] = new ParameterDescription
        {
            Parameter = NotificationParameter.RecommendedProductUrl,
            Name = "Recommended Product URL",
            Description = "URL to a recommended product based on user behavior, enhancing personalization.",
            SampleData = "https://trendythreads.com/products/floral-dress"
        },
        [NotificationParameter.LoyaltyPoints] = new ParameterDescription
        {
            Parameter = NotificationParameter.LoyaltyPoints,
            Name = "Loyalty Points",
            Description = "User’s current loyalty points balance, used to encourage reward redemption.",
            SampleData = "150 points"
        },
        [NotificationParameter.LoyaltyRewardUrl] = new ParameterDescription
        {
            Parameter = NotificationParameter.LoyaltyRewardUrl,
            Name = "Loyalty Reward URL",
            Description = "URL to redeem loyalty rewards, linking to the rewards program page.",
            SampleData = "https://trendythreads.com/rewards"
        }
        #endregion
    };
}
