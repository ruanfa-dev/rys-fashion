using Carter;

using Serilog;

using SharedKernel.Logging;

using Web.Api.Infrastructure.Configuration;
using Web.Api.Infrastructure.Middleware;

namespace Web.Api.Infrastructure;

public static class DependencyInjection
{
    #region Presentation Layer Configuration

    /// <summary>
    /// Configures all presentation layer services including controllers, endpoints, error handling, and CORS.
    /// </summary>
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Information(LogTemplate.ComponentStarted, "Presentation layer configuration");

        // Add: Controllers and API endpoints
        services.AddControllers();
        services.AddCarter();
        Log.Information(LogTemplate.AddFeature, "MVC Controllers and Carter endpoints");

        // Add: JSON serialization configuration
        services.AddJsonConfig();
        Log.Information(LogTemplate.AddFeature, "JSON serialization configuration");

        // Add: API documentation and exploration
        services.AddEndpointsExplorer();
        Log.Information(LogTemplate.AddFeature, "API documentation explorer");

        // Add: Global error handling
        services.AddErrorHandlers();
        Log.Information(LogTemplate.AddFeature, "Global error handling");

        // Add: Session management and CORS policies
        services.AddSessionAndCors(configuration);
        Log.Information(LogTemplate.AddFeatureWithConfig, "Session and CORS policies", new { HasConfiguration = configuration != null });

        Log.Information(LogTemplate.ComponentStarted, "Presentation layer services registration");
        return services;
    }

    /// <summary>
    /// Configures the middleware pipeline for the presentation layer in the correct order.
    /// </summary>
    public static IApplicationBuilder UsePresentation(this WebApplication app)
    {
        Log.Information(LogTemplate.ComponentStarted, "Presentation middleware pipeline configuration");

        // Configure: Development-only API documentation
        if (app.Environment.IsDevelopment())
        {
            app.UseEndpointsExplorer();
            Log.Information(LogTemplate.ConfigureMiddleware, "API documentation (Development environment)");
        }

        // Configure: Security and routing middleware (order is critical)
        app.UseHttpsRedirection();
        Log.Information(LogTemplate.ConfigureMiddleware, "HTTPS redirection");

        app.UseRouting();
        Log.Information(LogTemplate.ConfigureMiddleware, "Request routing");

        app.UseStaticFiles();
        Log.Information(LogTemplate.ConfigureMiddleware, "Static file serving");

        // Configure: Session and CORS (before authentication)
        app.UseSession();
        Log.Debug(LogTemplate.UseMiddleware, "Session management");

        app.UseCors();
        Log.Debug(LogTemplate.UseMiddleware, "CORS policy enforcement");

        // Configure: Error handling (early in pipeline)
        app.UseErrorHandlers();
        Log.Information(LogTemplate.ConfigureMiddleware, "Global error handling");

        // Configure: Authentication and authorization (order matters)
        app.UseAuthentication();
        Log.Debug(LogTemplate.UseMiddleware, "Request authentication");

        app.UseAuthorization();
        Log.Debug(LogTemplate.UseMiddleware, "Request authorization");

        // Configure: Endpoint mapping
        app.MapControllers();
        app.MapCarter();
        Log.Information(LogTemplate.ConfigureMiddleware, "Controller and Carter endpoint mapping");

        Log.Information(LogTemplate.ComponentStarted, "Presentation middleware pipeline");
        return app;
    }

    #endregion

    #region Error Handling Services

    /// <summary>
    /// Registers global exception handling and problem details services.
    /// </summary>
    public static IServiceCollection AddErrorHandlers(this IServiceCollection services)
    {
        Log.Debug(LogTemplate.ComponentStarted, "Error handling services registration");

        // Register: Global exception interceptor
        services.AddExceptionHandler<GlobalExceptionHandler>();
        Log.Debug(LogTemplate.RegisterService, nameof(GlobalExceptionHandler));

        // Register: Standardized problem details formatter
        services.AddProblemDetails(ConfigureProblemDetails);
        Log.Debug(LogTemplate.RegisterServiceWithOptions, "ProblemDetails", new { HasCustomConfiguration = true });

        Log.Information(LogTemplate.AddFeature, "Global error handling services");
        return services;
    }

    /// <summary>
    /// Configures exception handling middleware in the request pipeline.
    /// </summary>
    public static IApplicationBuilder UseErrorHandlers(this WebApplication app)
    {
        // Use: Global exception handling middleware
        app.UseExceptionHandler();
        Log.Debug(LogTemplate.UseMiddleware, "Global exception handler");

        return app;
    }

    /// <summary>
    /// Customizes problem details to include tracing and diagnostic information.
    /// </summary>
    private static void ConfigureProblemDetails(ProblemDetailsOptions options)
    {
        options.CustomizeProblemDetails = context =>
        {
            var traceId = context.HttpContext.TraceIdentifier;
            var userAgent = context.HttpContext.Request.Headers.UserAgent.ToString();

            context.ProblemDetails.Extensions["TraceId"] = traceId;
            context.ProblemDetails.Extensions["UserAgent"] = userAgent;

            Log.Debug("Enhanced problem details with TraceId: {TraceId}", traceId);
        };

        Log.Debug(LogTemplate.ConfigureMiddlewareWithOptions, "ProblemDetails", new { IncludesTracing = true, IncludesUserAgent = true });
    }

    #endregion

    #region Session and CORS Configuration

    /// <summary>
    /// Configures session management and Cross-Origin Resource Sharing (CORS) policies.
    /// </summary>
    public static IServiceCollection AddSessionAndCors(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Debug(LogTemplate.ComponentStarted, "Session and CORS services registration");

        // Register: CORS policies
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });
        Log.Information(LogTemplate.RegisterServiceWithOptions, "CORS policies", new { AllowAnyOrigin = true, AllowAnyMethod = true, AllowAnyHeader = true });

        // Register: Session configuration with security settings
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
        Log.Information(LogTemplate.RegisterServiceWithOptions, "Session management", new
        {
            IdleTimeoutMinutes = 30,
            HttpOnlyCookie = true,
            SameSiteMode = "Strict"
        });

        Log.Information(LogTemplate.AddFeature, "Session and CORS configuration");
        return services;
    }

    #endregion

    #region API Documentation and Exploration

    /// <summary>
    /// Registers API documentation services including OpenAPI, Swagger, and Scalar UI.
    /// </summary>
    public static IServiceCollection AddEndpointsExplorer(this IServiceCollection services)
    {
        Log.Debug(LogTemplate.ComponentStarted, "API documentation services registration");

        // Register: OpenAPI specification generator
        services.AddEndpointsApiExplorer();
        Log.Debug(LogTemplate.RegisterService, "EndpointsApiExplorer");

        // Register: OpenAPI with authentication support
        services.AddOpenApiWithAuth();
        Log.Debug(LogTemplate.RegisterService, "OpenAPI with authentication");

        // Register: Swagger documentation with authentication
        services.AddSwaggerWithAuth();
        Log.Debug(LogTemplate.RegisterService, "Swagger with authentication");

        Log.Information(LogTemplate.AddFeature, "API documentation and exploration tools");
        return services;
    }

    /// <summary>
    /// Configures API documentation middleware for development environments.
    /// </summary>
    public static IApplicationBuilder UseEndpointsExplorer(this WebApplication app)
    {
        Log.Debug(LogTemplate.ComponentStarted, "API documentation middleware configuration");

        // Configure: OpenAPI endpoint mapping
        app.MapOpenApi();
        Log.Debug(LogTemplate.ConfigureMiddleware, "OpenAPI endpoint mapping");

        // Configure: Swagger UI interface
        app.UseSwaggerWithUi();
        Log.Debug(LogTemplate.UseMiddleware, "Swagger UI interface");

        // Configure: Scalar API documentation UI
        app.UseScalarWithUi();
        Log.Debug(LogTemplate.UseMiddleware, "Scalar API documentation UI");

        Log.Information(LogTemplate.ConfigureMiddleware, "API documentation interfaces");
        return app;
    }

    #endregion

    #region Health and Diagnostics

    /// <summary>
    /// Registers health check services for monitoring application components.
    /// Example of additional services that could be added to improve observability.
    /// </summary>
    public static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Debug(LogTemplate.ComponentStarted, "Health check services registration");

        services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
            .AddCheck("database", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy()); 

        Log.Information(LogTemplate.AddFeature, "Application health monitoring");
        return services;
    }

    /// <summary>
    /// Configures health check endpoints for monitoring.
    /// </summary>
    public static IApplicationBuilder UseHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        Log.Information(LogTemplate.ConfigureMiddleware, "Health check endpoints at /health");

        return app;
    }

    #endregion
}