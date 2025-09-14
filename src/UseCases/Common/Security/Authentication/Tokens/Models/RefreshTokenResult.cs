namespace UseCases.Common.Security.Authentication.Tokens.Models;

/// <summary>
/// Result of refresh token operations.
/// </summary>
public sealed record RefreshTokenResult
{
    public string Token { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
    public Guid UserId { get; init; }
    public string CreatedByIp { get; init; } = string.Empty;
    public bool RememberMe { get; init; }
}
