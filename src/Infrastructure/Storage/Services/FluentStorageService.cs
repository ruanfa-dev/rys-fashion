using ErrorOr;

using FluentStorage;
using FluentStorage.Blobs;

using Infrastructure.Storage.Options;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using Serilog;

using UseCases.Common.Storage.Constants;
using UseCases.Common.Storage.Models;
using UseCases.Common.Storage.Services;

namespace Infrastructure.Storage.Services;

public class LocalStorageService : IStorageService
{
    private readonly IBlobStorage _storage;
    private readonly StorageOptions _options;

    public LocalStorageService(IOptions<StorageOptions> options)
    {
        _options = options.Value;

        try
        {
            Directory.CreateDirectory(_options.LocalPath);
            _storage = StorageFactory.Blobs.DirectoryFiles(_options.LocalPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize local storage at path: {Path}", _options.LocalPath);
            throw;
        }
    }

    public async Task<ErrorOr<string>> UploadFileAsync(
        IFormFile file,
        string? path = null,
        CancellationToken cancellationToken = default)
    {
        if (file is null)
            return StorageErrors.FileEmpty;

        if (file.Length == 0)
            return StorageErrors.FileEmptyContent;

        if (file.Length > _options.MaxFileSizeBytes)
            return StorageErrors.FileTooLarge(_options.MaxFileSizeBytes);

        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            return StorageErrors.FileInvalidType(extension, _options.AllowedExtensions);

        try
        {
            string dateFolder = DateTimeOffset.UtcNow.ToString("yyyy/MM/dd");
            string safeFileName = $"{DateTimeOffset.UtcNow.Ticks}_{Guid.NewGuid():N}{extension}";
            string blobPath = path != null
                ? Path.Combine(path, dateFolder, safeFileName).Replace("\\", "/")
                : Path.Combine(dateFolder, safeFileName).Replace("\\", "/");

            await using var stream = file.OpenReadStream();
            await _storage.WriteAsync(blobPath, stream, cancellationToken: cancellationToken);

            return Path.Combine(_options.BaseUrl, blobPath).Replace("\\", "/");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to upload file {FileName}", file.FileName);
            return StorageErrors.UploadFailed(ex.Message);
        }
    }

    public async Task<ErrorOr<Success>> DeleteFileAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return StorageErrors.InvalidUrl;

        try
        {
            string blobPath = GetBlobPath(fileUrl);

            if (!await _storage.ExistsAsync(blobPath, cancellationToken))
                return StorageErrors.FileNotFound(blobPath);

            await _storage.DeleteAsync(blobPath, cancellationToken);
            return Result.Success;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete file at {FileUrl}", fileUrl);
            return StorageErrors.DeleteFailed(fileUrl);
        }
    }

    public async Task<ErrorOr<Stream>> GetFileAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return StorageErrors.InvalidUrl;

        try
        {
            string blobPath = GetBlobPath(fileUrl);

            if (!await _storage.ExistsAsync(blobPath, cancellationToken))
                return StorageErrors.FileNotFound(blobPath);

            var memoryStream = new MemoryStream();
            await _storage.ReadToStreamAsync(blobPath, memoryStream, cancellationToken);
            memoryStream.Position = 0;

            return memoryStream;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to read file at {FileUrl}", fileUrl);
            return StorageErrors.ReadFailed(fileUrl);
        }
    }

    public async Task<ErrorOr<bool>> ExistsAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return StorageErrors.InvalidUrl;

        try
        {
            string blobPath = GetBlobPath(fileUrl);
            return await _storage.ExistsAsync(blobPath, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to check if file exists at {FileUrl}", fileUrl);
            return StorageErrors.ExistsFailed;
        }
    }

    public async Task<ErrorOr<IReadOnlyList<StorageFileInfo>>> ListFilesAsync(
        string? folder = null,
        bool recursive = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var blobs = await _storage.ListAsync(
                folderPath: folder,
                recurse: recursive,
                cancellationToken: cancellationToken);

            var fileInfos = blobs
                .Where(b => !b.IsFolder)
                .Select(blob => new StorageFileInfo
                {
                    Path = blob.FullPath,
                    Size = blob.Size,
                    LastModifiedUtc = blob.LastModificationTime,
                    Url = Path.Combine(_options.BaseUrl, blob.FullPath).Replace("\\", "/")
                })
                .ToList();

            return fileInfos.AsReadOnly();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to list files in folder {Folder}", folder ?? "root");
            return StorageErrors.ListFailed;
        }
    }

    private string GetBlobPath(string fileUrl)
    {
        return fileUrl
            .Replace(_options.BaseUrl, "", StringComparison.OrdinalIgnoreCase)
            .TrimStart('/');
    }
}