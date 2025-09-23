using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Catalogs;

/// <summary>
/// Domain model port of Spree::Zone (simplified).
/// - This is an in-memory/behavioral port; repository/EF configuration should implement persistence, scopes and efficient queries.
/// - Methods that in Rails use joins/SQL return best-effort results from provided collections; application/infra should replace with DB queries where needed.
/// </summary>
public sealed class Zone : AuditableEntity
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool DefaultTax { get; set; }

    /// <summary>
    /// Optional explicit kind: "country" or "state". If null the kind is inferred from members.
    /// </summary>
    public string? KindOverride { get; set; }

    public ICollection<ZoneMember> ZoneMembers { get; set; } = new List<ZoneMember>();
    public ICollection<TaxRate> TaxRates { get; set; } = new List<TaxRate>();

    public ICollection<ShippingMethodZone> ShippingMethodZones { get; set; } = new List<ShippingMethodZone>();
    public IEnumerable<ShippingMethod> ShippingMethods => ShippingMethodZones.Select(z => z.ShippingMethod).Where(m => m != null).ToList();

    private Zone() { }

    public static Zone Create(string name, string? description = null, bool defaultTax = false)
        => new Zone { Name = name?.Trim() ?? string.Empty, Description = description, DefaultTax = defaultTax };

    // --- Kind helpers ---

    /// <summary>
    /// "country" or "state" or null when undetermined.
    /// </summary>
    public string? Kind
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(KindOverride)) return KindOverride;
            var notNull = ZoneMembers.Where(m => !string.IsNullOrWhiteSpace(m.ZoneableType)).ToList();
            if (!notNull.Any()) return null;
            // choose the last inserted kind as a best-effort heuristic
            var last = notNull.Last().ZoneableType!;
            if (last.EndsWith("Country", StringComparison.OrdinalIgnoreCase)) return "country";
            if (last.EndsWith("State", StringComparison.OrdinalIgnoreCase)) return "state";
            return null;
        }
    }

    public bool IsCountry() => string.Equals(Kind, "country", StringComparison.OrdinalIgnoreCase);
    public bool IsState() => string.Equals(Kind, "state", StringComparison.OrdinalIgnoreCase);

    // --- Member helpers ---

    public IEnumerable<object?> Zoneables()
    {
        // Best-effort: return in-memory navigation if ZoneMember.Zoneable populated.
        return ZoneMembers.Select(m => m.Zoneable).Where(z => z != null).ToList();
    }

    public IEnumerable<Guid> CountryIds()
        => IsCountry() ? ZoneMembers.Where(m => string.Equals(m.ZoneableType, typeof(Country).Name, StringComparison.OrdinalIgnoreCase))
                                  .Select(m => m.ZoneableId ?? Guid.Empty).Where(id => id != Guid.Empty).ToList()
                       : Enumerable.Empty<Guid>();

    public IEnumerable<Guid> StateIds()
        => IsState() ? ZoneMembers.Where(m => string.Equals(m.ZoneableType, typeof(State).Name, StringComparison.OrdinalIgnoreCase))
                                 .Select(m => m.ZoneableId ?? Guid.Empty).Where(id => id != Guid.Empty).ToList()
                     : Enumerable.Empty<Guid>();

    public void SetCountryIds(IEnumerable<Guid> ids)
        => SetZoneMembers(ids, typeof(Country).Name);

    public void SetStateIds(IEnumerable<Guid> ids)
        => SetZoneMembers(ids, typeof(State).Name);

    // --- Matching / inclusion logic ---

    /// <summary>
    /// Returns true when the provided address is included in this zone (by country_id or state_id).
    /// </summary>
    public bool Includes(Address? address)
    {
        if (address == null) return false;

        foreach (var m in ZoneMembers)
        {
            if (string.Equals(m.ZoneableType, typeof(Country).Name, StringComparison.OrdinalIgnoreCase))
            {
                if (m.ZoneableId.HasValue && m.ZoneableId.Value == address.CountryId) return true;
            }
            else if (string.Equals(m.ZoneableType, typeof(State).Name, StringComparison.OrdinalIgnoreCase))
            {
                if (m.ZoneableId.HasValue && m.ZoneableId.Value == address.StateId) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Return whether this zone completely contains the target zone.
    /// Behavior mirrors Spree.contains? (best-effort with in-memory collections).
    /// </summary>
    public bool Contains(Zone target)
    {
        if (target == null) return false;
        if (IsState() && target.IsCountry()) return false;
        if (!ZoneMembers.Any() || !target.ZoneMembers.Any()) return false;

        if (Kind == target.Kind)
        {
            if (IsState())
            {
                var myStates = new HashSet<Guid>(StateIds());
                return !target.StateIds().Except(myStates).Any();
            }
            else if (IsCountry())
            {
                var myCountries = new HashSet<Guid>(CountryIds());
                return !target.CountryIds().Except(myCountries).Any();
            }
        }
        else
        {
            // cross-kind: target states' country ids must be subset of my countries
            var myCountries = new HashSet<Guid>(CountryIds());
            var targetStateCountryIds = target.ZoneMembers
                                              .Where(m => string.Equals(m.ZoneableType, typeof(State).Name, StringComparison.OrdinalIgnoreCase))
                                              .Select(m => (m.Zoneable as State)?.CountryId)
                                              .Where(cid => cid.HasValue)
                                              .Select(cid => cid!.Value)
                                              .ToList();
            return !targetStateCountryIds.Except(myCountries).Any();
        }

        return false;
    }

    // --- Static helpers that mirror Spree class methods (application should provide DB-backed equivalents) ---

    /// <summary>
    /// Best-effort in-memory search for a default tax zone from a given collection.
    /// </summary>
    public static Zone? DefaultTax(IEnumerable<Zone> zones)
        => zones?.FirstOrDefault(z => z.DefaultTax) ?? null;

    /// <summary>
    /// Return potential matching zones for the provided zone based on overlapping countries/states.
    /// Application/infra should implement DB queries for efficiency.
    /// </summary>
    public static IEnumerable<Zone> PotentialMatchingZones(Zone zone, IEnumerable<Zone> candidates)
    {
        if (zone == null || candidates == null) return Enumerable.Empty<Zone>();

        if (zone.IsCountry())
        {
            var ids = new HashSet<Guid>(zone.CountryIds());
            return candidates.Where(c => c.CountryIds().Any(id => ids.Contains(id))).Distinct();
        }
        else
        {
            var stateIds = new HashSet<Guid>(zone.StateIds());
            var stateCountries = candidates.SelectMany(c => c.StateIds())
                                           .SelectMany(sid => candidates.SelectMany(_ => Enumerable.Empty<Guid>())); // placeholder
            // best-effort: return candidates that share state ids or whose countries match target states' countries.
            return candidates.Where(c => c.StateIds().Any(sid => stateIds.Contains(sid)) ||
                                         c.CountryIds().Any(cid => zone.StateIds().Any() && c.CountryIds().Contains(cid)));
        }
    }

    /// <summary>
    /// Choose the best matching zone for the given address from a collection of zones.
    /// The order of preference is: state match, then country match. Ties are broken by member count then creation (not available here).
    /// </summary>
    public static Zone? Match(Address? address, IEnumerable<Zone> zones)
    {
        if (address == null || zones == null) return null;

        var matches = zones.Where(z => z.ZoneMembers.Any() && z.Includes(address)).ToList();
        if (!matches.Any()) return null;

        // prefer state matches
        var stateMatch = matches.FirstOrDefault(z => z.IsState());
        if (stateMatch != null) return stateMatch;

        // else prefer country match
        return matches.FirstOrDefault(z => z.IsCountry()) ?? matches.FirstOrDefault();
    }

    // --- Internal helpers ---

    private void SetZoneMembers(IEnumerable<Guid> ids, string zoneableType)
    {
        ZoneMembers.Clear();
        foreach (var id in ids.Where(i => i != Guid.Empty))
        {
            var member = ZoneMember.Create(this, zoneableType, id);
    
            ZoneMembers.Add(member);
        }
        MarkAsUpdated();
    }

    #region Validation / Errors

    public static class Errors
    {
        public static Error NameRequired => Error.Validation("Zone.NameRequired", "Zone name is required.");
        public static Error NotFound(Guid id) => Error.NotFound("Zone.NotFound", $"Zone with ID '{id}' was not found.");
    }

    public static List<Error> ValidateModel(string? name)
    {
        var errors = new List<Error>();
        if (string.IsNullOrWhiteSpace(name)) errors.Add(Errors.NameRequired);
        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid ZoneId) : DomainEvent;
        public record Updated(Guid ZoneId) : DomainEvent;
        public record Deleted(Guid ZoneId) : DomainEvent;
    }

    #endregion
}