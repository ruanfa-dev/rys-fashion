using Core.Identity;

using Infrastructure.BackgroundServices;
using Infrastructure.Identity;
using Infrastructure.Notification;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Infrastructure.Storage;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

using SharedKernel.Logging;

namespace Infrastructure;

public static class DependencyInjection
{
    #region Infrastructure Layer Configuration

    /// <summary>
    /// Configures all infrastructure layer services including persistence, security, notifications,
    /// background services, and storage solutions.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        Log.Information(LogTemplate.ComponentStarted, "Infrastructure layer configuration");

        // Add: Data persistence and database context
        services.AddPersistence(configuration, environment);
        Log.Information(LogTemplate.AddFeature, "Data persistence layer");

        // Add: Caching services (e.g., Redis, in-memory)
        // TODO: services.AddCaching(configuration, environment
        Log.Information(LogTemplate.AddFeature, "Caching services");

        // Add: Identity management and user services
        services.AddShopIdentityCore();
        Log.Information(LogTemplate.AddFeature, "Identity management and user services");

        // Add: Authentication and authorization services
        services.AddSecurity(configuration);
        Log.Information(LogTemplate.AddFeature, "Security and authentication");

        // Add: Notification and messaging services
        services.AddNotificationServices(configuration, environment);
        Log.Information(LogTemplate.AddFeature, "Notification and messaging");

        // Add: Background processing and scheduled tasks
        services.AddBackgroundServices(configuration, environment);
        Log.Information(LogTemplate.AddFeature, "Background services and scheduled tasks");

        // Add: File storage and blob management
        services.AddStorageServices(configuration, environment);
        Log.Information(LogTemplate.AddFeature, "Storage and file management");

        Log.Information(LogTemplate.ComponentStarted, "Infrastructure layer services registration");
        return services;
    }

    /// <summary>
    /// Configures infrastructure middleware and background services for the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseInfrastructure(
        this IApplicationBuilder app,
        IWebHostEnvironment environment)
    {
        Log.Information(LogTemplate.ComponentStarted, "Infrastructure middleware pipeline configuration");

        // Configure: Background services and hosted services
        app.UseBackgroundServices(environment);
        Log.Information(LogTemplate.ConfigureMiddleware, "Background services and scheduled tasks");

        Log.Information(LogTemplate.ComponentStarted, "Infrastructure middleware pipeline");
        return app;
    }

    #endregion
}