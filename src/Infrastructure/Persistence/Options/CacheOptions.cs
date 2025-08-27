using Ardalis.GuardClauses;

using Microsoft.Extensions.Options;

namespace Infrastructure.Persistence.Options;

public class CacheOptions : IValidateOptions<CacheOptions>
{
    public const string SectionName = "Cache";

    public string Type { get; set; } = "InMemory"; // Default to InMemory
    public string? RedisConnection { get; set; }
    public int DefaultExpiryMinutes { get; set; } = 30;
    public int LocalCacheExpirySeconds { get; set; } = 300;

    public ValidateOptionsResult Validate(string? name, CacheOptions options)
    {
        Guard.Against.Null(options, nameof(options));

        if (string.IsNullOrWhiteSpace(options.Type))
            return ValidateOptionsResult.Fail("Cache:Type must be specified.");

        if (options.Type != "InMemory" && options.Type != "Redis")
            return ValidateOptionsResult.Fail("Cache:Type must be 'InMemory' or 'Redis'.");

        if (options.Type == "Redis" && string.IsNullOrWhiteSpace(options.RedisConnection))
            return ValidateOptionsResult.Fail("Cache:RedisConnection is required for Redis caching.");

        if (options.DefaultExpiryMinutes <= 0)
            return ValidateOptionsResult.Fail("Cache:DefaultExpiryMinutes must be greater than 0.");

        if (options.LocalCacheExpirySeconds <= 0)
            return ValidateOptionsResult.Fail("Cache:LocalCacheExpirySeconds must be greater than 0.");

        return ValidateOptionsResult.Success;
    }
}