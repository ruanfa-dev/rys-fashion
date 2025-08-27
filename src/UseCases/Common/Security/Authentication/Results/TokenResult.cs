namespace UseCases.Common.Security.Authentication.Results;

public record TokenResult
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public long ExpiresAt { get; init; }
    public long RefreshExpiresAt { get; init; }
    public string TokenType { get; init; } = "Bearer";
}