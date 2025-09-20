using ErrorOr;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Users.GetList;

public static partial class GetUsersQuery
{
    public const string Name = nameof(GetUsersQuery);
    public const string Summary = "Get users from Keycloak";
    public const string Description = "Retrieves users from Keycloak realm with optional search and pagination";

    public record Query(GetUsersParam Param) : IQuery<List<UserResponse>>;

    public record GetUsersParam
    {
        public string? Search { get; init; }
        public int? Max { get; init; }
        public int? First { get; init; }
    }

    public record UserResponse
    {
        public string Id { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public bool Enabled { get; init; }
        public bool EmailVerified { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}