using System.Text.Json.Serialization;

namespace Infrastructure.Security.Authorization.Providers;

public record UserAuthorizationData(
    [property: JsonPropertyName("user_id")] Guid UserId,
    [property: JsonPropertyName("user_name")] string UserName,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("permissions")] IReadOnlyList<string> Permissions,
    [property: JsonPropertyName("roles")] IReadOnlyList<string> Roles,
    [property: JsonPropertyName("policies")] IReadOnlyList<string> Policies
);
