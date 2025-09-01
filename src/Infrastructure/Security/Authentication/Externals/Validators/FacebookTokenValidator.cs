using System.Text.Json;
using System.Text.Json.Serialization;

using ErrorOr;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using UseCases.Common.Security.Authentication.Externals;

namespace Infrastructure.Security.Authentication.Externals.Validators;
public sealed class FacebookTokenValidator : IExternalTokenValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FacebookTokenValidator> _logger;
    private readonly HttpClient _httpClient;

    public FacebookTokenValidator(
        IConfiguration configuration,
        ILogger<FacebookTokenValidator> logger,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<ErrorOr<ExternalUserInfo>> ValidateTokenAsync(
        string provider,
        string? accessToken,
        string? idToken,
        string? authorizationCode,
        string? redirectUri,
        CancellationToken cancellationToken = default)
    {
        if (!provider.Equals("facebook", StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation("Provider.NotSupported", "This validator only supports Facebook");
        }

        try
        {
            // If we have an authorization code, exchange it for access token first
            if (!string.IsNullOrWhiteSpace(authorizationCode))
            {
                var tokenExchangeResult = await ExchangeAuthorizationCodeAsync(authorizationCode, redirectUri, cancellationToken);
                if (tokenExchangeResult.IsError)
                {
                    return tokenExchangeResult.Errors;
                }

                accessToken = tokenExchangeResult.Value;
            }

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return Error.Validation("Token.Required", "Access token is required for Facebook validation");
            }

            return await ValidateAccessTokenAsync(accessToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Facebook token");
            return Error.Failure("Token.ValidationError", "Failed to validate Facebook token");
        }
    }

    private async Task<ErrorOr<string>> ExchangeAuthorizationCodeAsync(
        string authorizationCode,
        string? redirectUri,
        CancellationToken cancellationToken)
    {
        var appId = _configuration["Authentication:Facebook:AppId"];
        var appSecret = _configuration["Authentication:Facebook:AppSecret"];

        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(appSecret))
        {
            return Error.NotFound("Facebook.Configuration.Missing", "Facebook OAuth configuration is incomplete");
        }

        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = appId,
            ["client_secret"] = appSecret,
            ["code"] = authorizationCode
        };

        if (!string.IsNullOrWhiteSpace(redirectUri))
        {
            tokenRequest["redirect_uri"] = redirectUri;
        }

        var response = await _httpClient.PostAsync(
            "https://graph.facebook.com/v18.0/oauth/access_token",
            new FormUrlEncodedContent(tokenRequest),
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Facebook token exchange failed: {StatusCode} - {Content}",
                response.StatusCode, errorContent);
            return Error.Failure("Facebook.TokenExchange.Failed", "Failed to exchange authorization code");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenData = JsonSerializer.Deserialize<FacebookTokenResponse>(responseContent);

        if (tokenData == null || string.IsNullOrWhiteSpace(tokenData.AccessToken))
        {
            return Error.Failure("Facebook.TokenExchange.InvalidResponse", "Invalid token response from Facebook");
        }

        return tokenData.AccessToken;
    }

    private async Task<ErrorOr<ExternalUserInfo>> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
        // First, validate the token
        var appId = _configuration["Authentication:Facebook:AppId"];
        var appSecret = _configuration["Authentication:Facebook:AppSecret"];

        var debugResponse = await _httpClient.GetAsync(
            $"https://graph.facebook.com/debug_token?input_token={accessToken}&access_token={appId}|{appSecret}",
            cancellationToken
        );

        if (!debugResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Facebook token debug failed: {StatusCode}", debugResponse.StatusCode);
            return Error.Unauthorized("Facebook.Token.Invalid", "Invalid Facebook token");
        }

        var debugContent = await debugResponse.Content.ReadAsStringAsync(cancellationToken);
        var debugInfo = JsonSerializer.Deserialize<FacebookDebugResponse>(debugContent);

        if (debugInfo?.Data?.IsValid != true || debugInfo.Data.AppId != appId)
        {
            return Error.Unauthorized("Facebook.Token.Invalid", "Facebook token validation failed");
        }

        // Get user information
        var userResponse = await _httpClient.GetAsync(
            $"https://graph.facebook.com/me?fields=id,email,first_name,last_name,name,picture&access_token={accessToken}",
            cancellationToken
        );

        if (!userResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Facebook user info request failed: {StatusCode}", userResponse.StatusCode);
            return Error.Failure("Facebook.UserInfo.Failed", "Failed to get user information from Facebook");
        }

        var userContent = await userResponse.Content.ReadAsStringAsync(cancellationToken);
        var userInfo = JsonSerializer.Deserialize<FacebookUserInfo>(userContent);

        if (userInfo == null || string.IsNullOrWhiteSpace(userInfo.Id))
        {
            return Error.Failure("Facebook.UserInfo.Invalid", "Invalid user information from Facebook");
        }

        return new ExternalUserInfo
        {
            ProviderId = userInfo.Id,
            Email = userInfo.Email ?? $"{userInfo.Id}@facebook.local", // Facebook might not always provide email
            FirstName = userInfo.FirstName,
            LastName = userInfo.LastName,
            ProfilePictureUrl = userInfo.Picture?.Data?.Url,
            EmailVerified = !string.IsNullOrWhiteSpace(userInfo.Email), // Facebook emails are generally verified
            AdditionalClaims = new Dictionary<string, string>
            {
                ["name"] = userInfo.Name ?? ""
            }
        };
    }
    // Facebook API response models
    private sealed record FacebookTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = null!;

        [JsonPropertyName("token_type")]
        public string TokenType { get; init; } = null!;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }

    private sealed record FacebookDebugResponse
    {
        [JsonPropertyName("data")]
        public FacebookDebugData? Data { get; init; }
    }

    private sealed record FacebookDebugData
    {
        [JsonPropertyName("app_id")]
        public string AppId { get; init; } = null!;

        [JsonPropertyName("is_valid")]
        public bool IsValid { get; init; }

        [JsonPropertyName("user_id")]
        public string UserId { get; init; } = null!;
    }

    private sealed record FacebookUserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = null!;

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; init; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("picture")]
        public FacebookPicture? Picture { get; init; }
    }

    private sealed record FacebookPicture
    {
        [JsonPropertyName("data")]
        public FacebookPictureData? Data { get; init; }
    }

    private sealed record FacebookPictureData
    {
        [JsonPropertyName("url")]
        public string? Url { get; init; }
    }
}