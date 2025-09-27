using ErrorOr;

using UseCases.Common.Security.Authentication.Tokens.Models;

namespace UseCases.Common.Security.Authentication.Tokens.Services;

/// <summary>
/// Service for managing refresh tokens including generation, validation, rotation and revocation.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Generates a new refresh token for the specified user.
    /// </summary>
    /// <param name="userId">User ID for the token.</param>
    /// <param name="ipAddress">IP address where the token is created.</param>
    /// <param name="rememberMe">If true, uses extended expiry for "remember me".</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Generated refresh token result or error.</returns>
    Task<ErrorOr<RefreshTokenResult>> GenerateRefreshTokenAsync(
        Guid userId,
        string ipAddress,
        bool rememberMe = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a refresh token and returns the stored token and associated user when valid.
    /// </summary>
    /// <param name="token">Raw refresh token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result containing the stored token and user or an error.</returns>
    Task<ErrorOr<RefreshTokenValidationResult>> ValidateRefreshTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates a refresh token: revokes the provided token and issues a new one.
    /// </summary>
    /// <param name="rawCurrentToken">The current raw refresh token to rotate.</param>
    /// <param name="ipAddress">IP address performing the rotation.</param>
    /// <param name="rememberMe">If true, uses extended expiry for the new token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New refresh token result or error.</returns>
    Task<ErrorOr<RefreshTokenResult>> RotateRefreshTokenAsync(
        string rawCurrentToken,
        string ipAddress,
        bool rememberMe = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific refresh token.
    /// </summary>
    /// <param name="rawToken">Raw token to revoke.</param>
    /// <param name="ipAddress">IP address performing the revocation.</param>
    /// <param name="reason">Optional reason for revocation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or error.</returns>
    Task<ErrorOr<Success>> RevokeTokenAsync(
        string rawToken,
        string ipAddress,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all non-revoked refresh tokens for a user.
    /// </summary>
    /// <param name="userId">User ID whose tokens should be revoked.</param>
    /// <param name="ipAddress">IP address performing the revocation.</param>
    /// <param name="reason">Optional reason for revocation.</param>
    /// <param name="exceptToken">Optional raw refresh token to exclude from revocation (keep session).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of revoked tokens or error.</returns>
    Task<ErrorOr<int>> RevokeAllUserTokensAsync(
        Guid userId,
        string ipAddress,
        string? reason = null,
        string? exceptToken = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired and aged revoked tokens from storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of tokens removed or error.</returns>
    Task<ErrorOr<int>> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
}