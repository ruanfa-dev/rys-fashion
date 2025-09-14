using System.Text.Json;
using System.Text.Json.Serialization;

using ErrorOr;

using Google.Apis.Auth;

using Infrastructure.Security.Authentication.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using UseCases.Common.Security.Authentication.Externals;

namespace Infrastructure.Security.Authentication.Externals.Validators;

public sealed class GoogleTokenValidator : IExternalTokenValidator
{
    private readonly GoogleOption? _googleOptions;
    private readonly ILogger<GoogleTokenValidator> _logger;
    private readonly HttpClient _httpClient;

    // Cache for validation settings to avoid recreating
    private readonly Lazy<GoogleJsonWebSignature.ValidationSettings?> _validationSettings;

    public GoogleTokenValidator(
        IOptions<GoogleOption> googleSettings,
        ILogger<GoogleTokenValidator> logger,
        HttpClient httpClient)
    {
        _googleOptions = googleSettings.Value;
        _logger = logger;
        _httpClient = httpClient;
        
        _validationSettings = new Lazy<GoogleJsonWebSignature.ValidationSettings?>(() =>
        {
            if (string.IsNullOrWhiteSpace(_googleOptions?.ClientId))
                return null;
                
            return new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new[] { _googleOptions.ClientId }
            };
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
        if (!provider.Equals("google", StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation("Provider.NotSupported", "This validator only supports Google");
        }

        // Validate configuration
        if (_googleOptions == null)
        {
            _logger.LogError("Google configuration is not available");
            return Error.NotFound("Google.Configuration.Missing", "Google OAuth configuration is not available");
        }

        try
        {
            // If we have an authorization code, exchange it for tokens first
            if (!string.IsNullOrWhiteSpace(authorizationCode))
            {
                _logger.LogDebug("Exchanging Google authorization code for tokens");
                var tokenExchangeResult = await ExchangeAuthorizationCodeAsync(authorizationCode, redirectUri, cancellationToken);
                if (tokenExchangeResult.IsError)
                {
                    return tokenExchangeResult.Errors;
                }

                accessToken = tokenExchangeResult.Value.AccessToken;
                idToken = tokenExchangeResult.Value.IdToken;
            }

            // Prefer ID token validation (more secure and reliable)
            if (!string.IsNullOrWhiteSpace(idToken))
            {
                _logger.LogDebug("Validating Google ID token");
                return await ValidateIdTokenWithSdkAsync(idToken, cancellationToken);
            }

            // Fallback to access token validation
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogDebug("Validating Google access token");
                return await ValidateAccessTokenAsync(accessToken, cancellationToken);
            }

            return Error.Validation("Token.Invalid", "No valid token provided for Google authentication");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Google token validation was cancelled");
            return Error.Failure("Token.ValidationCancelled", "Google token validation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Google token");
            return Error.Failure("Token.ValidationError", "Failed to validate Google token");
        }
    }

    private async Task<ErrorOr<ExternalUserInfo>> ValidateIdTokenWithSdkAsync(string idToken, CancellationToken cancellationToken)
    {
        try
        {
            var validationSettings = _validationSettings.Value;
            if (validationSettings == null)
            {
                return Error.NotFound("Google.Configuration.Missing", "Google ClientId is not configured");
            }

            // Use Google.Apis.Auth SDK to validate ID token with proper signature verification
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);

            // Additional security validations
            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                _logger.LogWarning("Google ID token does not contain email claim");
                return Error.Validation("Google.IdToken.MissingEmail", "Email is required for authentication");
            }

            // Ensure email is verified for security
            if (!payload.EmailVerified)
            {
                _logger.LogWarning("Google account email is not verified: {Email}", payload.Email);
                return Error.Unauthorized("Google.Email.NotVerified", "Email must be verified to authenticate");
            }

            _logger.LogDebug("Successfully validated Google ID token for user: {Email}", payload.Email);

            return new ExternalUserInfo
            {
                ProviderId = payload.Subject,
                Email = payload.Email,
                FirstName = payload.GivenName ?? "",
                LastName = payload.FamilyName ?? "",
                ProfilePictureUrl = payload.Picture,
                EmailVerified = payload.EmailVerified,
                AdditionalClaims = new Dictionary<string, string>
                {
                    ["locale"] = payload.Locale ?? "",
                    ["name"] = payload.Name ?? "",
                    ["iss"] = payload.Issuer ?? "",
                    ["aud"] = payload.Audience?.ToString() ?? ""
                }
            };
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning("Invalid Google ID token: {Error}", ex.Message);
            return Error.Unauthorized("Google.IdToken.Invalid", "Invalid Google ID token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Google ID token");
            return Error.Failure("Google.IdToken.ParseError", "Failed to parse Google ID token");
        }
    }

    private async Task<ErrorOr<(string AccessToken, string? IdToken)>> ExchangeAuthorizationCodeAsync(
        string authorizationCode,
        string? redirectUri,
        CancellationToken cancellationToken)
    {
        var clientId = _googleOptions?.ClientId;
        var clientSecret = _googleOptions?.ClientSecret;

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return Error.NotFound("Google.Configuration.Missing", "Google OAuth configuration is incomplete");
        }

        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = authorizationCode
        };

        if (!string.IsNullOrWhiteSpace(redirectUri))
        {
            tokenRequest["redirect_uri"] = redirectUri;
        }

        try
        {
            var response = await _httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(tokenRequest),
                cancellationToken
            );

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google token exchange failed: {StatusCode} - {Content}",
                    response.StatusCode, responseContent);
                return Error.Failure("Google.TokenExchange.Failed", "Failed to exchange authorization code with Google");
            }

            var tokenData = JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent);

            if (tokenData == null || string.IsNullOrWhiteSpace(tokenData.AccessToken))
            {
                _logger.LogError("Invalid token response from Google: {Content}", responseContent);
                return Error.Failure("Google.TokenExchange.InvalidResponse", "Invalid token response from Google");
            }

            return (tokenData.AccessToken, tokenData.IdToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during Google token exchange");
            return Error.Failure("Google.TokenExchange.NetworkError", "Network error during token exchange");
        }
    }

    private async Task<ErrorOr<ExternalUserInfo>> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"https://www.googleapis.com/oauth2/v2/userinfo?access_token={Uri.EscapeDataString(accessToken)}",
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google access token validation failed: {StatusCode}", response.StatusCode);
                return Error.Unauthorized("Google.AccessToken.Invalid", "Invalid Google access token");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(responseContent);

            if (userInfo == null || string.IsNullOrWhiteSpace(userInfo.Email))
            {
                _logger.LogError("Invalid user info from Google access token: {Content}", responseContent);
                return Error.Unauthorized("Google.AccessToken.InvalidUserInfo", "Invalid user info from Google access token");
            }

            // Require verified email for security
            if (!userInfo.VerifiedEmail)
            {
                _logger.LogWarning("Google account email is not verified: {Email}", userInfo.Email);
                return Error.Unauthorized("Google.Email.NotVerified", "Email must be verified to authenticate");
            }

            _logger.LogDebug("Successfully validated Google access token for user: {Email}", userInfo.Email);

            return new ExternalUserInfo
            {
                ProviderId = userInfo.Id,
                Email = userInfo.Email,
                FirstName = userInfo.GivenName ?? "",
                LastName = userInfo.FamilyName ?? "",
                ProfilePictureUrl = userInfo.Picture,
                EmailVerified = userInfo.VerifiedEmail,
                AdditionalClaims = new Dictionary<string, string>
                {
                    ["locale"] = userInfo.Locale ?? "",
                    ["name"] = userInfo.Name ?? ""
                }
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during Google access token validation");
            return Error.Failure("Google.AccessToken.NetworkError", "Network error during token validation");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing Google user info response");
            return Error.Failure("Google.UserInfo.ParseError", "Error parsing user information");
        }
    }

    // Google API response models with proper validation
    private sealed record GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = null!;

        [JsonPropertyName("id_token")]
        public string? IdToken { get; init; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; init; } = null!;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; init; }

        [JsonPropertyName("scope")]
        public string? Scope { get; init; }
    }

    private sealed record GoogleUserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = null!;

        [JsonPropertyName("email")]
        public string Email { get; init; } = null!;

        [JsonPropertyName("verified_email")]
        public bool VerifiedEmail { get; init; }

        [JsonPropertyName("given_name")]
        public string? GivenName { get; init; }

        [JsonPropertyName("family_name")]
        public string? FamilyName { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("picture")]
        public string? Picture { get; init; }

        [JsonPropertyName("locale")]
        public string? Locale { get; init; }
    }
}
