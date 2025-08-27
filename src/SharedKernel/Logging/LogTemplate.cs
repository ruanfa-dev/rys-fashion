namespace SharedKernel.Logging;

/// <summary>
/// Provides predefined log message templates for consistent structured logging across
/// the application lifecycle using Serilog best practices.
/// <para>
/// <b>Template Design Principles:</b>
/// <list type="bullet">
/// <item><description>Uses PascalCase for property names (Serilog convention)</description></item>
/// <item><description>Includes LogLevel guidance for appropriate usage</description></item>
/// <item><description>Provides both simple and detailed variants</description></item>
/// <item><description>Uses destructuring (@) for complex objects</description></item>
/// <item><description>Includes timing information where relevant</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Usage Categories:</b>
/// <list type="bullet">
/// <item><description><b>Register</b> → Registering services into DI container (Debug/Information level)</description></item>
/// <item><description><b>Add</b> → Enabling features or modules (Information level)</description></item>
/// <item><description><b>Configure</b> → Configuring middleware pipeline (Information level)</description></item>
/// <item><description><b>Use</b> → Attaching middleware to request pipeline (Debug level)</description></item>
/// <item><description><b>Lifecycle</b> → Startup/shutdown events (Information level)</description></item>
/// <item><description><b>Performance</b> → Timing and performance metrics (Information/Warning level)</description></item>
/// <item><description><b>Error</b> → Error conditions and failures (Warning/Error level)</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Example Usage:</b>
/// <code>
/// // Service Registration (Debug level - detailed during development)
/// Log.Debug(LogTemplate.RegisterService, nameof(IUserService));
/// Log.Information(LogTemplate.RegisterServiceWithLifetime, nameof(IUserService), ServiceLifetime.Scoped);
/// 
/// // Feature Addition (Information level - important for operations)
/// Log.Information(LogTemplate.AddFeature, "Authentication");
/// Log.Information(LogTemplate.AddFeatureWithConfig, "JWT Authentication", new { Issuer = "MyApp", ExpirationMinutes = 60 });
/// 
/// // Middleware Configuration (Information level)
/// Log.Information(LogTemplate.ConfigureMiddleware, "CORS Policy");
/// 
/// // Middleware Usage (Debug level - verbose during request processing)
/// Log.Debug(LogTemplate.UseMiddleware, "Authentication Middleware");
/// 
/// // Lifecycle Events (Information level)
/// Log.Information(LogTemplate.ComponentStarted, "Application Host");
/// Log.Information(LogTemplate.ComponentStopped, "Background Service", TimeSpan.FromSeconds(5));
/// 
/// // Performance Monitoring
/// Log.Information(LogTemplate.OperationCompleted, "Database Migration", TimeSpan.FromSeconds(30));
/// Log.Warning(LogTemplate.SlowOperation, "User Query", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(2));
/// 
/// // Error Handling
/// Log.Error(LogTemplate.ComponentStartupFailed, "Email Service", "SMTP connection timeout");
/// </code>
/// </para>
/// </summary>
public static class LogTemplate
{
    #region Service Registration Templates

    /// <summary>
    /// Template for registering a service into DI container.
    /// <para><b>Recommended Level:</b> Debug</para>
    /// </summary>
    public const string RegisterService = "Registered service {ServiceName}";

    /// <summary>
    /// Template for registering a service with specific lifetime.
    /// <para><b>Recommended Level:</b> Information</para>
    /// </summary>
    public const string RegisterServiceWithLifetime = "Registered service {ServiceName} with {Lifetime} lifetime";

    /// <summary>
    /// Template for registering a service with configuration options.
    /// <para><b>Recommended Level:</b> Information</para>
    /// </summary>
    public const string RegisterServiceWithOptions = "Registered service {ServiceName} with options {@Options}";

    /// <summary>
    /// Template for registering multiple services.
    /// <para><b>Recommended Level:</b> Information</para>
    /// </summary>
    public const string RegisterServices = "Registered {ServiceCount} services for {ModuleName}";

    #endregion

    #region Feature Addition Templates

    /// <summary>
    /// Template for adding a high-level feature or module.
    /// <para><b>Recommended Level:</b> Information</para>
    /// </summary>
    public const string AddFeature = "Added feature: {FeatureName}";

    /// <summary>
    /// Template for adding a feature with configuration details.
    /// <para><b>Recommended Level:</b> Information</para>
    /// </summary>
    public const string AddFeatureWithConfig = "Added feature: {FeatureName} with configuration {@Configuration}";

    /// <summary>
    /// Template for adding a feature with assembly information.
    /// <para><b>Recommended Level:</b> Debug</para>
    /// </summary>
    public const string AddFeatureFromAssembly = "Added feature: {FeatureName} from assembly {AssemblyName}";

    #endregion

    #region Middleware Configuration Templates

    /// <summary>
    /// Template for configuring middleware in the pipeline.
    /// <para><b>Recommended Level:</b> Information</para>
    /// </summary>
    public const string ConfigureMiddleware = "Configured middleware: {MiddlewareName}";

    /// <summary>
    /// Template for configuring middleware with options.
    /// <para><b>Recommended Level:</b> Information</para>
    /// </summary>
    public const string ConfigureMiddlewareWithOptions = "Configured middleware: {MiddlewareName} with options {@Options}";

    /// <summary>
    /// Template for configuring middleware at specific pipeline position.
    /// <para><b>Recommended Level:</b> Debug</para>
    /// </summary>
    public const string ConfigureMiddlewareAtPosition = "Configured middleware: {MiddlewareName} at position {Position}";

    #endregion

    #region Middleware Usage Templates

    /// <summary>
    /// Template for using middleware in the request pipeline.
    /// <para><b>Recommended Level:</b> Debug</para>
    /// </summary>
    public const string UseMiddleware = "Using middleware: {MiddlewareName}";

    /// <summary>
    /// Template for using middleware with specific route pattern.
    /// <para><b>Recommended Level:</b> Debug</para>
    /// </summary>
    public const string UseMiddlewareWithPattern = "Using middleware: {MiddlewareName} for pattern {RoutePattern}";

    /// <summary>
    /// Template for conditional middleware usage.
    /// <para><b>Recommended Level:</b> Debug</para>
    /// </summary>
    public const string UseMiddlewareConditional = "Using middleware: {MiddlewareName} when {Condition}";

    #endregion

    #region Component Lifecycle Templates

    /// <summary>
    /// Template for successful component startup.
    /// <para><b>Recommended Level:</b> Information</para>
    /// </summary>
    public const string ComponentStarted = "{ComponentName} started successfully";

    /// <summary>
    /// Template for component startup with timing information.
    /// <para><b>Recommended Level:</b> Information</para>
    /// </summary>
    public const string ComponentStartedWithTiming = "{ComponentName} started successfully in {StartupDuration}";

    /// <summary>
    /// Template for component shutdown.
    /// <para><b>Recommended Level:</b> Information</para>
    /// </summary>
    public const string ComponentStopped = "{ComponentName} stopped successfully";

    /// <summary>
    /// Template for component shutdown with timing.
    /// <para><b>Recommended Level:</b> Information</para>
    /// </summary>
    public const string ComponentStoppedWithTiming = "{ComponentName} stopped successfully in {ShutdownDuration}";

    /// <summary>
    /// Template for operation completion.
    /// <para><b>Recommended Level:</b> Information</para>
    /// </summary>
    public const string OperationCompleted = "{OperationName} completed successfully in {Duration}";

    /// <summary>
    /// Template for component readiness check.
    /// <para><b>Recommended Level:</b> Information</para>
    /// </summary>
    public const string ComponentReady = "{ComponentName} is ready to accept requests";

    #endregion

    #region Performance Monitoring Templates

    /// <summary>
    /// Template for operations that exceed expected duration.
    /// <para><b>Recommended Level:</b> Warning</para>
    /// </summary>
    public const string SlowOperation = "{OperationName} completed in {ActualDuration}, expected under {ExpectedDuration}";

    /// <summary>
    /// Template for high resource usage warnings.
    /// <para><b>Recommended Level:</b> Warning</para>
    /// </summary>
    public const string HighResourceUsage = "{ComponentName} resource usage: {ResourceType} at {Usage}% (threshold: {Threshold}%)";

    /// <summary>
    /// Template for throughput metrics.
    /// <para><b>Recommended Level:</b> Information</para>
    /// </summary>
    public const string ThroughputMetric = "{ComponentName} processed {RequestCount} requests in {Duration} ({RequestsPerSecond:F2} req/sec)";

    #endregion

    #region Error and Warning Templates

    /// <summary>
    /// Template for component startup failures.
    /// <para><b>Recommended Level:</b> Error</para>
    /// </summary>
    public const string ComponentStartupFailed = "{ComponentName} failed to start: {ErrorMessage}";

    /// <summary>
    /// Template for component shutdown failures.
    /// <para><b>Recommended Level:</b> Error</para>
    /// </summary>
    public const string ComponentShutdownFailed = "{ComponentName} failed to shutdown gracefully: {ErrorMessage}";

    /// <summary>
    /// Template for configuration validation errors.
    /// <para><b>Recommended Level:</b> Error</para>
    /// </summary>
    public const string ConfigurationValidationFailed = "Configuration validation failed for {ComponentName}: {ValidationErrors}";

    /// <summary>
    /// Template for dependency check failures.
    /// <para><b>Recommended Level:</b> Warning</para>
    /// </summary>
    public const string DependencyCheckFailed = "Dependency check failed for {ComponentName}: {DependencyName} is not available";

    /// <summary>
    /// Template for service registration failures.
    /// <para><b>Recommended Level:</b> Error</para>
    /// </summary>
    public const string ServiceRegistrationFailed = "Failed to register service {ServiceName}: {ErrorMessage}";

    #endregion

    #region Health Check Templates

    /// <summary>
    /// Template for health check results.
    /// <para><b>Recommended Level:</b> Information (Healthy), Warning (Degraded), Error (Unhealthy)</para>
    /// </summary>
    public const string HealthCheckResult = "Health check {HealthCheckName}: {Status}";

    /// <summary>
    /// Template for detailed health check results.
    /// <para><b>Recommended Level:</b> Debug</para>
    /// </summary>
    public const string HealthCheckResultDetailed = "Health check {HealthCheckName}: {Status} in {Duration} - {Description}";

    #endregion

    #region Security Templates

    /// <summary>
    /// Template for authentication configuration.
    /// <para><b>Recommended Level:</b> Information</para>
    /// </summary>
    public const string AuthenticationConfigured = "Authentication configured with scheme: {AuthenticationScheme}";

    /// <summary>
    /// Template for authorization policy configuration.
    /// <para><b>Recommended Level:</b> Information</para>
    /// </summary>
    public const string AuthorizationPolicyConfigured = "Authorization policy configured: {PolicyName}";

    /// <summary>
    /// Template for security validation failures (avoid logging sensitive data).
    /// <para><b>Recommended Level:</b> Warning</para>
    /// </summary>
    public const string SecurityValidationFailed = "Security validation failed for {ComponentName}: {ValidationResult}";

    #endregion
}