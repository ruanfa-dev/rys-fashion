using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Catalogs;

/// <summary>
/// Domain model converted from Spree::ShippingCategory (simplified).
/// - Holds products and shipping method category joins. Infra/repository should enforce uniqueness and persistence concerns.
/// </summary>
public sealed class ShippingCategory : AuditableEntity
{
    public const string DIGITAL_NAME = "Digital";

    public string Name { get; set; } = default!;

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<ShippingMethodCategory> ShippingMethodCategories { get; set; } = new List<ShippingMethodCategory>();
    public IEnumerable<ShippingMethod> ShippingMethods => ShippingMethodCategories.Select(c => c.ShippingMethod).Where(m => m != null).ToList();

    private ShippingCategory() { }

    public static ShippingCategory Create(string name)
        => new ShippingCategory { Name = name?.Trim() ?? string.Empty };

    public void Update(string? name = null)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name!.Trim();
        MarkAsUpdated();
    }

    /// <summary>
    /// Repository/infra should implement lookup. This in-domain helper performs an in-memory search.
    /// </summary>
    public static ShippingCategory? Digital(IEnumerable<ShippingCategory> categories)
        => categories?.FirstOrDefault(c => string.Equals(c.Name, DIGITAL_NAME, StringComparison.OrdinalIgnoreCase));

    #region Validation / Constraints / Errors

    public static class Constraints
    {
        public const int NameMaxLength = 255;
    }

    public static class Errors
    {
        public static Error NameRequired => Error.Validation("ShippingCategory.NameRequired", "Shipping category name is required.");
        public static Error NameTooLong => Error.Validation("ShippingCategory.NameTooLong", $"Name must be at most {Constraints.NameMaxLength} characters.");
        public static Error NotFound(Guid id) => Error.NotFound("ShippingCategory.NotFound", $"ShippingCategory with ID '{id}' was not found.");
    }

    public static List<Error> ValidateModel(string? name)
    {
        var errors = new List<Error>();
        if (string.IsNullOrWhiteSpace(name)) errors.Add(Errors.NameRequired);
        else if (name!.Length > Constraints.NameMaxLength) errors.Add(Errors.NameTooLong);
        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid ShippingCategoryId) : DomainEvent;
        public record Updated(Guid ShippingCategoryId) : DomainEvent;
        public record Deleted(Guid ShippingCategoryId) : DomainEvent;
    }

    #endregion
}
