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

public class AzureStorageService : IStorageService
{
    private readonly IBlobStorage _storage;
    private readonly StorageOptions _options;

    public AzureStorageService(IOptions<StorageOptions> options)
    {
        _options = options.Value;

        try
        {
            var (accountName, accountKey) = ParseConnectionString(_options.AzureConnectionString!);
            _storage = StorageFactory.Blobs.AzureBlobStorageWithSharedKey(
                accountName,
                accountKey);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize Azure storage");
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
                ? $"{_options.AzureContainerName}/{path}/{dateFolder}/{safeFileName}"
                : $"{_options.AzureContainerName}/{dateFolder}/{safeFileName}";

            await using var stream = file.OpenReadStream();
            await _storage.WriteAsync(blobPath, stream, cancellationToken: cancellationToken);

            return GetFileUrl(blobPath);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to upload file {FileName} to Azure", file.FileName);
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
            Log.Error(ex, "Failed to delete file at {FileUrl} from Azure", fileUrl);
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
            Log.Error(ex, "Failed to read file at {FileUrl} from Azure", fileUrl);
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
            Log.Error(ex, "Failed to check if file exists at {FileUrl} in Azure", fileUrl);
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
            string folderPath = folder != null
                ? $"{_options.AzureContainerName!}/{folder}"
                : _options.AzureContainerName!;

            var blobs = await _storage.ListAsync(
                folderPath: folderPath,
                recurse: recursive,
                cancellationToken: cancellationToken);

            var fileInfos = blobs
                .Where(b => !b.IsFolder)
                .Select(blob => new StorageFileInfo
                {
                    Path = blob.FullPath,
                    Size = blob.Size,
                    LastModifiedUtc = blob.LastModificationTime,
                    Url = GetFileUrl(blob.FullPath)
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
            Log.Error(ex, "Failed to list files in Azure folder {Folder}", folder ?? "root");
            return StorageErrors.ListFailed;
        }
    }

    private string GetBlobPath(string fileUrl)
    {
        var baseUrl = !string.IsNullOrEmpty(_options.AzureCdnUrl)
            ? _options.AzureCdnUrl
            : $"https://{GetStorageAccount()}.blob.core.windows.net";

        return fileUrl
            .Replace(baseUrl, "", StringComparison.OrdinalIgnoreCase)
            .TrimStart('/');
    }

    private string GetFileUrl(string blobPath)
    {
        return !string.IsNullOrEmpty(_options.AzureCdnUrl)
            ? $"{_options.AzureCdnUrl.TrimEnd('/')}/{blobPath.TrimStart('/')}"
            : $"https://{GetStorageAccount()}.blob.core.windows.net/{blobPath}";
    }

    private (string accountName, string accountKey) ParseConnectionString(string connectionString)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);

        string? accountName = null;
        string? accountKey = null;

        foreach (var part in parts)
        {
            if (part.StartsWith("AccountName="))
                accountName = part.Substring(12);
            else if (part.StartsWith("AccountKey="))
                accountKey = part.Substring(11);
        }

        if (string.IsNullOrEmpty(accountName) || string.IsNullOrEmpty(accountKey))
            throw new InvalidOperationException("Invalid Azure storage connection string format");

        return (accountName, accountKey);
    }

    private string GetStorageAccount()
    {
        var (accountName, _) = ParseConnectionString(_options.AzureConnectionString!);
        return accountName;
    }
}