using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;

using System;
using System.Collections.Generic;

namespace Core.Catalogs;

/// <summary>
/// Domain model port of Spree::TaxonRule (simplified).
/// - Responsible for representing a rule used to automatically include/exclude products in a Taxon.
/// - Matching logic is intentionally small and applies to string values; complex matching or product attribute extraction
///   should be implemented by application services.
/// - Infrastructure is responsible for hooking persistence events (after_commit) and calling <see cref="TriggerRegenerateTaxonProducts"/>.
/// </summary>
public sealed class TaxonRule : AuditableEntity
{
    public static readonly IReadOnlyList<string> MATCH_POLICIES = new[]
    {
        "is_equal_to",
        "is_not_equal_to",
        "contains",
        "does_not_contain"
    };

    public Guid TaxonId { get; set; }
    public Taxon? Taxon { get; set; }

    // 'Type' in Spree often indicates what attribute the rule targets (e.g. 'product_property', 'option_value').
    // Avoid C# keyword: use RuleType
    public string RuleType { get; set; } = default!;

    // Value to compare against (string representation)
    public string Value { get; set; } = default!;

    // One of MATCH_POLICIES
    public string MatchPolicy { get; set; } = "is_equal_to";

    private TaxonRule() { }

    public static TaxonRule Create(Guid taxonId, string ruleType, string value, string matchPolicy = "is_equal_to")
    {
        return new TaxonRule
        {
            TaxonId = taxonId,
            RuleType = ruleType,
            Value = value,
            MatchPolicy = matchPolicy
        };
    }

    public void Update(string? ruleType = null, string? value = null, string? matchPolicy = null)
    {
        var previousValue = Value;
        var previousMatchPolicy = MatchPolicy;

        if (!string.IsNullOrWhiteSpace(ruleType)) RuleType = ruleType!.Trim();
        if (value != null) Value = value;
        if (!string.IsNullOrWhiteSpace(matchPolicy)) MatchPolicy = matchPolicy!.Trim();

        MarkAsUpdated();

        // Application/infrastructure should decide whether to call TriggerRegenerateTaxonProducts when appropriate.
        // For convenience, expose helper to detect relevant changes:
        if (previousValue != Value || previousMatchPolicy != MatchPolicy)
            TriggerRegenerateTaxonProducts();
    }

    /// <summary>
    /// Evaluate this rule against a candidate string (product attribute, option presentation, etc.).
    /// Returns true when the rule matches (meaning product should be included according to the rule).
    /// The interpretation of RuleType -> which attribute to supply is the responsibility of the caller.
    /// </summary>
    public bool AppliesTo(string? candidate)
    {
        candidate ??= string.Empty;
        var target = candidate;

        return MatchPolicy switch
        {
            "is_equal_to" => string.Equals(target, Value, StringComparison.OrdinalIgnoreCase),
            "is_not_equal_to" => !string.Equals(target, Value, StringComparison.OrdinalIgnoreCase),
            "contains" => target.IndexOf(Value ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0,
            "does_not_contain" => target.IndexOf(Value ?? string.Empty, StringComparison.OrdinalIgnoreCase) < 0,
            _ => false
        };
    }

    /// <summary>
    /// Called by application/infrastructure when a rule change should kick off taxon-product regeneration.
    /// Adds a domain event that handlers can react to (e.g. queue background job).
    /// </summary>
    public void TriggerRegenerateTaxonProducts()
    {
        AddDomainEvent(new Events.RegenerateTaxonProductsRequested(Id));
    }

    #region Validation / Constraints / Errors

    public static class Constraints
    {
        public const int RuleTypeMaxLength = 100;
        public const int ValueMaxLength = 1000;
        public const int MatchPolicyMaxLength = 50;
    }

    public static class Errors
    {
        public static Error TaxonRequired => Error.Validation("TaxonRule.TaxonRequired", "Taxon is required.");
        public static Error TypeRequired => Error.Validation("TaxonRule.TypeRequired", "Rule type is required.");
        public static Error ValueRequired => Error.Validation("TaxonRule.ValueRequired", "Rule value is required.");
        public static Error InvalidMatchPolicy => Error.Validation("TaxonRule.InvalidMatchPolicy", $"Match policy must be one of: {string.Join(", ", MATCH_POLICIES)}.");
        public static Error NotFound(Guid id) => Error.NotFound("TaxonRule.NotFound", $"TaxonRule with ID '{id}' was not found.");
    }

    /// <summary>
    /// Lightweight validation helper for handlers.
    /// </summary>
    public static List<Error> ValidateModel(Guid taxonId, string? ruleType, string? value, string? matchPolicy)
    {
        var errors = new List<Error>();
        if (taxonId == Guid.Empty) errors.Add(Errors.TaxonRequired);
        if (string.IsNullOrWhiteSpace(ruleType)) errors.Add(Errors.TypeRequired);
        if (string.IsNullOrWhiteSpace(value)) errors.Add(Errors.ValueRequired);
        if (!string.IsNullOrWhiteSpace(matchPolicy) && !MATCH_POLICIES.Contains(matchPolicy))
            errors.Add(Errors.InvalidMatchPolicy);
        return errors;
    }

    #endregion

    #region Domain events

    public static class Events
    {
        public record RegenerateTaxonProductsRequested(Guid TaxonRuleId) : DomainEvent;
        public record Created(Guid TaxonRuleId) : DomainEvent;
        public record Updated(Guid TaxonRuleId) : DomainEvent;
        public record Deleted(Guid TaxonRuleId) : DomainEvent;
    }

    #endregion
}