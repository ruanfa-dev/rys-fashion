using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Catalogs;

/// <summary>
/// Domain model converted from Spree::ShippingMethod (simplified).
/// - Calculator implementations, routing/tracking services and price formatting live in infra.
/// - Zones/ShippingCategory relations are represented as navigations; repository layer should provide queries.
/// </summary>
public sealed class ShippingMethod : AuditableEntity
{
    #region Public configuration hooks (set by infrastructure)

    // Infrastructure may provide a factory that returns a calculator type names or descriptors.
    // Example: ShippingMethod.CalculatorRegistry = () => new[] { "FlatRate", "FlexiRate" };
    public static Func<IEnumerable<string>>? CalculatorRegistry { get; set; }

    // Infrastructure may provide a tracking-number service factory.
    // Example: ShippingMethod.TrackingNumberServiceFactory = tracking => new SomeTrackingService(tracking);
    public static Func<string, ITrackingNumberService?>? TrackingNumberServiceFactory { get; set; }

    #endregion

    #region Constants

    public const int DISPLAY_ON_FRONT_END = 1;
    public const int DISPLAY_ON_BACK_END = 2;

    #endregion

    #region Properties

    public string Name { get; set; } = default!;
    /// <summary>
    /// 1 = front end, 2 = back end, both can be represented by a specific constant in infra.
    /// </summary>
    public int DisplayOn { get; set; }

    public int? EstimatedTransitBusinessDaysMin { get; set; }
    public int? EstimatedTransitBusinessDaysMax { get; set; }

    /// <summary>
    /// Optional tax category (nullable).
    /// </summary>
    public Guid? TaxCategoryId { get; set; }
    public TaxCategory? TaxCategory { get; set; }

    /// <summary>
    /// Name of the shipping calculator implementation that infra will resolve (e.g. "DigitalDelivery", "FlatRate").
    /// </summary>
    public string? CalculatorType { get; set; }

    /// <summary>
    /// Optional template tracking URL that contains a placeholder like ":tracking".
    /// Infra should provide URL building if needed.
    /// </summary>
    public string? TrackingUrl { get; set; }

    #endregion

    #region Navigations

    public ICollection<ShippingMethodCategory> ShippingMethodCategories { get; set; } = new List<ShippingMethodCategory>();
    public IEnumerable<ShippingCategory> ShippingCategories => ShippingMethodCategories.Select(sc => sc.ShippingCategory);

    public ICollection<ShippingRate> ShippingRates { get; set; } = new List<ShippingRate>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();

    public ICollection<ShippingMethodZone> ShippingMethodZones { get; set; } = new List<ShippingMethodZone>();
    public IEnumerable<Zone> Zones => ShippingMethodZones.Select(z => z.Zone).Where(z => z != null).ToList();

    #endregion

    #region Constructors / Factory

    private ShippingMethod() { }

    public static ShippingMethod Create(string name, int displayOn, string? calculatorType = null)
    {
        return new ShippingMethod
        {
            Name = name,
            DisplayOn = displayOn,
            CalculatorType = calculatorType
        };
    }

    public void Update(string? name = null, int? displayOn = null, string? calculatorType = null)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name!;
        if (displayOn.HasValue) DisplayOn = displayOn.Value;
        if (calculatorType != null) CalculatorType = calculatorType;
        MarkAsUpdated();
    }

    #endregion

    #region Behavior

    /// <summary>
    /// Check whether the method applies to the given address.
    /// Repository/zone logic should be implemented in infra; this is a best-effort in-domain check.
    /// </summary>
    public bool Includes(Address? address)
    {
        if (!RequiresZoneCheck()) return true;
        if (address == null) return false;

        // Best-effort: ask zones to include the address. Zone.Include(Address) expected in infra/domain.
        return Zones.Any(z => z.Include(address));
    }

    public bool RequiresZoneCheck()
    {
        // Digital delivery calculators don't require zone checking.
        return !(CalculatorType?.Contains("DigitalDelivery", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    public bool Digital()
    {
        return CalculatorType?.Contains("DigitalDelivery", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// Build a tracking URL using the configured template (replacing :tracking) or by delegating to a service provided by infra.
    /// </summary>
    public string? BuildTrackingUrl(string? tracking)
    {
        if (string.IsNullOrWhiteSpace(tracking)) return null;

        var normalized = tracking!.ToUpperInvariant();

        // If infra provided a tracking service factory, prefer it (service may validate and build URL)
        var svc = TrackingNumberServiceFactory?.Invoke(normalized);
        if (svc != null && svc.Valid())
            return svc.TrackingUrl();

        // fallback to template replacement
        if (string.IsNullOrWhiteSpace(TrackingUrl)) return null;
        var encoded = Uri.EscapeDataString(normalized);
        return TrackingUrl.Replace(":tracking", encoded, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns calculator identifiers available for shipping methods (delegated to infra via CalculatorRegistry).
    /// </summary>
    public static IEnumerable<string> Calculators()
    {
        return CalculatorRegistry?.Invoke() ?? Enumerable.Empty<string>();
    }

    public string? DeliveryRange()
    {
        if (!EstimatedTransitBusinessDaysMin.HasValue && !EstimatedTransitBusinessDaysMax.HasValue) return null;

        if (EstimatedTransitBusinessDaysMin == EstimatedTransitBusinessDaysMax)
            return EstimatedTransitBusinessDaysMin?.ToString();

        return string.Join("-", new[] { EstimatedTransitBusinessDaysMin?.ToString(), EstimatedTransitBusinessDaysMax?.ToString() }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    /// <summary>
    /// Human friendly estimated price description. Infra should use calculators and money formatting.
    /// </summary>
    public string? DisplayEstimatedPrice(Func<string, string>? moneyFormatter = null)
    {
        if (string.IsNullOrWhiteSpace(CalculatorType)) return null;
        // Placeholder behavior — infra should resolve a calculator and format price.
        if (moneyFormatter != null)
            return $"{CalculatorType}: {moneyFormatter("0.00")}";
        return $"{CalculatorType}";
    }

    #endregion

    #region Validation / Constraints / Errors

    public static class Constraints
    {
        public const int NameMaxLength = 255;
    }

    public static class Errors
    {
        public static Error NameRequired => Error.Validation("ShippingMethod.NameRequired", "Shipping method name is required.");
        public static Error DisplayOnRequired => Error.Validation("ShippingMethod.DisplayOnRequired", "DisplayOn is required.");
        public static Error TransitDaysInvalid => Error.Validation("ShippingMethod.TransitDaysInvalid", "Estimated transit days must be >= 1 when present.");
        public static Error NotFound(Guid id) => Error.NotFound("ShippingMethod.NotFound", $"ShippingMethod with ID '{id}' was not found.");
    }

    public static List<Error> ValidateModel(string? name, int? displayOn, int? minDays, int? maxDays)
    {
        var errors = new List<Error>();
        if (string.IsNullOrWhiteSpace(name)) errors.Add(Errors.NameRequired);
        if (!displayOn.HasValue) errors.Add(Errors.DisplayOnRequired);

        if (minDays.HasValue && minDays < 1) errors.Add(Errors.TransitDaysInvalid);
        if (maxDays.HasValue && maxDays < 1) errors.Add(Errors.TransitDaysInvalid);

        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid ShippingMethodId) : DomainEvent;
        public record Updated(Guid ShippingMethodId) : DomainEvent;
        public record Deleted(Guid ShippingMethodId) : DomainEvent;
    }

    #endregion

    #region Supporting types (hooks for infra)

    /// <summary>
    /// Minimal interface for a tracking-number helper service. Infra should provide an implementation and
    /// assign ShippingMethod.TrackingNumberServiceFactory accordingly.
    /// </summary>
    public interface ITrackingNumberService
    {
        bool Valid();
        string? TrackingUrl();
    }

    #endregion
}
