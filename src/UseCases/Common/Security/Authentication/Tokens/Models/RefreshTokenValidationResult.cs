using Core.Identity;

namespace UseCases.Common.Security.Authentication.Tokens.Models;

/// <summary>
/// Result of refresh token validation.
/// </summary>
public sealed record RefreshTokenValidationResult
{
    public Guid UserId { get; init; }
    public RefreshToken RefreshToken { get; init; } = null!;
    public User User { get; init; } = null!;
}
