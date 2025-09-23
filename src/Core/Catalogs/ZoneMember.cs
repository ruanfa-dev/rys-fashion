using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

namespace Core.Catalogs;

/// <summary>
/// Domain port of Spree::ZoneMember (simplified).
/// - Polymorphic relation to a "zoneable" entity is represented by ZoneableId + ZoneableType.
/// - Repository/EF configuration should map the polymorphic relationship and counter cache.
/// </summary>
public sealed class ZoneMember : AuditableEntity
{
    public Guid ZoneId { get; set; }
    public Zone? Zone { get; set; }

    // Polymorphic relation placeholder
    public Guid? ZoneableId { get; set; }
    public string? ZoneableType { get; set; }

    // Optional runtime navigation — infra may populate a concrete typed object
    public object? Zoneable { get; set; }

    private ZoneMember() { }

    public static ZoneMember Create(Guid zoneId, Guid? zoneableId, string? zoneableType)
    {
        return new ZoneMember
        {
            ZoneId = zoneId,
            ZoneableId = zoneableId,
            ZoneableType = string.IsNullOrWhiteSpace(zoneableType) ? null : zoneableType
        };
    }

    /// <summary>
    /// Lightweight validation used by handlers before persistence.
    /// </summary>
    public static List<Error> ValidateModel(Guid zoneId, Guid? zoneableId, string? zoneableType)
    {
        var errors = new List<Error>();
        if (zoneId == Guid.Empty) errors.Add(Errors.ZoneRequired);
        if (!zoneableId.HasValue && string.IsNullOrWhiteSpace(zoneableType)) errors.Add(Errors.ZoneableRequired);
        return errors;
    }

    /// <summary>
    /// In-memory filter that mirrors the Ruby scope:
    /// defunct_without_kind -> where('zoneable_id IS NULL OR zoneable_type != ?', "Spree::#{kind.classify}")
    /// </summary>
    public static IEnumerable<ZoneMember> DefunctWithoutKind(IEnumerable<ZoneMember> members, string kind)
    {
        if (members == null) return Enumerable.Empty<ZoneMember>();
        var expectedType = kind?.Trim();
        return members.Where(m => m.ZoneableId == null || !string.Equals(m.ZoneableType, expectedType, StringComparison.OrdinalIgnoreCase));
    }

    #region Errors / Events

    public static class Errors
    {
        public static Error ZoneRequired => Error.Validation("ZoneMember.ZoneRequired", "Zone is required.");
        public static Error ZoneableRequired => Error.Validation("ZoneMember.ZoneableRequired", "Zoneable (entity) is required.");
        public static Error NotFound(Guid id) => Error.NotFound("ZoneMember.NotFound", $"ZoneMember with ID '{id}' was not found.");
    }

    public static class Events
    {
        public record Created(Guid ZoneMemberId) : DomainEvent;
        public record Deleted(Guid ZoneMemberId) : DomainEvent;
    }

    #endregion
}