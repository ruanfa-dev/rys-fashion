using System.Security.Cryptography;

using Core.Identity;

using ErrorOr;

using Infrastructure.Security.Authentication.Options;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using UseCases.Common.Persistence.Context;
using UseCases.Common.Security.Authentication.Tokens.Models;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace Infrastructure.Security.Authentication.Tokens.Services;

/// <summary>
/// Production-ready refresh token service with essential security features.
/// Implements token rotation, reuse detection, and basic rate limiting.
/// </summary>
public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefreshTokenService> _logger;
    private readonly JwtOptions _options;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;

    // Essential security constants
    private const int MaxTokenGenerationAttempts = 5;
    private const int TokenCollisionRetryDelayMs = 50;
    private const int RateLimitWindowMinutes = 1;
    private const int MaxTokensPerIpPerMinute = 5;
    private const int MaxTokensPerUserPerMinute = 3;
    private const int BatchCleanupSize = 1000;

    public RefreshTokenService(
        IUnitOfWork unitOfWork,
        IOptions<JwtOptions> options,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        ILogger<RefreshTokenService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<ErrorOr<RefreshTokenResult>> GenerateRefreshTokenAsync(
         Guid userId,
         string ipAddress,
         bool rememberMe = false,
         CancellationToken cancellationToken = default)
    {
        // Input validation
        var validationResult = ValidateBasicInputs(userId, ipAddress);
        if (validationResult.IsError)
            return validationResult.Errors;

        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Error.NotFound("User.NotFound", "Associated user not found");

            // Security checks
            if (await _userManager.IsLockedOutAsync(user))
                return Error.Validation("RefreshToken.UserLocked", "User account is locked");

            // Basic rate limiting
            var rateLimitResult = await CheckBasicRateLimitAsync(userId, ipAddress, cancellationToken);
            if (rateLimitResult.IsError)
                return rateLimitResult.Errors;

            // Get user role information for token configuration
            var isAdmin = await IsUserAdminAsync(user, cancellationToken);
            var tokenConfig = GetTokenConfiguration(isAdmin, rememberMe);

            // Check active token limits
            var activeTokensResult = await ValidateActiveTokenLimitAsync(userId, tokenConfig.MaxActiveTokens, cancellationToken);
            if (activeTokensResult.IsError)
                return activeTokensResult.Errors;

            // Generate unique token
            var tokenGenerationResult = await GenerateUniqueTokenAsync(userId, ipAddress, tokenConfig.LifetimeDays, cancellationToken);
            if (tokenGenerationResult.IsError)
                return tokenGenerationResult.Errors;

            var (rawToken, token) = tokenGenerationResult.Value;

            // Save to database
            _unitOfWork.Context.RefreshTokens.Add(token);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Refresh token generated for user {UserId} from IP {IpAddress}. Admin: {IsAdmin}, RememberMe: {RememberMe}", 
                userId, ipAddress, isAdmin, rememberMe);

            return new RefreshTokenResult 
            { 
                Token = rawToken, 
                ExpiresAt = token.ExpiresAt, 
                UserId = userId 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generate refresh token failed for user {UserId} from IP {IpAddress}", userId, ipAddress);
            return Error.Failure("RefreshToken.GenerationFailed", "Failed to generate refresh token");
        }
    }

    public async Task<ErrorOr<RefreshTokenValidationResult>> ValidateRefreshTokenAsync(
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(rawToken))
            return RefreshToken.Errors.RefreshTokenRequired;

        if (rawToken.Length > 200)
            return RefreshToken.Errors.RefreshTokenInvalidFormat;

        try
        {
            var hash = RefreshToken.Hash(rawToken);
            var token = await _unitOfWork.Context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

            if (token is null)
            {
                _logger.LogWarning("Refresh token validation failed: token not found. Hash prefix: {HashPrefix}", 
                    hash[..Math.Min(8, hash.Length)]);
                return RefreshToken.Errors.RefreshTokenNotFound;
            }

            // Check for token reuse (critical security issue)
            if (token.IsRevoked)
            {
                await HandleTokenReuseAttemptAsync(token, cancellationToken);
                return RefreshToken.Errors.Revoked;
            }

            if (token.IsExpired)
            {
                _logger.LogInformation("Refresh token validation failed: token expired for user {UserId}", token.UserId);
                return RefreshToken.Errors.Expired;
            }

            if (token.User is null)
            {
                _logger.LogError("Refresh token validation failed: associated user not found for token {TokenId}", token.Id);
                return Error.NotFound("RefreshToken.UserNotFound", "Associated user not found");
            }

            // Additional user security validation
            if (await _userManager.IsLockedOutAsync(token.User))
                return Error.Validation("RefreshToken.UserLocked", "User account is locked");

            _logger.LogDebug("Refresh token validated successfully for user {UserId}", token.UserId);

            return new RefreshTokenValidationResult
            {
                RefreshToken = token,
                User = token.User
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token validation failed with exception");
            return Error.Failure("RefreshToken.ValidationFailed", "Token validation failed");
        }
    }

    public async Task<ErrorOr<RefreshTokenResult>> RotateRefreshTokenAsync(
      string rawCurrentToken,
      string ipAddress,
      bool rememberMe = false,
      CancellationToken cancellationToken = default)
    {
        // Input validation
        var validationResult = ValidateRotationInputs(rawCurrentToken, ipAddress);
        if (validationResult.IsError)
            return validationResult.Errors;

        await using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Validate current token
            var validatedResult = await ValidateRefreshTokenAsync(rawCurrentToken, cancellationToken);
            if (validatedResult.IsError)
                return validatedResult.Errors;

            var (oldToken, user) = (validatedResult.Value.RefreshToken, validatedResult.Value.User);

            // Log IP address change if it occurs
            LogIpAddressChange(oldToken, ipAddress);

            // Generate new token
            var isAdmin = await IsUserAdminAsync(user, cancellationToken);
            var tokenConfig = GetTokenConfiguration(isAdmin, rememberMe);

            var tokenGenerationResult = await GenerateUniqueTokenAsync(user.Id, ipAddress, tokenConfig.LifetimeDays, cancellationToken);
            if (tokenGenerationResult.IsError)
                return tokenGenerationResult.Errors;

            var (rawNewToken, newToken) = tokenGenerationResult.Value;

            // Atomic rotation: revoke old, create new
            oldToken.Revoke(ipAddress, "Rotated", newToken.TokenHash);

            _unitOfWork.Context.RefreshTokens.Update(oldToken);
            _unitOfWork.Context.RefreshTokens.Add(newToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Refresh token rotated successfully for user {UserId} from IP {IpAddress}", 
                user.Id, ipAddress);

            return new RefreshTokenResult 
            { 
                Token = rawNewToken, 
                ExpiresAt = newToken.ExpiresAt, 
                UserId = user.Id 
            };
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogWarning(ex, "Concurrency conflict during token rotation from IP {IpAddress}. Token may have been used concurrently.", ipAddress);
            
            // Return specific error to indicate the token was likely used by another request
            return Error.Conflict("RefreshToken.ConcurrentUse", 
                "This token is being used in another session. Please log in again for security.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Rotate refresh token failed from IP {IpAddress}", ipAddress);
            return RefreshToken.Errors.RotationFailed;
        }
    }

    public async Task<ErrorOr<Success>> RevokeTokenAsync(
        string rawToken,
        string ipAddress,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return RefreshToken.Errors.RefreshTokenRequired;
        if (string.IsNullOrWhiteSpace(ipAddress))
            return RefreshToken.Errors.InvalidIpAddress;

        try
        {
            var hash = RefreshToken.Hash(rawToken);
            var token = await _unitOfWork.Context.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

            if (token is null)
            {
                _logger.LogWarning("Attempted to revoke non-existent token from IP {IpAddress}", ipAddress);
                return RefreshToken.Errors.RefreshTokenNotFound;
            }

            if (!token.IsRevoked)
            {
                token.Revoke(ipAddress, reason);
                _unitOfWork.Context.RefreshTokens.Update(token);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Refresh token revoked for user {UserId} from IP {IpAddress}. Reason: {Reason}", 
                    token.UserId, ipAddress, reason ?? "Not specified");
            }

            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Revoke refresh token failed from IP {IpAddress}", ipAddress);
            return RefreshToken.Errors.RevocationFailed;
        }
    }

    public async Task<ErrorOr<int>> RevokeAllUserTokensAsync(
        Guid userId,
        string ipAddress,
         string? reason = null,
        string? exceptRawToken = null,
        CancellationToken cancellationToken = default)
    {
        var validationResult = ValidateBasicInputs(userId, ipAddress);
        if (validationResult.IsError)
            return validationResult.Errors;

        try
        {
            var exceptHash = string.IsNullOrWhiteSpace(exceptRawToken) ? null : RefreshToken.Hash(exceptRawToken);

            var tokens = await _unitOfWork.Context.RefreshTokens
                .Where(t => t.UserId == userId && 
                           !t.RevokedAt.HasValue && 
                           t.ExpiresAt > DateTimeOffset.UtcNow &&
                           (exceptHash == null || t.TokenHash != exceptHash))
                .ToListAsync(cancellationToken);

            if (tokens.Count == 0)
            {
                _logger.LogInformation("No active tokens found to revoke for user {UserId}", userId);
                return 0;
            }

            foreach (var token in tokens)
                token.Revoke(ipAddress, reason);

            _unitOfWork.Context.RefreshTokens.UpdateRange(tokens);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Revoked {TokenCount} active tokens for user {UserId} from IP {IpAddress}. Reason: {Reason}", 
                tokens.Count, userId, ipAddress, reason ?? "Not specified");

            return tokens.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Revoke all user tokens failed for user {UserId} from IP {IpAddress}", userId, ipAddress);
            return Error.Failure("RefreshToken.BulkRevocationFailed", "Bulk token revocation failed");
        }
    }

    public async Task<ErrorOr<int>> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var retentionCutoff = now.AddDays(-_options.RevokedTokenRetentionDays);
            var totalDeleted = 0;

            // Process in batches to avoid memory issues and long-running transactions
            while (true)
            {
                var tokensToDelete = await _unitOfWork.Context.RefreshTokens
                    .Where(t => t.ExpiresAt < now || (t.IsRevoked && t.RevokedAt < retentionCutoff))
                    .Take(BatchCleanupSize)
                    .ToListAsync(cancellationToken);

                if (tokensToDelete.Count == 0)
                    break;

                _unitOfWork.Context.RefreshTokens.RemoveRange(tokensToDelete);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                totalDeleted += tokensToDelete.Count;

                // Break if we processed less than the batch size (we're done)
                if (tokensToDelete.Count < BatchCleanupSize)
                    break;
            }

            if (totalDeleted > 0)
            {
                _logger.LogInformation("Cleanup completed: removed {TotalCount} expired/revoked tokens", totalDeleted);
            }

            return totalDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleanup expired tokens failed");
            return Error.Failure("RefreshToken.CleanupFailed", "Token cleanup failed");
        }
    }

    // ---- Private Helper Methods ------------------------------------------------

    private static ErrorOr<Success> ValidateBasicInputs(Guid userId, string ipAddress)
    {
        if (userId == Guid.Empty)
            return Error.Validation("RefreshToken.InvalidUserId", "User ID cannot be empty");
        if (string.IsNullOrWhiteSpace(ipAddress))
            return RefreshToken.Errors.InvalidIpAddress;
        if (ipAddress.Length > 45) // IPv6 max length
            return RefreshToken.Errors.InvalidIpAddress;

        return Result.Success;
    }

    private static ErrorOr<Success> ValidateRotationInputs(string rawToken, string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return RefreshToken.Errors.RefreshTokenRequired;
        if (string.IsNullOrWhiteSpace(ipAddress))
            return RefreshToken.Errors.InvalidIpAddress;

        return Result.Success;
    }

    private async Task<bool> IsUserAdminAsync(User user, CancellationToken cancellationToken)
    {
        try
        {
            var systemRoles = await _roleManager.Roles
                .Where(r => r.IsSystemRole)
                .Select(r => r.Name!)
                .ToListAsync(cancellationToken);
            
            if (systemRoles.Count == 0)
                return false;

            var userRoles = await _userManager.GetRolesAsync(user);
            return userRoles.Any(role => systemRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
        }
        catch
        {
            return false; // Default to non-admin on error
        }
    }

    private TokenConfiguration GetTokenConfiguration(bool isAdmin, bool rememberMe)
    {
        return new TokenConfiguration
        {
            MaxActiveTokens = isAdmin 
                ? _options.AdminMaxActiveRefreshTokensPerUser 
                : _options.MaxActiveRefreshTokensPerUser,
            LifetimeDays = isAdmin 
                ? _options.AdminRefreshTokenLifetimeDays 
                : (rememberMe 
                    ? _options.RefreshTokenRememberMeLifetimeDays 
                    : _options.RefreshTokenLifetimeDays)
        };
    }

    private async Task<ErrorOr<Success>> ValidateActiveTokenLimitAsync(
        Guid userId, 
        int maxActiveTokens, 
        CancellationToken cancellationToken)
    {
        var activeCount = await _unitOfWork.Context.RefreshTokens
            .CountAsync(r => r.UserId == userId && 
                            DateTimeOffset.UtcNow < r.ExpiresAt && 
                            !r.RevokedAt.HasValue, 
                       cancellationToken);

        if (activeCount >= maxActiveTokens)
        {
            _logger.LogWarning("User {UserId} has reached maximum active token limit ({MaxTokens})", 
                userId, maxActiveTokens);
            return RefreshToken.Errors.TooManyActiveTokens;
        }

        return Result.Success;
    }

    private async Task<ErrorOr<Success>> CheckBasicRateLimitAsync(
        Guid userId, 
        string ipAddress, 
        CancellationToken cancellationToken)
    {
        try
        {
            var windowStart = DateTimeOffset.UtcNow.AddMinutes(-RateLimitWindowMinutes);

            // Check rate limit per IP
            var recentFromIp = await _unitOfWork.Context.RefreshTokens
                .CountAsync(t => t.CreatedByIp == ipAddress && t.CreatedAt >= windowStart, cancellationToken);

            if (recentFromIp >= MaxTokensPerIpPerMinute)
            {
                _logger.LogWarning("Rate limit exceeded for IP {IpAddress}: {RequestCount} requests in {Minutes} minute(s)", 
                    ipAddress, recentFromIp, RateLimitWindowMinutes);
                return RefreshToken.Errors.RateLimitExceeded;
            }

            // Check rate limit per user
            var recentForUser = await _unitOfWork.Context.RefreshTokens
                .CountAsync(t => t.UserId == userId && t.CreatedAt >= windowStart, cancellationToken);

            if (recentForUser >= MaxTokensPerUserPerMinute)
            {
                _logger.LogWarning("Rate limit exceeded for user {UserId}: {RequestCount} requests in {Minutes} minute(s)", 
                    userId, recentForUser, RateLimitWindowMinutes);
                return RefreshToken.Errors.RateLimitExceeded;
            }

            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for user {UserId}", userId);
            return Result.Success; // Don't block on error
        }
    }

    private async Task<ErrorOr<(string rawToken, RefreshToken token)>> GenerateUniqueTokenAsync(
        Guid userId, 
        string ipAddress, 
        int lifetimeDays, 
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= MaxTokenGenerationAttempts; attempt++)
        {
            var rawToken = GenerateSecureToken();
            var hash = RefreshToken.Hash(rawToken);

            // Check for hash collision
            var exists = await _unitOfWork.Context.RefreshTokens
                .AnyAsync(t => t.TokenHash == hash, cancellationToken);

            if (!exists)
            {
                var expires = DateTimeOffset.UtcNow.AddDays(lifetimeDays);
                var token = RefreshToken.Create(userId, rawToken, expires, ipAddress);
                return (rawToken, token);
            }

            _logger.LogWarning("Token collision detected on attempt {Attempt} for user {UserId}", attempt, userId);
            
            if (attempt < MaxTokenGenerationAttempts)
                await Task.Delay(TokenCollisionRetryDelayMs, cancellationToken);
        }

        _logger.LogError("Failed to generate unique token after {Attempts} attempts for user {UserId}", 
            MaxTokenGenerationAttempts, userId);
        return RefreshToken.Errors.GenerationFailed;
    }

    private static string GenerateSecureToken()
    {
        // Generate 64 bytes (512 bits) of cryptographically secure random data
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_'); // Complete Base64URL encoding
    }

    private async Task HandleTokenReuseAttemptAsync(RefreshToken reuseToken, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Refresh token reuse attempt detected for user {UserId}. Token was revoked at {RevokedAt}", 
            reuseToken.UserId, reuseToken.RevokedAt);

        // If token was rotated, revoke the entire descendant chain
        if (!string.IsNullOrWhiteSpace(reuseToken.ReplacedByTokenHash))
        {
            await RevokeDescendantChainAsync(reuseToken, "Security violation: token reuse detected", cancellationToken);
            _logger.LogError("SECURITY INCIDENT: Token reuse detected. Revoked descendant chain for user {UserId}", 
                reuseToken.UserId);
        }
    }

    private async Task RevokeDescendantChainAsync(
        RefreshToken reuseToken,
        string reason,
        CancellationToken cancellationToken)
    {
        try
        {
            var tokensToRevoke = new List<RefreshToken> { reuseToken };

            // Walk forward through the rotation chain
            var current = reuseToken;
            while (!string.IsNullOrWhiteSpace(current.ReplacedByTokenHash))
            {
                var next = await _unitOfWork.Context.RefreshTokens
                    .FirstOrDefaultAsync(t => t.TokenHash == current.ReplacedByTokenHash, cancellationToken);

                if (next is null || next.IsRevoked)
                    break;

                tokensToRevoke.Add(next);
                current = next;
            }

            // Revoke all tokens in the chain
            foreach (var token in tokensToRevoke)
                token.Revoke(reuseToken.RevokedByIp ?? "system", reason);

            _unitOfWork.Context.RefreshTokens.UpdateRange(tokensToRevoke);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogError("SECURITY INCIDENT: Revoked {TokenCount} tokens in descendant chain for user {UserId}", 
                tokensToRevoke.Count, reuseToken.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke descendant chain for reused token {TokenId}", reuseToken.Id);
        }
    }

    private void LogIpAddressChange(RefreshToken oldToken, string currentIp)
    {
        if (oldToken.CreatedByIp != currentIp)
        {
            _logger.LogInformation("IP address changed during token rotation for user {UserId}: {OldIp} -> {NewIp}", 
                oldToken.UserId, oldToken.CreatedByIp, currentIp);
        }
    }
}

// ---- Internal Models --------------------------------------------------------

internal record TokenConfiguration
{
    public int MaxActiveTokens { get; init; }
    public int LifetimeDays { get; init; }
}
