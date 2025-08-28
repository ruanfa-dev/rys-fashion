using Core.Identity;

using ErrorOr;

using Microsoft.Extensions.Logging;

using UseCases.Common.Security.Authentication.Tokens.Models;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace Infrastructure.Security.Authentication.Tokens.Services;

public sealed class TokenManagementService : ITokenManagementService
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<TokenManagementService> _logger;

    public TokenManagementService(
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        ILogger<TokenManagementService> logger)
    {
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ErrorOr<AuthenticationResult>> AuthenticateAsync(
        User user,
        string ipAddress,
        bool rememberMe = false,
        bool isSystemUser = false,
        CancellationToken cancellationToken = default)
    {
        if (user == null)
        {
            _logger.LogError("User cannot be null.");
            return Error.Validation("User cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            _logger.LogError("IP address cannot be empty.");
            return Error.Validation("IP address is required.");
        }

        try
        {
            // Generate access token
            var accessTokenResult = await _jwtTokenService.GenerateAccessTokenAsync(user, cancellationToken);
            if (accessTokenResult.IsError)
            {
                _logger.LogError("Failed to generate access token for user {UserId}: {Errors}", user.Id, string.Join(", ", accessTokenResult.Errors));
                return accessTokenResult.Errors;
            }
            // Generate refresh token
            var refreshTokenResult = await _refreshTokenService.GenerateRefreshTokenAsync(
                user.Id,
                ipAddress,
                rememberMe,
                isSystemUser: isSystemUser,
                cancellationToken);
            if (refreshTokenResult.IsError)
            {
                _logger.LogError("Failed to generate refresh token for user {UserId}: {Errors}", user.Id, string.Join(", ", refreshTokenResult.Errors));
                return refreshTokenResult.Errors;
            }

            _logger.LogInformation("Successfully authenticated user {UserId} from IP {IpAddress}", user.Id, ipAddress);

            return new AuthenticationResult
            {
                AccessToken = accessTokenResult.Value.Token,
                RefreshToken = refreshTokenResult.Value.Token,
                AccessTokenExpiresAt = DateTimeOffset.FromUnixTimeSeconds(accessTokenResult.Value.ExpiresAt),
                RefreshTokenExpiresAt = refreshTokenResult.Value.ExpiresAt,
                TokenType = "Bearer"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for user {UserId}", user.Id);
            return Error.Failure($"Authentication failed: {ex.Message}");
        }
    }

    public async Task<ErrorOr<AuthenticationResult>> RefreshAsync(
        string refreshToken,
        string ipAddress,
        bool rememberMe = false,
        bool isSystemUser = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate: refresh token
            var validationResult = await _refreshTokenService.ValidateRefreshTokenAsync(refreshToken, cancellationToken);
            if (validationResult.IsError)
            {
                _logger.LogError("Refresh token validation failed: {Errors}", string.Join(", ", validationResult.Errors));
                return validationResult.Errors;
            }

            // Rotate: refresh token
            var user = validationResult.Value.User;
            var newRefreshTokenResult = await _refreshTokenService.RotateRefreshTokenAsync(refreshToken, ipAddress, rememberMe, isSystemUser, cancellationToken);
            if (newRefreshTokenResult.IsError)
            {
                _logger.LogError("Refresh token rotation failed for user {UserId}: {Errors}", user.Id, string.Join(", ", newRefreshTokenResult.Errors));
                return newRefreshTokenResult.Errors;
            }

            // Generate: new access token
            var accessTokenResult = await _jwtTokenService.GenerateAccessTokenAsync(user, cancellationToken);
            if (accessTokenResult.IsError)
            {
                _logger.LogError("Failed to generate new access token for user {UserId}: {Errors}", user.Id, string.Join(", ", accessTokenResult.Errors));
                return accessTokenResult.Errors;
            }

            _logger.LogInformation("Successfully refreshed tokens for user {UserId} from IP {IpAddress}", user.Id, ipAddress);

            return new AuthenticationResult
            {
                AccessToken = accessTokenResult.Value.Token,
                RefreshToken = newRefreshTokenResult.Value.Token,
                AccessTokenExpiresAt = DateTimeOffset.FromUnixTimeSeconds(accessTokenResult.Value.ExpiresAt),
                RefreshTokenExpiresAt = newRefreshTokenResult.Value.ExpiresAt,
                TokenType = "Bearer"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return Error.Failure($"Token refresh failed: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Success>> LogoutAsync(
        string refreshToken,
        string ipAddress,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogError("Refresh token cannot be empty.");
            return Error.Validation("Refresh token is required.");
        }

        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            _logger.LogError("IP address cannot be empty.");
            return Error.Validation("IP address is required.");
        }

        try
        {
            var result = await _refreshTokenService.RevokeTokenAsync(refreshToken, ipAddress, "User logout", cancellationToken);
            if (result.IsError)
            {
                _logger.LogError("Failed to revoke refresh token: {Errors}", string.Join(", ", result.Errors));
                return result.Errors;
            }

            _logger.LogInformation("Successfully logged out from IP {IpAddress}", ipAddress);
            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed");
            return Error.Failure($"Logout failed: {ex.Message}");
        }
    }

    public async Task<ErrorOr<int>> LogoutFromAllDevicesAsync(
        Guid userId,
        string ipAddress,
        string? currentToken = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogError("User ID cannot be empty.");
            return Error.Validation("User ID is required.");
        }

        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            _logger.LogError("IP address cannot be empty.");
            return Error.Validation("IP address is required.");
        }

        try
        {
            var result = await _refreshTokenService.RevokeAllUserTokensAsync(userId, ipAddress, "Logout from all devices", currentToken, cancellationToken);
            if (result.IsError)
            {
                _logger.LogError("Failed to revoke all tokens for user {UserId}: {Errors}", userId, string.Join(", ", result.Errors));
                return result.Errors;
            }

            _logger.LogInformation("Successfully revoked {Count} tokens for user {UserId} from IP {IpAddress}", result.Value, userId, ipAddress);
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout from all devices failed for user {UserId}", userId);
            return Error.Failure($"Logout from all devices failed: {ex.Message}");
        }
    }
}