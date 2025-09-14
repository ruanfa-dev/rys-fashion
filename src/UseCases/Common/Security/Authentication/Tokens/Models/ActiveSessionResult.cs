namespace UseCases.Common.Security.Authentication.Tokens.Models;

/// <summary>
/// Information about an active user session.
/// </summary>
public record ActiveSessionResult
{
    public Guid TokenId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public string CreatedByIp { get; init; } = string.Empty;
    public bool IsCurrentSession { get; init; }
    public TimeSpan RemainingTime => ExpiresAt > DateTimeOffset.UtcNow 
        ? ExpiresAt - DateTimeOffset.UtcNow 
        : TimeSpan.Zero;
}