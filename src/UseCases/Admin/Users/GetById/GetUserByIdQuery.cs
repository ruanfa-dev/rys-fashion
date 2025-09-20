using ErrorOr;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Users.GetById;

public static partial class GetUserByIdQuery
{
    public const string Name = nameof(GetUserByIdQuery);
    public const string Summary = "Get user by ID from Keycloak";
    public const string Description = "Retrieves a specific user from Keycloak by their ID including roles";

    public record Query(string Id) : IQuery<UserDetailResponse>;

    public record UserDetailResponse
    {
        public string Id { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public bool Enabled { get; init; }
        public bool EmailVerified { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public List<string> Roles { get; set; } = new();
        public Dictionary<string, object[]>? Attributes { get; init; }
    }
}