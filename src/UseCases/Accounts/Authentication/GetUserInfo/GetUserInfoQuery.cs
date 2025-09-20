using ErrorOr;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Accounts.Authentication.GetUserInfo;

public static partial class GetUserInfoQuery
{
    public const string Name = nameof(GetUserInfoQuery);
    public const string Summary = "Get user information";
    public const string Description = "Retrieves user information from Keycloak using access token";

    public record Query(string AccessToken) : IQuery<UserInfoResult>;

    public record UserInfoResult
    {
        public string Subject { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public bool EmailVerified { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public List<string> Roles { get; init; } = new();
    }
}