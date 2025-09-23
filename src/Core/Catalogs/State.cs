using ErrorOr;
using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Catalogs;

/// <summary>
/// Domain model converted from Spree::State (simplified).
/// - Keeps relationships to Country, Addresses and ZoneMembers.
/// - Repository/infra is responsible for ordering, uniqueness and persistence concerns.
/// </summary>
public sealed class State : AuditableEntity, IComparable<State>
{
    #region Properties

    public string Name { get; set; } = default!;
    public string? Abbr { get; set; }

    public Guid CountryId { get; set; }
    public Country Country { get; set; } = default!;

    #endregion

    #region Relationships

    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<ZoneMember> ZoneMembers { get; set; } = new List<ZoneMember>();
    public IEnumerable<Zone> Zones => ZoneMembers.Select(zm => zm.Zone).Where(z => z != null).ToList();

    #endregion

    #region Construction / Factory

    private State() { }

    public static State Create(string name, Guid countryId, string? abbr = null)
        => new State { Name = name?.Trim() ?? string.Empty, CountryId = countryId, Abbr = abbr?.Trim() };

    public void Update(string? name = null, string? abbr = null, Guid? countryId = null)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name!.Trim();
        if (abbr != null) Abbr = abbr.Trim();
        if (countryId.HasValue && countryId.Value != Guid.Empty) CountryId = countryId.Value;
        MarkAsUpdated();
    }

    #endregion

    #region Query / Helpers

    /// <summary>
    /// Finds states matching the given name or abbreviation from an in-memory sequence.
    /// Repository-level implementation preferred for DB queries.
    /// </summary>
    public static IEnumerable<State> FindAllByNameOrAbbr(IEnumerable<State> states, string nameOrAbbr)
    {
        if (states == null || string.IsNullOrWhiteSpace(nameOrAbbr)) return Enumerable.Empty<State>();
        var q = nameOrAbbr.Trim();
        return states.Where(s =>
            string.Equals(s.Name, q, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(s.Abbr) && string.Equals(s.Abbr, q, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Returns dictionary mapping country id (string) to a list of (state id, state name) sorted by name.
    /// Mirrors Spree.states_group_by_country_id behaviour.
    /// </summary>
    public static IDictionary<string, List<(Guid Id, string Name)>> StatesGroupByCountryId(IEnumerable<State> states)
    {
        var map = new Dictionary<string, List<(Guid, string)>>(StringComparer.Ordinal);
        if (states == null) return map;

        foreach (var state in states.OrderBy(s => s.Name))
        {
            var key = state.CountryId.ToString();
            if (!map.TryGetValue(key, out var list))
            {
                list = new List<(Guid, string)>();
                map[key] = list;
            }
            map[key].Add((state.Id, state.Name));
        }

        return map;
    }

    #endregion

    #region Validation / Constraints / Errors

    public static class Constraints
    {
        public const int NameMaxLength = 255;
        public const int AbbrMaxLength = 32;
    }

    public static class Errors
    {
        public static Error CountryRequired => Error.Validation("State.CountryRequired", "Country is required.");
        public static Error NameRequired => Error.Validation("State.NameRequired", "State name is required.");
        public static Error NameTooLong => Error.Validation("State.NameTooLong", $"State name must be at most {Constraints.NameMaxLength} characters.");
        public static Error AbbrTooLong => Error.Validation("State.AbbrTooLong", $"State abbreviation must be at most {Constraints.AbbrMaxLength} characters.");
        public static Error NotFound(Guid id) => Error.NotFound("State.NotFound", $"State with ID '{id}' was not found.");
    }

    public static List<Error> ValidateModel(string? name, string? abbr, Guid countryId)
    {
        var errors = new List<Error>();
        if (countryId == Guid.Empty) errors.Add(Errors.CountryRequired);
        if (string.IsNullOrWhiteSpace(name)) errors.Add(Errors.NameRequired);
        else if (name!.Length > Constraints.NameMaxLength) errors.Add(Errors.NameTooLong);

        if (!string.IsNullOrWhiteSpace(abbr) && abbr!.Length > Constraints.AbbrMaxLength) errors.Add(Errors.AbbrTooLong);

        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record Created(Guid StateId) : DomainEvent;
        public record Updated(Guid StateId) : DomainEvent;
        public record Deleted(Guid StateId) : DomainEvent;
    }

    #endregion

    #region Comparable / ToString

    public int CompareTo(State? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }

    public override string ToString() => Name;

    #endregion
}
