using System.Security.Cryptography;

using Core.Identity;

using ErrorOr;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using UseCases.Common.Persistence.Context;
using UseCases.Common.Security.Authentication.Options;
using UseCases.Common.Security.Authentication.Tokens.Models;
using UseCases.Common.Security.Authentication.Tokens.Services;

namespace Infrastructure.Security.Authentication.Tokens.Services;

/// <summary>
/// Issues, validates, rotates and revokes refresh tokens using hash-at-rest and one-time use.
/// Implements reuse detection (revokes descendant chain).
/// </summary>
public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RefreshTokenService> _logger;
    private readonly JwtOptions _options;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;

    public RefreshTokenService(
        IUnitOfWork uow,
        IOptions<JwtOptions> options,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        ILogger<RefreshTokenService> logger)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
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
        if (userId == Guid.Empty)
            return Error.Validation("RefreshToken.InvalidUserId", "User ID cannot be empty");
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Error.Validation("RefreshToken.InvalidIpAddress", "IP address is required");

        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Error.NotFound("User.NotFound", "Associated user not found");

            // Check: is user admin (system role)
            var systemRoles = await _roleManager.Roles
                .Where(r => r.IsSystemRole)
                .Select(r => r.Name)
                .ToListAsync(cancellationToken);
            var userRoles = await _userManager.GetRolesAsync(user);
            var isAdmin = userRoles.Any(r => systemRoles.Contains(r));

            var maxActiveTokens = isAdmin
                ? _options.AdminMaxActiveRefreshTokensPerUser
                : _options.MaxActiveRefreshTokensPerUser;

            var lifetimeDays = isAdmin
                ? _options.AdminRefreshTokenLifetimeDays
                : (rememberMe ? _options.RefreshTokenRememberMeLifetimeDays : _options.RefreshTokenLifetimeDays);

            var activeCount = await _uow.Context.RefreshTokens
                .CountAsync(r => r.UserId == userId && DateTimeOffset.UtcNow < r.ExpiresAt && !r.RevokedAt.HasValue, cancellationToken);

            if (activeCount >= maxActiveTokens)
                return Error.Conflict("RefreshToken.TooManyActive", "Maximum number of active refresh tokens reached");

            var raw = GenerateRawToken();
            var expires = DateTimeOffset.UtcNow.AddDays(lifetimeDays);

            var token = RefreshToken.Create(userId, raw, expires, ipAddress);
            _uow.Context.RefreshTokens.Add(token);
            await _uow.SaveChangesAsync(cancellationToken);

            return new RefreshTokenResult { Token = raw, ExpiresAt = expires, UserId = userId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generate refresh token failed for user {UserId}", userId);
            return Error.Failure("RefreshToken.GenerationFailed", "Failed to generate refresh token");
        }
    }

    public async Task<ErrorOr<RefreshTokenValidationResult>> ValidateRefreshTokenAsync(
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        // Validate: token is not empty
        if (string.IsNullOrWhiteSpace(rawToken))
            return Error.Validation("RefreshToken.Empty", "Refresh token is required");

        try
        {
            // Fetch: refresh token by hash
            var hash = RefreshToken.Hash(rawToken);
            var token = await _uow.Context.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

            if (token is null)
                return RefreshToken.Errors.RefreshTokenNotFound;

            if (token.IsRevoked)
            {
                // Reuse attempt? If it was rotated, we treat as incident and revoke descendants.
                if (!string.IsNullOrWhiteSpace(token.ReplacedByTokenHash))
                    await RevokeDescendantChainAsync(token, "Detected refresh token reuse", cancellationToken);

                return Error.Unauthorized("RefreshToken.Revoked", "Refresh token is revoked");
            }

            if (token.IsExpired)
                return Error.Unauthorized("RefreshToken.Expired", "Refresh token is expired");

            if (token.User is null)
                return Error.NotFound("RefreshToken.UserNotFound", "Associated user not found");

            return new RefreshTokenValidationResult()
            {
                RefreshToken = token,
                User = token.User
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validate refresh token failed");
            return Error.Failure("RefreshToken.ValidationFailed", "Token validation failed");
        }
    }

    public async Task<ErrorOr<RefreshTokenResult>> RotateRefreshTokenAsync(
      string rawCurrentToken,
      string ipAddress,
      bool rememberMe = false,
      CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Error.Validation("RefreshToken.InvalidIpAddress", "IP address is required");

        try
        {
            await _uow.BeginTransactionAsync(cancellationToken);
            var validated = await ValidateRefreshTokenAsync(rawCurrentToken, cancellationToken);
            if (validated.IsError)
                return validated.Errors;

            var (oldToken, user) = (validated.Value.RefreshToken, validated.Value.User);

            // Check: is user admin (system role)
            var systemRoles = await _roleManager.Roles
                .Where(r => r.IsSystemRole)
                .Select(r => r.Name)
                .ToListAsync(cancellationToken);
            var userRoles = await _userManager.GetRolesAsync(user);
            var isAdmin = userRoles.Any(r => systemRoles.Contains(r));

            var lifetimeDays = isAdmin
                ? _options.AdminRefreshTokenLifetimeDays
                : (rememberMe ? _options.RefreshTokenRememberMeLifetimeDays : _options.RefreshTokenLifetimeDays);

            // Create new token before revoking old one
            var rawNewToken = GenerateRawToken();
            var expires = DateTimeOffset.UtcNow.AddDays(lifetimeDays);
            var newToken = RefreshToken.Create(user.Id, rawNewToken, expires, ipAddress);

            // Revoke old token and link it to the new one
            oldToken.Revoke(ipAddress, "Rotated", newToken.TokenHash);

            _uow.Context.RefreshTokens.Update(oldToken);
            _uow.Context.RefreshTokens.Add(newToken);

            await _uow.SaveChangesAsync(cancellationToken);
            await _uow.CommitTransactionAsync(cancellationToken);

            return new RefreshTokenResult { Token = rawNewToken, ExpiresAt = expires, UserId = user.Id };
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            _logger.LogWarning(ex, "Concurrency conflict during token rotation. Possible race condition detected.");
            return Error.Conflict("RefreshToken.ConcurrencyConflict", "This token has been used. Please log in again.");
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Rotate refresh token failed");
            return Error.Failure("RefreshToken.RotationFailed", "Token rotation failed");
        }
    }


    public async Task<ErrorOr<Success>> RevokeTokenAsync(
        string rawToken,
        string ipAddress,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return Error.Validation("RefreshToken.Empty", "Refresh token is required");
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Error.Validation("RefreshToken.InvalidIpAddress", "IP address is required");

        try
        {
            var hash = RefreshToken.Hash(rawToken);
            var token = await _uow.Context.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

            if (token is null)
                return Error.NotFound("RefreshToken.NotFound", "Refresh token not found");

            if (!token.IsRevoked)
            {
                token.Revoke(ipAddress, reason);
                _uow.Context.RefreshTokens.Update(token);
                await _uow.SaveChangesAsync(cancellationToken);
            }

            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Revoke refresh token failed");
            return Error.Failure("RefreshToken.RevocationFailed", "Token revocation failed");
        }
    }

    public async Task<ErrorOr<int>> RevokeAllUserTokensAsync(
        Guid userId,
        string ipAddress,
         string? reason = null,
        string? exceptRawToken = null,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return Error.Validation("RefreshToken.InvalidUserId", "User ID cannot be empty");
        if (string.IsNullOrWhiteSpace(ipAddress))
            return Error.Validation("RefreshToken.InvalidIpAddress", "IP address is required");

        try
        {
            var exceptHash = string.IsNullOrWhiteSpace(exceptRawToken) ? null : RefreshToken.Hash(exceptRawToken);

            var tokens = await _uow.Context.RefreshTokens
                .Where(t => t.UserId == userId && !t.RevokedAt.HasValue && !(DateTimeOffset.UtcNow >= t.ExpiresAt))
                .ToListAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(exceptHash))
                tokens = tokens.Where(t => t.TokenHash != exceptHash).ToList();

            foreach (var t in tokens)
                t.Revoke(ipAddress, reason);

            if (tokens.Count > 0)
            {
                _uow.Context.RefreshTokens.UpdateRange(tokens);
                await _uow.SaveChangesAsync(cancellationToken);
            }

            return tokens.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Revoke all user tokens failed for {UserId}", userId);
            return Error.Failure("RefreshToken.BulkRevocationFailed", "Bulk token revocation failed");
        }
    }

    public async Task<ErrorOr<int>> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var cutoff = now.AddDays(-_options.RevokedTokenRetentionDays);

            var toDelete = await _uow.Context.RefreshTokens
                .Where(t => t.ExpiresAt < now || (t.IsRevoked && t.RevokedAt < cutoff))
                .ToListAsync(cancellationToken);

            if (toDelete.Count == 0) return 0;

            _uow.Context.RefreshTokens.RemoveRange(toDelete);
            await _uow.SaveChangesAsync(cancellationToken);
            return toDelete.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleanup expired tokens failed");
            return Error.Failure("RefreshToken.CleanupFailed", "Token cleanup failed");
        }
    }

    // ----- Private ----------------------------------------------------------

    private static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_'); // base64url
    }

    /// <summary>
    /// Revoke all descendants in a rotation chain starting from a reused token.
    /// </summary>
    private async Task RevokeDescendantChainAsync(
        RefreshToken reused,
        string reason,
        CancellationToken ct)
    {
        try
        {
            var toRevoke = new List<RefreshToken> { reused };

            // Walk forward: find child by ReplacedByTokenHash repeatedly
            var current = reused;
            while (!string.IsNullOrWhiteSpace(current.ReplacedByTokenHash))
            {
                var next = await _uow.Context.RefreshTokens
                    .FirstOrDefaultAsync(t => t.TokenHash == current.ReplacedByTokenHash, ct);

                if (next is null || next.IsRevoked)
                    break;

                toRevoke.Add(next);
                current = next;
            }

            foreach (var t in toRevoke)
                t.Revoke(reused.RevokedByIp ?? "n/a", reason);

            _uow.Context.RefreshTokens.UpdateRange(toRevoke);
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke descendant chain for reused token {TokenId}", reused.Id);
        }
    }
}
