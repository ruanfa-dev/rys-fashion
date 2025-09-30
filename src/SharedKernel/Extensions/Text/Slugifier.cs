using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SharedKernel.Extensions.Text;

/// <summary>
/// Minimal, safe slug/parameterize helper. Designed to work well with EF Core validation flows
/// and to replace Rails' `parameterize` behavior in a deterministic way.
/// </summary>
public static class Slugifier
{
    private static readonly Regex InvalidChars = new(@"[^a-z0-9\-]+", RegexOptions.Compiled);
    private static readonly Regex DuplicateHyphens = new(@"\-{2,}", RegexOptions.Compiled);

    public static string Parameterize(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Normalize and remove diacritics
        string normalized = input.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder(capacity: normalized.Length);

        foreach (char ch in normalized)
        {
            UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        string ascii = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

        // Replace non-alphanum with hyphen
        string cleaned = InvalidChars.Replace(ascii, "-");

        // Collapse repeated hyphens and trim
        cleaned = DuplicateHyphens.Replace(cleaned, "-").Trim('-');

        return cleaned;
    }
}