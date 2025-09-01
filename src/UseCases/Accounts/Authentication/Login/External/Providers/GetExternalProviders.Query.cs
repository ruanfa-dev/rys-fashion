using Core.Identity;

using ErrorOr;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Accounts.Authentication.Login.External.Providers;
public record ExternalProvider
{
    public string Name { get; init; } = null!;
    public string DisplayName { get; init; } = null!;
    public string LoginUrl { get; init; } = null!;
    public string? IconUrl { get; init; }
    public bool IsEnabled { get; init; } = true;
}

public static partial class GetExternalProviders
{
    public sealed record Query : IQuery<List<Result>>;
    public sealed record Result : ExternalProvider;

    public sealed class Handler(
        SignInManager<User> signInManager,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<Handler> logger
    ) : IQueryHandler<Query, List<Result>>
    {
        public async Task<ErrorOr<List<Result>>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                var schemes = await signInManager.GetExternalAuthenticationSchemesAsync();
                var baseUrl = GetBaseUrl();
                const string externalRoute = ExternalLoginEndpoint.Route;

                var providers = new List<Result>();

                foreach (var scheme in schemes)
                {
                    if (!IsProviderConfigured(scheme.Name))
                    {
                        logger.LogDebug("Skipping unconfigured provider: {Provider}", scheme.Name);
                        continue;
                    }

                    var provider = new Result
                    {
                        Name = scheme.Name.ToLowerInvariant(),
                        DisplayName = GetProviderDisplayName(scheme.Name),
                        LoginUrl = BuildLoginUrl(baseUrl, externalRoute, scheme.Name),
                        IconUrl = GetProviderIconUrl(scheme.Name),
                        IsEnabled = true
                    };

                    providers.Add(provider);
                }

                logger.LogInformation("Retrieved {Count} configured external authentication providers", providers.Count);
                return providers;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve external authentication providers");
                return Error.Failure(
                    code: "ExternalProviders.RetrievalFailed",
                    description: "Failed to retrieve external authentication providers"
                );
            }
        }

        private string GetBaseUrl()
        {
            var configuredBaseUrl = configuration["App:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
            {
                return configuredBaseUrl.TrimEnd('/');
            }

            var context = httpContextAccessor.HttpContext;
            if (context != null)
            {
                var request = context.Request;
                var scheme = request.Scheme;
                var host = request.Host.Value;
                return $"{scheme}://{host}";
            }

            logger.LogWarning("No base URL configured and no HTTP context available. Using localhost fallback.");
            return "https://localhost:5001";
        }

        private static string BuildLoginUrl(string baseUrl, string externalRoute, string providerName)
        {
            return $"{baseUrl}/{externalRoute}/token/exchange/{providerName.ToLowerInvariant()}";
        }

        private static string GetProviderDisplayName(string providerName) =>
            providerName.ToLowerInvariant() switch
            {
                "google" => "Google",
                "facebook" => "Facebook",
                "github" => "GitHub",
                _ => providerName
            };

        private static string? GetProviderIconUrl(string providerName) =>
            providerName.ToLowerInvariant() switch
            {
                "google" => "https://developers.google.com/identity/images/g-logo.png",
                "facebook" => "https://upload.wikimedia.org/wikipedia/commons/5/51/Facebook_f_logo_%282019%29.svg",
                "github" => "https://github.githubassets.com/images/modules/logos_page/GitHub-Mark.png",
                _ => null
            };

        private bool IsProviderConfigured(string providerName)
        {
            try
            {
                var section = configuration.GetSection($"Authentication:{providerName}");
                if (!section.Exists())
                {
                    return false;
                }

                return providerName.ToLowerInvariant() switch
                {
                    "google" => HasRequiredGoogleConfig(section),
                    "facebook" => HasRequiredFacebookConfig(section),
                    "github" => HasRequiredGenericOAuthConfig(section, "ClientId", "ClientSecret"),
                    _ => HasRequiredGenericOAuthConfig(section, "ClientId", "ClientSecret")
                };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error checking configuration for provider {Provider}", providerName);
                return false;
            }
        }

        private static bool HasRequiredGoogleConfig(IConfigurationSection section)
        {
            var clientId = section["ClientId"];
            var clientSecret = section["ClientSecret"];
            return !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret);
        }

        private static bool HasRequiredFacebookConfig(IConfigurationSection section)
        {
            var appId = section["AppId"];
            var appSecret = section["AppSecret"];
            return !string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(appSecret);
        }

        private static bool HasRequiredGenericOAuthConfig(IConfigurationSection section, string clientIdKey, string clientSecretKey)
        {
            var clientId = section[clientIdKey];
            var clientSecret = section[clientSecretKey];
            return !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret);
        }
    }
}