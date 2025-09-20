namespace UseCases.Common.Security.Authentication.Models;

#region Authentication Models

/// <summary>
/// Request model for obtaining access tokens from Keycloak
/// </summary>
public record TokenRequest
{
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? RefreshToken { get; init; }
    public string GrantType { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string? ClientSecret { get; init; }
    public string? Scope { get; init; }
}

/// <summary>
/// Response model for token requests from Keycloak
/// </summary>
public record TokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string? RefreshToken { get; init; }
    public string TokenType { get; init; } = "Bearer";
    public int ExpiresIn { get; init; }
    public int RefreshExpiresIn { get; init; }
    public string? Scope { get; init; }
}

/// <summary>
/// User information from Keycloak user info endpoint
/// </summary>
public record UserInfo
{
    public string Subject { get; init; } = string.Empty;
    public string PreferredUsername { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool EmailVerified { get; init; }
    public string GivenName { get; init; } = string.Empty;
    public string FamilyName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public RealmAccess? RealmAccess { get; init; }
}

/// <summary>
/// Realm access information containing user roles
/// </summary>
public record RealmAccess
{
    public List<string> Roles { get; init; } = new();
}

#endregion

#region User Management Models

/// <summary>
/// Keycloak user representation
/// </summary>
public record KeycloakUser
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public bool EmailVerified { get; init; }
    public long CreatedTimestamp { get; init; }
    public Dictionary<string, object[]>? Attributes { get; init; }
    public List<string>? RequiredActions { get; init; }
    public Dictionary<string, bool>? Access { get; init; }
}

/// <summary>
/// Request model for creating a user in Keycloak
/// </summary>
public record CreateKeycloakUserRequest
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    public bool EmailVerified { get; init; } = false;
    public Dictionary<string, object[]>? Attributes { get; init; }
    public List<string>? RequiredActions { get; init; }
}

/// <summary>
/// Request model for updating a user in Keycloak
/// </summary>
public record UpdateKeycloakUserRequest
{
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool? Enabled { get; init; }
    public bool? EmailVerified { get; init; }
    public Dictionary<string, object[]>? Attributes { get; init; }
    public List<string>? RequiredActions { get; init; }
}

#endregion

#region Role Management Models

/// <summary>
/// Keycloak role representation
/// </summary>
public record KeycloakRole
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool Composite { get; init; }
    public bool ClientRole { get; init; }
    public string? ContainerId { get; init; }
    public Dictionary<string, object[]>? Attributes { get; init; }
}

/// <summary>
/// Request model for creating a role in Keycloak
/// </summary>
public record CreateKeycloakRoleRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool Composite { get; init; } = false;
    public Dictionary<string, object[]>? Attributes { get; init; }
}

/// <summary>
/// Request model for updating a role in Keycloak
/// </summary>
public record UpdateKeycloakRoleRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool? Composite { get; init; }
    public Dictionary<string, object[]>? Attributes { get; init; }
}

#endregion

#region Group Management Models

/// <summary>
/// Keycloak group representation
/// </summary>
public record KeycloakGroup
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public Dictionary<string, List<string>>? Attributes { get; init; }
    public List<KeycloakGroup>? SubGroups { get; init; }
    public List<KeycloakRole>? RealmRoles { get; init; }
    public Dictionary<string, List<KeycloakRole>>? ClientRoles { get; init; }
}

/// <summary>
/// Request model for creating a group in Keycloak
/// </summary>
public record CreateKeycloakGroupRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Path { get; init; }
    public Dictionary<string, List<string>>? Attributes { get; init; }
    public List<string>? RealmRoles { get; init; }
}

/// <summary>
/// Request model for updating a group in Keycloak
/// </summary>
public record UpdateKeycloakGroupRequest
{
    public string Name { get; init; } = string.Empty;
    public Dictionary<string, List<string>>? Attributes { get; init; }
    public List<string>? RealmRoles { get; init; }
}

#endregion

#region Session Management Models

/// <summary>
/// Keycloak user session representation
/// </summary>
public record KeycloakSession
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public long Start { get; init; }
    public long LastAccess { get; init; }
    public Dictionary<string, string>? Clients { get; init; }
}

#endregion

#region Client Management Models

/// <summary>
/// Keycloak client representation
/// </summary>
public record KeycloakClient
{
    public string Id { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool Enabled { get; init; }
    public bool ServiceAccountsEnabled { get; init; }
    public bool PublicClient { get; init; }
    public List<string>? RedirectUris { get; init; }
    public string? Protocol { get; init; }
    public Dictionary<string, object>? Attributes { get; init; }
}

#endregion

#region Statistics Models

/// <summary>
/// Keycloak realm statistics
/// </summary>
public record KeycloakRealmStats
{
    public int TotalUsers { get; init; }
    public int EnabledUsers { get; init; }
    public int DisabledUsers { get; init; }
    public int TotalRoles { get; init; }
    public int TotalGroups { get; init; }
    public int TotalClients { get; init; }
    public int ActiveSessions { get; init; }
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
}

#endregion