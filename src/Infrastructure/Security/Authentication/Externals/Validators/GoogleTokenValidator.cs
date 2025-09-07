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

    public GoogleTokenValidator(
        IOptions<GoogleOption> googleSettings,
        ILogger<GoogleTokenValidator> logger,
        HttpClient httpClient)
    {
        _googleOptions = googleSettings.Value;
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
        if (!provider.Equals("google", StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation("Provider.NotSupported", "This validator only supports Google");
        }

        try
        {
            // If we have an authorization code, exchange it for tokens first
            if (!string.IsNullOrWhiteSpace(authorizationCode))
            {
                var tokenExchangeResult = await ExchangeAuthorizationCodeAsync(authorizationCode, redirectUri, cancellationToken);
                if (tokenExchangeResult.IsError)
                {
                    return tokenExchangeResult.Errors;
                }

                accessToken = tokenExchangeResult.Value.AccessToken;
                idToken = tokenExchangeResult.Value.IdToken;
            }

            // Validate ID token using Google.Apis.Auth SDK (preferred method)
            if (!string.IsNullOrWhiteSpace(idToken))
            {
                return await ValidateIdTokenWithSdkAsync(idToken, cancellationToken);
            }

            // Fallback to access token validation
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                return await ValidateAccessTokenAsync(accessToken, cancellationToken);
            }

            return Error.Validation("Token.Invalid", "No valid token provided");
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
            var expectedClientId = _googleOptions?.ClientId;
            if (string.IsNullOrWhiteSpace(expectedClientId))
            {
                return Error.NotFound("Google.Configuration.Missing", "Google ClientId is not configured");
            }

            // Use Google.Apis.Auth SDK to validate ID token
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings()
                {
                    Audience = new[] { expectedClientId }
                });

            return new ExternalUserInfo
            {
                ProviderId = payload.Subject,
                Email = payload.Email,
                FirstName = payload.GivenName,
                LastName = payload.FamilyName,
                ProfilePictureUrl = payload.Picture,
                EmailVerified = payload.EmailVerified,
                AdditionalClaims = new Dictionary<string, string>
                {
                    ["locale"] = payload.Locale ?? "",
                    ["name"] = payload.Name ?? ""
                }
            };
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning("Invalid Google ID token: {Error}", ex.Message);
            return Error.Unauthorized("Google.IdToken.Invalid", "Invalid Google ID token");
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

        var response = await _httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(tokenRequest),
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Google token exchange failed: {StatusCode} - {Content}",
                response.StatusCode, errorContent);
            return Error.Failure("Google.TokenExchange.Failed", "Failed to exchange authorization code");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenData = JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent);

        if (tokenData == null || string.IsNullOrWhiteSpace(tokenData.AccessToken))
        {
            return Error.Failure("Google.TokenExchange.InvalidResponse", "Invalid token response from Google");
        }

        return (tokenData.AccessToken, tokenData.IdToken);
    }

    private async Task<ErrorOr<ExternalUserInfo>> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(
            $"https://www.googleapis.com/oauth2/v2/userinfo?access_token={accessToken}",
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
            return Error.Unauthorized("Google.AccessToken.InvalidUserInfo", "Invalid user info from access token");
        }

        return new ExternalUserInfo
        {
            ProviderId = userInfo.Id,
            Email = userInfo.Email,
            FirstName = userInfo.GivenName,
            LastName = userInfo.FamilyName,
            ProfilePictureUrl = userInfo.Picture,
            EmailVerified = userInfo.VerifiedEmail,
            AdditionalClaims = new Dictionary<string, string>
            {
                ["locale"] = userInfo.Locale ?? "",
                ["name"] = userInfo.Name ?? ""
            }
        };
    }

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
