using Core.Catalog.Variants;

using ErrorOr;

using SharedKernel.Domain.Attributes.Metadata;
using SharedKernel.Domain.Attributes.Parameterizable;
using SharedKernel.Domain.Attributes.TranslatableResource;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalog.Options;

public sealed class OptionValue :
    AuditableEntity,
    IParameterizableName,
    IMetadataSupport,
    ITranslatable<OptionValueTranslation>
{
    #region Properties
    public Guid OptionTypeId { get; set; }
    public string Name { get; set; } = null!;
    public string Presentation { get; set; } = null!;
    public int Position { get; set; }
    #endregion

    #region Relationships
    public OptionType OptionType { get; set; } = null!;
    public ICollection<OptionValueVariant> OptionValueVariants { get; set; } = new List<OptionValueVariant>();
    public ICollection<Variant> Variants => OptionValueVariants.Select(ovv => ovv.Variant).ToList();
    #endregion

    #region Metadata
    // Translations
    public ICollection<OptionValueTranslation> Translations { get; set; } = new List<OptionValueTranslation>();

    // Metadata
    public IDictionary<string, string?>? PublicMetadata { get; set; } = new Dictionary<string, string?>();
    public IDictionary<string, string?>? PrivateMetadata { get; set; } = new Dictionary<string, string?>();
    public IReadOnlyCollection<string> TranslatableFields => [nameof(Presentation)];
    #endregion

    #region Constraints
    public static class Constraints
    {
        public const int NameMinLength = 1;
        public const int NameMaxLength = 100;

        public const int PresentationMinLength = 1;
        public const int PresentationMaxLength = 255;

        public const int PositionMin = 0;
        public const int PositionMax = 100000;
    }
    #endregion

    #region Errors
    public static class Errors
    {
        #region Validations
        // ID: required, non-empty
        public static Error IdRequired => Error.Validation("Property.InvalidId", "Property ID is required.");

        // Name: required, length
        public static Error NameRequired => Error.Validation("Property.NameRequired", "Property name is required.");
        public static Error InvalidNameLength => Error.Validation(
            "Property.InvalidNameLength",
            $"Property name must be between {Constraints.NameMinLength} and {Constraints.NameMaxLength} characters long."
        );

        // Presentation: required, length
        public static Error PresentationRequired => Error.Validation("Property.PresentationRequired", "Property presentation is required.");
        public static Error InvalidPresentationLength => Error.Validation(
            "Property.InvalidPresentationLength",
            $"Property presentation must be between {Constraints.PresentationMinLength} and {Constraints.PresentationMaxLength} characters long."
        );

        // Position: non-negative
        public static Error InvalidPosition => Error.Validation(
            "Property.InvalidPosition",
            $"Position must be between {Constraints.PositionMin} and {Constraints.PositionMax}."
        );

        // OptionType: required
        public static Error OptionTypeRequired => Error.Validation("OptionValue.OptionTypeRequired", "OptionType is required.");

        #endregion

        #region NotFound
        public static Error NotFound(Guid id) => Error.NotFound(
            "OptionValue.NotFound",
            $"OptionValue with ID '{id}' was not found."
        );
        #endregion

        #region Conflicts
        public static Error NameAlreadyExists => Error.Conflict("OptionValue.NameAlreadyExists", "An OptionValue with the same name already exists for this OptionType.");
        #endregion
    }
    #endregion

    private OptionValue() { }

    public static ErrorOr<OptionValue> Create(Guid optionTypeId, string name, string presentation, int position = 0)
    {
        if (optionTypeId == Guid.Empty)
            return Errors.OptionTypeRequired;
        if (string.IsNullOrWhiteSpace(name))
            return Errors.NameRequired;
        if (string.IsNullOrWhiteSpace(presentation))
            return Errors.PresentationRequired;

        OptionValue ov = new OptionValue
        {
            OptionTypeId = optionTypeId,
            Name = name.Trim(),
            Presentation = presentation.Trim(),
            Position = Math.Max(position, 0),
            PublicMetadata = new Dictionary<string, string?>(),
            PrivateMetadata = new Dictionary<string, string?>()
        };

        ov.AddDomainEvent(new Events.Created(ov.Id));
        return ov;
    }

    public ErrorOr<OptionValue> Update(string? name = null, string? presentation = null, int? position = null)
    {
        bool changed = false;
        bool presentationChanged = false;

        if (!string.IsNullOrWhiteSpace(name) && name.Trim() != Name)
        {
            Name = name.Trim();
            changed = true;
        }
        if (presentation != null && presentation.Trim() != Presentation)
        {
            if (string.IsNullOrWhiteSpace(presentation)) return Error.Validation("OptionValue.PresentationRequired", "Presentation is required.");
            Presentation = presentation.Trim();
            changed = true;
            presentationChanged = true;
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
            // mimic after_update behavior: touch related products when presentation changes
            if (presentationChanged)
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

    /// <summary>
    /// Request a touch of related variants and products. Handled by event handlers which will mark related entities as updated.
    /// </summary>
    public void Touch()
    {
        AddDomainEvent(new Events.TouchVariants(Id));
        AddDomainEvent(new Events.TouchProducts(Id));
    }

    public static class Events
    {
        public record Created(Guid OptionValueId) : DomainEvent;
        public record Updated(Guid OptionValueId) : DomainEvent;
        public record Deleted(Guid OptionValueId) : DomainEvent;
        public record TouchVariants(Guid OptionValueId) : DomainEvent;
        public record TouchProducts(Guid OptionValueId) : DomainEvent;
    }
}
