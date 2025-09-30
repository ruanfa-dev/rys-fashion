using System.Security.Cryptography;

using Core.Identity.Tokens;
using Core.Identity.Users;

using ErrorOr;

using Infrastructure.Security.Authentication.Options;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using UseCases.Common.Persistence.Context;
using UseCases.Common.Security.Authentication.Tokens.Models;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace Infrastructure.Security.Authentication.Tokens.Services;

public sealed class RefreshTokenService(
    IUnitOfWork unitOfWork,
    IOptions<JwtOptions> options,
    UserManager<User> userManager,
    ILogger<RefreshTokenService> logger)
    : IRefreshTokenService
{
    private readonly JwtOptions _options = options.Value;

    private const int SecureTokenBytes = 64;

    public async Task<ErrorOr<RefreshTokenResult>> GenerateRefreshTokenAsync(
        Guid userId, string ipAddress, bool rememberMe = false, CancellationToken cancellationToken = default)
    {
        User? user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return User.Errors.UserNotFound;

        if (await userManager.IsLockedOutAsync(user))
            return User.Errors.LockedOut;

        try
        {
            string rawToken = GenerateSecureToken();
            int lifetimeDays = rememberMe
                ? _options.RefreshTokenRememberMeLifetimeDays
                : _options.RefreshTokenLifetimeDays;

            RefreshToken token = RefreshToken.Create(
                userId,
                rawToken,
                DateTimeOffset.UtcNow.AddDays(lifetimeDays),
                ipAddress);

            unitOfWork.Context.RefreshTokens.Add(token);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Refresh token generated for user {UserId}", userId);

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
            logger.LogError(ex, "Refresh token generation failed for user {UserId}", userId);
            return RefreshToken.Errors.GenerationFailed;
        }
    }

    public async Task<ErrorOr<RefreshTokenResult>> RotateRefreshTokenAsync(
      string rawCurrentToken, string ipAddress, bool rememberMe = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawCurrentToken))
            return RefreshToken.Errors.RefreshTokenRequired;

        string hash = RefreshToken.Hash(rawCurrentToken);
        RefreshToken? oldToken = await unitOfWork.Context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        // Basic validation checks
        if (oldToken is null) return RefreshToken.Errors.RefreshTokenNotFound;
        if (oldToken.IsRevoked)
        {
            // SECURITY ALERT: A revoked token was used. This is a critical sign of token theft.
            logger.LogError("SECURITY ALERT: Token reuse attempt detected for user {UserId} from IP {IpAddress}",
                oldToken.UserId, ipAddress);
            return RefreshToken.Errors.Revoked;
        }
        if (oldToken.IsExpired) return RefreshToken.Errors.Expired;

        // Start transaction for atomic operation
        await using IDbContextTransaction transaction = await unitOfWork.Context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Revoke the old token
            oldToken.Revoke(ipAddress, "Token rotated");
            unitOfWork.Context.RefreshTokens.Update(oldToken);

            // 2. Generate and save the new token
            ErrorOr<RefreshTokenResult> newRefreshTokenResult = await GenerateRefreshTokenAsync(oldToken.UserId, ipAddress, rememberMe, cancellationToken);
            if (newRefreshTokenResult.IsError)
            {
                await transaction.RollbackAsync(cancellationToken);
                return newRefreshTokenResult.Errors;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Token rotated successfully for user {UserId}", oldToken.UserId);
            return newRefreshTokenResult.Value;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Token rotation failed for user {UserId}", oldToken.UserId);
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

        string hash = RefreshToken.Hash(rawToken);
        RefreshToken? token = await unitOfWork.Context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token is null || token.IsRevoked)
            return Result.Success; // Idempotent: already revoked or doesn't exist

        try
        {
            token.Revoke(ipAddress, reason ?? "Manual revocation");
            unitOfWork.Context.RefreshTokens.Update(token);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Token revoked successfully for user {UserId}", token.UserId);
            return Result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Token revocation failed for token {TokenHash}", token.TokenHash);
            return RefreshToken.Errors.RevocationFailed;
        }
    }

    public async Task<ErrorOr<int>> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset retentionCutoff = now.AddDays(-_options.RevokedTokenRetentionDays);
            int deletedCount = await unitOfWork.Context.RefreshTokens
                .Where(t => t.ExpiresAt < now || (t.IsRevoked && t.RevokedAt < retentionCutoff))
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedCount > 0)
                logger.LogInformation("Token cleanup removed {Count} tokens", deletedCount);

            return deletedCount;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Token cleanup operation failed");
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
            string hash = RefreshToken.Hash(token);
            RefreshToken? stored = await unitOfWork.Context.RefreshTokens
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
            logger.LogError(ex, "Refresh token validation failed");
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

            List<RefreshToken> tokens = await unitOfWork.Context.RefreshTokens
                .Where(t => t.UserId == userId && !t.IsRevoked && (exceptHash == null || t.TokenHash != exceptHash))
                .ToListAsync(cancellationToken);

            if (tokens.Count == 0)
                return 0;

            foreach (RefreshToken t in tokens)
            {
                t.Revoke(ipAddress, reason ?? "Revoke all user tokens");
                unitOfWork.Context.RefreshTokens.Update(t);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Revoked {Count} tokens for user {UserId}", tokens.Count, userId);

            return tokens.Count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke all tokens for user {UserId}", userId);
            return RefreshToken.Errors.RevokeAllFailed;
        }
    }

    private static string GenerateSecureToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(SecureTokenBytes);
        // Use Base64Url encoding for URL safety
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}