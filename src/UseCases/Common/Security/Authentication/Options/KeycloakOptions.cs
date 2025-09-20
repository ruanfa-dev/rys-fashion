namespace UseCases.Common.Security.Authentication.Options;

/// <summary>
/// Configuration options for Keycloak integration
/// </summary>
public class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    /// <summary>
    /// Keycloak authority URL (e.g., http://localhost:8080/realms/rys-fashion)
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Client ID for the application
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret for the application
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Audience for token validation
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Realm name
    /// </summary>
    public string Realm { get; set; } = string.Empty;

    /// <summary>
    /// Admin API base URL
    /// </summary>
    public string AdminApiUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether to require HTTPS metadata
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Whether to validate audience
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Whether to validate issuer
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Clock skew tolerance
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Save tokens in authentication properties
    /// </summary>
    public bool SaveTokens { get; set; } = true;
}