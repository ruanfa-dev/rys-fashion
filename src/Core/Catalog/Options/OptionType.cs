using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;
using Core.Catalog.Products;

namespace Core.Catalog.Options;

public sealed class OptionType : AuditableEntity
{

    public string Name { get; set; } = null!;
    public string Presentation { get; set; } = null!;
    public bool Filterable { get; set; }
    public int Position { get; set; }

    public ICollection<OptionValue> OptionValues { get; set; } = new List<OptionValue>();
    public ICollection<ProductOptionType> ProductOptionTypes { get; set; } = new List<ProductOptionType>();

    public ICollection<OptionTypePrototype> OptionTypePrototypes { get; set; } = new List<OptionTypePrototype>();

    #region Constraints
    public static class Constraints
    {
        public const int PresentationMaxLength = 255;
    }
    #endregion

    #region Errors
    public static class Errors
    {
        public static Error PresentationRequired => Error.Validation("OptionType.PresentationRequired", "Presentation is required.");
        public static Error NotFound(Guid id) => Error.NotFound("OptionType.NotFound", $"OptionType with ID '{id}' was not found.");
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
        }

        return this;
    }

    public ErrorOr<Deleted> Delete()
    {
        AddDomainEvent(new Events.Deleted(Id));
        return Result.Deleted;
    }

    public static class Events
    {
        public record Created(Guid OptionTypeId) : DomainEvent;
        public record Updated(Guid OptionTypeId) : DomainEvent;
        public record Deleted(Guid OptionTypeId) : DomainEvent;
    }
}