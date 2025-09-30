using Core.Identity.Tokens;
using Core.Identity.Users;

namespace UseCases.Common.Security.Authentication.Tokens.Models;

/// <summary>
/// Result of refresh token validation.
/// </summary>
public sealed record RefreshTokenValidationResult
{
    public RefreshToken RefreshToken { get; init; } = null!;
    public User User { get; init; } = null!;
}
