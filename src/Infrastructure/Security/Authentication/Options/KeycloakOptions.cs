using UseCases.Common.Security.Authentication.Options;

namespace Infrastructure.Security.Authentication.Options;

/// <summary>
/// Infrastructure-specific configuration options for Keycloak integration
/// Inherits from the base options in UseCases layer
/// </summary>
public class KeycloakOptions : UseCases.Common.Security.Authentication.Options.KeycloakOptions
{
    // Infrastructure-specific properties can be added here if needed
}