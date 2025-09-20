using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using UseCases.Common.Security.Authentication.Models;
using UseCases.Common.Security.Authentication.Services;

namespace UseCases.Accounts.Authentication.External.ExchangeToken;

/// <summary>
/// Handler for exchanging external OAuth tokens with Keycloak
/// </summary>
public sealed class ExchangeExternalTokenHandler : IRequestHandler<ExchangeExternalTokenCommand, ErrorOr<ExternalAuthResponse>>
{
    private readonly IKeycloakAdminService _keycloakService;
    private readonly ILogger<ExchangeExternalTokenHandler> _logger;

    private static readonly HashSet<string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "google",
        "facebook"
    };

    public ExchangeExternalTokenHandler(
        IKeycloakAdminService keycloakService,
        ILogger<ExchangeExternalTokenHandler> logger)
    {
        _keycloakService = keycloakService;
        _logger = logger;
    }

    public async Task<ErrorOr<ExternalAuthResponse>> Handle(ExchangeExternalTokenCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting external token exchange for provider: {Provider}", request.Provider);

        // Validate provider
        if (!SupportedProviders.Contains(request.Provider))
        {
            _logger.LogWarning("Unsupported OAuth provider: {Provider}", request.Provider);
            return Error.Validation("ExternalAuth.UnsupportedProvider", $"Provider '{request.Provider}' is not supported");
        }

        // Validate access token
        if (string.IsNullOrWhiteSpace(request.AccessToken))
        {
            return Error.Validation("ExternalAuth.MissingToken", "Access token is required");
        }

        try
        {
            // Handle account linking if requested
            if (request.LinkToExistingUser && !string.IsNullOrWhiteSpace(request.ExistingUserId))
            {
                var linkResult = await HandleAccountLinking(request, cancellationToken);
                if (linkResult.IsError)
                    return linkResult.Errors;
            }

            // Exchange external token for Keycloak token
            var exchangeRequest = new ExternalTokenExchangeRequest
            {
                ExternalToken = request.AccessToken,
                Provider = request.Provider.ToLowerInvariant(),
                Scope = "openid profile email"
            };

            var tokenResponse = await _keycloakService.ExchangeExternalTokenAsync(exchangeRequest, cancellationToken);

            // Get user info from the new Keycloak token
            var userInfo = await _keycloakService.GetUserInfoAsync(tokenResponse.AccessToken, cancellationToken);

            _logger.LogInformation("Successfully exchanged {Provider} token for user: {Email}", request.Provider, userInfo.Email);

            return new ExternalAuthResponse
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken,
                TokenType = tokenResponse.TokenType,
                ExpiresIn = tokenResponse.ExpiresIn,
                UserInfo = userInfo,
                IsNewUser = DetermineIfNewUser(userInfo),
                IsLinkedAccount = request.LinkToExistingUser
            };
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Token exchange failed"))
        {
            _logger.LogError(ex, "Token exchange failed for provider: {Provider}", request.Provider);
            
            // Check if it's a new user that needs to go through Keycloak's first login flow
            if (ex.Message.Contains("404") || ex.Message.Contains("not found"))
            {
                return Error.NotFound("ExternalAuth.UserNotFound", 
                    "User not found in Keycloak. Please complete the initial login flow through Keycloak first.");
            }
            
            return Error.Failure("ExternalAuth.TokenExchangeFailed", "Failed to exchange external token");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during external token exchange for provider: {Provider}", request.Provider);
            return Error.Unexpected("ExternalAuth.UnexpectedError", "An unexpected error occurred during authentication");
        }
    }

    private async Task<ErrorOr<Success>> HandleAccountLinking(ExchangeExternalTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _keycloakService.LinkExternalAccountAsync(
                request.ExistingUserId!,
                request.Provider.ToLowerInvariant(),
                request.AccessToken,
                cancellationToken);

            _logger.LogInformation("Successfully linked {Provider} account to user: {UserId}", 
                request.Provider, request.ExistingUserId);

            return Result.Success;
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("409"))
        {
            _logger.LogWarning("Account already linked for user: {UserId}, provider: {Provider}", 
                request.ExistingUserId, request.Provider);
            return Error.Conflict("ExternalAuth.AccountAlreadyLinked", 
                "This external account is already linked to a user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to link external account for user: {UserId}, provider: {Provider}", 
                request.ExistingUserId, request.Provider);
            return Error.Failure("ExternalAuth.LinkingFailed", "Failed to link external account");
        }
    }

    private static bool DetermineIfNewUser(UserInfo userInfo)
    {
        // Simple heuristic: if the user has no roles assigned, they might be new
        // In practice, you might want to track this information differently
        return userInfo.RealmAccess?.Roles?.Count == 0;
    }
}