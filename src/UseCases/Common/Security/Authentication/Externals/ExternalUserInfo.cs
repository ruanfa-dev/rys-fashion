namespace UseCases.Common.Security.Authentication.Externals;
public record ExternalUserInfo
{
    public string ProviderId { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? ProfilePictureUrl { get; init; }
    public bool EmailVerified { get; init; }
    public Dictionary<string, string> AdditionalClaims { get; init; } = new();
}
