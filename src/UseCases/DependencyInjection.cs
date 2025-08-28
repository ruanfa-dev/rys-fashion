using System.Reflection;

using FluentValidation;

using Mapster;

using MapsterMapper;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Serilog;

using SharedKernel.Logging;

using UseCases.Common.Behaviors;

namespace UseCases;

public static class DependencyInjection
{
    #region Use Cases Layer Configuration

    /// <summary>
    /// Configures all use cases layer services including CQRS, validation, and object mapping.
    /// </summary>
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        Log.Information(LogTemplate.ComponentStarted, "Use Cases layer configuration");

        // Add: CQRS pattern implementation
        services.AddCqrs();

        // Add: Input validation pipeline
        services.AddValidations();

        // Add: Object-to-object mapping
        services.AddMappings();

        Log.Information(LogTemplate.ComponentStarted, "Use Cases layer services registration");
        return services;
    }
    /// <summary>
    /// Configures infrastructure middleware and background services for the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseUseCases(
        this IApplicationBuilder app)
    {
        Log.Information(LogTemplate.ComponentStarted, "Use Cases middleware pipeline");
        return app;
    }
    #endregion

    #region CQRS (Command Query Responsibility Segregation)

    /// <summary>
    /// Registers MediatR for CQRS pattern implementation with pipeline behaviors.
    /// </summary>
    public static IServiceCollection AddCqrs(this IServiceCollection services)
    {
        Log.Debug(LogTemplate.ComponentStarted, "CQRS services registration");

        var executingAssembly = Assembly.GetExecutingAssembly();

        // Register: MediatR with assembly scanning and behaviors
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(executingAssembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        Log.Information(LogTemplate.RegisterServiceWithOptions, "MediatR", new
        {
            Assembly = executingAssembly.GetName().FullName,
            BehaviorsCount = 1,
            HasValidationBehavior = true
        });

        // Register: Pipeline behavior for request validation
        Log.Debug(LogTemplate.RegisterService, $"ValidationBehavior pipeline behavior");

        Log.Information(LogTemplate.AddFeature, "CQRS pattern with MediatR");
        return services;
    }

    #endregion

    #region Input Validation

    /// <summary>
    /// Registers FluentValidation for comprehensive input validation across all use cases.
    /// </summary>
    public static IServiceCollection AddValidations(this IServiceCollection services)
    {
        Log.Debug(LogTemplate.ComponentStarted, "Validation services registration");

        var executingAssembly = Assembly.GetExecutingAssembly();

        // Register: FluentValidation validators from assembly
        services.AddValidatorsFromAssembly(executingAssembly);

        var validatorTypes = executingAssembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>)))
            .ToList();

        Log.Information(LogTemplate.RegisterServiceWithOptions, "FluentValidation", new
        {
            Assembly = executingAssembly.GetName().FullName,
            ValidatorCount = validatorTypes.Count,
            ValidatorTypes = validatorTypes.Select(t => t.Name).ToArray()
        });

        Log.Information(LogTemplate.AddFeature, "Input validation with FluentValidation");
        return services;
    }

    #endregion

    #region Object Mapping

    /// <summary>
    /// Registers Mapster for high-performance object-to-object mapping with global configuration.
    /// </summary>
    public static IServiceCollection AddMappings(this IServiceCollection services)
    {
        Log.Debug(LogTemplate.ComponentStarted, "Object mapping services registration");

        var executingAssembly = Assembly.GetExecutingAssembly();

        // Register: Mapster global configuration
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(executingAssembly);

        // Register: Global mapping configuration as singleton
        services.AddSingleton(config);
        Log.Debug(LogTemplate.RegisterServiceWithLifetime, "TypeAdapterConfig", ServiceLifetime.Singleton);

        // Register: Scoped mapper service for dependency injection
        services.AddScoped<IMapper, ServiceMapper>();
        Log.Debug(LogTemplate.RegisterServiceWithLifetime, nameof(IMapper), ServiceLifetime.Scoped);

        // Get mapping statistics for logging
        var mappingCount = config.RuleMap?.Count ?? 0;

        Log.Information(LogTemplate.RegisterServiceWithOptions, "Mapster", new
        {
            Assembly = executingAssembly.GetName().Name,
            ConfigurationType = "Global",
            MappingRulesCount = mappingCount,
            MapperLifetime = "Scoped"
        });

        Log.Information(LogTemplate.AddFeature, "Object mapping with Mapster");
        return services;
    }

    #endregion
}