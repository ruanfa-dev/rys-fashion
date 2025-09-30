using System.Text.RegularExpressions;

using Core.Commons.Extensions; // For ImageMethods
using Core.Medias;

namespace Core.Catalog.Taxonomies;

/// <summary>
/// Represents a taxon-specific image. Ports minimal behaviour from Spree::TaxonImage:
/// - exposes configurable style definitions (e.g. "48x48", "100x100")
/// - produces per-style descriptors with generated URLs and parsed width/height
/// </summary>
public sealed class TaxonImage : Asset
{
    // Class-level styles map. Key is a symbolic name, value is size in "WIDTHxHEIGHT" format.
    public static IReadOnlyDictionary<string, string> StylesMap { get; } = new Dictionary<string, string>
    {
        ["mini"] = "48x48",
        ["small"] = "100x100",
        ["large"] = "600x600"
    };

    public sealed record StyleDescriptor(string Url, string Size, int Width, int Height);

    /// <summary>
    /// Returns descriptors for every style defined on the class.
    /// </summary>
    public IEnumerable<StyleDescriptor> Styles()
    {
        return StylesMap.Select(kvp =>
        {
            var size = kvp.Value;
            var (w, h) = ParseSize(size);
            var url = ImageMethods.GenerateUrl(Url, size);
            return new StyleDescriptor(url, size, w, h);
        });
    }

    private static (int width, int height) ParseSize(string size)
    {
        if (string.IsNullOrWhiteSpace(size)) return (0, 0);
        var m = Regex.Match(size, @"(\\d+)x(\\d+)");
        if (m.Success && int.TryParse(m.Groups[1].Value, out var w) && int.TryParse(m.Groups[2].Value, out var h))
            return (w, h);

        var parts = size.Split('x', 'X');
        if (parts.Length >= 2 &&
            int.TryParse(parts[0].Trim(), out var w2) &&
            int.TryParse(parts[1].Trim(), out var h2))
        {
            return (w2, h2);
        }
        return (0, 0);
    }
}