using Ardalis.GuardClauses;

using Microsoft.Extensions.Options;

namespace Infrastructure.Security.Authorization.Options;

public class AuthUserCacheOption : IValidateOptions<AuthUserCacheOption>
{
    public const string Section = "Authorization:AuthUserCache";

    public int UserAuthCacheExpiryInMinutes { get; set; } = 60;
    public int UserAuthCacheSlidingInMinutes { get; set; } = 30;
    public int RoleClaimsCacheExpiryInMinutes { get; set; } = 120;

    public ValidateOptionsResult Validate(string? name, AuthUserCacheOption options)
    {
        Guard.Against.Null(options, nameof(options));

        if (options.UserAuthCacheExpiryInMinutes <= 0)
            return ValidateOptionsResult.Fail("Authorization:AuthUserCache:UserAuthCacheExpiryInMinutes must be greater than 0.");

        if (options.UserAuthCacheSlidingInMinutes <= 0 || options.UserAuthCacheSlidingInMinutes > options.UserAuthCacheExpiryInMinutes)
            return ValidateOptionsResult.Fail("Authorization:AuthUserCache:UserAuthCacheSlidingInMinutes must be greater than 0 and less than or equal to UserAuthCacheExpiryInMinutes.");

        if (options.RoleClaimsCacheExpiryInMinutes <= 0)
            return ValidateOptionsResult.Fail("Authorization:AuthUserCache:RoleClaimsCacheExpiryInMinutes must be greater than 0.");

        return ValidateOptionsResult.Success;
    }
}