namespace UseCases.Common.Security.Authentication.Tokens.Models;

public record AuthenticationResult
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTimeOffset AccessTokenExpiresAt { get; init; }
    public DateTimeOffset RefreshTokenExpiresAt { get; init; }
    public string TokenType { get; init; } = "Bearer";
    public int ExpiresIn => (int)(AccessTokenExpiresAt - DateTimeOffset.UtcNow).TotalSeconds;
}