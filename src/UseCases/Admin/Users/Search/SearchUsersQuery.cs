using ErrorOr;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Users.Search;

public static partial class SearchUsersQuery
{
    public const string Name = nameof(SearchUsersQuery);
    public const string Summary = "Search users in Keycloak";
    public const string Description = "Searches users in Keycloak realm with advanced filters";

    public record Query(SearchUsersParam Param) : IQuery<SearchUsersResult>;

    public record SearchUsersParam
    {
        public string? Username { get; init; }
        public string? Email { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public bool? Enabled { get; init; }
        public bool? EmailVerified { get; init; }
        public int? First { get; init; }
        public int? Max { get; init; }
    }

    public record SearchUsersResult
    {
        public List<UserSearchItem> Users { get; init; } = new();
        public int TotalCount { get; init; }
        public bool HasMore { get; init; }
    }

    public record UserSearchItem
    {
        public string Id { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public bool Enabled { get; init; }
        public bool EmailVerified { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public List<string> Roles { get; init; } = new();
    }
}