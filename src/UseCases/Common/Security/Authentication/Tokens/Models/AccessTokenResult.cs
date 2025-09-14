namespace UseCases.Common.Security.Authentication.Tokens.Models;

public sealed record AccessTokenResult
{
    public string Token { get; init; } = string.Empty;
    public long ExpiresAt { get; init; }
}
