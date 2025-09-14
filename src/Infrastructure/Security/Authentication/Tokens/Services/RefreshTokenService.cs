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
/// Implements token rotation, reuse detection, and rate limiting.
/// </summary>
public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefreshTokenService> _logger;
    private readonly JwtOptions _options;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;

    // Security constants
    private const int MaxTokenGenerationAttempts = 5;
    private const int TokenCollisionRetryDelayMs = 50;
    private const int RateLimitWindowMinutes = 1;
    private const int MaxTokensPerIpPerMinute = 5;
    private const int MaxTokensPerUserPerMinute = 3;
    private const int BatchCleanupSize = 1000;
    private const int SecureTokenBytes = 64; // 512 bits

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
            {
                _logger.LogWarning("Token generation failed: User {UserId} not found", userId);
                return Error.NotFound("User.NotFound", "User not found");
            }

            // Security validations
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Token generation blocked: User {UserId} is locked out", userId);
                return Error.Validation("RefreshToken.UserLocked", "User account is locked");
            }

            // Rate limiting check
            var rateLimitResult = await CheckRateLimitAsync(userId, ipAddress, cancellationToken);
            if (rateLimitResult.IsError)
                return rateLimitResult.Errors;

            // Token configuration
            var isAdmin = await IsUserAdminAsync(user, cancellationToken);
            var tokenConfig = GetTokenConfiguration(isAdmin, rememberMe);

            // Active token limit validation
            var activeTokensResult = await ValidateActiveTokenLimitAsync(userId, tokenConfig.MaxActiveTokens, cancellationToken);
            if (activeTokensResult.IsError)
                return activeTokensResult.Errors;

            // Generate unique token with collision detection
            var tokenGenerationResult = await GenerateUniqueTokenAsync(userId, ipAddress, tokenConfig.LifetimeDays, cancellationToken);
            if (tokenGenerationResult.IsError)
                return tokenGenerationResult.Errors;

            var (rawToken, token) = tokenGenerationResult.Value;

            // Persist token
            _unitOfWork.Context.RefreshTokens.Add(token);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Refresh token generated successfully for user {UserId} from IP {IpAddress}. Admin: {IsAdmin}, RememberMe: {RememberMe}",
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
            _logger.LogError(ex, "Refresh token generation failed for user {UserId} from IP {IpAddress}", userId, ipAddress);
            return Error.Failure("RefreshToken.GenerationFailed", "Failed to generate refresh token");
        }
    }

    public async Task<ErrorOr<RefreshTokenValidationResult>> ValidateRefreshTokenAsync(
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            _logger.LogWarning("Token validation failed: Empty or null token provided");
            return RefreshToken.Errors.RefreshTokenRequired;
        }

        if (rawToken.Length > 200)
        {
            _logger.LogWarning("Token validation failed: Token length exceeds maximum allowed");
            return RefreshToken.Errors.RefreshTokenInvalidFormat;
        }

        try
        {
            var hash = RefreshToken.Hash(rawToken);
            var token = await _unitOfWork.Context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

            if (token is null)
            {
                _logger.LogWarning("Token validation failed: Token not found. Hash prefix: {HashPrefix}",
                    hash[..Math.Min(8, hash.Length)]);
                return RefreshToken.Errors.RefreshTokenNotFound;
            }

            // Critical security check: Token reuse detection
            if (token.IsRevoked)
            {
                _logger.LogError("SECURITY ALERT: Token reuse attempt detected for user {UserId}. Token was revoked at {RevokedAt}",
                    token.UserId, token.RevokedAt);
                await HandleTokenReuseAttemptAsync(token, cancellationToken);
                return RefreshToken.Errors.Revoked;
            }

            // Expiration check
            if (token.IsExpired)
            {
                _logger.LogInformation("Token validation failed: Token expired for user {UserId}", token.UserId);
                return RefreshToken.Errors.Expired;
            }

            // User validation
            if (token.User is null)
            {
                _logger.LogError("Token validation failed: Associated user not found for token {TokenId}", token.Id);
                return Error.NotFound("RefreshToken.UserNotFound", "Associated user not found");
            }

            // Additional user security validation
            if (await _userManager.IsLockedOutAsync(token.User))
            {
                _logger.LogWarning("Token validation failed: User {UserId} is locked out", token.UserId);
                return Error.Validation("RefreshToken.UserLocked", "User account is locked");
            }

            _logger.LogDebug("Token validated successfully for user {UserId}", token.UserId);

            return new RefreshTokenValidationResult
            {
                RefreshToken = token,
                User = token.User
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed with exception");
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
            {
                _logger.LogWarning("Token rotation failed: Current token invalid from IP {IpAddress}", ipAddress);
                return validatedResult.Errors;
            }

            var (oldToken, user) = (validatedResult.Value.RefreshToken, validatedResult.Value.User);

            // Log IP address changes for audit
            LogIpAddressChange(oldToken, ipAddress);

            // Generate new token
            var isAdmin = await IsUserAdminAsync(user, cancellationToken);
            var tokenConfig = GetTokenConfiguration(isAdmin, rememberMe);

            var tokenGenerationResult = await GenerateUniqueTokenAsync(user.Id, ipAddress, tokenConfig.LifetimeDays, cancellationToken);
            if (tokenGenerationResult.IsError)
                return tokenGenerationResult.Errors;

            var (rawNewToken, newToken) = tokenGenerationResult.Value;

            // Atomic rotation: revoke old, create new with chain link
            oldToken.Revoke(ipAddress, "Token rotation", newToken.TokenHash);

            _unitOfWork.Context.RefreshTokens.Update(oldToken);
            _unitOfWork.Context.RefreshTokens.Add(newToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Token rotated successfully for user {UserId} from IP {IpAddress}",
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
            _logger.LogWarning(ex, "Concurrency conflict during token rotation from IP {IpAddress}. Possible concurrent usage.", ipAddress);

            return Error.Conflict("RefreshToken.ConcurrentUse",
                "Token is being used concurrently. Please authenticate again for security.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Token rotation failed from IP {IpAddress}", ipAddress);
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
                _logger.LogWarning("Token revocation attempt for non-existent token from IP {IpAddress}", ipAddress);
                return RefreshToken.Errors.RefreshTokenNotFound;
            }

            if (!token.IsRevoked)
            {
                token.Revoke(ipAddress, reason ?? "Manual revocation");
                _unitOfWork.Context.RefreshTokens.Update(token);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Token revoked successfully for user {UserId} from IP {IpAddress}. Reason: {Reason}",
                    token.UserId, ipAddress, reason ?? "Manual revocation");
            }
            else
            {
                _logger.LogDebug("Token revocation skipped: Token already revoked for user {UserId}", token.UserId);
            }

            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token revocation failed from IP {IpAddress}", ipAddress);
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

            var effectiveReason = reason ?? "Bulk revocation";
            foreach (var token in tokens)
                token.Revoke(ipAddress, effectiveReason);

            _unitOfWork.Context.RefreshTokens.UpdateRange(tokens);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully revoked {TokenCount} active tokens for user {UserId} from IP {IpAddress}. Reason: {Reason}",
                tokens.Count, userId, ipAddress, effectiveReason);

            return tokens.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk token revocation failed for user {UserId} from IP {IpAddress}", userId, ipAddress);
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

            _logger.LogDebug("Starting token cleanup process. Retention cutoff: {RetentionCutoff}", retentionCutoff);

            // Process in batches to prevent memory issues
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

                _logger.LogDebug("Deleted {BatchCount} tokens in current batch. Total deleted: {TotalDeleted}",
                    tokensToDelete.Count, totalDeleted);

                // Break if we processed less than the batch size (we're done)
                if (tokensToDelete.Count < BatchCleanupSize)
                    break;
            }

            if (totalDeleted > 0)
            {
                _logger.LogInformation("Token cleanup completed: removed {TotalCount} expired/revoked tokens", totalDeleted);
            }
            else
            {
                _logger.LogDebug("Token cleanup completed: no tokens needed cleanup");
            }

            return totalDeleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token cleanup operation failed");
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking admin status for user {UserId}. Defaulting to non-admin.", user.Id);
            return false; // Fail safe to non-admin
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
            _logger.LogWarning("User {UserId} has reached maximum active token limit ({MaxTokens}/{ActiveCount})",
                userId, maxActiveTokens, activeCount);
            return RefreshToken.Errors.TooManyActiveTokens;
        }

        return Result.Success;
    }

    private async Task<ErrorOr<Success>> CheckRateLimitAsync(
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
                _logger.LogWarning("Rate limit exceeded for IP {IpAddress}: {RequestCount}/{MaxRequests} requests in {Minutes} minute(s)",
                    ipAddress, recentFromIp, MaxTokensPerIpPerMinute, RateLimitWindowMinutes);
                return RefreshToken.Errors.RateLimitExceeded;
            }

            // Check rate limit per user
            var recentForUser = await _unitOfWork.Context.RefreshTokens
                .CountAsync(t => t.UserId == userId && t.CreatedAt >= windowStart, cancellationToken);

            if (recentForUser >= MaxTokensPerUserPerMinute)
            {
                _logger.LogWarning("Rate limit exceeded for user {UserId}: {RequestCount}/{MaxRequests} requests in {Minutes} minute(s)",
                    userId, recentForUser, MaxTokensPerUserPerMinute, RateLimitWindowMinutes);
                return RefreshToken.Errors.RateLimitExceeded;
            }

            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for user {UserId}. Allowing request.", userId);
            return Result.Success; // Fail open - don't block users due to rate limit check errors
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

            _logger.LogWarning("Token hash collision detected on attempt {Attempt}/{MaxAttempts} for user {UserId}",
                attempt, MaxTokenGenerationAttempts, userId);

            if (attempt < MaxTokenGenerationAttempts)
                await Task.Delay(TokenCollisionRetryDelayMs, cancellationToken);
        }

        _logger.LogError("Failed to generate unique token after {Attempts} attempts for user {UserId}. This indicates a serious entropy issue.",
            MaxTokenGenerationAttempts, userId);
        return RefreshToken.Errors.GenerationFailed;
    }

    private static string GenerateSecureToken()
    {
        // Generate cryptographically secure random data
        var bytes = RandomNumberGenerator.GetBytes(SecureTokenBytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_'); // Base64URL encoding for URL safety
    }

    private async Task HandleTokenReuseAttemptAsync(RefreshToken reuseToken, CancellationToken cancellationToken)
    {
        _logger.LogError("SECURITY INCIDENT: Token reuse detected for user {UserId}. Token was revoked at {RevokedAt}",
            reuseToken.UserId, reuseToken.RevokedAt);

        // If token was rotated, revoke the entire descendant chain to prevent further misuse
        if (!string.IsNullOrWhiteSpace(reuseToken.ReplacedByTokenHash))
        {
            await RevokeDescendantChainAsync(reuseToken, "Security violation: token reuse detected", cancellationToken);
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
            var maxChainLength = 100; // Prevent infinite loops
            var chainLength = 0;

            while (!string.IsNullOrWhiteSpace(current.ReplacedByTokenHash) && chainLength < maxChainLength)
            {
                var next = await _unitOfWork.Context.RefreshTokens
                    .FirstOrDefaultAsync(t => t.TokenHash == current.ReplacedByTokenHash, cancellationToken);

                if (next is null || next.IsRevoked)
                    break;

                tokensToRevoke.Add(next);
                current = next;
                chainLength++;
            }

            // Revoke all tokens in the chain
            foreach (var token in tokensToRevoke)
                token.Revoke(reuseToken.RevokedByIp ?? "system", reason);

            _unitOfWork.Context.RefreshTokens.UpdateRange(tokensToRevoke);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogError("SECURITY INCIDENT: Revoked {TokenCount} tokens in descendant chain for user {UserId} due to token reuse",
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
