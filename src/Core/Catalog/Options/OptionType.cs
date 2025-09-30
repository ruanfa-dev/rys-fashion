using Core.Catalog.Products;

using ErrorOr;

using SharedKernel.Domain.Attributes.Metadata;
using SharedKernel.Domain.Attributes.Parameterizable;
using SharedKernel.Domain.Attributes.TranslatableResource;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalog.Options;

public sealed class OptionType :
    AuditableEntity,
    IParameterizableName,
    IMetadataSupport,
    ITranslatable<OptionTypeTranslation>
{
    #region Properties
    public string Name { get; set; } = null!;
    public string Presentation { get; set; } = null!;
    public bool Filterable { get; set; }
    public int Position { get; set; }

    #endregion

    #region Relationships
    public ICollection<OptionValue> OptionValues { get; set; } = new List<OptionValue>();
    public ICollection<ProductOptionType> ProductOptionTypes { get; set; } = new List<ProductOptionType>();
    public ICollection<OptionTypePrototype> OptionTypePrototypes { get; set; } = new List<OptionTypePrototype>();

    // Translations
    public ICollection<OptionTypeTranslation> Translations { get; set; } = new List<OptionTypeTranslation>();
    #endregion

    #region Metadata

    // Metadata
    public IDictionary<string, string?>? PublicMetadata { get; set; } = new Dictionary<string, string?>();
    public IDictionary<string, string?>? PrivateMetadata { get; set; } = new Dictionary<string, string?>();
    public IReadOnlyCollection<string> TranslatableFields => [nameof(Presentation)];
    #endregion

    #region Constraints
    public static class Constraints
    {
        public const int NameMinLength = 2;
        public const int NameMaxLength = 100;

        public const int PresentationMinLength = 2;
        public const int PresentationMaxLength = 255;

        public const int PositionMin = 0;
        public const int PositionMax = 100000;
    }
    #endregion

    #region Errors
    public static class Errors
    {
        // Validations:
        // ID: required, non-empty
        public static Error IdRequired => Error.Validation("OptionType.InvalidId", "OptionType ID is required.");

        // Name: required, length
        public static Error NameRequired => Error.Validation("OptionType.NameRequired", "OptionType name is required.");
        public static Error InvalidNameLength => Error.Validation(
            "OptionType.InvalidNameLength",
            $"OptionType name must be between {Constraints.NameMinLength} and {Constraints.NameMaxLength} characters long."
        );

        // Presentation: required, length
        public static Error PresentationRequired => Error.Validation("OptionType.PresentationRequired", "OptionType presentation is required.");
        public static Error InvalidPresentationLength => Error.Validation(
            "OptionType.InvalidPresentationLength",
            $"OptionType presentation must be between {Constraints.PresentationMinLength} and {Constraints.PresentationMaxLength} characters long."
        );

        // Position: non-negative
        public static Error InvalidPosition => Error.Validation(
            "OptionType.InvalidPosition",
            $"Position must be between {Constraints.PositionMin} and {Constraints.PositionMax}."
        );

        // Not Found:
        public static Error NotFound(Guid id) => Error.NotFound(
            "OptionType.NotFound",
            $"OptionType with ID '{id}' was not found."
        );

        // Conflict: Name already exists
        public static Error NameAlreadyExists(string name) => Error.Conflict(
            "OptionType.NameAlreadyExists",
            $"A OptionType with the name '{name}' already exists."
        );

        // Cannot delete if product is using this OptionType
        public static Error CannotDeleteInUse(Guid id, int usageCount) => Error.Validation(
            "OptionType.CannotDeleteInUse",
            $"Cannot delete OptionType '{id}' because it is used by {usageCount} product(s)."
        );

        // Unexpected error:
        public static Error UnexpectedError(string operationName, Exception? ex = null) => Error.Unexpected(
          code: $"OptionType.{operationName}UnexpectedError",
          description: $"An unexpected error occurred during execution of {operationName} operation on OptionType. {ex?.Message}");
    }

    #endregion

    private OptionType() { }

    public static ErrorOr<OptionType> Create(string name, string presentation, bool filterable = false, int position = 0)
    {
        if (string.IsNullOrWhiteSpace(presentation)) return Errors.PresentationRequired;

        OptionType ot = new OptionType
        {
            Name = name.Trim(),
            Presentation = presentation.Trim(),
            Filterable = filterable,
            Position = Math.Max(position, 0)
        };

        ot.AddDomainEvent(new Events.Created(ot.Id));
        return ot;
    }

    public ErrorOr<OptionType> Update(string? presentation = null, bool? filterable = null, int? position = null)
    {
        bool changed = false;
        if (presentation != null && presentation.Trim() != Presentation)
        {
            if (string.IsNullOrWhiteSpace(presentation)) return Errors.PresentationRequired;
            Presentation = presentation.Trim();
            changed = true;
        }

        if (filterable.HasValue && filterable.Value != Filterable)
        {
            Filterable = filterable.Value;
            changed = true;
        }

        if (position.HasValue && position.Value != Position)
        {
            Position = position.Value;
            changed = true;
        }

        if (changed)
        {
            MarkAsUpdated();
            AddDomainEvent(new Events.Updated(Id));
            AddDomainEvent(new Events.TouchProducts(Id));
        }

        return this;
    }

    public ErrorOr<Deleted> Delete()
    {
        AddDomainEvent(new Events.Deleted(Id));
        AddDomainEvent(new Events.TouchProducts(Id));
        return Result.Deleted;
    }

    private void TouchAllProducts()
    {
        // raise a domain event to be handled by handlers which will touch related products
        AddDomainEvent(new Events.TouchProducts(Id));
    }

    public static class Events
    {
        public record Created(Guid OptionTypeId) : DomainEvent;
        public record Updated(Guid OptionTypeId) : DomainEvent;
        public record Deleted(Guid OptionTypeId) : DomainEvent;
        public record TouchProducts(Guid OptionTypeId) : DomainEvent;
    }
}