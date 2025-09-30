using System.Text.RegularExpressions;

namespace Core.Commons.Extensions;

/// <summary>
/// Image helper that mirrors the Ruby ImageMethods#generate_url behavior:
/// - accepts size like "48x48"
/// - default gravity "centre" with translation for mini_magick -> "center"
/// - returns a URL encoded with transformation params (size, mode, gravity, quality)
/// Note: actual derivative generation / CDN signing should be implemented in infra.
/// </summary>
public static class ImageMethods
{
    public enum VariantProcessor
    {
        Unknown = 0,
        MiniMagick,
        Vips
    }

    /// <summary>
    /// Generate a derivative URL for an original image URL.
    /// Default gravity is 'centre' (matching the Ruby API). When the processor is MiniMagick
    /// (or unknown/null) 'centre' is translated to 'center' — matching Rails behaviour.
    /// </summary>
    public static string GenerateUrl(
        string? originalUrl,
        string size,
        string gravity = "centre",
        int quality = 80,
        string? background = null,
        VariantProcessor processor = VariantProcessor.Unknown)
    {
        if (string.IsNullOrWhiteSpace(originalUrl)) return string.Empty;
        if (string.IsNullOrWhiteSpace(size)) return string.Empty;

        // normalize size (remove whitespace) and validate
        size = Regex.Replace(size, @"\s+", "");
        var m = Regex.Match(size, @"^(\d+)x(\d+)$", RegexOptions.IgnoreCase);
        if (!m.Success) return string.Empty;

        var width = m.Groups[1].Value;
        var height = m.Groups[2].Value;

        var translatedGravity = TranslateGravityForMiniMagick(gravity, processor);

        // Build query string (simple approach). Replace with signed URL / CDN params in infra.
        var qs = $"mode=resize_and_pad&width={Uri.EscapeDataString(width)}&height={Uri.EscapeDataString(height)}&gravity={Uri.EscapeDataString(translatedGravity)}&quality={quality}";

        // background not implemented in this helper (kept for API parity)
        if (!string.IsNullOrWhiteSpace(background))
        {
            // background is expected as e.g. "0,0,0" or hex; escape and append
            qs += $"&background={Uri.EscapeDataString(background)}";
        }

        var sep = originalUrl.Contains('?') ? '&' : '?';
        return $"{originalUrl}{sep}{qs}";
    }

    /// <summary>
    /// Return the original / full-size URL (equivalent to cdn_image_url(attachment) in Rails).
    /// </summary>
    public static string OriginalUrl(string? originalUrl) => originalUrl ?? string.Empty;

    private static string TranslateGravityForMiniMagick(string gravity, VariantProcessor processor)
    {
        if (string.IsNullOrWhiteSpace(gravity)) gravity = "centre";

        // If gravity is 'centre' and the processor is MiniMagick or unknown (nil in Rails),
        // return 'center' to match MiniMagick's expected term.
        if (string.Equals(gravity, "centre", StringComparison.OrdinalIgnoreCase) &&
            (processor == VariantProcessor.MiniMagick || processor == VariantProcessor.Unknown))
        {
            return "center";
        }

        return gravity;
    }
}