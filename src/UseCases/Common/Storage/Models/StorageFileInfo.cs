namespace UseCases.Common.Storage.Models;

public sealed class StorageFileInfo
{
    public required string Path { get; init; }
    public long? Size { get; init; }
    public DateTimeOffset? LastModifiedUtc { get; init; }
    public string? Url { get; init; }
}
