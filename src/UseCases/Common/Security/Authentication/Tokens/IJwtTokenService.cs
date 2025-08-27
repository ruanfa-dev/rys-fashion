using System.Security.Claims;

using ErrorOr;

namespace UseCases.Common.Security.Authentication.Tokens;

public interface IJwtTokenService
{
    Task<(string Token, DateTimeOffset ExpiresAt)> GenerateAccessTokenAsync(
        string userId,
        string email,
        string userName,
        IEnumerable<string> roles,
        IEnumerable<string> permissions);

    Task<(string Token, DateTimeOffset ExpiresAt)> GenerateRefreshTokenAsync(string userId, bool rememberMe = false);

    Task<ErrorOr<ClaimsPrincipal?>> ValidateTokenAsync(string token, bool validateLifetime = true);
}
