using Core.Identity.Users;

using ErrorOr;

using UseCases.Common.Security.Authentication.Tokens.Models;

namespace UseCases.Common.Security.Authentication.Tokens.Services;

/// <summary>
/// High-level service for managing user authentication and session tokens.
/// Provides comprehensive token lifecycle management with security features.
/// </summary>
public interface ITokenManagementService
{
    /// <summary>
    /// Authenticates a user and generates both access and refresh tokens.
    /// </summary>
    /// <param name="user">User to authenticate</param>
    /// <param name="ipAddress">IP address of the authentication request</param>
    /// <param name="rememberMe">Whether to extend token lifetime</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication result with tokens or error</returns>
    Task<ErrorOr<AuthenticationResult>> AuthenticateAsync(
        User user,
        string ipAddress,
        bool rememberMe = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an access token using a valid refresh token.
    /// </summary>
    /// <param name="refreshToken">Current refresh token</param>
    /// <param name="ipAddress">IP address of the refresh request</param>
    /// <param name="rememberMe">Whether to extend token lifetime</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New authentication result with fresh tokens or error</returns>
    Task<ErrorOr<AuthenticationResult>> RefreshAsync(
        string refreshToken,
        string ipAddress,
        bool rememberMe = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out a user by revoking their refresh token.
    /// </summary>
    /// <param name="refreshToken">Refresh token to revoke</param>
    /// <param name="ipAddress">IP address of the logout request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or error</returns>
    Task<ErrorOr<Deleted>> LogoutAsync(
        string refreshToken,
        string ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out a user from all devices by revoking all their refresh tokens.
    /// </summary>
    /// <param name="userId">User ID to logout from all devices</param>
    /// <param name="ipAddress">IP address of the logout request</param>
    /// <param name="currentToken">Current token to optionally preserve</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of revoked tokens or error</returns>
    Task<ErrorOr<int>> LogoutFromAllDevicesAsync(
        Guid userId,
        string ipAddress,
        string? currentToken = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active sessions for a user.
    /// </summary>
    /// <param name="userId">User ID to get session count for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of active sessions or error</returns>
    Task<ErrorOr<int>> GetActiveSessionCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about all active sessions for a user.
    /// </summary>
    /// <param name="userId">User ID to get sessions for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active session information or error</returns>
    Task<ErrorOr<List<ActiveSessionResult>>> GetActiveSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific session for a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="tokenId">ID of the token/session to revoke</param>
    /// <param name="ipAddress">IP address performing the revocation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or error</returns>
    Task<ErrorOr<Success>> RevokeSessionAsync(
        Guid userId,
        Guid tokenId,
        string ipAddress,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a refresh token is currently valid.
    /// </summary>
    /// <param name="refreshToken">Refresh token to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if valid, false otherwise, or error</returns>
    Task<ErrorOr<bool>> IsTokenValidAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}