using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;
using Core.Catalog.Taxonomies;

namespace Core.Catalog.Prototypes;

/// <summary>
/// Join entity between Prototype and Taxon (ports Spree::PrototypeTaxon)
/// </summary>
public sealed class PrototypeTaxon : AuditableEntity
{
    #region Properties

    public Guid PrototypeId { get; set; }
    public Prototype? Prototype { get; set; }

    public Guid TaxonId { get; set; }
    public Taxon? Taxon { get; set; }

    #endregion

    #region Errors

    public static class Errors
    {
        public static Error PrototypeRequired => Error.Validation("PrototypeTaxon.PrototypeRequired", "Prototype is required.");
        public static Error TaxonRequired => Error.Validation("PrototypeTaxon.TaxonRequired", "Taxon is required.");
        public static Error DuplicateAssociation => Error.Conflict("PrototypeTaxon.Duplicate", "A prototype-taxonomy association already exists for the given prototype and taxon.");
    }

    #endregion

    #region Constructors

    private PrototypeTaxon() { }

    #endregion

    #region Factory

    public static ErrorOr<PrototypeTaxon> Create(Guid prototypeId, Guid taxonId)
    {
        if (prototypeId == Guid.Empty) return Errors.PrototypeRequired;
        if (taxonId == Guid.Empty) return Errors.TaxonRequired;

        PrototypeTaxon pt = new PrototypeTaxon
        {
            PrototypeId = prototypeId,
            TaxonId = taxonId
        };

        pt.AddDomainEvent(new Events.Created(pt.Id));
        return pt;
    }

    #endregion

    #region Behavior

    public ErrorOr<PrototypeTaxon> Update(Guid? prototypeId = null, Guid? taxonId = null)
    {
        bool changed = false;

        if (prototypeId.HasValue && prototypeId.Value != PrototypeId)
        {
            if (prototypeId.Value == Guid.Empty) return Errors.PrototypeRequired;
            PrototypeId = prototypeId.Value;
            changed = true;
        }

        if (taxonId.HasValue && taxonId.Value != TaxonId)
        {
            if (taxonId.Value == Guid.Empty) return Errors.TaxonRequired;
            TaxonId = taxonId.Value;
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

    #endregion

    #region Events

    public static class Events
    {
        public record Created(Guid PrototypeTaxonId) : DomainEvent;
        public record Updated(Guid PrototypeTaxonId) : DomainEvent;
        public record Deleted(Guid PrototypeTaxonId) : DomainEvent;
    }

    #endregion
}