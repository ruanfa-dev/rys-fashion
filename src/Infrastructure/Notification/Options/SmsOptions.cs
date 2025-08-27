namespace Infrastructure.Notification.Options;

public class SmsOptions
{
    public const string Section = "Notifications:SmsOptions";
    public bool EnableSmsNotifications { get; init; }
    public string DefaultSenderNumber { get; init; } = null!;

    public SinchConfig SinchConfig { get; init; } = null!;
}
public sealed class SinchConfig
{
    public string ProjectId { get; init; } = null!;
    public string KeyId { get; init; } = null!;
    public string KeySecret { get; init; } = null!;
    public string SenderPhoneNumber { get; init; } = null!;
    public string SmsRegion { get; init; } = null!; // e.g. "Us", "Eu"
}
