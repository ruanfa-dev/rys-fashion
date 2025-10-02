using Microsoft.Extensions.Options;

namespace Infrastructure.Notification.Options;

public sealed class SmsOptions : IValidateOptions<SmsOptions>
{
    public const string Section = "Notifications:SmsOptions";
    public bool EnableSmsNotifications { get; init; }
    public string DefaultSenderNumber { get; init; } = null!;

    public SinchConfig SinchConfig { get; init; } = null!;

    public ValidateOptionsResult Validate(string? name, SmsOptions options)
    {
        List<string> errors = [];

        // Validate that EnableSmsNotifications is true if DefaultSenderNumber is set
        if (options.EnableSmsNotifications)
        {
            if (string.IsNullOrEmpty(options.DefaultSenderNumber))
            {
                errors.Add("DefaultSenderNumber must be provided when SMS notifications are enabled.");
            }
        }

        // Validate SinchConfig
        if (options.SinchConfig != null)
        {
            // Validate required fields in SinchConfig
            if (string.IsNullOrEmpty(options.SinchConfig.ProjectId))
            {
                errors.Add("SinchConfig ProjectId is required.");
            }

            if (string.IsNullOrEmpty(options.SinchConfig.KeyId))
            {
                errors.Add("SinchConfig KeyId is required.");
            }

            if (string.IsNullOrEmpty(options.SinchConfig.KeySecret))
            {
                errors.Add("SinchConfig KeySecret is required.");
            }

            if (string.IsNullOrEmpty(options.SinchConfig.SenderPhoneNumber))
            {
                errors.Add("SinchConfig SenderPhoneNumber is required.");
            }

            if (string.IsNullOrEmpty(options.SinchConfig.SmsRegion))
            {
                errors.Add("SinchConfig SmsRegion is required.");
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
public sealed class SinchConfig
{
    public string ProjectId { get; init; } = null!;
    public string KeyId { get; init; } = null!;
    public string KeySecret { get; init; } = null!;
    public string SenderPhoneNumber { get; init; } = null!;
    public string SmsRegion { get; init; } = null!; // e.g. "Us", "Eu"
}
