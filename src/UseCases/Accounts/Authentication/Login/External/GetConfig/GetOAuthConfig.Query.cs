using ErrorOr;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Accounts.Authentication.Login.External.GetConfig;

public static partial class GetOAuthConfig
{
    public sealed record Query(string Provider) : IQuery<Result>;

    public sealed record Result
    {
        public string Provider { get; init; } = null!;
        public string ClientId { get; init; } = null!;
        public string AuthorizationUrl { get; init; } = null!;
        public string TokenUrl { get; init; } = null!;
        public string[] Scopes { get; init; } = [];
        public string ResponseType { get; init; } = "code";
        public Dictionary<string, string> AdditionalParameters { get; init; } = new();
        public string TokenExchangeUrl { get; init; } = null!;
        public string ProviderName { get; init; } = null!;
        public bool RequiresPKCE { get; init; } = true; // Default to true for security
    }

    public sealed class Handler(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<Handler> logger
    ) : IQueryHandler<Query, Result>
    {
        // Supported providers for production e-commerce
        private static readonly HashSet<string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase)
        {
            "google",
            "facebook"
        };

        public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
        {
            string provider = request.Provider.ToLowerInvariant().Trim();

            // Security: Validate provider is supported
            if (!SupportedProviders.Contains(provider))
            {
                logger.LogWarning("Attempted to get config for unsupported provider: {Provider}", request.Provider);
                return Error.NotFound("Provider.NotSupported", $"Provider '{request.Provider}' is not supported. Supported providers: {string.Join(", ", SupportedProviders)}");
            }

            IConfigurationSection configSection = configuration.GetSection($"Authentication:{provider}");

            if (!configSection.Exists())
            {
                logger.LogWarning("Configuration not found for provider: {Provider}", provider);
                return Error.NotFound("Provider.NotConfigured", $"Provider '{request.Provider}' is not configured");
            }

            string baseUrl = GetBaseUrl();

            try
            {
                ErrorOr<Result> result = provider switch
                {
                    "google" => await GetGoogleConfigAsync(configSection, baseUrl),
                    "facebook" => await GetFacebookConfigAsync(configSection, baseUrl),
                    _ => Error.NotFound("Provider.NotSupported", $"Provider '{request.Provider}' is not supported")
                };

                if (result.IsError)
                {
                    logger.LogWarning("Failed to get config for provider {Provider}: {Errors}",
                        provider, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
                else
                {
                    logger.LogDebug("Successfully retrieved config for provider: {Provider}", provider);
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting OAuth config for provider: {Provider}", provider);
                return Error.Failure("Provider.ConfigError", $"Failed to retrieve configuration for provider '{request.Provider}'");
            }
        }

        private async Task<ErrorOr<Result>> GetGoogleConfigAsync(IConfigurationSection config, string baseUrl)
        {
            await Task.CompletedTask;

            string? clientId = config["ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Error.Validation("Google.ClientId.Missing", "Google ClientId is not configured");
            }

            // Validate client secret exists (don't return it to frontend)
            string? clientSecret = config["ClientSecret"];
            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                return Error.Validation("Google.ClientSecret.Missing", "Google ClientSecret is not configured");
            }

            return new Result
            {
                Provider = "google",
                ProviderName = "Google",
                ClientId = clientId,
                AuthorizationUrl = "https://accounts.google.com/o/oauth2/v2/auth",
                TokenUrl = "https://oauth2.googleapis.com/token",
                Scopes = ["openid", "email", "profile"],
                ResponseType = "code",
                RequiresPKCE = true,
                AdditionalParameters = new Dictionary<string, string>
                {
                    ["access_type"] = "offline",
                    ["prompt"] = "consent",
                    ["include_granted_scopes"] = "true"
                },
                TokenExchangeUrl = $"{baseUrl}{ExternalLoginEndpoint.Route}/token/exchange/google"
            };
        }

        private async Task<ErrorOr<Result>> GetFacebookConfigAsync(IConfigurationSection config, string baseUrl)
        {
            await Task.CompletedTask;

            string? appId = config["AppId"];
            if (string.IsNullOrWhiteSpace(appId))
            {
                return Error.Validation("Facebook.AppId.Missing", "Facebook AppId is not configured");
            }

            // Validate app secret exists (don't return it to frontend)
            string? appSecret = config["AppSecret"];
            if (string.IsNullOrWhiteSpace(appSecret))
            {
                return Error.Validation("Facebook.AppSecret.Missing", "Facebook AppSecret is not configured");
            }

            return new Result
            {
                Provider = "facebook",
                ProviderName = "Facebook",
                ClientId = appId,
                AuthorizationUrl = "https://www.facebook.com/v18.0/dialog/oauth",
                TokenUrl = "https://graph.facebook.com/v18.0/oauth/access_token",
                Scopes = ["email", "public_profile"],
                ResponseType = "code",
                RequiresPKCE = false, // Facebook doesn't require PKCE but supports it
                AdditionalParameters = new Dictionary<string, string>
                {
                    ["display"] = "popup",
                    ["auth_type"] = "rerequest" // Re-request permissions if declined
                },
                TokenExchangeUrl = $"{baseUrl}{ExternalLoginEndpoint.Route}/token/exchange/facebook"
            };
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
                return $"{request.Scheme}://{request.Host}";
            }

            logger.LogWarning("No base URL configured and no HTTP context available. Using localhost fallback.");
            return "https://localhost:5001";
        }
    }
}