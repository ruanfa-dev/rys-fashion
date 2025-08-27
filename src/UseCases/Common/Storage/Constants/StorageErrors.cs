using ErrorOr;

namespace UseCases.Common.Storage.Constants;

public static class StorageErrors
{
    // File not found
    public static Error FileNotFound(string path) =>
        Error.NotFound("Storage.FileNotFound", $"File not found: {path}");

    // Upload errors
    public static Error UploadFailed(string reason) =>
        Error.Failure("Storage.UploadFailed", $"File upload failed: {reason}");

    public static Error FileEmpty =>
        Error.Validation("File.Empty", "No file was provided");

    public static Error FileEmptyContent =>
        Error.Validation("File.Empty", "The file is empty");

    public static Error FileTooLarge(long maxSizeBytes) =>
        Error.Validation("File.TooLarge",
            $"File size exceeds the maximum allowed size of {maxSizeBytes / 1024 / 1024}MB");

    public static Error FileInvalidType(string extension, string[] allowedExtensions) =>
        Error.Validation("File.InvalidType",
            $"File type {extension} is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");

    // Delete errors
    public static Error DeleteFailed(string path) =>
        Error.Failure("Storage.DeleteFailed", $"Failed to delete file: {path}");

    // Read errors
    public static Error ReadFailed(string path) =>
        Error.Failure("Storage.ReadFailed", $"Failed to read file: {path}");

    // General validation errors
    public static Error InvalidUrl =>
        Error.Validation("File.InvalidUrl", "File URL cannot be empty");

    // Operation errors
    public static Error ExistsFailed =>
        Error.Failure("File.ExistsFailed", "An error occurred while checking file existence");

    public static Error ListFailed =>
        Error.Failure("File.ListFailed", "An error occurred while listing files");
}