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
        // Input validation
        if (user?.Id == Guid.Empty)
            return Error.Validation("Authentication.InvalidUser", "Valid user is required");
        
        if (string.IsNullOrWhiteSpace(ipAddress) || ipAddress.Length > 45)
            return Error.Validation("Authentication.InvalidIpAddress", "Valid IP address is required");

        await using var tx = await _unitOfWork.Context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Security status validation
            var securityValidation = await ValidateUserSecurityAsync(user!, cancellationToken);
            if (securityValidation.IsError)
            {
                await LogSecurityEventAsync(user!.Id, ipAddress, "Authentication blocked", 
                    securityValidation.Errors.First().Description, cancellationToken);
                return securityValidation.Errors;
            }

            // Generate tokens
            var accessResult = await _jwtTokenService.GenerateAccessTokenAsync(user!, cancellationToken);
            if (accessResult.IsError)
            {
                _logger.LogError("Access token generation failed for user {UserId}", user!.Id);
                return accessResult.Errors;
            }

            var refreshResult = await _refreshTokenService.GenerateRefreshTokenAsync(
                user!.Id, ipAddress, rememberMe, cancellationToken);
            if (refreshResult.IsError)
            {
                _logger.LogError("Refresh token generation failed for user {UserId}", user.Id);
                return refreshResult.Errors;
            }

            // Update user tracking
            await UpdateUserLoginAsync(user, ipAddress, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            await LogSecurityEventAsync(user.Id, ipAddress, "Authentication success", 
                $"RememberMe: {rememberMe}", cancellationToken);

            return new AuthenticationResult
            {
                AccessToken = accessResult.Value.Token,
                AccessTokenExpiresAt = DateTimeOffset.FromUnixTimeSeconds(accessResult.Value.ExpiresAt),
                RefreshToken = refreshResult.Value.Token,
                RefreshTokenExpiresAt = refreshResult.Value.ExpiresAt,
                TokenType = "Bearer"
            };
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Authentication failed for user {UserId}", user?.Id);
            return Error.Failure("Authentication.Failed", "Authentication failed");
        }
    }

    public async Task<ErrorOr<AuthenticationResult>> RefreshAsync(
        string refreshToken,
        string ipAddress,
        bool rememberMe = false,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(refreshToken))
            return Error.Validation("Refresh.InvalidToken", "Refresh token is required");
        
        if (string.IsNullOrWhiteSpace(ipAddress) || ipAddress.Length > 45)
            return Error.Validation("Refresh.InvalidIpAddress", "Valid IP address is required");

        await using var tx = await _unitOfWork.Context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Validate refresh token
            var validationResult = await _refreshTokenService.ValidateRefreshTokenAsync(refreshToken, cancellationToken);
            if (validationResult.IsError)
            {
                _logger.LogWarning("Invalid refresh token used from IP {IpAddress}", ipAddress);
                return validationResult.Errors;
            }

            var user = validationResult.Value.User;

            // Security validation
            var securityValidation = await ValidateUserSecurityAsync(user, cancellationToken);
            if (securityValidation.IsError)
            {
                // Revoke token for security
                await _refreshTokenService.RevokeTokenAsync(refreshToken, ipAddress, 
                    "User security status changed", cancellationToken);
                await LogSecurityEventAsync(user.Id, ipAddress, "Refresh blocked", 
                    securityValidation.Errors.First().Description, cancellationToken);
                return securityValidation.Errors;
            }

            // Rotate token
            var rotationResult = await _refreshTokenService.RotateRefreshTokenAsync(
                refreshToken, ipAddress, rememberMe, cancellationToken);
            if (rotationResult.IsError)
            {
                return rotationResult.Errors;
            }

            // Generate new access token
            var accessResult = await _jwtTokenService.GenerateAccessTokenAsync(user, cancellationToken);
            if (accessResult.IsError)
            {
                return accessResult.Errors;
            }

            // Update user activity
            await UpdateUserLoginAsync(user, ipAddress, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            await LogSecurityEventAsync(user.Id, ipAddress, "Token refresh success", 
                $"RememberMe: {rememberMe}", cancellationToken);

            return new AuthenticationResult
            {
                AccessToken = accessResult.Value.Token,
                AccessTokenExpiresAt = DateTimeOffset.FromUnixTimeSeconds(accessResult.Value.ExpiresAt),
                RefreshToken = rotationResult.Value.Token,
                RefreshTokenExpiresAt = rotationResult.Value.ExpiresAt,
                TokenType = "Bearer"
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            await tx.RollbackAsync(cancellationToken);
            _logger.LogWarning("Token rotation conflict from IP {IpAddress}", ipAddress);
            return Error.Conflict("Refresh.ConcurrentUse", 
                "Token is being used elsewhere. Please authenticate again.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Token refresh failed from IP {IpAddress}", ipAddress);
            return Error.Failure("Refresh.Failed", "Token refresh failed");
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
            // Get user info before revoking
            var validationResult = await _refreshTokenService.ValidateRefreshTokenAsync(refreshToken, cancellationToken);
            var userId = validationResult.IsError ? (Guid?)null : validationResult.Value.User?.Id;

            var revokeResult = await _refreshTokenService.RevokeTokenAsync(
                refreshToken, ipAddress, "User logout", cancellationToken);

            if (revokeResult.IsError)
            {
                _logger.LogWarning("Token revocation failed during logout from IP {IpAddress}", ipAddress);
                return revokeResult.Errors;
            }

            if (userId.HasValue)
            {
                await LogSecurityEventAsync(userId.Value, ipAddress, "Logout success", 
                    "User initiated", cancellationToken);
                _logger.LogInformation("User {UserId} logged out from IP {IpAddress}", userId.Value, ipAddress);
            }

            return Result.Deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed from IP {IpAddress}", ipAddress);
            return Error.Failure("Logout.Failed", "Logout failed");
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
            var result = await _refreshTokenService.RevokeAllUserTokensAsync(
                userId, ipAddress, "Logout from all devices", currentToken, cancellationToken);

            if (result.IsError)
            {
                return result.Errors;
            }

            await LogSecurityEventAsync(userId, ipAddress, "Logout all devices", 
                $"Revoked {result.Value} tokens", cancellationToken);

            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout all devices failed for user {UserId}", userId);
            return Error.Failure("LogoutAll.Failed", "Logout from all devices failed");
        }
    }

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
            _logger.LogError(ex, "Failed to get session count for user {UserId}", userId);
            return Error.Failure("Session.CountFailed", "Failed to get session count");
        }
    }

    public async Task<ErrorOr<List<ActiveSessionResult>>> GetActiveSessionsAsync(
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
                .Select(t => new ActiveSessionResult
                {
                    TokenId = t.Id,
                    CreatedAt = t.CreatedAt,
                    ExpiresAt = t.ExpiresAt,
                    CreatedByIp = t.CreatedByIp,
                    IsCurrentSession = false // Caller determines this
                })
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(cancellationToken);

            return sessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sessions for user {UserId}", userId);
            return Error.Failure("Session.ListFailed", "Failed to get sessions");
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
                return Error.Validation("Session.AlreadyRevoked", "Session already revoked");

            token.Revoke(ipAddress, "Session revoked by user");
            _unitOfWork.Context.RefreshTokens.Update(token);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await LogSecurityEventAsync(userId, ipAddress, "Session revoked", 
                $"TokenId: {tokenId}", cancellationToken);

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
            var result = await _refreshTokenService.ValidateRefreshTokenAsync(refreshToken, cancellationToken);
            return !result.IsError;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation check failed");
            return Error.Failure("Token.ValidationFailed", "Token validation failed");
        }
    }

    // ---- Private Helper Methods ------------------------------------------------

    private async Task<ErrorOr<Success>> ValidateUserSecurityAsync(User user, CancellationToken ct)
    {
        try
        {
            // Check lockout status
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Authentication blocked for locked user {UserId}", user.Id);
                return Error.Validation("Authentication.UserLocked", "Account is locked");
            }

            // Check email confirmation (if required)
            if (!user.EmailConfirmed)
            {
                _logger.LogWarning("Authentication blocked for unconfirmed user {UserId}", user.Id);
                return Error.Validation("Authentication.EmailNotConfirmed", "Email must be confirmed");
            }

            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Security validation failed for user {UserId}", user.Id);
            return Error.Failure("Authentication.SecurityCheckFailed", "Security validation failed");
        }
    }

    private async Task UpdateUserLoginAsync(User user, string ipAddress, CancellationToken ct)
    {
        try
        {
            user.LastSignInAt = DateTimeOffset.UtcNow;
            user.LastSignInIp = ipAddress;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Login tracking update failed for user {UserId}", user.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login tracking update error for user {UserId}", user.Id);
        }
    }

    private Task LogSecurityEventAsync(Guid userId, string ipAddress, string eventType, string details, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Security Event - User: {UserId}, IP: {IpAddress}, Event: {EventType}, Details: {Details}", 
                userId, ipAddress, eventType, details);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Security event logging failed for user {UserId}", userId);
            return Task.CompletedTask;
        }
    }
}

