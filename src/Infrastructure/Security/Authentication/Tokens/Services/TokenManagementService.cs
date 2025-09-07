using Core.Identity;

using ErrorOr;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    private readonly RoleManager<Role> _roleManager;
    private readonly IUnitOfWork _unitOfWork;

    public TokenManagementService(
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IUnitOfWork unitOfWork,
        ILogger<TokenManagementService> logger)
    {
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ErrorOr<AuthenticationResult>> AuthenticateAsync(
        User user,
        string ipAddress,
        bool rememberMe = false,
        CancellationToken cancellationToken = default)
    {
        // Enhanced input validation
        var validationResult = ValidateAuthenticationInput(user, ipAddress);
        if (validationResult.IsError)
            return validationResult.Errors;

        await using var tx = await _unitOfWork.Context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Security status validation
            var security = await ValidateUserSecurityStatusAsync(user, cancellationToken);
            if (security.IsError)
            {
                await LogSecurityEvent(user.Id, ipAddress, "Authentication failed - security check", security.Errors.First().Description, cancellationToken);
                return security.Errors;
            }

            // Generate access token
            var accessRes = await _jwtTokenService.GenerateAccessTokenAsync(user, cancellationToken);
            if (accessRes.IsError)
            {
                _logger.LogError("Access token generation failed for user {UserId}: {Errors}", 
                    user.Id, string.Join(", ", accessRes.Errors.Select(e => e.Description)));
                return accessRes.Errors;
            }

            // Generate refresh token
            var refreshRes = await _refreshTokenService.GenerateRefreshTokenAsync(
                user.Id, ipAddress, rememberMe, cancellationToken);

            if (refreshRes.IsError)
            {
                _logger.LogError("Refresh token generation failed for user {UserId}: {Errors}", 
                    user.Id, string.Join(", ", refreshRes.Errors.Select(e => e.Description)));
                return refreshRes.Errors;
            }

            // Update user login tracking
            await UpdateUserLoginTrackingAsync(user, ipAddress, cancellationToken);

            // Log successful authentication
            await LogSecurityEvent(user.Id, ipAddress, "Authentication successful", $"Remember me: {rememberMe}", cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            _logger.LogInformation("User {UserId} authenticated successfully from IP {IpAddress}", user.Id, ipAddress);

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
            _logger.LogError(ex, "Authentication failed for user {UserId} from IP {IpAddress}", user.Id, ipAddress);
            await LogSecurityEvent(user.Id, ipAddress, "Authentication error", ex.Message, cancellationToken);
            return Error.Failure("Authentication.Failed", "Authentication failed due to an unexpected error");
        }
    }

    public async Task<ErrorOr<AuthenticationResult>> RefreshAsync(
        string refreshToken,
        string ipAddress,
        bool rememberMe = false,
        CancellationToken cancellationToken = default)
    {
        // Enhanced input validation
        var validationResult = ValidateRefreshInput(refreshToken, ipAddress);
        if (validationResult.IsError)
            return validationResult.Errors;

        await using var tx = await _unitOfWork.Context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Validate and load user
            var validated = await _refreshTokenService.ValidateRefreshTokenAsync(refreshToken, cancellationToken);
            if (validated.IsError)
            {
                _logger.LogWarning("Refresh token validation failed from IP {IpAddress}: {Errors}", 
                    ipAddress, string.Join(", ", validated.Errors.Select(e => e.Description)));
                return validated.Errors;
            }

            var (token, user) = (validated.Value.RefreshToken, validated.Value.User);

            // Enhanced security status validation
            var security = await ValidateUserSecurityStatusAsync(user, cancellationToken);
            if (security.IsError)
            {
                // Revoke the presented token for safety
                await _refreshTokenService.RevokeTokenAsync(refreshToken, ipAddress, "User security status changed", cancellationToken);
                await LogSecurityEvent(user.Id, ipAddress, "Refresh blocked - security status changed", security.Errors.First().Description, cancellationToken);
                return security.Errors;
            }

            // Check for IP address changes (potential security risk)
            await ValidateIpAddressConsistencyAsync(token, ipAddress, user.Id, cancellationToken);

            // Rotate token (one-time use)
            var rotated = await _refreshTokenService.RotateRefreshTokenAsync(refreshToken, ipAddress, rememberMe, cancellationToken);
            if (rotated.IsError)
            {
                _logger.LogError("Token rotation failed for user {UserId}: {Errors}", 
                    user.Id, string.Join(", ", rotated.Errors.Select(e => e.Description)));
                return rotated.Errors;
            }

            // Generate new access token
            var accessRes = await _jwtTokenService.GenerateAccessTokenAsync(user, cancellationToken);
            if (accessRes.IsError)
            {
                _logger.LogError("Access token generation failed during refresh for user {UserId}: {Errors}", 
                    user.Id, string.Join(", ", accessRes.Errors.Select(e => e.Description)));
                return accessRes.Errors;
            }

            // Update last activity
            await UpdateUserLoginTrackingAsync(user, ipAddress, cancellationToken);

            // Log successful token refresh
            await LogSecurityEvent(user.Id, ipAddress, "Token refresh successful", $"Remember me: {rememberMe}", cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            _logger.LogInformation("Token refreshed successfully for user {UserId} from IP {IpAddress}", user.Id, ipAddress);

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
            await tx.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Token refresh failed from IP {IpAddress}", ipAddress);
            return Error.Failure("Refresh.Failed", "Token refresh failed due to an unexpected error");
        }
    }

    public async Task<ErrorOr<Deleted>> LogoutAsync(
        string refreshToken,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        // Enhanced input validation
        var validationResult = ValidateLogoutInput(refreshToken, ipAddress);
        if (validationResult.IsError)
            return validationResult.Errors;

        try
        {
            // Get user info for logging before revoking token
            var validated = await _refreshTokenService.ValidateRefreshTokenAsync(refreshToken, cancellationToken);
            var userId = validated.IsError ? (Guid?)null : validated.Value.User?.Id;

            var res = await _refreshTokenService.RevokeTokenAsync(refreshToken, ipAddress, "User logout", cancellationToken);
            if (res.IsError)
            {
                _logger.LogWarning("Token revocation failed during logout from IP {IpAddress}: {Errors}", 
                    ipAddress, string.Join(", ", res.Errors.Select(e => e.Description)));
                return res.Errors;
            }

            if (userId.HasValue)
            {
                await LogSecurityEvent(userId.Value, ipAddress, "Logout successful", "User initiated logout", cancellationToken);
                _logger.LogInformation("User {UserId} logged out successfully from IP {IpAddress}", userId.Value, ipAddress);
            }

            return Result.Deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed from IP {IpAddress}", ipAddress);
            return Error.Failure("Logout.Failed", "Logout failed due to an unexpected error");
        }
    }

    public async Task<ErrorOr<int>> LogoutFromAllDevicesAsync(
        Guid userId,
        string ipAddress,
        string? currentToken = null,
        CancellationToken cancellationToken = default)
    {
        // Enhanced input validation
        var validationResult = ValidateLogoutAllInput(userId, ipAddress);
        if (validationResult.IsError)
            return validationResult.Errors;

        try
        {
            var res = await _refreshTokenService.RevokeAllUserTokensAsync(
                userId, ipAddress, "Logout from all devices", currentToken, cancellationToken);

            if (res.IsError)
            {
                _logger.LogError("Bulk token revocation failed for user {UserId}: {Errors}", 
                    userId, string.Join(", ", res.Errors.Select(e => e.Description)));
                return res.Errors;
            }

            await LogSecurityEvent(userId, ipAddress, "Logout from all devices", $"Revoked {res.Value} tokens", cancellationToken);
            _logger.LogInformation("User {UserId} logged out from all devices. Revoked {TokenCount} tokens", userId, res.Value);

            return res.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout from all devices failed for user {UserId}", userId);
            return Error.Failure("LogoutAll.Failed", "Logout from all devices failed due to an unexpected error");
        }
    }

    // ---- Session Management Methods ------------------------------------------

    public async Task<ErrorOr<int>> GetActiveSessionCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return Error.Validation("Session.InvalidUserId", "Valid user ID is required");

        try
        {
            var count = await _unitOfWork.Context.RefreshTokens
                .CountAsync(t => t.UserId == userId && 
                                !t.RevokedAt.HasValue && 
                                DateTimeOffset.UtcNow < t.ExpiresAt, 
                           cancellationToken);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active session count for user {UserId}", userId);
            return Error.Failure("Session.CountFailed", "Failed to retrieve active session count");
        }
    }

    public async Task<ErrorOr<List<ActiveSessionInfo>>> GetActiveSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return Error.Validation("Session.InvalidUserId", "Valid user ID is required");

        try
        {
            var sessions = await _unitOfWork.Context.RefreshTokens
                .Where(t => t.UserId == userId && 
                           !t.RevokedAt.HasValue && 
                           DateTimeOffset.UtcNow < t.ExpiresAt)
                .Select(t => new ActiveSessionInfo
                {
                    TokenId = t.Id,
                    CreatedAt = t.CreatedAt,
                    ExpiresAt = t.ExpiresAt,
                    CreatedByIp = t.CreatedByIp,
                    IsCurrentSession = false // Will be determined by caller
                })
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(cancellationToken);

            return sessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active sessions for user {UserId}", userId);
            return Error.Failure("Session.ListFailed", "Failed to retrieve active sessions");
        }
    }

    public async Task<ErrorOr<Success>> RevokeSessionAsync(
        Guid userId,
        Guid tokenId,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return Error.Validation("Session.InvalidUserId", "Valid user ID is required");
        if (tokenId == Guid.Empty)
            return Error.Validation("Session.InvalidTokenId", "Valid token ID is required");
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Error.Validation("Session.InvalidIpAddress", "IP address is required");

        try
        {
            var token = await _unitOfWork.Context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Id == tokenId && t.UserId == userId, cancellationToken);

            if (token is null)
                return Error.NotFound("Session.NotFound", "Session not found");

            if (token.IsRevoked)
                return Error.Validation("Session.AlreadyRevoked", "Session is already revoked");

            token.Revoke(ipAddress, "Session revoked by user");
            _unitOfWork.Context.RefreshTokens.Update(token);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await LogSecurityEvent(userId, ipAddress, "Session revoked", $"Token ID: {tokenId}", cancellationToken);
            _logger.LogInformation("Session {TokenId} revoked for user {UserId}", tokenId, userId);

            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke session {TokenId} for user {UserId}", tokenId, userId);
            return Error.Failure("Session.RevokeFailed", "Failed to revoke session");
        }
    }

    public async Task<ErrorOr<bool>> IsTokenValidAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Error.Validation("Token.Invalid", "Refresh token is required");

        try
        {
            var validated = await _refreshTokenService.ValidateRefreshTokenAsync(refreshToken, cancellationToken);
            return !validated.IsError;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation check failed");
            return Error.Failure("Token.ValidationFailed", "Token validation check failed");
        }
    }

    // ---- Private Helper Methods ------------------------------------------------

    private static ErrorOr<Success> ValidateAuthenticationInput(User user, string ipAddress)
    {
        if (user is null || user.Id == Guid.Empty)
            return Error.Validation("Authentication.InvalidUser", "Valid user is required");
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Error.Validation("Authentication.InvalidIpAddress", "IP address is required");
        if (ipAddress.Length > 45) // Max length for IPv6
            return Error.Validation("Authentication.InvalidIpAddress", "IP address format is invalid");

        return Result.Success;
    }

    private static ErrorOr<Success> ValidateRefreshInput(string refreshToken, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Error.Validation("Refresh.InvalidToken", "Refresh token is required");
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Error.Validation("Refresh.InvalidIpAddress", "IP address is required");
        if (ipAddress.Length > 45)
            return Error.Validation("Refresh.InvalidIpAddress", "IP address format is invalid");

        return Result.Success;
    }

    private static ErrorOr<Success> ValidateLogoutInput(string refreshToken, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Error.Validation("Logout.InvalidToken", "Refresh token is required");
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Error.Validation("Logout.InvalidIpAddress", "IP address is required");

        return Result.Success;
    }

    private static ErrorOr<Success> ValidateLogoutAllInput(Guid userId, string ipAddress)
    {
        if (userId == Guid.Empty)
            return Error.Validation("LogoutAll.InvalidUserId", "Valid user ID is required");
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Error.Validation("LogoutAll.InvalidIpAddress", "IP address is required");

        return Result.Success;
    }

    private async Task<ErrorOr<Success>> ValidateUserSecurityStatusAsync(User user, CancellationToken ct)
    {
        try
        {
            // Check if user is locked out
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Authentication attempt for locked user {UserId}", user.Id);
                return Error.Validation("Authentication.UserLocked", "User account is locked");
            }

            // Check email confirmation
            if (!user.EmailConfirmed)
            {
                _logger.LogWarning("Authentication attempt for unconfirmed email {UserId}", user.Id);
                return Error.Validation("Authentication.EmailNotConfirmed", "Email address must be confirmed");
            }

            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Security status validation failed for user {UserId}", user.Id);
            return Error.Failure("Authentication.SecurityCheckFailed", "Security validation failed");
        }
    }

    private async Task ValidateIpAddressConsistencyAsync(RefreshToken token, string currentIp, Guid userId, CancellationToken ct)
    {
        try
        {
            // Log if IP address has changed (for audit purposes)
            if (token.CreatedByIp != currentIp)
            {
                _logger.LogInformation("IP address changed for user {UserId}: {OldIp} -> {NewIp}", 
                    userId, token.CreatedByIp, currentIp);
                
                await LogSecurityEvent(userId, currentIp, "IP address changed", 
                    $"Previous: {token.CreatedByIp}, Current: {currentIp}", ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IP address consistency check failed for user {UserId}", userId);
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
            {
                _logger.LogWarning("User login tracking update failed for {UserId}: {Errors}",
                    user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception updating user login tracking for {UserId}", user.Id);
        }
    }

    private Task LogSecurityEvent(Guid userId, string ipAddress, string eventType, string details, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Security Event - User: {UserId}, IP: {IpAddress}, Event: {EventType}, Details: {Details}", 
                userId, ipAddress, eventType, details);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event for user {UserId}", userId);
            return Task.CompletedTask;
        }
    }
}

