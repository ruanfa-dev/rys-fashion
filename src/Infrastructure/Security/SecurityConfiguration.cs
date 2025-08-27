using Infrastructure.Security.Authentication;
using Infrastructure.Security.Authorization;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Security;
public static class SecurityConfiguration
{
    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Register: Authentication
        services.AddAuthenticationInternal(configuration);

        // Register: Authorization
        services.AddAuthorizationInternal(configuration);

        return services;
    }
    public static IApplicationBuilder UseSecurity(this IApplicationBuilder app)
    {
        return app;
    }
}
