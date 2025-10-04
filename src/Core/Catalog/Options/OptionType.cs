using Core.Catalog.Products;
using Core.Catalog.Prototypes;

using ErrorOr;

using SharedKernel.Domain.Attributes.Metadata;
using SharedKernel.Domain.Attributes.Parameterizable;
using SharedKernel.Domain.Attributes.Positionable;
using SharedKernel.Domain.Attributes.TranslatableResource;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalog.Options;

/// <summary>
/// Domain model representing a product OptionType.
/// Mirrors Spree::OptionType: has name, presentation, filterable flag, ordering (position), color support, and metadata.
/// </summary>
public sealed class OptionType :
    AuditableEntity,
    IParameterizableName,
    IMetadataSupport,
    IPositionable,
    ITranslatable<OptionTypeTranslation>
{
    #region Constants

    /// <summary>
    /// Names that indicate color option types.
    /// </summary>
    public static readonly string[] ColorNames = ["color", "colour"];

    #endregion

    #region Properties

    /// <summary>
    /// Internal identifier name (required, unique).
    /// Example: "color".
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// User-facing label (required).
    /// Example: "Color".
    /// </summary>
    public string Presentation { get; set; } = null!;

    /// <summary>
    /// Whether this option type is filterable (used in storefront filtering).
    /// </summary>
    public bool Filterable { get; set; }

    /// <summary>
    /// Position for ordering in lists.
    /// </summary>
    public int Position { get; set; }

    #endregion

    #region Relationships

    public ICollection<OptionValue> OptionValues { get; set; } = new List<OptionValue>();
    public ICollection<ProductOptionType> ProductOptionTypes { get; set; } = new List<ProductOptionType>();
    public List<Product> Products => ProductOptionTypes.Select(pot => pot.Product).ToList();
    public ICollection<OptionTypePrototype> OptionTypePrototypes { get; set; } = new List<OptionTypePrototype>();
    public IEnumerable<Prototype> Prototypes => OptionTypePrototypes.Select(otp => otp.Prototype).Where(p => p != null).Cast<Prototype>();

    // Translations
    public ICollection<OptionTypeTranslation> Translations { get; set; } = new List<OptionTypeTranslation>();

    #endregion

    #region Metadata

    public IDictionary<string, string?>? PublicMetadata { get; set; } = new Dictionary<string, string?>();
    public IDictionary<string, string?>? PrivateMetadata { get; set; } = new Dictionary<string, string?>();

    public IReadOnlyCollection<string> TranslatableFields => [nameof(Presentation)];

    #endregion

    #region Errors

    public static class Errors
    {
        public static Error IdRequired => Error.Validation("OptionType.InvalidId", "OptionType ID is required.");

        public static Error NotFound(Guid id) => Error.NotFound(
            "OptionType.NotFound",
            $"OptionType with ID '{id}' was not found."
        );

        public static Error NameAlreadyExists(string name) => Error.Conflict(
            "OptionType.NameAlreadyExists",
            $"An OptionType with the name '{name}' already exists."
        );

        public static Error InUseByProducts => Error.Failure(
            "OptionType.InUseByProducts",
            "OptionType cannot be deleted as it is in use by one or more products."
        );

        public static Error InUseByPrototypes => Error.Failure(
            "OptionType.InUseByPrototypes",
            "OptionType cannot be deleted as it is in use by one or more prototypes."
        );

        public static Error UnexpectedError(string operationName, Exception? ex = null) => Error.Unexpected(
          code: $"OptionType.{operationName}UnexpectedError",
          description: $"An unexpected error occurred during execution of {operationName} operation on OptionType. {ex?.Message}");
    }

    #endregion

    #region Constructors

    private OptionType() { }

    #endregion

    #region Factory

    public static ErrorOr<OptionType> Create(
        string name,
        string presentation,
        bool filterable = false,
        int position = 0,
        IDictionary<string, string?>? publicMetadata = null,
        IDictionary<string, string?>? privateMetadata = null)
    {
        OptionType ot = new()
        {
            Name = name.Trim(),
            Presentation = presentation.Trim(),
            Filterable = filterable,
            Position = Math.Max(position, PositionableConstraints.PositionMin)
        };

        if (publicMetadata != null)
            ot.PublicMetadata = new Dictionary<string, string?>(publicMetadata);
        if (privateMetadata != null)
            ot.PrivateMetadata = new Dictionary<string, string?>(privateMetadata);

        ot.AddDomainEvent(new Events.Created(ot.Id));
        return ot;
    }

    #endregion

    #region Behavior

    public ErrorOr<OptionType> Update(
        string? name = null,
        string? presentation = null,
        bool? filterable = null,
        int? position = null,
        IDictionary<string, string?>? publicMetadata = null,
        IDictionary<string, string?>? privateMetadata = null)
    {
        var changedFields = new HashSet<string>();

        if (!string.IsNullOrWhiteSpace(name) && name != Name)
        {
            Name = name.Trim();
            changedFields.Add(nameof(Name));
        }

        if (!string.IsNullOrWhiteSpace(presentation) && presentation != Presentation)
        {
            Presentation = presentation.Trim();
            changedFields.Add(nameof(Presentation));
        }

        if (filterable.HasValue && filterable.Value != Filterable)
        {
            Filterable = filterable.Value;
            changedFields.Add(nameof(Filterable));
        }

        if (position.HasValue && position.Value != Position)
        {
            Position = Math.Clamp(position.Value, PositionableConstraints.PositionMin, PositionableConstraints.PositionMax);
            changedFields.Add(nameof(Position));
        }

        if (publicMetadata != null)
        {
            PublicMetadata = new Dictionary<string, string?>(publicMetadata);
        }

        if (privateMetadata != null)
        {
            PrivateMetadata = new Dictionary<string, string?>(privateMetadata);
        }

        if (changedFields.Overlaps([nameof(Name), nameof(Presentation), nameof(Filterable), nameof(Position)]))
        {
            TouchAllProducts();
        }

        return this;
    }

    /// <summary>
    /// Returns true if this option type is a color (name is "color" or "colour").
    /// </summary>
    public bool IsColor() => Name != null && ColorNames.Contains(Name, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the filter parameter for this option type (parameterized name).
    /// </summary>
    public string FilterParam => Name?.Replace(" ", "-").ToLowerInvariant() ?? string.Empty;

    /// <summary>
    /// Returns the first color option type from a queryable.
    /// </summary>
    public static OptionType? FirstColor(IQueryable<OptionType> query) =>
        query.Where(ot => ColorNames.Contains(ot.Name, StringComparer.OrdinalIgnoreCase)).OrderBy(ot => ot.Position).FirstOrDefault();

    /// <summary>
    /// Applies default ordering (by position then created date).
    /// </summary>
    public static IQueryable<OptionType> ApplyDefaultOrdering(IQueryable<OptionType> query)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        return query.OrderBy(ot => ot.Position).ThenBy(ot => ot.CreatedAt);
    }

    /// <summary>
    /// Applies sorted ordering (by name).
    /// </summary>
    public static IQueryable<OptionType> ApplySorted(IQueryable<OptionType> query)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        return query.OrderBy(ot => ot.Name);
    }

    /// <summary>
    /// Returns only filterable option types.
    /// </summary>
    public static IQueryable<OptionType> FilterableScope(IQueryable<OptionType> query)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        return query.Where(ot => ot.Filterable);
    }

    /// <summary>
    /// Touches all associated products (for cache invalidation, etc).
    /// </summary>
    public void TouchAllProducts()
    {
        // foreach (var prod in Products)
        //     prod.Touch();
    }

    public ErrorOr<Deleted> Delete()
    {
        if (ProductOptionTypes.Any())
            return Errors.InUseByProducts;

        if (OptionTypePrototypes.Any())
            return Errors.InUseByPrototypes;

        AddDomainEvent(new Events.Deleted(Id));
        AddDomainEvent(new Events.TouchProducts(Id));
        return Result.Deleted;
    }

    #endregion

    #region Events

    public static class Events
    {
        public record Created(Guid OptionTypeId) : DomainEvent;
        public record Updated(Guid OptionTypeId) : DomainEvent;
        public record Deleted(Guid OptionTypeId) : DomainEvent;
        public record TouchProducts(Guid OptionTypeId) : DomainEvent;
    }

    #endregion
}