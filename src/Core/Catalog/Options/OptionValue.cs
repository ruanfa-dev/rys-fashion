using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using Core.Catalog.Variants;

namespace Core.Catalog.Options;

public sealed class OptionValue : AuditableEntity
{
    public Guid OptionTypeId { get; set; }
    public OptionType OptionType { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string Presentation { get; set; } = null!;
    public int Position { get; set; }

    public ICollection<OptionValueVariant> OptionValueVariants { get; set; } = new List<OptionValueVariant>();
    public ICollection<Variant> Variants { get; set; } = new List<Variant>();

    private OptionValue() { }

    public static ErrorOr<OptionValue> Create(Guid optionTypeId, string name, string presentation, int position = 0)
    {
        if (optionTypeId == Guid.Empty) return Error.Validation("OptionValue.OptionTypeRequired", "OptionType is required.");
        if (string.IsNullOrWhiteSpace(name)) return Error.Validation("OptionValue.NameRequired", "Name is required.");
        if (string.IsNullOrWhiteSpace(presentation)) return Error.Validation("OptionValue.PresentationRequired", "Presentation is required.");

        var ov = new OptionValue
        {
            OptionTypeId = optionTypeId,
            Name = name.Trim(),
            Presentation = presentation.Trim(),
            Position = Math.Max(position, 0)
        };

        ov.AddDomainEvent(new Events.Created(ov.Id));
        return ov;
    }

    public ErrorOr<OptionValue> Update(string? name = null, string? presentation = null, int? position = null)
    {
        var changed = false;
        if (!string.IsNullOrWhiteSpace(name) && name.Trim() != Name)
        {
            Name = name.Trim();
            changed = true;
        }
        if (presentation != null && presentation.Trim() != Presentation)
        {
            Presentation = presentation.Trim();
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
        public record Created(Guid OptionValueId) : DomainEvent;
        public record Updated(Guid OptionValueId) : DomainEvent;
        public record Deleted(Guid OptionValueId) : DomainEvent;
    }
}
