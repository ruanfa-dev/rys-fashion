using SharedKernel.Extensions.Text;
using SharedKernel.Domain.Attributes.TranslatableResource;

namespace Core.Catalog.Taxonomies;

public sealed class TaxonTranslation : ITranslation
{
    public Guid Id { get; set; }

    public Guid TaxonId { get; set; }

    // Keep Locale for backward compatibility; map Culture to Locale for ITranslation
    public string Locale { get; set; } = null!;

    // ITranslation implementation
    public string Culture
    {
        get => Locale;
        set => Locale = value;
    }

    public bool IsDefault { get; set; }

    /// <summary>
    /// Generic fields dictionary used to store translated values for keys such as
    /// "Name", "PrettyName", "Description", "Permalink". This replaces the previous
    /// explicit properties to support the new ITranslation contract.
    /// </summary>
    public IDictionary<string, string?>? Fields { get; set; }

    // Backing reference to parent taxon
    public Taxon? Taxon { get; set; }

    // Convenience accessors that read/write from Fields with sensible fallbacks
    private string? GetField(string key)
    {
        if (Fields != null && Fields.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
            return v;

        // case-insensitive fallback
        if (Fields != null)
        {
            var matched = Fields.FirstOrDefault(kv => string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(matched.Value)) return matched.Value;
        }

        return null;
    }

    private void SetField(string key, string? value)
    {
        Fields ??= new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        Fields[key] = value;
    }

    // Backward-compatible explicit properties mapped to Fields
    public string? Name
    {
        get => GetField("Name");
        set => SetField("Name", value);
    }

    public string? PrettyName
    {
        get => GetField("PrettyName");
        set => SetField("PrettyName", value);
    }

    public string? Description
    {
        get => GetField("Description");
        set => SetField("Description", value);
    }

    public string? Permalink
    {
        get => GetField("Permalink");
        set => SetField("Permalink", value);
    }

    public void SetPermalink()
    {
        SetField("Permalink", GenerateSlug());
    }

    public void SetPrettyName()
    {
        SetField("PrettyName", GeneratePrettyName());
    }

    public void RegeneratePrettyNameAndPermalink()
    {
        UpdatePrettyNameAndPermalink(Taxon!);
    }

    public string Slug
    {
        get => Permalink ?? string.Empty;
        set => Permalink = value;
    }

    /// <summary>
    /// Update translation's pretty name and permalink using the provided taxon as context.
    /// This mirrors the Rails translation callback that updates localized pretty_name and permalink.
    /// </summary>
    public void UpdatePrettyNameAndPermalink(Taxon taxon)
    {
        // Resolve localized name (translation field or fallback to taxon.Name)
        var localizedName = Name ?? taxon.Name;

        // If translation lacks pretty name, generate from taxon/parent context
        if (string.IsNullOrWhiteSpace(PrettyName))
        {
            var parentPretty = taxon.Parent?.PrettyName;
            // Try to use parent's localized pretty name if available
            if (taxon.Parent != null)
            {
                var localizedParent = taxon.Parent.Translations.FirstOrDefault(t => string.Equals(t.Culture, Culture, StringComparison.OrdinalIgnoreCase));
                if (localizedParent != null)
                    parentPretty = localizedParent.PrettyName ?? parentPretty;
            }

            var pretty = string.Join(" -> ", new[] { parentPretty, localizedName }.Where(s => !string.IsNullOrWhiteSpace(s)));
            PrettyName = pretty;
        }

        if (string.IsNullOrWhiteSpace(Permalink))
        {
            var source = string.IsNullOrWhiteSpace(Permalink) ? localizedName : Permalink?.Split('/').Last();
            var slugPart = Slugifier.Parameterize(source ?? string.Empty);
            string? parentPermalink = null;

            if (taxon.Parent != null)
            {
                // Prefer localized parent's permalink when available
                var localizedParent = taxon.Parent.Translations.FirstOrDefault(t => string.Equals(t.Culture, Culture, StringComparison.OrdinalIgnoreCase));
                var localizedParentPermalink = localizedParent?.Permalink;
                parentPermalink = !string.IsNullOrWhiteSpace(localizedParentPermalink) ? localizedParentPermalink : taxon.Parent.Permalink;
            }

            if (!string.IsNullOrWhiteSpace(parentPermalink))
                Permalink = string.Join('/', new[] { parentPermalink.TrimEnd('/'), slugPart }.Where(x => !string.IsNullOrWhiteSpace(x)));
            else
                Permalink = slugPart;
        }
    }

    string GenerateSlug()
    {
        if (Taxon == null)
            return Slugifier.Parameterize(Permalink ?? Name ?? string.Empty);

        if (Taxon.Parent != null)
        {
            var localizedParent = Taxon.Parent.Translations.FirstOrDefault(t => string.Equals(t.Culture, Culture, StringComparison.OrdinalIgnoreCase));
            var parentPermalink = localizedParent != null && !string.IsNullOrWhiteSpace(localizedParent.Permalink)
                ? localizedParent.Permalink
                : Taxon.Parent.Permalink;

            var source = string.IsNullOrWhiteSpace(Permalink) ? Name : Permalink?.Split('/').Last();
            var slugPart = Slugifier.Parameterize(source ?? string.Empty);
            return string.Join('/', new[] { parentPermalink?.TrimEnd('/'), slugPart }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        if (string.IsNullOrWhiteSpace(Permalink))
            return Slugifier.Parameterize(Name ?? string.Empty);

        return Slugifier.Parameterize(Permalink ?? string.Empty);
    }

    string GeneratePrettyName()
    {
        if (Taxon == null)
            return string.IsNullOrWhiteSpace(PrettyName) ? (Name ?? string.Empty) : PrettyName!;

        if (Taxon.Parent != null)
        {
            var localizedParent = Taxon.Parent.Translations.FirstOrDefault(t => string.Equals(t.Culture, Culture, StringComparison.OrdinalIgnoreCase));
            var parentPretty = localizedParent != null && !string.IsNullOrWhiteSpace(localizedParent.PrettyName)
                ? localizedParent.PrettyName
                : Taxon.Parent.PrettyName;

            return string.Join(" -> ", new[] { parentPretty, Name }.Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        return string.IsNullOrWhiteSpace(PrettyName) ? (Name ?? string.Empty) : PrettyName!;
    }
}
