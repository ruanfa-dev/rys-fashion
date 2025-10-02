using System.Text.Json;
using System.Text.Json.Serialization;

using ErrorOr;

using Infrastructure.Security.Authentication.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using UseCases.Common.Security.Authentication.Externals;

namespace Infrastructure.Security.Authentication.Externals.Validators;

public sealed class FacebookTokenValidator : IExternalTokenValidator
{
    private readonly FacebookOption? _facebookOptions;
    private readonly ILogger<FacebookTokenValidator> _logger;
    private readonly HttpClient _httpClient;

    // Cache for app access token to avoid regenerating it frequently
    private readonly Lazy<string?> _appAccessToken;

    public FacebookTokenValidator(
        IOptions<FacebookOption> facebookSettings,
        ILogger<FacebookTokenValidator> logger,
        HttpClient httpClient)
    {
        _facebookOptions = facebookSettings.Value;
        _logger = logger;
        _httpClient = httpClient;
        
        _appAccessToken = new Lazy<string?>(() =>
        {
            if (string.IsNullOrWhiteSpace(_facebookOptions?.AppId) || 
                string.IsNullOrWhiteSpace(_facebookOptions?.AppSecret))
            {
                return null;
            }
            return $"{_facebookOptions.AppId}|{_facebookOptions.AppSecret}";
        });
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

        // Validate configuration
        if (_facebookOptions == null || _appAccessToken.Value == null)
        {
            _logger.LogError("Facebook configuration is not available or incomplete");
            return Error.NotFound("Facebook.Configuration.Missing", "Facebook OAuth configuration is not available or incomplete");
        }

        try
        {
            // If we have an authorization code, exchange it for access token first
            if (!string.IsNullOrWhiteSpace(authorizationCode))
            {
                _logger.LogDebug("Exchanging Facebook authorization code for access token");
                ErrorOr<string> tokenExchangeResult = await ExchangeAuthorizationCodeAsync(authorizationCode, redirectUri, cancellationToken);
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

            // Validate and get user information in one step for better performance
            return await ValidateTokenAndGetUserInfoAsync(accessToken, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Facebook token validation was cancelled");
            return Error.Failure("Token.ValidationCancelled", "Facebook token validation was cancelled");
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
        string? appId = _facebookOptions?.AppId;
        string? appSecret = _facebookOptions?.AppSecret;

        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(appSecret))
        {
            return Error.NotFound("Facebook.Configuration.Missing", "Facebook OAuth configuration is incomplete");
        }

        Dictionary<string, string> tokenRequest = new Dictionary<string, string>
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

        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(
                "https://graph.facebook.com/v18.0/oauth/access_token",
                new FormUrlEncodedContent(tokenRequest),
                cancellationToken
            );

            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Facebook token exchange failed: {StatusCode} - {Content}",
                    response.StatusCode, responseContent);
                return Error.Failure("Facebook.TokenExchange.Failed", "Failed to exchange authorization code with Facebook");
            }

            FacebookTokenResponse? tokenData = JsonSerializer.Deserialize<FacebookTokenResponse>(responseContent);

            if (tokenData == null || string.IsNullOrWhiteSpace(tokenData.AccessToken))
            {
                _logger.LogError("Invalid token response from Facebook: {Content}", responseContent);
                return Error.Failure("Facebook.TokenExchange.InvalidResponse", "Invalid token response from Facebook");
            }

            _logger.LogDebug("Successfully exchanged Facebook authorization code for access token");
            return tokenData.AccessToken;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during Facebook token exchange");
            return Error.Failure("Facebook.TokenExchange.NetworkError", "Network error during token exchange");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing Facebook token exchange response");
            return Error.Failure("Facebook.TokenExchange.ParseError", "Error parsing token exchange response");
        }
    }

    private async Task<ErrorOr<ExternalUserInfo>> ValidateTokenAndGetUserInfoAsync(
        string accessToken, 
        CancellationToken cancellationToken)
    {
        string appAccessToken = _appAccessToken.Value!;

        try
        {
            // First, validate the token using Facebook's debug endpoint for security
            HttpResponseMessage debugResponse = await _httpClient.GetAsync(
                $"https://graph.facebook.com/debug_token?input_token={Uri.EscapeDataString(accessToken)}&access_token={Uri.EscapeDataString(appAccessToken)}",
                cancellationToken
            );

            if (!debugResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Facebook token debug failed: {StatusCode}", debugResponse.StatusCode);
                return Error.Unauthorized("Facebook.Token.Invalid", "Invalid Facebook token");
            }

            string debugContent = await debugResponse.Content.ReadAsStringAsync(cancellationToken);
            FacebookDebugResponse? debugInfo = JsonSerializer.Deserialize<FacebookDebugResponse>(debugContent);

            // Validate token debug response
            ErrorOr<Success> validationResult = ValidateTokenDebugInfo(debugInfo);
            if (validationResult.IsError)
            {
                return validationResult.Errors;
            }

            // Token is valid, now get user information
            return await GetUserInformationAsync(accessToken, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during Facebook token validation");
            return Error.Failure("Facebook.Token.NetworkError", "Network error during token validation");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing Facebook debug response");
            return Error.Failure("Facebook.Debug.ParseError", "Error parsing Facebook debug response");
        }
    }

    private ErrorOr<Success> ValidateTokenDebugInfo(FacebookDebugResponse? debugInfo)
    {
        if (debugInfo?.Data == null)
        {
            _logger.LogWarning("Facebook token debug response is null or invalid");
            return Error.Unauthorized("Facebook.Token.Invalid", "Invalid Facebook token debug response");
        }

        if (!debugInfo.Data.IsValid)
        {
            _logger.LogWarning("Facebook token validation failed: token is not valid");
            return Error.Unauthorized("Facebook.Token.Invalid", "Facebook token validation failed");
        }

        if (debugInfo.Data.AppId != _facebookOptions?.AppId)
        {
            _logger.LogWarning("Facebook token app ID mismatch: expected {ExpectedAppId}, got {ActualAppId}", 
                _facebookOptions?.AppId, debugInfo.Data.AppId);
            return Error.Unauthorized("Facebook.Token.AppIdMismatch", "Facebook token app ID mismatch");
        }

        // Check token expiration if available
        if (debugInfo.Data.ExpiresAt.HasValue)
        {
            DateTimeOffset expiresAt = DateTimeOffset.FromUnixTimeSeconds(debugInfo.Data.ExpiresAt.Value);
            if (expiresAt <= DateTimeOffset.UtcNow.AddMinutes(1)) // 1 minute buffer
            {
                _logger.LogWarning("Facebook token has expired or expires very soon");
                return Error.Unauthorized("Facebook.Token.Expired", "Facebook token has expired");
            }
        }

        // Validate required scopes for e-commerce
        if (debugInfo.Data.Scopes != null)
        {
            string[] requiredScopes = ["email", "public_profile"];
            bool hasRequiredScopes = requiredScopes.All(scope => 
                debugInfo.Data.Scopes.Contains(scope, StringComparer.OrdinalIgnoreCase));

            if (!hasRequiredScopes)
            {
                _logger.LogWarning("Facebook token missing required scopes. Required: {RequiredScopes}, Got: {ActualScopes}",
                    string.Join(", ", requiredScopes), string.Join(", ", debugInfo.Data.Scopes));
                return Error.Forbidden("Facebook.Token.InsufficientScopes", 
                    "Facebook token does not have required permissions for authentication");
            }
        }

        return Result.Success;
    }

    private async Task<ErrorOr<ExternalUserInfo>> GetUserInformationAsync(
        string accessToken, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Get user information with essential fields only for e-commerce
            HttpResponseMessage userResponse = await _httpClient.GetAsync(
                $"https://graph.facebook.com/v18.0/me?fields=id,email,first_name,last_name,name,picture.width(200).height(200),verified,locale&access_token={Uri.EscapeDataString(accessToken)}",
                cancellationToken
            );

            if (!userResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Facebook user info request failed: {StatusCode}", userResponse.StatusCode);
                return Error.Failure("Facebook.UserInfo.Failed", "Failed to get user information from Facebook");
            }

            string userContent = await userResponse.Content.ReadAsStringAsync(cancellationToken);
            FacebookUserInfo? userInfo = JsonSerializer.Deserialize<FacebookUserInfo>(userContent);

            if (userInfo == null || string.IsNullOrWhiteSpace(userInfo.Id))
            {
                _logger.LogError("Invalid user information from Facebook: {Content}", userContent);
                return Error.Failure("Facebook.UserInfo.Invalid", "Invalid user information from Facebook");
            }

            // Handle email requirements for e-commerce
            string? email = userInfo.Email;
            bool emailVerified = !string.IsNullOrWhiteSpace(email);
            
            if (string.IsNullOrWhiteSpace(email))
            {
                // For e-commerce, we might want to require email
                // But Facebook doesn't always provide it, so we'll use a placeholder
                email = $"fb_{userInfo.Id}@facebook.local";
                emailVerified = false;
                _logger.LogInformation("Facebook user {UserId} does not have email, using placeholder", userInfo.Id);
            }

            // Additional validation for e-commerce security
            if (userInfo.Verified == false)
            {
                _logger.LogWarning("Facebook user {UserId} account is not verified", userInfo.Id);
                // For production e-commerce, you might want to return an error here
                // return Error.Forbidden("Facebook.Account.NotVerified", "Facebook account is not verified");
            }

            _logger.LogDebug("Successfully retrieved Facebook user information for: {Email}", email);

            return new ExternalUserInfo
            {
                ProviderId = userInfo.Id,
                Email = email,
                FirstName = NormalizeDisplayName(userInfo.FirstName),
                LastName = NormalizeDisplayName(userInfo.LastName),
                ProfilePictureUrl = userInfo.Picture?.Data?.Url,
                EmailVerified = emailVerified,
                AdditionalClaims = new Dictionary<string, string>
                {
                    ["name"] = NormalizeDisplayName(userInfo.Name) ?? "",
                    ["has_email"] = emailVerified.ToString().ToLowerInvariant(),
                    ["verified"] = (userInfo.Verified ?? false).ToString().ToLowerInvariant(),
                    ["locale"] = userInfo.Locale ?? "en_US",
                    ["provider_user_id"] = userInfo.Id,
                    ["provider"] = "facebook"
                }
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during Facebook user info retrieval");
            return Error.Failure("Facebook.UserInfo.NetworkError", "Network error during user info retrieval");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing Facebook user info response");
            return Error.Failure("Facebook.UserInfo.ParseError", "Error parsing Facebook user info response");
        }
    }

    /// <summary>
    /// Normalizes display names to prevent XSS and ensure clean data
    /// </summary>
    private static string? NormalizeDisplayName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        // Basic sanitization for display names
        return name.Trim()
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#x27;");
    }

    // Facebook API response models with proper validation and security considerations
    private sealed record FacebookTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = null!;

        [JsonPropertyName("token_type")]
        public string TokenType { get; init; } = null!;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }

        [JsonPropertyName("scope")]
        public string? Scope { get; init; }
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

        [JsonPropertyName("expires_at")]
        public long? ExpiresAt { get; init; }

        [JsonPropertyName("scopes")]
        public string[]? Scopes { get; init; }

        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("application")]
        public string? Application { get; init; }
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

        [JsonPropertyName("verified")]
        public bool? Verified { get; init; }

        [JsonPropertyName("locale")]
        public string? Locale { get; init; }
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

        [JsonPropertyName("width")]
        public int Width { get; init; }

        [JsonPropertyName("height")]
        public int Height { get; init; }

        [JsonPropertyName("is_silhouette")]
        public bool IsSilhouette { get; init; }
    }
}