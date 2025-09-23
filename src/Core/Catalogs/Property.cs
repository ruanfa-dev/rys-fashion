using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

public partial class Property : AuditableEntity
{
    #region Properties
    public string Name { get; set; } = default!;
    public string Presentation { get; set; } = default!;
    public PropertyKind Kind { get; set; }
    public bool Filterable { get; set; }
    public DisplayOnKind DisplayOn { get; set; }
    public int Position { get; set; }
    #endregion

    #region Relationship
    public ICollection<PrototypeProperty> PrototypeProperties { get; set; } = new List<PrototypeProperty>();
    public ICollection<ProductProperty> ProductProperties { get; set; } = new List<ProductProperty>();
    #endregion

    #region Contructors
    public Property()
    {
    }
    #endregion

    #region Methods
    public static Property Create(
        string name,
        string presentation,
        PropertyKind kind,
        bool filterable,
        DisplayOnKind displayOn,
        int position)
    {
        var property = new Property
        {
            Name = name,
            Presentation = presentation,
            Kind = kind,
            Filterable = filterable,
            DisplayOn = displayOn,
            Position = position
        };

        return property;
    }

    public void Update(
        string? name,
        string? presentation,
        PropertyKind? kind,
        bool? filterable,
        DisplayOnKind? displayOn,
        int? position)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name;
        if (!string.IsNullOrWhiteSpace(presentation))
            Presentation = presentation;
        if (kind.HasValue)
            Kind = kind.Value;
        if (filterable.HasValue)
            Filterable = filterable.Value;
        if (displayOn.HasValue)
            DisplayOn = displayOn.Value;
        if (position.HasValue)
            Position = position.Value;
    }

    /// <summary>
    /// Touch (mark updated) all related products.
    /// In Ruby/Spree this is handled with after_touch / after_update callbacks.
    /// Here we expose a method that application layer / handlers can call after persistence.
    /// </summary>
    public void TouchAllProducts()
    {
        // If related Product entities are loaded, mark them as updated.
        foreach (var pp in ProductProperties)
        {
            try
            {
                pp.Product?.MarkAsUpdated();
            }
            catch
            {
                // Guard: entity might be a proxy or not loaded — swallow to avoid domain explosion.
            }
        }

        // Bump this property's UpdatedAt so cache keys based on it will change.
        this.MarkAsUpdated();
    }

    /// <summary>
    /// Ensure product properties have a computed filter parameter when this property is filterable.
    /// This mirrors Spree's ensure_product_properties_have_filter_params callback where
    /// product properties with a value but missing filter_param are saved (recomputed).
    /// </summary>
    public void EnsureProductPropertiesHaveFilterParams()
    {
        if (!Filterable)
            return;

        foreach (var pp in ProductProperties)
        {
            // Only compute if there is a value and filter param is missing/empty.
            if (!string.IsNullOrWhiteSpace(pp.Value) && string.IsNullOrWhiteSpace(pp.FilterParam))
            {
                pp.FilterParam = ComputeFilterParam(pp.Value!);
                // Note: we set the value on the entity. Persistence (SaveChanges) must be called by the application layer.
            }
        }
    }

    /// <summary>
    /// Returns distinct pairs of (filterParam, value) across the product properties.
    /// If a scope (list of product property ids) is provided the method limits the result to that scope.
    /// When no scope is provided a simple in-memory cache keyed by property id + version (UpdatedAt/CreatedAt) is used.
    /// </summary>
    /// <param name="productPropertyIds">Optional scope of product property ids to consider</param>
    /// <returns>Distinct (filterParam, value) pairs</returns>
    public IEnumerable<(string FilterParam, string Value)> UniqValues(IEnumerable<Guid>? productPropertyIds = null)
    {
        // If a scope is provided, bypass caching and compute directly.
        if (productPropertyIds != null)
        {
            var idSet = productPropertyIds as ISet<Guid> ?? new HashSet<Guid>(productPropertyIds);
            return ProductProperties
                .Where(pp => idSet.Contains(pp.Id) && !string.IsNullOrWhiteSpace(pp.Value))
                .Select(pp => (FilterParam: pp.FilterParam ?? ComputeFilterParam(pp.Value!), Value: pp.Value!))
                .Distinct()
                .ToList();
        }

        // Use cache when no scope is provided.
        var cacheKey = GetUniqValuesCacheKey();
        if (UniqValuesCache.TryGetValue(cacheKey, out var cached) && cached is List<(string, string)> list)
        {
            return list;
        }

        var result = ProductProperties
            .Where(pp => !string.IsNullOrWhiteSpace(pp.Value))
            .Select(pp => (FilterParam: pp.FilterParam ?? ComputeFilterParam(pp.Value!), Value: pp.Value!))
            .Distinct()
            .ToList();

        // Store in cache (best-effort; cache lifecycle is process-lifetime)
        UniqValuesCache[cacheKey] = result;

        return result;
    }

    private static string ComputeFilterParam(string input)
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

    private string GetUniqValuesCacheKey()
    {
        // Use UpdatedAt if present, otherwise CreatedAt. If both null, fallback to ticks 0.
        var versionTicks = UpdatedAt?.UtcTicks ?? CreatedAt.UtcTicks;
        return $"property-uniq-values:{Id}:{versionTicks}";
    }

    #endregion

    #region Cache (process-level, simple)
    // Simple in-memory cache for uniq values to approximate Rails.cache.fetch used in Spree.
    private static readonly ConcurrentDictionary<string, object> UniqValuesCache = new();
    #endregion

    public static class Events
    {
        public record Created(Guid PropertyId) : DomainEvent;
        public record Updated(Guid PropertyId) : DomainEvent;
        public record Deleted(Guid PropertyId) : DomainEvent;
    }
    public enum PropertyKind
    {
        ShortText = 0,
        LongText = 1,
        Number = 2,
        RichText = 3
    }

    public enum DisplayOnKind
    {
        None = 0,
        Storefront = 1,
        Admin = 2,
        Both = 3
    }

    public static class Contrainsts
    {
        // Title: Alphanumeric, spaces, hyphens, underscores 
        public const int NameMinLength = 2;
        public const int NameMaxLength = 100;
        public const string NameRegex = @"^[A-Za-z0-9 _-]+$";

        // Presentation: Alphanumeric, spaces, hyphens, underscores allowed
        public const int PresentationMinLength = 2;
        public const int PresentationMaxLength = 150;
        public const string PresentationRegex = @"^[A-Za-z0-9 _-]+$";
    }
    public static class Errors
    {
        // Validation Errors

        // # Name
        public static Error NameRequired => Error.Validation(
            code: "Property.NameRequired",
            description: "Property name is required.");
        public static Error InvalidNameLength => Error.Validation(
            code: "Property.InvalidNameLength",
            description: $"Property name must be between {Contrainsts.NameMinLength} and {Contrainsts.NameMaxLength} characters long.");
        public static Error InvalidNameFormat => Error.Validation(
            code: "Property.InvalidNameFormat",
            description: "Property name contains invalid characters. Only alphanumeric characters, spaces, hyphens, and underscores are allowed.");

        // # Presentation
        public static Error PresentationRequired => Error.Validation(
            code: "Property.PresentationRequired",
            description: "Property presentation is required.");
        public static Error InvalidPresentationLength => Error.Validation(
            code: "Property.InvalidPresentationLength",
            description: $"Property presentation must be between {Contrainsts.PresentationMinLength} and {Contrainsts.PresentationMaxLength} characters long.");
        public static Error InvalidPresentationFormat => Error.Validation(
            code: "Property.InvalidPresentationFormat",
            description: "Property presentation contains invalid characters. Only alphanumeric characters, spaces, hyphens, and underscores are allowed.");

        // # Kind
        public static Error InvalidKind => Error.Validation(
            code: "Property.InvalidKind",
            description: "Property kind is invalid.");

        // # DisplayOn
        public static Error InvalidDisplayOn => Error.Validation(
            code: "Property.InvalidDisplayOn",
            description: "Property display on is invalid.");

        public static Error NotFound(Guid id) => Error.NotFound(
            code: "Property.NotFound",
            description: $"Property with ID '{id}' was not found.");

        public static Error NameAlreadyExists(string name) => Error.Conflict(
            code: "Property.NameAlreadyExists",
            description: $"A property with the name '{name}' already exists.");

        public static Error PropertyInUse(Guid id) => Error.Conflict(
            code: "Property.PropertyInUse",
            description: $"Property with ID '{id}' cannot be deleted because it is associated with existing products.");

        public static Error PrototypeInUse(Guid id) => Error.Conflict(
            code: "Property.PrototypeInUse",
            description: $"Property with ID '{id}' cannot be deleted because it is associated with existing prototypes.");
        
        public static Error PropertyUnexpected(string operation, string reason) => Error.Failure(
            code: $"Property.{operation}UnexpectedError",
            description: $"An unexpected error occurred: {reason}");
    }
}
