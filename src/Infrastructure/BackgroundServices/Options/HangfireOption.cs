using System.ComponentModel.DataAnnotations;

namespace Infrastructure.BackgroundServices.Options;

public class HangfireOptions
{
    public const string Section = "BackgroundServices:Hangfire";

    /// <summary>
    /// Enable or disable Hangfire dashboard
    /// </summary>
    public bool EnableDashboard { get; init; } = false;

    /// <summary>
    /// Dashboard URL path (e.g., "/hangfire", "/admin/jobs")
    /// </summary>
    [Required]
    public string DashboardPath { get; init; } = "/hangfire";

    /// <summary>
    /// Hangfire server configuration
    /// </summary>
    [Required]
    public HangfireServerOptions Server { get; init; } = new();
}

public class HangfireServerOptions
{
    /// <summary>
    /// Number of worker threads (1-2 for single store e-shop)
    /// </summary>
    [Range(1, 10)]
    public int WorkerCount { get; init; } = 1;

    /// <summary>
    /// Background job queues for different priorities
    /// For fashion e-shop: default, email, inventory, reports
    /// </summary>
    public string[] Queues { get; init; } = new[] { "default", "email", "inventory", "reports" };

    /// <summary>
    /// Server polling interval in seconds
    /// </summary>
    [Range(1, 300)]
    public int PollingInterval { get; init; } = 15;

    /// <summary>
    /// How long to keep job history in days
    /// </summary>
    [Range(1, 365)]
    public int JobRetentionDays { get; init; } = 7;
}