using Core.Identity;

using ErrorOr;

using UseCases.Common.Security.Authentication.Tokens.Models;

namespace UseCases.Common.Security.Authentication.Tokens.Services;
/// <summary>
/// Combined service for complete token management operations.
/// </summary>
public interface ITokenManagementService
{
    /// <summary>
    /// Performs complete login flow: generates access and refresh tokens.
    /// </summary>
    /// <param name="user">The authenticated user</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="rememberMe">Remember me option</param>
    /// <param name="isSystemUser">Is system user</param>"
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete authentication result or error</returns>
    Task<ErrorOr<AuthenticationResult>> AuthenticateAsync(
        User user,
        string ipAddress,
        bool rememberMe = false,
        bool isSystemUser = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes access token using refresh token.
    /// </summary>
    /// <param name="refreshToken">Current refresh token</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="isSystemUser">Is system user</param>
    /// <param name="rememberMe">Remember me option</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New authentication result or error</returns>
    Task<ErrorOr<AuthenticationResult>> RefreshAsync(
        string refreshToken,
        string ipAddress,
        bool rememberMe = false,
        bool isSystemUser = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs complete logout: revokes refresh token.
    /// </summary>
    /// <param name="refreshToken">Refresh token to revoke</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or error</returns>
    Task<ErrorOr<Success>> LogoutAsync(
        string refreshToken,
        string ipAddress,
        string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out from all devices: revokes all user tokens.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="currentToken">Current session token to exclude</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of revoked tokens or error</returns>
    Task<ErrorOr<int>> LogoutFromAllDevicesAsync(
        Guid userId,
        string ipAddress,
        string? currentToken = null,
        CancellationToken cancellationToken = default);
}