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

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefreshTokenService> _logger;
    private readonly JwtOptions _options;
    private readonly UserManager<User> _userManager;

    private const int SecureTokenBytes = 64;

    public RefreshTokenService(
        IUnitOfWork unitOfWork,
        IOptions<JwtOptions> options,
        UserManager<User> userManager,
        ILogger<RefreshTokenService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _userManager = userManager;
        _options = options.Value;
    }

    public async Task<ErrorOr<RefreshTokenResult>> GenerateRefreshTokenAsync(
        Guid userId, string ipAddress, bool rememberMe = false, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return User.Errors.UserNotFound;

        if (await _userManager.IsLockedOutAsync(user))
            return User.Errors.LockedOut;

        try
        {
            var rawToken = GenerateSecureToken();
            var lifetimeDays = rememberMe
                ? _options.RefreshTokenRememberMeLifetimeDays
                : _options.RefreshTokenLifetimeDays;

            var token = RefreshToken.Create(
                userId,
                rawToken,
                DateTimeOffset.UtcNow.AddDays(lifetimeDays),
                ipAddress);

            _unitOfWork.Context.RefreshTokens.Add(token);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Refresh token generated for user {UserId}", userId);

            return new RefreshTokenResult
            {
                Token = rawToken,
                ExpiresAt = token.ExpiresAt.ToUnixTimeSeconds(),
                UserId = userId,
                CreatedByIp = token.CreatedByIp,
                RememberMe = rememberMe
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token generation failed for user {UserId}", userId);
            return RefreshToken.Errors.GenerationFailed;
        }
    }

    public async Task<ErrorOr<RefreshTokenResult>> RotateRefreshTokenAsync(
      string rawCurrentToken, string ipAddress, bool rememberMe = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawCurrentToken))
            return RefreshToken.Errors.RefreshTokenRequired;

        var hash = RefreshToken.Hash(rawCurrentToken);
        var oldToken = await _unitOfWork.Context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        // Basic validation checks
        if (oldToken is null) return RefreshToken.Errors.RefreshTokenNotFound;
        if (oldToken.IsRevoked)
        {
            // SECURITY ALERT: A revoked token was used. This is a critical sign of token theft.
            _logger.LogError("SECURITY ALERT: Token reuse attempt detected for user {UserId} from IP {IpAddress}",
                oldToken.UserId, ipAddress);
            return RefreshToken.Errors.Revoked;
        }
        if (oldToken.IsExpired) return RefreshToken.Errors.Expired;
        if (oldToken.User is null) return User.Errors.UserNotFound;

        // Start transaction for atomic operation
        await using var transaction = await _unitOfWork.Context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Revoke the old token
            oldToken.Revoke(ipAddress, "Token rotated");
            _unitOfWork.Context.RefreshTokens.Update(oldToken);

            // 2. Generate and save the new token
            var newRefreshTokenResult = await GenerateRefreshTokenAsync(oldToken.UserId, ipAddress, rememberMe, cancellationToken);
            if (newRefreshTokenResult.IsError)
            {
                await transaction.RollbackAsync(cancellationToken);
                return newRefreshTokenResult.Errors;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Token rotated successfully for user {UserId}", oldToken.UserId);
            return newRefreshTokenResult.Value;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Token rotation failed for user {UserId}", oldToken.UserId);
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

        var hash = RefreshToken.Hash(rawToken);
        var token = await _unitOfWork.Context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token is null || token.IsRevoked)
            return Result.Success; // Idempotent: already revoked or doesn't exist

        try
        {
            token.Revoke(ipAddress, reason ?? "Manual revocation");
            _unitOfWork.Context.RefreshTokens.Update(token);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Token revoked successfully for user {UserId}", token.UserId);
            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token revocation failed for token {TokenHash}", token.TokenHash);
            return RefreshToken.Errors.RevocationFailed;
        }
    }

    public async Task<ErrorOr<int>> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var retentionCutoff = now.AddDays(-_options.RevokedTokenRetentionDays);
            var deletedCount = await _unitOfWork.Context.RefreshTokens
                .Where(t => t.ExpiresAt < now || (t.IsRevoked && t.RevokedAt < retentionCutoff))
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedCount > 0)
                _logger.LogInformation("Token cleanup removed {Count} tokens", deletedCount);

            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token cleanup operation failed");
            return RefreshToken.Errors.CleanupFailed;
        }
    }

    public async Task<ErrorOr<RefreshTokenValidationResult>> ValidateRefreshTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return RefreshToken.Errors.RefreshTokenRequired;

        try
        {
            var hash = RefreshToken.Hash(token);
            var stored = await _unitOfWork.Context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

            if (stored is null) return RefreshToken.Errors.RefreshTokenNotFound;
            if (stored.IsRevoked) return RefreshToken.Errors.Revoked;
            if (stored.IsExpired) return RefreshToken.Errors.Expired;
            if (stored.User is null) return User.Errors.UserNotFound;

            return new RefreshTokenValidationResult
            {
                RefreshToken = stored,
                User = stored.User
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token validation failed");
            return RefreshToken.Errors.ValidationFailed;
        }
    }

    public async Task<ErrorOr<int>> RevokeAllUserTokensAsync(
        Guid userId,
        string ipAddress,
        string? reason = null,
        string? exceptToken = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return User.Errors.UserIdRequired;

        if (string.IsNullOrWhiteSpace(ipAddress))
            return RefreshToken.Errors.InvalidIpAddress;

        try
        {

            string? exceptHash = null;
            if (!string.IsNullOrWhiteSpace(exceptToken))
            {
                try
                {
                    exceptHash = RefreshToken.Hash(exceptToken);
                }
                catch
                {
                    // If hashing fails for some reason, just treat as no exception token provided.
                    exceptHash = null;
                }
            }

            var tokens = await _unitOfWork.Context.RefreshTokens
                .Where(t => t.UserId == userId && !t.IsRevoked && (exceptHash == null || t.TokenHash != exceptHash))
                .ToListAsync(cancellationToken);

            if (tokens.Count == 0)
                return 0;

            foreach (var t in tokens)
            {
                t.Revoke(ipAddress, reason ?? "Revoke all user tokens");
                _unitOfWork.Context.RefreshTokens.Update(t);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Revoked {Count} tokens for user {UserId}", tokens.Count, userId);

            return tokens.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke all tokens for user {UserId}", userId);
            return RefreshToken.Errors.RevokeAllFailed;
        }
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(SecureTokenBytes);
        // Use Base64Url encoding for URL safety
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}