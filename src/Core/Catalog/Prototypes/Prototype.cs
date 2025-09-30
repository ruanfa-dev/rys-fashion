using Core.Catalog.Options;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;
using SharedKernel.Domain.Attributes.Metadata;

namespace Core.Catalog.Prototypes;

/// <summary>
/// Domain model for Prototype (ports Spree::Prototype).
/// Holds associations to properties via PrototypeProperty.
/// </summary>
public sealed class Prototype : AuditableEntity, IMetadataSupport
{
    #region Properties

    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int Position { get; set; }

    // Associations
    public ICollection<PropertyPrototype> PropertyPrototypes { get; set; } = new List<PropertyPrototype>();
    public ICollection<PrototypeTaxon> PrototypeTaxons { get; set; } = new List<PrototypeTaxon>();
    public ICollection<OptionTypePrototype> OptionTypePrototypes { get; set; } = new List<OptionTypePrototype>();

    public IDictionary<string, string?>? PublicMetadata { get; set; } = new Dictionary<string, string?>();
    public IDictionary<string, string?>? PrivateMetadata { get; set; } = new Dictionary<string, string?>();

    #endregion

    #region Constraints

    public static class Constraints
    {
        public const int NameMinLength = 1;
        public const int NameMaxLength = 200;

        public const int DescriptionMaxLength = 2000;

        public const int PositionMin = 0;
        public const int PositionMax = 100000;
    }

    #endregion

    #region Errors

    public static class Errors
    {
        public static Error NameRequired => Error.Validation("Prototype.NameRequired", "Prototype name is required.");
        public static Error InvalidNameLength => Error.Validation(
            "Prototype.InvalidNameLength",
            $"Prototype name must be between {Constraints.NameMinLength} and {Constraints.NameMaxLength} characters long."
        );

        public static Error DescriptionTooLong => Error.Validation(
            "Prototype.DescriptionTooLong",
            $"Prototype description must be at most {Constraints.DescriptionMaxLength} characters long."
        );

        public static Error InvalidPosition => Error.Validation(
            "Prototype.InvalidPosition",
            $"Position must be between {Constraints.PositionMin} and {Constraints.PositionMax}."
        );

        public static Error NotFound(Guid id) => Error.NotFound(
            "Prototype.NotFound",
            $"Prototype with ID '{id}' was not found."
        );

        public static Error UnexpectedError(string operationName, Exception? ex = null) => Error.Unexpected(
          code: $"Prototype.{operationName}UnexpectedError",
          description: $"An unexpected error occurred during execution of {operationName} operation on Prototype. {ex?.Message}");
    }

    #endregion

    #region Constructors

    private Prototype() { }

    #endregion

    #region Factory

    public static ErrorOr<Prototype> Create(string name,
        string? description = null,
        int position = 0,
        IDictionary<string, string?>? publicMetadata = null,
        IDictionary<string, string?>? privateMetadata = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Errors.NameRequired;

        if (name.Length is < Constraints.NameMinLength or > Constraints.NameMaxLength)
            return Errors.InvalidNameLength;

        if (description is { Length: > Constraints.DescriptionMaxLength })
            return Errors.DescriptionTooLong;

        var prototype = new Prototype
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            Position = Math.Clamp(position, Constraints.PositionMin, Constraints.PositionMax)
        };

        // assign optional metadata if provided
        if (publicMetadata != null)
            prototype.PublicMetadata = new Dictionary<string, string?>(publicMetadata);
        if (privateMetadata != null)
            prototype.PrivateMetadata = new Dictionary<string, string?>(privateMetadata);

        prototype.AddDomainEvent(new Events.Created(prototype.Id));
        return prototype;
    }

    #endregion

    #region Behavior

    public ErrorOr<Prototype> Update(string? name = null, string? description = null, int? position = null,
        IDictionary<string, string?>? publicMetadata = null,
        IDictionary<string, string?>? privateMetadata = null)
    {
        var changed = false;

        if (!string.IsNullOrWhiteSpace(name) && name.Trim() != Name)
        {
            if (name.Length < Constraints.NameMinLength || name.Length > Constraints.NameMaxLength)
                return Errors.InvalidNameLength;

            Name = name.Trim();
            changed = true;
        }

        if (description is not null && description != Description)
        {
            if (description.Length > Constraints.DescriptionMaxLength)
                return Errors.DescriptionTooLong;

            Description = description.Trim();
            changed = true;
        }

        if (position.HasValue && position.Value != Position)
        {
            if (position.Value < Constraints.PositionMin || position.Value > Constraints.PositionMax)
                return Errors.InvalidPosition;

            Position = position.Value;
            changed = true;
        }

        if (publicMetadata != null)
        {
            PublicMetadata = new Dictionary<string, string?>(publicMetadata);
            changed = true;
        }

        if (privateMetadata != null)
        {
            PrivateMetadata = new Dictionary<string, string?>(privateMetadata);
            changed = true;
        }

        if (changed)
        {
            MarkAsUpdated();
            AddDomainEvent(new Events.Updated(Id));
        }

        return this;
    }

    public ErrorOr<Deleted> Delete()
    {
        // If associated properties exist, it's allowed to delete the prototype
        // but concrete business rules may restrict deletion. Keep simple here.
        AddDomainEvent(new Events.Deleted(Id));
        return Result.Deleted;
    }

    #endregion

    #region Events

    public static class Events
    {
        public record Created(Guid PrototypeId) : DomainEvent;
        public record Updated(Guid PrototypeId) : DomainEvent;
        public record Deleted(Guid PrototypeId) : DomainEvent;
    }

    #endregion
}