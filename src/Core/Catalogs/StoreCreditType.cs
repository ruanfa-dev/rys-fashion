using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System;
using System.Collections.Generic;

namespace Core.Catalogs;

/// <summary>
/// Domain port of Spree::StoreCreditType (simplified).
/// - Groups StoreCredits by a type (e.g. "Expiring").
/// - Persistence, uniqueness and cascade rules belong to infra layer.
/// </summary>
public sealed class StoreCreditType : AuditableEntity
{
    public const string DEFAULT_TYPE_NAME = "Expiring";

    public string Name { get; set; } = DEFAULT_TYPE_NAME;

    // Navigation
    public ICollection<StoreCredit> StoreCredits { get; set; } = new List<StoreCredit>();

    private StoreCreditType() { }

    public static StoreCreditType Create(string? name = null)
        => new StoreCreditType
        {
            Name = string.IsNullOrWhiteSpace(name) ? DEFAULT_TYPE_NAME : name!.Trim()
        };

    public void Update(string? name = null)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name!.Trim();
        MarkAsUpdated();
    }

    #region Validation / Errors

    public static class Errors
    {
        public static Error NameRequired => Error.Validation("StoreCreditType.NameRequired", "Name is required.");
        public static Error NotFound(Guid id) => Error.NotFound("StoreCreditType.NotFound", $"StoreCreditType with ID '{id}' was not found.");
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
        public record Created(Guid StoreCreditTypeId) : DomainEvent;
        public record Updated(Guid StoreCreditTypeId) : DomainEvent;
        public record Deleted(Guid StoreCreditTypeId) : DomainEvent;
    }

    #endregion
}