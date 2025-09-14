using Microsoft.Extensions.Options;

namespace Infrastructure.Notification.Options;

public sealed class SmtpOptions : IValidateOptions<SmtpOptions>
{
    public const string Section = "Notifications:SmtpOptions";

    public bool EnableEmailNotifications { get; init; }
    public string Provider { get; init; } = "papercut"; // papercut | smtp | sendgrid
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public int? MaxAttachmentSize { get; set; } = 25;

    public SmtpConfig? SmtpConfig { get; init; }
    public SendGridConfig? SendGridConfig { get; init; }

    public ValidateOptionsResult Validate(string? name, SmtpOptions options)
    {
        var errors = new List<string>();

        // Validate that EnableEmailNotifications is true if FromEmail and FromName are set
        if (options.EnableEmailNotifications)
        {
            if (string.IsNullOrEmpty(options.FromEmail))
            {
                errors.Add("FromEmail must be provided when email notifications are enabled.");
            }

            if (string.IsNullOrEmpty(options.FromName))
            {
                errors.Add("FromName must be provided when email notifications are enabled.");
            }
        }

        // Validate provider
        if (string.IsNullOrEmpty(options.Provider))
        {
            errors.Add("Provider is required.");
        }
        else if (options.Provider != "papercut" && options.Provider != "smtp" && options.Provider != "sendgrid")
        {
            errors.Add($"Invalid provider: {options.Provider}. Allowed values are 'papercut', 'smtp', 'sendgrid'.");
        }

        // Validate SmtpConfig when the provider is 'smtp'
        if (options.Provider == "smtp" && options.SmtpConfig != null)
        {
            if (string.IsNullOrEmpty(options.SmtpConfig.Host))
            {
                errors.Add("SmtpConfig Host is required for 'smtp' provider.");
            }

            if (options.SmtpConfig.Port <= 0)
            {
                errors.Add("SmtpConfig Port must be a positive integer.");
            }
        }

        // Validate SendGridConfig when the provider is 'sendgrid'
        if (options.Provider == "sendgrid" && options.SendGridConfig != null)
        {
            if (string.IsNullOrEmpty(options.SendGridConfig.ApiKey))
            {
                errors.Add("SendGridConfig ApiKey is required for 'sendgrid' provider.");
            }
        }

        // Return validation result
        if (errors.Count > 0)
        {
            return ValidateOptionsResult.Fail(errors);
        }

        return ValidateOptionsResult.Success;
    }
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
