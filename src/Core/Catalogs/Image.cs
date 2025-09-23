using System.Text.RegularExpressions;

namespace Core.Catalogs;

/// <summary>
/// Domain representation of an image asset.  Small helper methods mirror Spree semantics but
/// avoid infra concerns (URL generation, ActiveStorage).  The application/infrastructure layer
/// must provide real URLs via storage service using the Asset.AttachmentKey.
/// </summary>
public class Image : Asset
{
    // Example style definitions. Infra/configuration can override or provide real values.
    // Keys should match usage in the app (e.g. "plp_and_carousel").
    public static readonly IReadOnlyDictionary<string, string> Styles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["thumb"] = "50x50",
        ["small"] = "100x100",
        ["medium"] = "300x300",
        ["large"] = "800x800",
        ["plp_and_carousel"] = "400x400"
    };

    private Image() : base() { }

    public Image(Guid? viewableId, string? viewableType, string? attachmentKey = null)
        : base(viewableId, viewableType, attachmentKey)
    {
    }

    /// <summary>
    /// Return list of style descriptors with size and dimension. URL generation is delegated to the caller via <paramref name="urlFactory"/>.
    /// urlFactory receives (attachmentKey, styleSize) and must return a URL string.
    /// </summary>
    public IEnumerable<(string Name, string Url, string Size, int Width, int Height)> StylesWithUrls(Func<string?, string, string> urlFactory)
    {
        foreach (var kv in Styles)
        {
            var (w, h) = ParseDimensions(kv.Value);
            yield return (kv.Key, urlFactory(AttachmentKey, kv.Value), kv.Value, w, h);
        }
    }

    /// <summary>
    /// Return a single style descriptor or null.
    /// </summary>
    public (string Name, string Url, string Size, int Width, int Height)? Style(string name, Func<string?, string, string> urlFactory)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        if (!Styles.TryGetValue(name, out var size)) return null;
        var (w, h) = ParseDimensions(size);
        return (name, urlFactory(AttachmentKey, size), size, w, h);
    }

    public (int Width, int Height)? StyleDimensions(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        if (!Styles.TryGetValue(name, out var size)) return null;
        var (w, h) = ParseDimensions(size);
        return (w, h);
    }

    public string PlpUrl(Func<string?, string, string> urlFactory)
    {
        if (!Styles.TryGetValue("plp_and_carousel", out var size)) size = Styles.Values.First();
        return urlFactory(AttachmentKey, size);
    }

    /// <summary>
    /// Touches all variants of the product owning the provided viewable when conditions are met.
    /// The application layer should call this after persisting changes and passing the proper 'variant' and 'positionChanged' flag.
    /// </summary>
    public void TouchProductVariantsIfNeeded(Variant? viewableVariant, bool positionChanged)
    {
        if (!ShouldTouchProductVariants(viewableVariant, positionChanged)) return;

        var product = viewableVariant!.Product;
        foreach (var v in product.Variants)
        {
            try { v.MarkAsUpdated(); } catch { }
        }
    }

    /// <summary>
    /// Logic that mirrors Spree's conditions for touching product variants after image update.
    /// </summary>
    public bool ShouldTouchProductVariants(Variant? viewableVariant, bool positionChanged)
    {
        if (viewableVariant == null) return false;
        if (!viewableVariant.IsMaster) return false;
        if (viewableVariant.Product == null) return false;
        if (!viewableVariant.Product.HasVariants()) return false;
        if (!positionChanged) return false;
        return true;
    }

    private static (int Width, int Height) ParseDimensions(string dim)
    {
        var match = Regex.Match(dim ?? "", @"^(?<w>\d+)x(?<h>\d+)$");
        if (match.Success && int.TryParse(match.Groups["w"].Value, out var w) && int.TryParse(match.Groups["h"].Value, out var h))
            return (w, h);
        return (0, 0);
    }
}
