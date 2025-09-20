using ErrorOr;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Roles.GetByName;

public static partial class GetRoleByNameQuery
{
    public const string Name = nameof(GetRoleByNameQuery);
    public const string Summary = "Get role by name from Keycloak";
    public const string Description = "Retrieves a specific role from Keycloak by name";

    public record Query(string RoleName) : IQuery<RoleDetailResponse>;

    public record RoleDetailResponse
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool Composite { get; init; }
        public bool ClientRole { get; init; }
        public string? ContainerId { get; init; }
    }
}