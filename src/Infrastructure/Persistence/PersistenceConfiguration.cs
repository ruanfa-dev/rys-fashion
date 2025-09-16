using Ardalis.GuardClauses;

using Infrastructure.Persistence.Constants;
using Infrastructure.Persistence.Contexts;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Options;
using Infrastructure.Persistence.Seeders;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;

using StackExchange.Redis;

using UseCases.Common.Persistence.Context;

namespace Infrastructure.Persistence;

public static class PersistenceConfiguration
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Register: Interceptors into DI
        services.AddInterceptors();

        // Register: DbContext into DI
        services.AddDatabase(configuration, environment);

        // Register: Cache services into DI
        services.AddCaching(configuration, environment);

        // Register: Unit of Work into DI
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register: Seeders into DI
        services.AddSeeders();

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Get connection string
        var connectionString = configuration.GetConnectionString(DbConnectionOptions.Default);
        Guard.Against.Null(connectionString, message: "Connection string 'DefaultConnection' not found.");

        // Register: DbContext into DI with environment-specific configuration
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

            if (environment.IsEnvironment("Test"))
            {
                // Test: Use in-memory database for fast testing
                options.UseInMemoryDatabase("TestDb")
                       .EnableDetailedErrors()
                       .EnableSensitiveDataLogging();
                Log.Information("Using In-Memory database for Test environment");
            }
            else if (environment.IsDevelopment())
            {
                // Development: Use PostgreSQL with detailed logging
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schema.Default);
                    npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                })
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .UseSnakeCaseNamingConvention()
                .LogTo(Console.WriteLine, LogLevel.Information);

                // Temporarily: using In-Memory for Development as well
                //options.UseInMemoryDatabase("TestDb")
                //.EnableSensitiveDataLogging()
                //.EnableDetailedErrors()
                //.UseSnakeCaseNamingConvention()
                //.LogTo(Console.WriteLine, LogLevel.Information);
                Log.Information("Using PostgreSQL for Development environment");
            }
            else
            {
                // Production/Staging: Use PostgreSQL with minimal logging
                options.UseNpgsql(connectionString)
                       .UseSnakeCaseNamingConvention();
                Log.Information("Using PostgreSQL for {Environment} environment", environment.EnvironmentName);
            }
        });

        // Register: ApplicationDbContext as IApplicationDbContext into DI
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    private static IServiceCollection AddInterceptors(this IServiceCollection services)
    {
        // Register: Interceptors into DI
        services.AddScoped<ISaveChangesInterceptor, ActionTrackingEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        return services;
    }

    private static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsEnvironment("Test") || environment.IsDevelopment())
        {
            // Test/Dev: Use in-memory distributed cache
            services.AddDistributedMemoryCache();
            Log.Information("Using in-memory distributed cache for {Environment} environment", environment.EnvironmentName);
        }
        else
        {
            // Production/Staging: Use Redis distributed cache
            var redisConnection = configuration.GetValue<string>("Cache:RedisConnection");
            Guard.Against.NullOrWhiteSpace(redisConnection, message: "Cache:RedisConnection not found or empty.");

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.ConfigurationOptions = new ConfigurationOptions
                {
                    AbortOnConnectFail = false, // Prevent app failure if Redis is down
                    ConnectRetry = 3,
                    ConnectTimeout = 5000,
                    SyncTimeout = 5000
                };
            });
            Log.Information("Using Redis distributed cache for {Environment} environment", environment.EnvironmentName);
        }

        return services;
    }

    private static IServiceCollection AddSeeders(this IServiceCollection services)
    {
        // Register: seeders
        services.AddTransient<IDataSeeder, IdentitySeedProvider>();

        // Register orchestrator as hosted service
        services.AddHostedService<SeedOrchestrator>();

        return services;
    }
}