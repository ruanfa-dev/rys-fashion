using ErrorOr;

using Microsoft.AspNetCore.Http;

using UseCases.Common.Storage.Models;

namespace UseCases.Common.Storage.Services;

public interface IStorageService
{
    /// <summary>
    /// Upload a file to storage.
    /// Returns the file URL or path.
    /// </summary>
    Task<ErrorOr<string>> UploadFileAsync(
        IFormFile file,
        string? path = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file from storage.
    /// </summary>
    Task<ErrorOr<Success>> DeleteFileAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a file stream from storage.
    /// </summary>
    Task<ErrorOr<Stream>> GetFileAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a file exists in storage.
    /// </summary>
    Task<ErrorOr<bool>> ExistsAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List all files in a given folder (recursively if required).
    /// </summary>
    Task<ErrorOr<IReadOnlyList<StorageFileInfo>>> ListFilesAsync(
        string? folder = null,
        bool recursive = false,
        CancellationToken cancellationToken = default);
}
