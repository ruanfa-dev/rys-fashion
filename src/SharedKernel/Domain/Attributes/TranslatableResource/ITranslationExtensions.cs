using ErrorOr;

namespace SharedKernel.Domain.Attributes.TranslatableResource;

/// <summary>
/// Helper extensions for ITranslation providing field access and lightweight validation.
/// Placed in SharedKernel so other projects can use without referencing UseCases.
/// </summary>
public static class ITranslationExtensions
{
    /// <summary>
    /// Try to get a translation field value from the Fields dictionary (case-insensitive).
    /// </summary>
    public static bool TryGetField(this ITranslation translation, string fieldName, out string? value)
    {
        value = null;
        if (translation is null) return false;
        if (translation.Fields is null) return false;
        if (translation.Fields.TryGetValue(fieldName, out string? v) && !string.IsNullOrEmpty(v))
        {
            value = v; return true;
        }

        // case-insensitive fallback
        KeyValuePair<string, string?> matched = translation.Fields.FirstOrDefault(kv => string.Equals(kv.Key, fieldName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(matched.Value))
        {
            value = matched.Value; return true;
        }

        return false;
    }

    /// <summary>
    /// Get a translation field value or null.
    /// </summary>
    public static string? GetField(this ITranslation translation, string fieldName)
    {
        return translation.TryGetField(fieldName, out string? v) ? v : null;
    }

    /// <summary>
    /// Set a translation field value. Creates the Fields dictionary if null.
    /// </summary>
    public static void SetField(this ITranslation translation, string fieldName, string? value)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        translation.Fields ??= new Dictionary<string, string?>();
        translation.Fields[fieldName] = value;
    }

    /// <summary>
    /// Lightweight validation of the translation's fields and culture according to TranslatableConstraints.
    /// Returns an array of Error objects (empty when valid).
    /// </summary>
    public static Error[] Validate(this ITranslation translation, string? prefix = null)
    {
        if (translation is null) throw new ArgumentNullException(nameof(translation));
        List<Error> errors = new List<Error>();

        // Culture
        if (string.IsNullOrWhiteSpace(translation.Culture))
            errors.Add(TranslatableErrors.CultureRequired(prefix));
        else if (translation.Culture.Length > TranslatableConstraints.CultureMaxLength)
            errors.Add(TranslatableErrors.InvalidCultureLength(prefix));

        // Fields count
        if (translation.Fields != null)
        {
            if (translation.Fields.Count > TranslatableConstraints.MaxFields)
                errors.Add(TranslatableErrors.FieldsTooManyEntries(prefix));

            foreach (KeyValuePair<string, string?> kv in translation.Fields)
            {
                string key = kv.Key ?? string.Empty;
                string? val = kv.Value;

                if (key.Length < TranslatableConstraints.FieldKeyMinLength || key.Length > TranslatableConstraints.FieldKeyMaxLength)
                {
                    errors.Add(TranslatableErrors.FieldKeyInvalidLength(prefix));
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(key, TranslatableConstraints.FieldKeyAllowedPattern))
                {
                    errors.Add(TranslatableErrors.FieldKeyInvalidChars(prefix));
                }

                if (val != null && (val.Length < TranslatableConstraints.FieldValueMinLength || val.Length > TranslatableConstraints.FieldValueMaxLength))
                {
                    errors.Add(TranslatableErrors.FieldValueInvalidLength(prefix));
                }
            }
        }

        return errors.ToArray();
    }
}
