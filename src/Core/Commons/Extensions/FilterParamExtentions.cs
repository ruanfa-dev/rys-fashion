using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.Commons.Extensions;
public static class FilterParamExtensions
{
    public static string ComputeFilterParam(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Basic parameterization: normalize, remove diacritics, to lower, trim, replace spaces with '-',
        // remove invalid chars (keep alphanumeric, -, _).
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        var cleaned = sb.ToString().Normalize(NormalizationForm.FormC);
        cleaned = cleaned.ToLowerInvariant().Trim();

        // Replace whitespace sequences with single dash
        cleaned = Regex.Replace(cleaned, @"\s+", "-");

        // Remove chars other than letters, numbers, dash and underscore
        cleaned = Regex.Replace(cleaned, @"[^a-z0-9\-_]", string.Empty);

        // Trim leading/trailing dashes/underscores
        cleaned = cleaned.Trim('-', '_');

        return cleaned;
    }
}
