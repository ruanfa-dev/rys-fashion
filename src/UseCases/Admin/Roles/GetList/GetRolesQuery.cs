using ErrorOr;

using SharedKernel.Messaging.Abstracts;

namespace UseCases.Admin.Roles.GetList;

public static partial class GetRolesQuery
{
    public const string Name = nameof(GetRolesQuery);
    public const string Summary = "Get roles from Keycloak";
    public const string Description = "Retrieves all roles from Keycloak realm";

    public record Query : IQuery<List<RoleResponse>>;

    public record RoleResponse
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public bool Composite { get; init; }
        public bool ClientRole { get; init; }
    }
}