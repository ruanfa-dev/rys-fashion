using Microsoft.Extensions.Options;

namespace Infrastructure.Security.Authentication.Options;

public sealed class JwtOptions : IValidateOptions<JwtOptions>
{
    public const string Section = "Authentication:Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
    public int ExpiryInMinutes { get; init; } = 60;
    public int RefreshTokenExpiryInDays { get; init; } = 7;
    public int RefreshTokenExpiryRememberMeInDays { get; init; } = 30;
    public int MaxTokensSystemUser { get; init; } = 1;
    public int MaxTokensCustomer { get; init; } = 5;
    public int MaxTokenAgeInDays { get; init; } = 90;

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
            failures.Add("JwtOptions.Issuer is required.");
        if (string.IsNullOrWhiteSpace(options.Audience))
            failures.Add("JwtOptions.Audience is required.");
        if (string.IsNullOrWhiteSpace(options.Secret))
            failures.Add("JwtOptions.Secret is required.");
        if (options.ExpiryInMinutes < 1)
            failures.Add("JwtOptions.ExpiryInMinutes must be greater than zero.");
        if (options.RefreshTokenExpiryInDays < 1)
            failures.Add("JwtOptions.RefreshTokenExpiryInDays must be greater than zero.");
        if (options.RefreshTokenExpiryRememberMeInDays < 1)
            failures.Add("JwtOptions.RefreshTokenExpiryRememberMeInDays must be greater than zero.");
        if (options.MaxTokensSystemUser < 1)
            failures.Add("JwtOptions.MaxTokensSystemUser must be greater than zero.");
        if (options.MaxTokensCustomer < 1)
            failures.Add("JwtOptions.MaxTokensCustomer must be greater than zero.");
        if (options.MaxTokenAgeInDays < 1)
            failures.Add("JwtOptions.MaxTokenAgeInDays must be greater than zero.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
