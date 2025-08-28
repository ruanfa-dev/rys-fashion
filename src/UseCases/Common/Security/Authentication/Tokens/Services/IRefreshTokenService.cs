using ErrorOr;

using UseCases.Common.Security.Authentication.Tokens.Models;

namespace UseCases.Common.Security.Authentication.Tokens.Services;
/// <summary>
/// Service for managing refresh tokens including generation, validation, and rotation.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Generates a new refresh token.
    /// </summary>
    /// <param name="userId">User ID for the token</param>
    /// <param name="ipAddress">IP address where token is created</param>
    /// <param name="rememberMe">Whether this is a "remember me" token with extended expiry</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated refresh token or error</returns>
    Task<ErrorOr<RefreshTokenResult>> GenerateRefreshTokenAsync(
        Guid userId,
        string ipAddress,
        bool rememberMe = false,
        bool isSystemUser = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a refresh token and returns the associated user ID.
    /// </summary>
    /// <param name="token">The refresh token to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result or error</returns>
    Task<ErrorOr<RefreshTokenValidationResult>> ValidateRefreshTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates a refresh token (revokes old, creates new).
    /// </summary>
    /// <param name="currentToken">Current refresh token</param>
    /// <param name="ipAddress">IP address performing the rotation</param>
    /// <param name="isSystemUser">Whether the token belongs to a system user</param>
    /// <param name="rememberMe">Whether this is a "remember me" token with extended expiry</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New refresh token or error</returns>
    Task<ErrorOr<RefreshTokenResult>> RotateRefreshTokenAsync(
        string currentToken,
        string ipAddress,
        bool rememberMe = false,
        bool isSystemUser = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific refresh token.
    /// </summary>
    /// <param name="token">Token to revoke</param>
    /// <param name="ipAddress">IP address performing the revocation</param>
    /// <param name="reason">Reason for revocation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or error</returns>
    Task<ErrorOr<Success>> RevokeTokenAsync(
        string token,
        string ipAddress,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens for a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ipAddress">IP address performing the revocation</param>
    /// <param name="reason">Reason for revocation</param>
    /// <param name="exceptToken">Token to exclude from revocation (current session)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of revoked tokens or error</returns>
    Task<ErrorOr<int>> RevokeAllUserTokensAsync(
        Guid userId,
        string ipAddress,
        string? reason = null,
        string? exceptToken = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired and revoked tokens from storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of tokens cleaned up or error</returns>
    Task<ErrorOr<int>> CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
}