namespace UseCases.Accounts.Authentication.Sessions;

public sealed record AccountSessionResult
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public bool IsAuthenticated { get; init; }
    public DateTimeOffset SessionStarted { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}