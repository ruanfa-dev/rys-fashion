using System.Security.Claims;

using Core.Identity;

using ErrorOr;

using Microsoft.IdentityModel.Tokens;

using UseCases.Common.Security.Authentication.Tokens.Models;

namespace UseCases.Common.Security.Authentication.Tokens.Services;

/// <summary>
/// Service for generating and validating JWT access tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT access token for the specified user.
    /// </summary>
    /// <param name="user">The user to generate the token for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT token result or error</returns>
    Task<ErrorOr<AccessTokenResult>> GenerateAccessTokenAsync(
        User user,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a JWT token and extracts claims.
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <param name="validateLifetime">Whether to validate token expiration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Token validation result or error</returns>
    Task<ErrorOr<TokenValidationResult>> ValidateTokenAsync(
        string token,
        bool validateLifetime = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts claims from a JWT token without validation (for expired tokens).
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>Claims from the token or error</returns>
    ErrorOr<ClaimsPrincipal> GetPrincipalFromToken(string token);

    /// <summary>
    /// Gets the remaining time until token expires.
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>Time remaining or error if token is invalid</returns>
    ErrorOr<TimeSpan> GetTokenRemainingTime(string token);
}