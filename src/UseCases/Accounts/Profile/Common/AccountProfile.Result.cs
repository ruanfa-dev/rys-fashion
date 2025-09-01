namespace UseCases.Accounts.Profile.Common;

public record AccountProfileResult: AccountProfileParam
{
    public Guid Id { get; set; }
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }

    public DateTimeOffset? LastSignInAt { get; set; }
    public string? LastSignInIp { get; set; }
}
