using System.Text;

using Microsoft.Extensions.Options;

namespace Infrastructure.Security.Authentication.Options;

public sealed class JwtOptions : IValidateOptions<JwtOptions>
{
    public const string Section = "Authentication:Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;

    // JWT Access Token Settings
    public int AccessTokenLifetimeMinutes { get; init; } = 15;

    // Refresh Token Settings
    public int RefreshTokenLifetimeDays { get; init; } = 7; // Standard expiry
    public int RefreshTokenRememberMeLifetimeDays { get; init; } = 30; // Extended for "remember me"

    // Admin-Specific Refresh Token Settings for stricter security
    public int AdminRefreshTokenLifetimeDays { get; init; } = 1; // Shorter expiry for admins
    public int AdminMaxActiveRefreshTokensPerUser { get; init; } = 2; // Fewer concurrent sessions

    // Token Limits
    public int MaxActiveRefreshTokensPerUser { get; init; } = 5;
    public int RevokedTokenRetentionDays { get; init; } = 5;

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        List<string> failures = new List<string>();

        // Required string validations
        if (string.IsNullOrWhiteSpace(options.Issuer))
            failures.Add("JwtOptions.Issuer is required and cannot be empty.");

        if (string.IsNullOrWhiteSpace(options.Audience))
            failures.Add("JwtOptions.Audience is required and cannot be empty.");

        if (string.IsNullOrWhiteSpace(options.Secret))
            failures.Add("JwtOptions.Secret is required and cannot be empty.");

        // Security validations for Secret
        if (!string.IsNullOrWhiteSpace(options.Secret))
        {
            byte[] secretBytes = Encoding.UTF8.GetBytes(options.Secret);
            if (secretBytes.Length < 32) // 256 bits minimum
                failures.Add("JwtOptions.Secret must be at least 32 characters (256 bits) for HMAC-SHA256 security.");

            if (secretBytes.Length < 64) // Recommend 512 bits
                failures.Add("JwtOptions.Secret should be at least 64 characters (512 bits) for optimal security.");
        }

        // Access token expiry validations
        if (options.AccessTokenLifetimeMinutes < 5)
            failures.Add("JwtOptions.AccessTokenLifetimeMinutes must be at least 5 minutes.");

        if (options.AccessTokenLifetimeMinutes > 60)
            failures.Add("JwtOptions.AccessTokenLifetimeMinutes should not exceed 60 minutes for security reasons. Use refresh tokens for longer sessions.");

        // Refresh token expiry validations
        if (options.RefreshTokenLifetimeDays < 1)
            failures.Add("JwtOptions.RefreshTokenExpiryInDays must be at least 1 day.");

        if (options.AdminRefreshTokenLifetimeDays < 1)
            failures.Add("JwtOptions.AdminRefreshTokenLifetimeDays must be at least 1 day.");

        if (options.AdminMaxActiveRefreshTokensPerUser < 1)
            failures.Add("JwtOptions.AdminMaxActiveRefreshTokensPerUser must be at least 1.");

        if (options.RefreshTokenLifetimeDays > 30)
            failures.Add("JwtOptions.RefreshTokenExpiryInDays should not exceed 30 days for security reasons.");

        if (options.RefreshTokenRememberMeLifetimeDays < options.RefreshTokenLifetimeDays)
            failures.Add("JwtOptions.RefreshTokenExpiryRememberMeInDays must be greater than or equal to RefreshTokenExpiryInDays.");

        if (options.RefreshTokenRememberMeLifetimeDays > 90)
            failures.Add("JwtOptions.RefreshTokenExpiryRememberMeInDays should not exceed 90 days for security reasons.");

        // Token limit validations
        if (options.MaxActiveRefreshTokensPerUser < 1)
            failures.Add("JwtOptions.MaxActiveRefreshTokensPerUser must be at least 1.");

        if (options.MaxActiveRefreshTokensPerUser > 20)
            failures.Add("JwtOptions.MaxActiveRefreshTokensPerUser should not exceed 20 to prevent abuse.");

        // Revoked token retention validation
        if (options.RevokedTokenRetentionDays < 0)
            failures.Add("JwtOptions.RevokedTokenRetentionDays must be non-negative.");

        if (options.RevokedTokenRetentionDays > 365)
            failures.Add("JwtOptions.RevokedTokenRetentionDays should not exceed 365 days to manage storage.");

        // Security recommendations (warnings, not failures)
        if (options.AccessTokenLifetimeMinutes > 30)
            failures.Add("SECURITY WARNING: JwtOptions.AccessTokenLifetimeMinutes exceeds 30 minutes. Consider shorter durations for better security.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}