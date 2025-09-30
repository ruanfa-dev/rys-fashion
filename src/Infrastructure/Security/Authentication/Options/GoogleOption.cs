using Microsoft.Extensions.Options;

namespace Infrastructure.Security.Authentication.Options;

public sealed class GoogleOption : IValidateOptions<GoogleOption>
{
    public const string Section = "Authentication:Google";

    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;

    public ValidateOptionsResult Validate(string? name, GoogleOption options)
    {
        List<string> errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            errors.Add("ClientId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            errors.Add("ClientSecret is required.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
