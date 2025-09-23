using SharedKernel.Domain.Primitives;

namespace Core.Catalogs;

/// <summary>
/// Simplified domain representation of a stored asset.
/// Tracks polymorphic owner via ViewableType/ViewableId and lightweight attachment metadata.
/// Persistence (ActiveStorage-like attachments, file URLs) belongs to infra layer.
/// </summary>
public class Asset : AuditableEntity
{
    // Polymorphic reference (e.g. "Variant", "Product", etc.)
    public Guid? ViewableId { get; set; }
    public string? ViewableType { get; set; }

    // Acts as list scope: position per viewable
    public int? Position { get; set; }

    // Minimal attachment metadata (actual binary/blob managed by infra)
    public string? AttachmentKey { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long? Size { get; set; }

    // Free-form metadata store; infra may persist as JSON/kv-store
    public IDictionary<string, string?> PrivateMetadata { get; set; } = new Dictionary<string, string?>();

    // Convenience: returns the session uuid when used by upload flows (stored in metadata)
    public string? SessionUploadedAssetsUuid
    {
        get => PrivateMetadata.TryGetValue("session_uploaded_assets_uuid", out var v) ? v : null;
        set => PrivateMetadata["session_uploaded_assets_uuid"] = value;
    }

    protected Asset() { }

    public Asset(Guid? viewableId, string? viewableType, string? attachmentKey = null)
    {
        ViewableId = viewableId;
        ViewableType = viewableType;
        AttachmentKey = attachmentKey;
    }

    /// <summary>
    /// Domain helper used by application layer to determine product for this asset when the owner is a Variant.
    /// The application should resolve the viewable entity and pass it to this helper when needed.
    /// </summary>
    public Product? GetProductFromViewable(object? viewable)
    {
        return viewable switch
        {
            Variant v => v.Product,
            Product p => p,
            _ => null
        };
    }
}