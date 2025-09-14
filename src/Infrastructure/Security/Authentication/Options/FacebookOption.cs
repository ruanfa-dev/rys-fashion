using Microsoft.Extensions.Options;

namespace Infrastructure.Security.Authentication.Options;

public sealed class FacebookOption : IValidateOptions<FacebookOption>
{
    public const string Section = "Authentication:Facebook";

    public string AppId { get; init; } = string.Empty;
    public string AppSecret { get; init; } = string.Empty;

    public ValidateOptionsResult Validate(string? name, FacebookOption options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.AppId))
        {
            errors.Add("AppId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.AppSecret))
        {
            errors.Add("AppSecret is required.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
