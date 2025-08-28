using System.Security.Cryptography;
using System.Text.RegularExpressions;

using Core.Identity;

using ErrorOr;

using Infrastructure.Security.Authentication.Options;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using UseCases.Common.Persistence.Context;
using UseCases.Common.Security.Authentication.Tokens.Models;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace Infrastructure.Security.Authentication.Tokens.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefreshTokenService> _logger;
    private readonly JwtOptions _jwtOptions;

    public RefreshTokenService(
        IUnitOfWork unitOfWork,
        IOptions<JwtOptions> jwtOptions,
        ILogger<RefreshTokenService> logger)
    {
        _unitOfWork = unitOfWork;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public async Task<ErrorOr<RefreshTokenResult>> GenerateRefreshTokenAsync(
        Guid userId,
        string ipAddress,
        bool rememberMe = false,
        bool isSystemUser = false,
        CancellationToken cancellationToken = default)
    {
        // Input validation
        if (userId == Guid.Empty)
            return RefreshToken.Errors.Invalid;

        if (string.IsNullOrWhiteSpace(ipAddress))
            return RefreshToken.Errors.InvalidIpAddress;

        try
        {
            // Check: token limits
            var canHaveMoreResult = await CanUserHaveMoreTokensAsync(userId, isSystemUser, cancellationToken);
            if (canHaveMoreResult.IsError)
                return canHaveMoreResult.Errors;

            if (!canHaveMoreResult.Value)
                return RefreshToken.Errors.TooManyActiveTokens;

            // Generate: secure token using constraints
            var token = GenerateSecureToken();

            var expiresAt = DateTimeOffset.UtcNow.AddDays(
                rememberMe ? _jwtOptions.RefreshTokenExpiryRememberMeInDays
                          : _jwtOptions.RefreshTokenExpiryInDays);

            // Create and add token using domain factory method
            var refreshToken = RefreshToken.Create(userId, token, expiresAt, ipAddress);
            _unitOfWork.Context.RefreshTokens.Add(refreshToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Generated refresh token for user {UserId} from IP {IpAddress}",
                userId, ipAddress);

            return new RefreshTokenResult
            {
                Token = token,
                ExpiresAt = expiresAt,
                UserId = userId,
                CreatedByIp = ipAddress,
                RememberMe = rememberMe
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate refresh token for user {UserId}", userId);
            return RefreshToken.Errors.GenerationFailed;
        }
    }

    public async Task<ErrorOr<RefreshTokenValidationResult>> ValidateRefreshTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        // Format validation using domain constraints
        var formatValidation = ValidateTokenFormat(token);
        if (formatValidation.IsError)
            return formatValidation.Errors;

        try
        {
            // Find token using UnitOfWork context
            var refreshToken = await _unitOfWork.Context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

            if (refreshToken == null)
                return RefreshToken.Errors.NotFound;

            // Use domain method for validation
            var validationResult = ValidateRefreshTokenSecurity(refreshToken);
            if (validationResult.IsError)
                return validationResult.Errors;

            return new RefreshTokenValidationResult
            {
                UserId = refreshToken.UserId,
                RefreshToken = refreshToken,
                User = refreshToken.User
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate refresh token");
            return RefreshToken.Errors.Invalid;
        }
    }

    public async Task<ErrorOr<RefreshTokenResult>> RotateRefreshTokenAsync(
        string currentToken,
        string ipAddress,
        bool rememberMe = false,
        bool isSystemUser = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _unitOfWork.Context.Database.BeginTransactionAsync(cancellationToken);
            // 1. Validate current token
            var validationResult = await ValidateRefreshTokenAsync(currentToken, cancellationToken);
            if (validationResult.IsError)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return validationResult.Errors;
            }

            var validation = validationResult.Value;

            var canHaveMoreResult = await CanUserHaveMoreTokensAsync(validation.UserId, isSystemUser, cancellationToken);
            if (canHaveMoreResult.IsError || !canHaveMoreResult.Value)
                return RefreshToken.Errors.TooManyActiveTokens;

            // Generate: secure token using constraints
            var newToken = GenerateSecureToken();

            var newExpiresAt = DateTimeOffset.UtcNow.AddDays(
                rememberMe ? _jwtOptions.RefreshTokenExpiryRememberMeInDays
                          : _jwtOptions.RefreshTokenExpiryInDays);

            // 3. Use domain method for token replacement
            var newRefreshToken = validation.RefreshToken.Replace(newToken, newExpiresAt, ipAddress);

            // 4. Update old token and add new token
            _unitOfWork.Context.RefreshTokens.Update(validation.RefreshToken);
            _unitOfWork.Context.RefreshTokens.Add(newRefreshToken);

            // 5. Save all changes in transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Token rotation successful for user {UserId}", validation.UserId);

            return new RefreshTokenResult
            {
                Token = newToken,
                ExpiresAt = newExpiresAt,
                UserId = validation.UserId,
                CreatedByIp = ipAddress,
                RememberMe = false
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Token rotation failed");
            return RefreshToken.Errors.RotationFailed;
        }
    }

    public async Task<ErrorOr<Success>> RevokeTokenAsync(
        string token,
        string ipAddress,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return RefreshToken.Errors.Invalid;

        if (string.IsNullOrWhiteSpace(ipAddress))
            return RefreshToken.Errors.InvalidIpAddress;

        try
        {
            var refreshToken = await _unitOfWork.Context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);

            if (refreshToken == null)
                return RefreshToken.Errors.NotFound;

            if (refreshToken.IsRevoked)
                return RefreshToken.Errors.Revoked;

            // Use domain method for revocation
            refreshToken.Revoke(ipAddress, null, reason ?? "Manual revocation");
            _unitOfWork.Context.RefreshTokens.Update(refreshToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Revoked refresh token for user {UserId} from IP {IpAddress}",
                refreshToken.UserId, ipAddress);

            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke refresh token");
            return RefreshToken.Errors.RevocationFailed;
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
            return RefreshToken.Errors.Invalid;

        if (string.IsNullOrWhiteSpace(ipAddress))
            return RefreshToken.Errors.InvalidIpAddress;

        try
        {
            // Get all active tokens for user
            var activeTokens = await _unitOfWork.Context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.IsActive)
                .Where(rt => exceptToken == null || rt.Token != exceptToken)
                .ToListAsync(cancellationToken);

            if (!activeTokens.Any())
                return 0;

            // Use domain method for revocation
            foreach (var token in activeTokens)
            {
                token.Revoke(ipAddress, null, reason ?? "All tokens revoked");
            }

            _unitOfWork.Context.RefreshTokens.UpdateRange(activeTokens);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Revoked {Count} tokens for user {UserId}",
                activeTokens.Count, userId);

            return activeTokens.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke all tokens for user {UserId}", userId);
            return RefreshToken.Errors.RevocationFailed;
        }
    }

    public async Task<ErrorOr<int>> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-_jwtOptions.MaxTokenAgeInDays);
            var now = DateTimeOffset.UtcNow;

            var expiredTokens = await _unitOfWork.Context.RefreshTokens
                .Where(rt => rt.ExpiresAt < now || (rt.IsRevoked && rt.RevokedAt < cutoffDate))
                .ToListAsync(cancellationToken);

            if (!expiredTokens.Any())
                return 0;

            _unitOfWork.Context.RefreshTokens.RemoveRange(expiredTokens);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cleaned up {Count} expired/revoked tokens", expiredTokens.Count);

            return expiredTokens.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired tokens");
            return RefreshToken.Errors.RevocationFailed;
        }
    }


    #region Private Helper Methods

    private async Task<ErrorOr<bool>> CanUserHaveMoreTokensAsync(
        Guid userId,
        bool isSystemUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var maxTokens = isSystemUser
                ? _jwtOptions.MaxTokensSystemUser
                : _jwtOptions.MaxTokensCustomer;

            var activeTokenCount = await _unitOfWork.Context.RefreshTokens
                .CountAsync(rt => rt.UserId == userId && rt.IsActive, cancellationToken);

            return activeTokenCount < maxTokens;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check token limits for user {UserId}", userId);
            return RefreshToken.Errors.Invalid;
        }
    }

    private static string GenerateSecureToken()
    {
        // Generate token according to domain constraints (64 alphanumeric characters)
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        using var rng = RandomNumberGenerator.Create();
        var tokenChars = new char[RefreshToken.Constraints.TokenLength];

        var bytes = new byte[RefreshToken.Constraints.TokenLength];
        rng.GetBytes(bytes);

        for (int i = 0; i < RefreshToken.Constraints.TokenLength; i++)
        {
            tokenChars[i] = chars[bytes[i] % chars.Length];
        }

        return new string(tokenChars);
    }

    private static ErrorOr<Success> ValidateTokenFormat(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return RefreshToken.Errors.Invalid;

        // Use domain constraints for validation
        if (token.Length != RefreshToken.Constraints.TokenLength)
            return RefreshToken.Errors.InvalidFormat;

        if (!Regex.IsMatch(token, RefreshToken.Constraints.TokenAllowedPattern))
            return RefreshToken.Errors.InvalidFormat;

        return Result.Success;
    }

    private static ErrorOr<Success> ValidateRefreshTokenSecurity(RefreshToken refreshToken)
    {
        // Check if token is expired
        if (refreshToken.IsExpired)
            return RefreshToken.Errors.Expired;

        // Check if token is revoked
        if (refreshToken.IsRevoked)
            return RefreshToken.Errors.Revoked;

        // Check if token can be refreshed using domain method
        if (!refreshToken.CanBeRefreshed())
            return RefreshToken.Errors.Invalid;

        return Result.Success;
    }

    #endregion
}