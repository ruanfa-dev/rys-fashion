using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Storage.Options;

public class StorageOptions
{
    public const string Section = "Storage";

    // Local storage settings (Development)
    public string LocalPath { get; set; } = "wwwroot/uploads";
    public string BaseUrl { get; set; } = "/uploads";

    // Azure storage settings (Production)
    public string? AzureConnectionString { get; set; }
    public string? AzureContainerName { get; set; }
    public string? AzureCdnUrl { get; set; }

    // Common settings
    [Range(1, long.MaxValue, ErrorMessage = "MaxFileSizeBytes must be greater than 0")]
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB

    [Required(ErrorMessage = "At least one file extension must be allowed")]
    [MinLength(1, ErrorMessage = "At least one file extension must be allowed")]
    public string[] AllowedExtensions { get; set; } = new[]
    {
        ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx"
    };

    // Performance settings
    public int UploadTimeoutSeconds { get; set; } = 300; // 5 minutes
    public bool EnableCompression { get; set; } = true;
    public int MaxConcurrentUploads { get; set; } = 10;
}