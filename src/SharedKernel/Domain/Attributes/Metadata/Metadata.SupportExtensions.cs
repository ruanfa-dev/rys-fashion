namespace SharedKernel.Domain.Attributes.Metadata;

/// <summary>
/// Extension helpers for convenient access to metadata keys.
/// Initializes underlying dictionaries when needed instead of throwing.
/// </summary>
public static class MetadataSupportExtensions
{
    // Public metadata helpers
    public static string? GetPublicMetadataValue(this IMetadataSupport holder, string key)
    {
        return holder.PublicMetadata?.TryGetValue(key, out var v) == true ? v : null;
    }

    public static void SetPublicMetadataValue(this IMetadataSupport holder, string key, string? value)
    {
        // If removing and the dictionary is null, nothing to do.
        if (value is null)
        {
            holder.PublicMetadata?.Remove(key);
            return;
        }

        // Ensure dictionary is initialized before setting.
        if (holder.PublicMetadata is null)
        {
            holder.PublicMetadata = new Dictionary<string, string?>();
        }

        holder.PublicMetadata[key] = value;
    }

    // Private metadata helpers
    public static string? GetPrivateMetadataValue(this IMetadataSupport holder, string key)
    {
        return holder.PrivateMetadata?.TryGetValue(key, out var v) == true ? v : null;
    }

    public static void SetPrivateMetadataValue(this IMetadataSupport holder, string key, string? value)
    {
        // If removing and the dictionary is null, nothing to do.
        if (value is null)
        {
            holder.PrivateMetadata?.Remove(key);
            return;
        }

        // Ensure dictionary is initialized before setting.
        if (holder.PrivateMetadata is null)
        {
            holder.PrivateMetadata = new Dictionary<string, string?>();
        }

        holder.PrivateMetadata[key] = value;
    }
}
