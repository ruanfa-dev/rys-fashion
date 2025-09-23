using ErrorOr;

using SharedKernel.Domain.Primitives;

namespace Core.Catalogs;

public sealed class PrototypeTaxon : AuditableEntity
{
    public Guid PrototypeId { get; set; }
    public Prototype Prototype { get; set; } = default!;

    public Guid TaxonId { get; set; }
    public Taxon Taxon { get; set; } = default!;

    private PrototypeTaxon() { }

    public static PrototypeTaxon Create(Guid prototypeId, Guid taxonId)
    {
        return new PrototypeTaxon
        {
            PrototypeId = prototypeId,
            TaxonId = taxonId
        };
    }

    /// <summary>
    /// Lightweight validation used by application/handlers before persistence.
    /// Note: uniqueness must be checked against the database.
    /// </summary>
    public static List<Error> ValidateModel(Guid prototypeId, Guid taxonId)
    {
        var errors = new List<Error>();

        if (prototypeId == Guid.Empty)
            errors.Add(Errors.PrototypeRequired);

        if (taxonId == Guid.Empty)
            errors.Add(Errors.TaxonRequired);

        return errors;
    }

    public static class Errors
    {
        public static Error PrototypeRequired =>
            Error.Validation("PrototypeTaxon.PrototypeRequired", "Prototype is required.");

        public static Error TaxonRequired =>
            Error.Validation("PrototypeTaxon.TaxonRequired", "Taxon is required.");

        public static Error DuplicateAssignment(Guid prototypeId, Guid taxonId) =>
            Error.Conflict(
                "PrototypeTaxon.DuplicateAssignment",
                $"Prototype '{prototypeId}' is already assigned to Taxon '{taxonId}'.");
    }
}
