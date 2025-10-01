using System.Reflection;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;
using Core.Catalog.Products;
using SharedKernel.Domain.Attributes.Metadata;

namespace Core.Medias;

public class Asset : AuditableEntity, IMetadataSupport
{
    public string ViewableType { get; set; } = string.Empty;
    public Guid? ViewableId { get; set; }
    public int Position { get; set; }
    public string? Url { get; set; }
    public string? Key { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long? Size { get; set; }

    public IDictionary<string, string?>? PublicMetadata { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<string, string?>? PrivateMetadata { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    protected Asset() { }

    public static Asset Create(
        string? viewableType,
        Guid? viewableId,
        string? url,
        string? key = null,
        string? fileName = null,
        string? contentType = null,
        long? size = null,
        int position = 0,
        IDictionary<string, string?>? privateMetadata = null,
        IDictionary<string, string?>? publicMetadata = null)
    {
        Asset asset = new Asset
        {
            ViewableType = viewableType ?? string.Empty,
            ViewableId = viewableId,
            Url = url,
            Key = key,
            FileName = fileName,
            ContentType = contentType,
            Size = size,
            Position = Math.Max(0, position),
            PrivateMetadata = privateMetadata != null ? new Dictionary<string, string?>(privateMetadata, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase),
            PublicMetadata = publicMetadata != null ? new Dictionary<string, string?>(publicMetadata, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        };
        asset.AddDomainEvent(new Events.Created(asset.Id));
        return asset;
    }

    public void Attach(string url, string? key = null, string? fileName = null, string? contentType = null, long? size = null)
    {
        Url = url;
        Key = key;
        FileName = fileName;
        ContentType = contentType;
        Size = size;
        AddDomainEvent(new Events.Updated(Id));
    }

    public bool IsAttached() => !string.IsNullOrWhiteSpace(Url);

    public void SetPosition(int position)
    {
        Position = Math.Max(0, position);
        AddDomainEvent(new Events.Updated(Id));
    }


    public string? SessionUploadedAssetsUuid
    {
        get => this.GetPrivateMetadataValue("session_uploaded_assets_uuid");
        set => this.SetPrivateMetadataValue("session_uploaded_assets_uuid", value);
    }

    /// <summary>
    /// Query helper that mirrors the Rails scope `with_session_uploaded_assets_uuid`.
    /// Usage: Asset.WithSessionUploadedAssetsUuid(dbContext.Set&lt;Asset&gt;(), uuid)
    /// </summary>
    public static IQueryable<Asset> WithSessionUploadedAssetsUuid(IQueryable<Asset> query, string? uuid)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        return string.IsNullOrWhiteSpace(uuid) ? query.Where(a => a.SessionUploadedAssetsUuid == null) : query.Where(a => a.SessionUploadedAssetsUuid == uuid);
    }

    public async Task<T?> ResolveViewableAsync<T>(Func<string, Guid?, Task<T?>> resolver) where T : class
    {
        if (resolver == null) throw new ArgumentNullException(nameof(resolver));
        return await resolver(ViewableType, ViewableId);
    }

    /// <summary>
    /// Resolve the polymorphic viewable using a resolver delegate.
    /// This keeps the Core model free of Infrastructure dependencies. Example resolver: (type,id) => ViewableResolver.ResolveAsync(dbContext,type,id)
    /// </summary>
    public async Task<object?> ResolveViewableAsync(Func<string, Guid?, Task<object?>> resolver)
    {
        if (resolver == null) throw new ArgumentNullException(nameof(resolver));
        return await resolver(ViewableType, ViewableId);
    }

    /// <summary>
    /// Returns the associated Product when the viewable is a Variant (mirrors the Ruby helper).
    /// Uses a resolver delegate to avoid coupling Core to Infrastructure.
    /// </summary>
    public async Task<Product?> ResolveProductAsync(Func<string, Guid?, Task<object?>> resolver)
    {
        object? viewable = await ResolveViewableAsync(resolver);
        if (viewable == null) return null;

        // If the resolved object is a Variant with a Product property, try to read it.
        Type variantType = viewable.GetType();
        PropertyInfo? productProp = variantType.GetProperty("Product");
        if (productProp != null)
        {
            object? productVal = productProp.GetValue(viewable);
            return productVal as Product;
        }

        // Otherwise, if the viewable itself is a Product, return it.
        if (viewable is Product p) return p;

        return null;
    }

    public static class Events
    {
        public record Created(Guid AssetId) : DomainEvent;
        public record Updated(Guid AssetId) : DomainEvent;
        public record Deleted(Guid AssetId) : DomainEvent;
    }
}