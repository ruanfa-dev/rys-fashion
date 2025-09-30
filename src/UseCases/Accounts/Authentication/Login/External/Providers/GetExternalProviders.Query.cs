using Core.Identity.Users;

using ErrorOr;

using Microsoft.AspNetCore.Authentication;
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
    public string[] RequiredScopes { get; init; } = Array.Empty<string>();
    public string ConfigurationUrl { get; init; } = null!;
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
        // Supported providers for production e-commerce (removed Microsoft)
        private static readonly HashSet<string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase)
        {
            "google",
            "facebook"
        };

        public async Task<ErrorOr<List<Result>>> Handle(Query request, CancellationToken cancellationToken)
        {
            try
            {
                IEnumerable<AuthenticationScheme> schemes = await signInManager.GetExternalAuthenticationSchemesAsync();
                string baseUrl = GetBaseUrl();
                const string externalRoute = ExternalLoginEndpoint.Route;

                List<Result> providers = new List<Result>();

                foreach (AuthenticationScheme scheme in schemes)
                {
                    string providerName = scheme.Name.ToLowerInvariant();
                    
                    // Security: Only include supported providers
                    if (!SupportedProviders.Contains(providerName))
                    {
                        logger.LogDebug("Skipping unsupported provider: {Provider}", scheme.Name);
                        continue;
                    }

                    if (!IsProviderConfigured(scheme.Name))
                    {
                        logger.LogDebug("Skipping unconfigured provider: {Provider}", scheme.Name);
                        continue;
                    }

                    Result provider = new Result
                    {
                        Name = providerName,
                        DisplayName = GetProviderDisplayName(providerName),
                        LoginUrl = BuildTokenExchangeUrl(baseUrl, externalRoute, providerName),
                        IconUrl = GetProviderIconUrl(providerName),
                        RequiredScopes = GetProviderRequiredScopes(providerName),
                        ConfigurationUrl = BuildConfigurationUrl(baseUrl, externalRoute, providerName),
                        IsEnabled = true
                    };

                    providers.Add(provider);
                }

                logger.LogInformation("Retrieved {Count} configured external authentication providers: {Providers}", 
                    providers.Count, string.Join(", ", providers.Select(p => p.Name)));
                
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
            string? configuredBaseUrl = configuration["App:BaseUrl"];
            if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
            {
                return configuredBaseUrl.TrimEnd('/');
            }

            HttpContext? context = httpContextAccessor.HttpContext;
            if (context != null)
            {
                HttpRequest request = context.Request;
                string scheme = request.Scheme;
                string? host = request.Host.Value;
                return $"{scheme}://{host}";
            }

            logger.LogWarning("No base URL configured and no HTTP context available. Using localhost fallback.");
            return "https://localhost:5001";
        }

        private static string BuildTokenExchangeUrl(string baseUrl, string externalRoute, string providerName)
        {
            return $"{baseUrl}{externalRoute}/token/exchange/{providerName}";
        }

        private static string BuildConfigurationUrl(string baseUrl, string externalRoute, string providerName)
        {
            return $"{baseUrl}{externalRoute}/config/{providerName}";
        }

        private static string GetProviderDisplayName(string providerName) =>
            providerName switch
            {
                "google" => "Google",
                "facebook" => "Facebook",
                _ => char.ToUpperInvariant(providerName[0]) + providerName[1..].ToLowerInvariant()
            };

        private static string? GetProviderIconUrl(string providerName) =>
            providerName switch
            {
                "google" => "https://developers.google.com/identity/images/g-logo.png",
                "facebook" => "https://upload.wikimedia.org/wikipedia/commons/5/51/Facebook_f_logo_%282019%29.svg",
                _ => null
            };

        private static string[] GetProviderRequiredScopes(string providerName) =>
            providerName switch
            {
                "google" => ["openid", "email", "profile"],
                "facebook" => ["email", "public_profile"],
                _ => Array.Empty<string>()
            };

        private bool IsProviderConfigured(string providerName)
        {
            try
            {
                string normalizedName = providerName.ToLowerInvariant();
                
                // Only check configuration for supported providers
                if (!SupportedProviders.Contains(normalizedName))
                {
                    return false;
                }

                IConfigurationSection section = configuration.GetSection($"Authentication:{providerName}");
                if (!section.Exists())
                {
                    logger.LogDebug("Configuration section not found for provider: {Provider}", providerName);
                    return false;
                }

                bool isConfigured = normalizedName switch
                {
                    "google" => HasRequiredGoogleConfig(section),
                    "facebook" => HasRequiredFacebookConfig(section),
                    _ => false
                };

                if (!isConfigured)
                {
                    logger.LogDebug("Provider {Provider} is not properly configured", providerName);
                }

                return isConfigured;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error checking configuration for provider {Provider}", providerName);
                return false;
            }
        }

        private static bool HasRequiredGoogleConfig(IConfigurationSection section)
        {
            string? clientId = section["ClientId"];
            string? clientSecret = section["ClientSecret"];
            return !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret);
        }

        private static bool HasRequiredFacebookConfig(IConfigurationSection section)
        {
            string? appId = section["AppId"];
            string? appSecret = section["AppSecret"];
            return !string.IsNullOrWhiteSpace(appId) && !string.IsNullOrWhiteSpace(appSecret);
        }
    }
}