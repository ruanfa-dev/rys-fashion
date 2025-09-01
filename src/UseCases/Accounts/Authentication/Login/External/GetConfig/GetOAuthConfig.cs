using ErrorOr;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Accounts.Authentication.Login.External.GetConfig;
public static partial class GetOAuthConfig
{
    public const string Name = "GetOAuthConfig";
    public const string Summary = "Get OAuth configuration for frontend";
    public const string Description = "Returns OAuth configuration needed for frontend applications to handle OAuth flow";

    public sealed record Query(string Provider) : IQuery<Result>;

    public sealed record Result
    {
        public string Provider { get; init; } = null!;
        public string ClientId { get; init; } = null!;
        public string AuthorizationUrl { get; init; } = null!;
        public string TokenUrl { get; init; } = null!;
        public string[] Scopes { get; init; } = Array.Empty<string>();
        public string ResponseType { get; init; } = "code";
        public Dictionary<string, string> AdditionalParameters { get; init; } = new();
        public string TokenExchangeUrl { get; init; } = null!;
    }

    public sealed class Handler(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor
    ) : IQueryHandler<Query, Result>
    {
        public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
        {
            var provider = request.Provider.ToLowerInvariant();
            var configSection = configuration.GetSection($"Authentication:{provider}");

            if (!configSection.Exists())
            {
                return Error.NotFound("Provider.NotConfigured", $"Provider '{request.Provider}' is not configured");
            }

            var baseUrl = GetBaseUrl();

            var result = provider switch
            {
                "google" => await GetGoogleConfigAsync(configSection, baseUrl),
                "facebook" => await GetFacebookConfigAsync(configSection, baseUrl),
                _ => Error.NotFound("Provider.NotSupported", $"Provider '{request.Provider}' is not supported")
            };

            return result;
        }

        private async Task<ErrorOr<Result>> GetGoogleConfigAsync(IConfigurationSection config, string baseUrl)
        {
            await Task.CompletedTask;
            var clientId = config["ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Error.Validation("Google.ClientId.Missing", "Google ClientId is not configured");
            }

            return new Result
            {
                Provider = "google",
                ClientId = clientId,
                AuthorizationUrl = "https://accounts.google.com/o/oauth2/v2/auth",
                TokenUrl = "https://oauth2.googleapis.com/token",
                Scopes = new[] { "openid", "email", "profile" },
                ResponseType = "code",
                AdditionalParameters = new Dictionary<string, string>
                {
                    ["access_type"] = "offline",
                    ["prompt"] = "consent"
                },
                TokenExchangeUrl = $"{baseUrl}/{ExternalLoginEndpoint.Route}/token/exchange/facebook"
            };
        }

        private async Task<ErrorOr<Result>> GetFacebookConfigAsync(IConfigurationSection config, string baseUrl)
        {
            await Task.CompletedTask;
            var appId = config["AppId"];
            if (string.IsNullOrWhiteSpace(appId))
            {
                return Error.Validation("Facebook.AppId.Missing", "Facebook AppId is not configured");
            }

            return new Result
            {
                Provider = "facebook",
                ClientId = appId,
                AuthorizationUrl = "https://www.facebook.com/v18.0/dialog/oauth",
                TokenUrl = "https://graph.facebook.com/v18.0/oauth/access_token",
                Scopes = new[] { "email", "public_profile" },
                ResponseType = "code",
                TokenExchangeUrl = $"{baseUrl}/{ExternalLoginEndpoint.Route}/token/exchange/facebook"
            };
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
                return $"{request.Scheme}://{request.Host}";
            }

            return "https://localhost:5001";
        }
    }
}