using System;
using System.Collections.Generic;
using System.Linq;

using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Domain model converted from Spree::Country (simplified).
/// - Holds basic country fields and navigation to states / zones / addresses.
/// - Repository / infra must implement uniqueness, ordering and persistence concerns.
/// </summary>
public sealed class Country : AuditableEntity, IComparable<Country>
{
    #region Properties

    public string Name { get; set; } = default!;
    public string IsoName { get; set; } = default!;
    public string Iso { get; set; } = default!;   // 2-letter code
    public string Iso3 { get; set; } = default!;  // 3-letter code

    #endregion

    #region Relationships

    // Addresses and States (persistence layer should enforce ordering on States)
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<State> States { get; set; } = new List<State>();

    // All zone members — repository/EF should ideally scope these to zoneable_type='Spree::Country'
    // The domain-level helper `CountryZoneMembers` returns only members that actually reference this country.
    public ICollection<ZoneMember> ZoneMembers { get; set; } = new List<ZoneMember>();

    public IEnumerable<Zone> Zones =>
        ZoneMembers
            .Where(zm => !string.IsNullOrWhiteSpace(zm.ZoneableType) &&
                         (zm.ZoneableType.EndsWith("Country", StringComparison.OrdinalIgnoreCase) ||
                          zm.ZoneableType.Equals("Spree::Country", StringComparison.OrdinalIgnoreCase)))
            .Select(zm => zm.Zone)
            .Where(z => z != null)
            .ToList()!;

    #endregion

    #region Construction / Factory

    private Country() { }

    public static Country Create(string name, string isoName, string iso, string iso3)
    {
        return new Country
        {
            Name = name?.Trim() ?? string.Empty,
            IsoName = isoName?.Trim() ?? string.Empty,
            Iso = iso?.Trim() ?? string.Empty,
            Iso3 = iso3?.Trim() ?? string.Empty
        };
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Lookup helper that finds a country by its 2- or 3-letter ISO code (case-insensitive).
    /// Repository-level lookup is preferred for large datasets.
    /// </summary>
    public static Country? FindByIso(IEnumerable<Country> countries, string iso)
    {
        if (countries == null || string.IsNullOrWhiteSpace(iso)) return null;
        var q = iso.Trim();
        return countries.FirstOrDefault(c =>
            string.Equals(c.Iso, q, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.Iso3, q, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Convenience alias that mirrors Spree::Country.by_iso semantics when an in-memory collection is available.
    /// </summary>
    public static Country? ByIso(IEnumerable<Country> countries, string iso) => FindByIso(countries, iso);

    /// <summary>
    /// Checks if this country is the default for the provided store.
    /// The Store domain/infra is responsible for exposing default country details.
    /// </summary>
    public bool IsDefault(Store? store)
    {
        if (store == null) return false;

        // Best-effort: compare by object reference or by Id if store exposes DefaultCountry or DefaultCountryId.
        if (store.DefaultCountry != null)
            return store.DefaultCountry.Id == this.Id;

        var prop = store.GetType().GetProperty("DefaultCountryId");
        if (prop != null && prop.GetValue(store) is Guid defaultCountryId)
            return defaultCountryId == this.Id;

        return false;
    }

    public override string ToString() => Name;

    #endregion

    #region Validation / Constraints / Errors

    public static class Constraints
    {
        public const int NameMaxLength = 255;
        public const int IsoMaxLength = 3;
        public const int IsoMinLength = 2;
    }

    public static class Errors
    {
        public static Error NameRequired => Error.Validation("Country.NameRequired", "Country name is required.");
        public static Error IsoRequired => Error.Validation("Country.IsoRequired", "ISO code is required.");
        public static Error IsoInvalid => Error.Validation("Country.IsoInvalid", $"ISO must be {Constraints.IsoMinLength} or {Constraints.IsoMaxLength} characters.");
        public static Error NotFound(Guid id) => Error.NotFound("Country.NotFound", $"Country with ID '{id}' was not found.");
    }

    public static List<Error> ValidateModel(string? name, string? iso)
    {
        var errors = new List<Error>();
        if (string.IsNullOrWhiteSpace(name)) errors.Add(Errors.NameRequired);
        if (string.IsNullOrWhiteSpace(iso)) errors.Add(Errors.IsoRequired);
        else
        {
            var len = iso!.Trim().Length;
            if (len < Constraints.IsoMinLength || len > Constraints.IsoMaxLength) errors.Add(Errors.IsoInvalid);
        }
        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid CountryId) : DomainEvent;
        public record Updated(Guid CountryId) : DomainEvent;
        public record Deleted(Guid CountryId) : DomainEvent;
    }

    #endregion

    #region Comparisons

    public int CompareTo(Country? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }

    #endregion
}