namespace Infrastructure.Notification.Options;

public sealed class SmtpOptions
{
    public const string Section = "Notifications:SmtpOptions";

    public bool EnableEmailNotifications { get; init; }
    public string Provider { get; init; } = "papercut"; // papercut | smtp | sendgrid
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public int? MaxAttachmentSize { get; set; } = 25;

    public SmtpConfig? SmtpConfig { get; init; }
    public SendGridConfig? SendGridConfig { get; init; }
}

public sealed class SmtpConfig
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 25;
    public bool EnableSsl { get; init; } = false;
    public bool UseDefaultCredentials { get; init; } = true;
    public string? Username { get; init; }
    public string? Password { get; init; }
}

public sealed class SendGridConfig
{
    public string ApiKey { get; init; } = null!;
}
