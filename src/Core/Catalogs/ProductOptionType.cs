using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Join entity between Product and OptionType (acts_as_list scope: product).
/// DB-level constraints (unique composite index on ProductId + OptionTypeId, table name) should be configured in EF.
/// </summary>
public sealed class ProductOptionType : AuditableEntity
{
    // Join keys
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public Guid OptionTypeId { get; set; }
    public OptionType OptionType { get; set; } = default!;

    // acts_as_list -> position (nullable to match Rails allow_blank/allow_nil behavior)
    public int? Position { get; set; }

    private ProductOptionType() { }

    public static ProductOptionType Create(Guid productId, Guid optionTypeId, int? position = null)
    {
        return new ProductOptionType
        {
            ProductId = productId,
            OptionTypeId = optionTypeId,
            Position = position
        };
    }

    public void UpdatePosition(int? position)
    {
        Position = position;
    }

    /// <summary>
    /// Lightweight validation used by application/handlers before persistence.
    /// Uniqueness must be enforced/checked at the database level.
    /// </summary>
    public static List<Error> ValidateModel(Guid productId, Guid optionTypeId, int? position)
    {
        var errors = new List<Error>();

        if (productId == Guid.Empty)
            errors.Add(Errors.ProductRequired);

        if (optionTypeId == Guid.Empty)
            errors.Add(Errors.OptionTypeRequired);

        if (position.HasValue && position < 0)
            errors.Add(Errors.InvalidPosition);

        return errors;
    }

    public static class Errors
    {
        public static Error ProductRequired => Error.Validation("ProductOptionType.ProductRequired", "Product is required.");
        public static Error OptionTypeRequired => Error.Validation("ProductOptionType.OptionTypeRequired", "OptionType is required.");
        public static Error InvalidPosition => Error.Validation("ProductOptionType.InvalidPosition", "Position must be a non-negative integer.");
        public static Error AlreadyLinked(Guid productId, Guid optionTypeId) =>
            Error.Conflict("ProductOptionType.AlreadyLinked", $"Product '{productId}' is already linked to OptionType '{optionTypeId}'.");
    }

    public static class Events
    {
        public record Created(Guid ProductOptionTypeId) : DomainEvent;
        public record Updated(Guid ProductOptionTypeId) : DomainEvent;
        public record Deleted(Guid ProductOptionTypeId) : DomainEvent;
    }
}
