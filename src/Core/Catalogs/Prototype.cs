using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Core.Catalogs;

/// <summary>
/// Domain model converted from Spree::Prototype (Ruby).
/// - Contains relationships to prototype-property join, option-type join and prototype-taxon join.
/// - Exposes factory/update methods and reusable validation errors.
/// </summary>
public partial class Prototype : AuditableEntity
{
    #region Properties

    /// <summary>
    /// Human readable internal name (required).
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Optional description for the prototype.
    /// </summary>
    public string? Description { get; set; }

    #endregion

    #region Relationships

    // Join entity between Prototype and Property
    public ICollection<PrototypeProperty> PrototypeProperties { get; set; } = new List<PrototypeProperty>();

    // Convenience accessors (not mapped by EF as navigation collections - they materialize from join collection)
    // Use these in application code when PrototypeProperties are loaded.
    public IEnumerable<Property> Properties => PrototypeProperties.Select(pp => pp.Property);

    // Join entity between Prototype and OptionType (if implemented)
    public ICollection<PrototypeOptionType> OptionTypePrototypes { get; set; } = new List<PrototypeOptionType>();
    public IEnumerable<OptionType> OptionTypes => OptionTypePrototypes.Select(op => op.OptionType);

    // Join entity between Prototype and Taxon (if implemented)
    public ICollection<PrototypeTaxon> PrototypeTaxons { get; set; } = new List<PrototypeTaxon>();
    public IEnumerable<Taxon> Taxons => PrototypeTaxons.Select(pt => pt.Taxon);

    #endregion

    #region Constructors

    public Prototype()
    {
    }

    #endregion

    #region Factory / Update

    public static Prototype Create(string name, string? description = null)
    {
        return new Prototype
        {
            Name = name,
            Description = description
        };
    }

    public void Update(string? name = null, string? description = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name!;
        if (description != null)
            Description = description;
    }

    #endregion

    #region Validation helpers & constraints

    public static class Constraints
    {
        // Name: max length 100, min length 2, alphanumeric, spaces, hyphens, underscores
        public const int NameMaxLength = 100;
        public const int NameMinLength = 2;
        public const string NameRegex = @"^[a-zA-Z0-9 _-]+$";

        // Description: max length 1000
        public const int DescriptionMaxLength = 1000;
    }

    public static class Errors
    {
        public static Error NameRequired => Error.Validation(
            code: "Prototype.NameRequired",
            description: "Prototype name is required.");

        public static Error InvalidNameLength => Error.Validation(
            code: "Prototype.InvalidNameLength",
            description: $"Prototype name must be between {Constraints.NameMinLength} and {Constraints.NameMaxLength} characters long.");

        public static Error InvalidNameFormat => Error.Validation(
            code: "Prototype.InvalidNameFormat",
            description: "Prototype name contains invalid characters. Only alphanumeric characters, spaces, hyphens and underscores are allowed.");

        public static Error InvalidDescriptionLength => Error.Validation(
            code: "Prototype.InvalidDescriptionLength",
            description: $"Prototype description must be at most {Constraints.DescriptionMaxLength} characters long.");

        public static Error NotFound(Guid id) => Error.NotFound(
            code: "Prototype.NotFound",
            description: $"Prototype with ID '{id}' was not found.");

        public static Error NameAlreadyExists(string name) => Error.Conflict(
            code: "Prototype.NameAlreadyExists",
            description: $"A prototype with the name '{name}' already exists.");
    }
    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid PrototypeId) : DomainEvent;
        public record Updated(Guid PrototypeId) : DomainEvent;
        public record Deleted(Guid PrototypeId) : DomainEvent;
    }

    #endregion
}
