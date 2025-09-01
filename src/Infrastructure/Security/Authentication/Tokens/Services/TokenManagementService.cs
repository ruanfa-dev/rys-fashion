using Core.Identity;

using ErrorOr;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using UseCases.Common.Persistence.Context;
using UseCases.Common.Security.Authentication.Tokens.Models;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace Infrastructure.Security.Authentication.Tokens.Services;

public sealed class TokenManagementService : ITokenManagementService
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<TokenManagementService> _logger;
    private readonly UserManager<User> _userManager;
    private readonly IUnitOfWork _unitOfWork;

    public TokenManagementService(
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        UserManager<User> userManager,
        IUnitOfWork unitOfWork,
        ILogger<TokenManagementService> logger)
    {
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ErrorOr<AuthenticationResult>> AuthenticateAsync(
        User user,
        string ipAddress,
        bool rememberMe = false,
        CancellationToken cancellationToken = default)
    {
        if (user is null || user.Id == Guid.Empty)
            return Error.Validation("Authentication.InvalidUser", "Valid user is required");
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Error.Validation("Authentication.InvalidIpAddress", "IP address is required");

        await using var tx = await _unitOfWork.Context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var security = await ValidateUserSecurityStatusAsync(user, cancellationToken);
            if (security.IsError)
                return security.Errors;

            var accessRes = await _jwtTokenService.GenerateAccessTokenAsync(user, cancellationToken);
            if (accessRes.IsError)
                return accessRes.Errors;

            var refreshRes = await _refreshTokenService.GenerateRefreshTokenAsync(
                user.Id, ipAddress, rememberMe, cancellationToken);

            if (refreshRes.IsError)
                return refreshRes.Errors;

            await UpdateUserLoginTrackingAsync(user, ipAddress, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return new AuthenticationResult
            {
                AccessToken = accessRes.Value.Token,
                AccessTokenExpiresAt = DateTimeOffset.FromUnixTimeSeconds(accessRes.Value.ExpiresAt),
                RefreshToken = refreshRes.Value.Token,
                RefreshTokenExpiresAt = refreshRes.Value.ExpiresAt,
                TokenType = "Bearer"
            };
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Authenticate failed for user {UserId}", user.Id);
            return Error.Failure("Authentication.Failed", "Authentication failed due to an unexpected error");
        }
    }

    public async Task<ErrorOr<AuthenticationResult>> RefreshAsync(
        string refreshToken,
        string ipAddress,
        bool rememberMe = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Error.Validation("Refresh.InvalidToken", "Refresh token is required");
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Error.Validation("Refresh.InvalidIpAddress", "IP address is required");

        try
        {
            // Validate and load user
            var validated = await _refreshTokenService.ValidateRefreshTokenAsync(refreshToken, cancellationToken);
            if (validated.IsError)
                return validated.Errors;

            var (token, user) = (validated.Value.RefreshToken, validated.Value.User);

            // User still allowed?
            var security = await ValidateUserSecurityStatusAsync(user, cancellationToken);
            if (security.IsError)
            {
                // Revoke the presented token for safety
                await _refreshTokenService.RevokeTokenAsync(refreshToken, ipAddress, "User security status changed", cancellationToken);
                return security.Errors;
            }

            // Rotate (one-time use)
            var rotated = await _refreshTokenService.RotateRefreshTokenAsync(refreshToken, ipAddress, rememberMe, cancellationToken);
            if (rotated.IsError)
                return rotated.Errors;

            // New access token
            var accessRes = await _jwtTokenService.GenerateAccessTokenAsync(user, cancellationToken);
            if (accessRes.IsError)
                return accessRes.Errors;

            return new AuthenticationResult
            {
                AccessToken = accessRes.Value.Token,
                AccessTokenExpiresAt = DateTimeOffset.FromUnixTimeSeconds(accessRes.Value.ExpiresAt),
                RefreshToken = rotated.Value.Token,
                RefreshTokenExpiresAt = rotated.Value.ExpiresAt,
                TokenType = "Bearer"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh failed from IP {Ip}", ipAddress);
            return Error.Failure("Refresh.Failed", "Token refresh failed due to an unexpected error");
        }
    }

    public async Task<ErrorOr<Deleted>> LogoutAsync(
        string refreshToken,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Error.Validation("Logout.InvalidToken", "Refresh token is required");
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Error.Validation("Logout.InvalidIpAddress", "IP address is required");

        try
        {
            var res = await _refreshTokenService.RevokeTokenAsync(refreshToken, ipAddress, "User logout", cancellationToken);
            if (res.IsError)
                return res.Errors;

            return Result.Deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed from IP {Ip}", ipAddress);
            return Error.Failure("Logout.Failed", "Logout failed due to an unexpected error");
        }
    }

    public async Task<ErrorOr<int>> LogoutFromAllDevicesAsync(
        Guid userId,
        string ipAddress,
        string? currentToken = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return Error.Validation("LogoutAll.InvalidUserId", "Valid user ID is required");
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Error.Validation("LogoutAll.InvalidIpAddress", "IP address is required");

        try
        {
            var res = await _refreshTokenService.RevokeAllUserTokensAsync(
                userId, ipAddress, "Logout from all devices", currentToken, cancellationToken);

            if (res.IsError)
                return res.Errors;

            return res.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LogoutAll failed for user {UserId}", userId);
            return Error.Failure("LogoutAll.Failed", "Logout from all devices failed due to an unexpected error");
        }
    }

    // ---- private helpers ---------------------------------------------------

    private async Task<ErrorOr<Success>> ValidateUserSecurityStatusAsync(User user, CancellationToken ct)
    {
        try
        {
            if (await _userManager.IsLockedOutAsync(user))
                return Error.Validation("Authentication.UserLocked", "User account is locked");

            if (!user.EmailConfirmed)
                return Error.Validation("Authentication.EmailNotConfirmed", "Email address must be confirmed");

            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Security status validation failed for {UserId}", user.Id);
            return Error.Failure("Authentication.SecurityCheckFailed", "Security validation failed");
        }
    }

    private async Task UpdateUserLoginTrackingAsync(User user, string ipAddress, CancellationToken ct)
    {
        try
        {
            user.LastSignInAt = DateTimeOffset.UtcNow;
            user.LastSignInIp = ipAddress;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                _logger.LogWarning("User login tracking failed for {UserId}: {Errors}",
                    user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception updating user login tracking for {UserId}", user.Id);
        }
    }
}
