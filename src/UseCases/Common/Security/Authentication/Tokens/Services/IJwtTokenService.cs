using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Core.Identity.Users;

using ErrorOr;

using UseCases.Common.Security.Authentication.Tokens.Models;

namespace UseCases.Common.Security.Authentication.Tokens.Services;

/// <summary>
/// Service for managing JWT access tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT access token for the specified user.
    /// </summary>
    /// <param name="user">User to generate token for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated access token result or error</returns>
    Task<ErrorOr<AccessTokenResult>> GenerateAccessTokenAsync(
        User user,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the principal from a JWT token without validating expiration.
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Claims principal or error</returns>
    ErrorOr<ClaimsPrincipal> GetPrincipalFromToken(string token);

    /// <summary>
    /// Gets the remaining time until token expiration.
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Time remaining or error</returns>
    ErrorOr<TimeSpan> GetTokenRemainingTime(string token);

    /// <summary>
    /// Validates the format of a JWT token without full validation.
    /// </summary>
    /// <param name="token">JWT token to validate format</param>
    /// <returns>True if format is valid, false otherwise, or error</returns>
    ErrorOr<bool> ValidateTokenFormat(string token);

    /// <summary>
    /// Parses a JWT token and returns the security token object.
    /// </summary>
    /// <param name="token">JWT token to parse</param>
    /// <returns>Parsed JWT security token or error</returns>
    ErrorOr<JwtSecurityToken> ParseToken(string token);

    /// <summary>
    /// Validates a JWT token with configurable lifetime validation.
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <param name="validateLifetime">Whether to validate token expiration</param>
    /// <returns>Validation result or error</returns>
    ErrorOr<JwtTokenValidationResult> ValidateToken(
        string token,
        bool validateLifetime = true);

    /// <summary>
    /// Extracts all claims from a JWT token.
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>Dictionary of claims or error</returns>
    ErrorOr<Dictionary<string, object>> GetTokenClaims(string token);
}