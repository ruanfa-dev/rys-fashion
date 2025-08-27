using Ardalis.GuardClauses;

using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;

using Infrastructure.BackgroundServices.Filters;
using Infrastructure.BackgroundServices.Jobs;
using Infrastructure.BackgroundServices.Options;
using Infrastructure.Persistence.Options;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Serilog;

using SharedKernel.Logging;

namespace Infrastructure.BackgroundServices;

public static class BackgroundServiceConfiguration
{
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Register: Hangfire options
        services.Configure<HangfireOptions>(configuration.GetSection(HangfireOptions.Section))
                .AddOptionsWithValidateOnStart<HangfireOptions>();

        // Configure: Hangfire based on environment
        services.AddHangfire((serviceProvider, config) =>
        {
            var hangfireOptions = serviceProvider.GetRequiredService<IOptions<HangfireOptions>>().Value;

            config.UseSimpleAssemblyNameTypeSerializer()
                  .UseRecommendedSerializerSettings();

            if (environment.IsEnvironment("Test"))
            {
                // Test: Use in-memory storage
                config.UseInMemoryStorage();
                Log.Information("Hangfire configured with In-Memory storage for Test environment");
            }
            else if (environment.IsDevelopment())
            {
                // Development: Use in-memory storage for simplicity
                config.UseInMemoryStorage();
                Log.Information("Hangfire configured with In-Memory storage for Development environment");
            }
            else
            {
                // Production/Staging: Use PostgreSQL storage
                var connectionString = configuration.GetConnectionString(DbConnectionOptions.Default);
                Guard.Against.Null(connectionString, message: "Connection string required for Hangfire in production");

                config.UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(connectionString);
                });
                Log.Information("Hangfire configured with PostgreSQL storage for {Environment} environment", environment.EnvironmentName);
            }
        });

        // Add: Hangfire server with options configuration
        services.AddHangfireServer((serviceProvider, options) =>
        {
            var hangfireOptions = serviceProvider.GetRequiredService<IOptions<HangfireOptions>>().Value;
            var serverConfig = hangfireOptions.Server;

            options.WorkerCount = serverConfig.WorkerCount;
            options.Queues = serverConfig.Queues;
            options.SchedulePollingInterval = TimeSpan.FromSeconds(serverConfig.PollingInterval);

            // Configure: job retention
            options.ServerTimeout = TimeSpan.FromMinutes(5);
            options.ShutdownTimeout = TimeSpan.FromMinutes(1);

            Log.Information("Hangfire server configured with {WorkerCount} workers and queues: {Queues}",
                serverConfig.WorkerCount, string.Join(", ", serverConfig.Queues));
        });



        Log.Information(LogTemplate.AddFeature, "Background Services");
        return services;
    }

    public static IApplicationBuilder UseBackgroundServices(this IApplicationBuilder app, IHostEnvironment environment)
    {
        var serviceProvider = app.ApplicationServices;
        var hangfireOptions = serviceProvider.GetRequiredService<IOptions<HangfireOptions>>().Value;

        // Configure: Hangfire Dashboard if enabled
        if (hangfireOptions.EnableDashboard)
        {
            app.UseHangfireDashboard(hangfireOptions.DashboardPath, new DashboardOptions
            {
                Authorization = GetDashboardAuthorization(environment),
                DashboardTitle = "Rys Fashion - Background Jobs",
                StatsPollingInterval = 2000,
                DisplayStorageConnectionString = false,
                AppPath = null // Disable back-to-app link
            });

            Log.Information("Hangfire Dashboard enabled at {DashboardPath}", hangfireOptions.DashboardPath);
        }
        else
        {
            Log.Information("Hangfire Dashboard is disabled");
        }

        // Register: jobs only in non-test environments
        if (!environment.IsEnvironment("Test"))
        {
            // Use: DI-based job registration
            var recurringJobManager = serviceProvider.GetRequiredService<IRecurringJobManager>();
            RegisterFashionEshopJobs(recurringJobManager);
        }

        return app;
    }

    private static IDashboardAuthorizationFilter[] GetDashboardAuthorization(IHostEnvironment environment)
    {
        if (environment.IsEnvironment("Test") || environment.IsDevelopment())
        {
            // No authorization in test/development
            return [];
        }
        else
        {
            // Require authorization in production/staging
            return [new HangfireAuthorizationFilter()];
        }
    }

    private static void RegisterFashionEshopJobs(IRecurringJobManager recurringJobManager)
    {
        recurringJobManager.AddOrUpdate<RefreshTokenCleanupJob>(
            recurringJobId: RefreshTokenCleanupJob.RecurringJobId,
            queue: RefreshTokenCleanupJob.Tag,
            methodCall: job => job.ExecuteAsync(CancellationToken.None),
            cronExpression: RefreshTokenCleanupJob.CronExpression,
            options: new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local
            });

        recurringJobManager.AddOrUpdate<InventoryUpdateJob>(
            recurringJobId: InventoryUpdateJob.RecurringJobId,
            queue: InventoryUpdateJob.Tag,
            methodCall: job => job.ExecuteAsync(CancellationToken.None),
            cronExpression: InventoryUpdateJob.CronExpression,
            options: new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local,
            });

        recurringJobManager.AddOrUpdate<AbandonedCartEmailJob>(
            recurringJobId: AbandonedCartEmailJob.RecurringJobId,
            queue: AbandonedCartEmailJob.Tag,
            methodCall: job => job.ExecuteAsync(CancellationToken.None),
            cronExpression: AbandonedCartEmailJob.CronExpression,
            options: new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local,
            });

        recurringJobManager.AddOrUpdate<DailySalesReportJob>(
            recurringJobId: DailySalesReportJob.RecurringJobId,
            queue: DailySalesReportJob.Tag,
            methodCall: job => job.ExecuteAsync(CancellationToken.None),
            cronExpression: DailySalesReportJob.CronExpression,
            options: new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local,
            });

        recurringJobManager.AddOrUpdate<LowStockAlertJob>(
            recurringJobId: LowStockAlertJob.RecurringJobId,
            queue: LowStockAlertJob.Tag,
            methodCall: job => job.ExecuteAsync(CancellationToken.None),
            cronExpression: LowStockAlertJob.CronExpression,
            options: new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local,
            });
    }
}
