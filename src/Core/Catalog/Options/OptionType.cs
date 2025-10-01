using Core.Catalog.Products;

using ErrorOr;

using SharedKernel.Domain.Attributes.Metadata;
using SharedKernel.Domain.Attributes.Parameterizable;
using SharedKernel.Domain.Attributes.Positionable;
using SharedKernel.Domain.Attributes.TranslatableResource;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalog.Options;

public sealed class OptionType :
    AuditableEntity,
    IParameterizableName,
    IMetadataSupport,
    IPositionable,
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


    #region Errors
    public static class Errors
    {
        // Validations:
        // ID: required, non-empty
        public static Error IdRequired => Error.Validation("OptionType.InvalidId", "OptionType ID is required.");

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

        // Delete: 
        // In use by products
        public static Error InUseByProducts => Error.Failure(
            "OptionType.InUseByProducts",
            "OptionType cannot be deleted as it is in use by one or more products."
        );

        // In use by prototypes
        public static Error InUseByPrototypes => Error.Failure(
            "OptionType.InUseByPrototypes",
            "OptionType cannot be deleted as it is in use by one or more prototypes."
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
        // Validate: already in fluent validation
        // Create: instantiate
        OptionType ot = new OptionType
        {
            Name = name.Trim(),
            Presentation = presentation.Trim(),
            Filterable = filterable,
            Position = Math.Max(position, 0)
        };

        // Raise: create event
        ot.AddDomainEvent(new Events.Created(ot.Id));
        return ot;
    }

    public ErrorOr<OptionType> Update(string? presentation = null, bool? filterable = null, int? position = null)
    {
        bool changed = false;
        if (presentation != null && presentation.Trim() != Presentation)
        {
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
            AddDomainEvent(new Events.Updated(Id));
            AddDomainEvent(new Events.TouchProducts(Id));
        }

        return this;
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

    public static class Events
    {
        public record Created(Guid OptionTypeId) : DomainEvent;
        public record Updated(Guid OptionTypeId) : DomainEvent;
        public record Deleted(Guid OptionTypeId) : DomainEvent;
        public record TouchProducts(Guid OptionTypeId) : DomainEvent;
    }
}