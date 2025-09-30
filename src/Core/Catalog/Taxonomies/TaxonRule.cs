using Core.Catalog.Products;

using ErrorOr;

using SharedKernel.Domain.Primitives;
using SharedKernel.Messaging;
using SharedKernel.Models.Filter;

namespace Core.Catalog.Taxonomies;

/// <summary>
/// Domain model for TaxonRule following DDD principles.
/// Represents a rule that determines which products should be automatically classified under a taxon.
/// </summary>
public sealed class TaxonRule : AuditableEntity
{
    #region Constants & Constraints

    public static class Constraints
    {
        public const int TypeMaxLength = 100;
        public const int ValueMaxLength = 500;
        public const int MatchPolicyMaxLength = 50;
    }

    // Match policies that map to filter operations
    public static readonly string[] MATCH_POLICIES =
    [
        "is_equal_to",
        "is_not_equal_to",
        "contains",
        "does_not_contain",
        "starts_with",
        "ends_with",
        "greater_than",
        "less_than",
        "greater_than_or_equal",
        "less_than_or_equal",
        "in",
        "not_in",
        "is_null",
        "is_not_null"
    ];

    // Rule types that define what product property/field to target
    public static readonly string[] RULE_TYPES =
    [
        "product_name",
        "product_sku",
        "product_description",
        "product_price",
        "product_weight",
        "product_tag",
        "product_property", // For custom properties like "color", "size", etc.
        "variant_price",
        "variant_sku"
    ];

    #endregion

    #region Errors

    public static class Errors
    {
        public static Error TaxonRequired => Error.Validation("TaxonRule.TaxonRequired", "Taxon is required.");
        public static Error TypeRequired => Error.Validation("TaxonRule.TypeRequired", "Rule type is required.");
        public static Error ValueRequired => Error.Validation("TaxonRule.ValueRequired", "Rule value is required.");

        public static Error InvalidMatchPolicy => Error.Validation(
            "TaxonRule.InvalidMatchPolicy",
            $"Match policy must be one of: {string.Join(", ", MATCH_POLICIES)}"
        );

        public static Error InvalidRuleType => Error.Validation(
            "TaxonRule.InvalidRuleType",
            $"Rule type must be one of: {string.Join(", ", RULE_TYPES)}"
        );

        public static Error TypeTooLong => Error.Validation(
            "TaxonRule.TypeTooLong",
            $"Type cannot exceed {Constraints.TypeMaxLength} characters."
        );

        public static Error ValueTooLong => Error.Validation(
            "TaxonRule.ValueTooLong",
            $"Value cannot exceed {Constraints.ValueMaxLength} characters."
        );

        public static Error NotFound(Guid id) => Error.NotFound(
            "TaxonRule.NotFound",
            $"TaxonRule with ID '{id}' was not found."
        );
    }

    #endregion

    #region Properties

    public Guid TaxonId { get; set; }
    public Taxon? Taxon { get; set; }

    // The type of rule - what product field/property to match against
    public string Type { get; set; } = null!;

    // The value to match against
    public string Value { get; set; } = null!;

    // How to match the value (equals, contains, etc.)
    public string MatchPolicy { get; set; } = MATCH_POLICIES[0];

    // For property-based rules, this specifies the property name
    public string? PropertyName { get; set; }

    #endregion

    #region Constructors & Factory

    private TaxonRule() { }

    public static ErrorOr<TaxonRule> Create(
        Guid taxonId,
        string type,
        string value,
        string? matchPolicy = null,
        string? propertyName = null)
    {
        // Validate required fields
        if (taxonId == Guid.Empty) return Errors.TaxonRequired;

        if (string.IsNullOrWhiteSpace(type)) return Errors.TypeRequired;
        if (type.Length > Constraints.TypeMaxLength) return Errors.TypeTooLong;

        if (string.IsNullOrWhiteSpace(value)) return Errors.ValueRequired;
        if (value.Length > Constraints.ValueMaxLength) return Errors.ValueTooLong;

        // Validate match policy
        matchPolicy ??= MATCH_POLICIES[0];
        if (!MATCH_POLICIES.Contains(matchPolicy))
            return Errors.InvalidMatchPolicy;

        // Validate rule type
        var trimmedType = type.Trim();
        if (!RULE_TYPES.Contains(trimmedType))
            return Errors.InvalidRuleType;

        // For property-based rules, property name is required
        if (trimmedType == "product_property" && string.IsNullOrWhiteSpace(propertyName))
            return Error.Validation("TaxonRule.PropertyNameRequired", "Property name is required for product_property rules.");

        var rule = new TaxonRule
        {
            TaxonId = taxonId,
            Type = trimmedType,
            Value = value.Trim(),
            MatchPolicy = matchPolicy,
            PropertyName = propertyName?.Trim()
        };

        rule.AddDomainEvent(new Events.Created(rule.Id));
        return rule;
    }

    #endregion

    #region Business Operations

    public ErrorOr<TaxonRule> Update(
        string? type = null,
        string? value = null,
        string? matchPolicy = null,
        string? propertyName = null)
    {
        var changed = false;

        // Update type
        if (!string.IsNullOrWhiteSpace(type) && type.Trim() != Type)
        {
            if (type.Length > Constraints.TypeMaxLength) return Errors.TypeTooLong;
            if (!RULE_TYPES.Contains(type.Trim())) return Errors.InvalidRuleType;

            Type = type.Trim();
            changed = true;
        }

        // Update value
        if (!string.IsNullOrWhiteSpace(value) && value.Trim() != Value)
        {
            if (value.Length > Constraints.ValueMaxLength) return Errors.ValueTooLong;

            Value = value.Trim();
            changed = true;
        }

        // Update match policy
        if (!string.IsNullOrWhiteSpace(matchPolicy) && matchPolicy != MatchPolicy)
        {
            if (!MATCH_POLICIES.Contains(matchPolicy))
                return Errors.InvalidMatchPolicy;

            MatchPolicy = matchPolicy;
            changed = true;
        }

        // Update property name
        if (propertyName != null && propertyName.Trim() != PropertyName)
        {
            PropertyName = propertyName.Trim();
            changed = true;
        }

        if (changed)
        {
            MarkAsUpdated();
            AddDomainEvent(new Events.Updated(Id));

            // Trigger taxon product regeneration
            if (TaxonId != Guid.Empty)
                AddDomainEvent(new Taxon.Events.RegenerateProducts(TaxonId, true));
        }

        return this;
    }

    public ErrorOr<Deleted> Delete()
    {
        AddDomainEvent(new Events.Deleted(Id));

        // Trigger taxon product regeneration
        if (TaxonId != Guid.Empty)
            AddDomainEvent(new Taxon.Events.RegenerateProducts(TaxonId, true));

        return Result.Deleted;
    }

    /// <summary>
    /// Internal method to set taxon ID (used by Taxon aggregate)
    /// </summary>
    internal void SetTaxon(Guid taxonId)
    {
        TaxonId = taxonId;
    }

    #endregion

    #region Rule Application

    /// <summary>
    /// Apply this rule to a product queryable using the QueryFilterBuilder.
    /// Returns products that match this rule's criteria.
    /// </summary>
    public IQueryable<Product> Apply(IQueryable<Product> products)
    {
        try
        {
            var builder = QueryFilterBuilder.Create();
            var fieldName = GetFieldName();
            var filterOperator = GetFilterOperator();

            // Build the appropriate filter based on operator type
            switch (filterOperator)
            {
                case FilterOperator.Equal:
                    builder.Equal(fieldName, Value);
                    break;

                case FilterOperator.NotEqual:
                    builder.And(fieldName, FilterOperator.NotEqual, Value);
                    break;

                case FilterOperator.Contains:
                    builder.Contains(fieldName, Value);
                    break;

                case FilterOperator.NotContains:
                    builder.And(fieldName, FilterOperator.NotContains, Value);
                    break;

                case FilterOperator.StartsWith:
                    builder.And(fieldName, FilterOperator.StartsWith, Value);
                    break;

                case FilterOperator.EndsWith:
                    builder.And(fieldName, FilterOperator.EndsWith, Value);
                    break;

                case FilterOperator.GreaterThan:
                    builder.GreaterThan(fieldName, Value);
                    break;

                case FilterOperator.LessThan:
                    builder.LessThan(fieldName, Value);
                    break;

                case FilterOperator.GreaterThanOrEqual:
                    builder.And(fieldName, FilterOperator.GreaterThanOrEqual, Value);
                    break;

                case FilterOperator.LessThanOrEqual:
                    builder.And(fieldName, FilterOperator.LessThanOrEqual, Value);
                    break;

                case FilterOperator.In:
                    builder.In(fieldName, Value);
                    break;

                case FilterOperator.NotIn:
                    builder.And(fieldName, FilterOperator.NotIn, Value);
                    break;

                case FilterOperator.IsNull:
                    builder.IsNull(fieldName);
                    break;

                case FilterOperator.IsNotNull:
                    builder.IsNotNull(fieldName);
                    break;

                default:
                    builder.Contains(fieldName, Value);
                    break;
            }

            return builder.ApplyTo(products);
        }
        catch (Exception)
        {
            // On any error, return empty result to be safe
            return products.Where(p => false);
        }
    }

    /// <summary>
    /// Get the field name for filtering based on the rule type.
    /// Maps rule types to actual product/variant property paths.
    /// </summary>
    private string GetFieldName()
    {
        return Type switch
        {
            "product_name" => "Name",
            "product_sku" => "Sku",
            "product_description" => "Description",
            "product_price" => "Price",
            "product_weight" => "Weight",
            "product_tag" => "Tag.Name",
            "product_property" => $"Properties.{PropertyName}",
            "variant_price" => "Variants.Price",
            "variant_sku" => "Variants.Sku",
            _ => "Name" // Default fallback
        };
    }

    /// <summary>
    /// Map match policy strings to FilterOperator enum values.
    /// </summary>
    private FilterOperator GetFilterOperator()
    {
        return MatchPolicy switch
        {
            "is_equal_to" => FilterOperator.Equal,
            "is_not_equal_to" => FilterOperator.NotEqual,
            "contains" => FilterOperator.Contains,
            "does_not_contain" => FilterOperator.NotContains,
            "starts_with" => FilterOperator.StartsWith,
            "ends_with" => FilterOperator.EndsWith,
            "greater_than" => FilterOperator.GreaterThan,
            "less_than" => FilterOperator.LessThan,
            "greater_than_or_equal" => FilterOperator.GreaterThanOrEqual,
            "less_than_or_equal" => FilterOperator.LessThanOrEqual,
            "in" => FilterOperator.In,
            "not_in" => FilterOperator.NotIn,
            "is_null" => FilterOperator.IsNull,
            "is_not_null" => FilterOperator.IsNotNull,
            _ => FilterOperator.Contains
        };
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Check if this rule is valid for the given match policy and value combination.
    /// </summary>
    public bool IsValidForMatchPolicy()
    {
        return MatchPolicy switch
        {
            "is_null" or "is_not_null" => true, // No value needed
            "in" or "not_in" => !string.IsNullOrWhiteSpace(Value), // Value should contain comma-separated items
            _ => !string.IsNullOrWhiteSpace(Value) // Most operations need a value
        };
    }

    /// <summary>
    /// Get a human-readable description of this rule.
    /// </summary>
    public string GetDescription()
    {
        var fieldDisplay = Type switch
        {
            "product_name" => "Product Name",
            "product_sku" => "Product SKU",
            "product_description" => "Product Description",
            "product_price" => "Product Price",
            "product_weight" => "Product Weight",
            "product_brand" => "Product Brand",
            "product_category" => "Product Category",
            "product_property" => $"Product {PropertyName}",
            "variant_price" => "Variant Price",
            "variant_sku" => "Variant SKU",
            _ => Type
        };

        var operatorDisplay = MatchPolicy switch
        {
            "is_equal_to" => "equals",
            "is_not_equal_to" => "does not equal",
            "contains" => "contains",
            "does_not_contain" => "does not contain",
            "starts_with" => "starts with",
            "ends_with" => "ends with",
            "greater_than" => "is greater than",
            "less_than" => "is less than",
            "greater_than_or_equal" => "is greater than or equal to",
            "less_than_or_equal" => "is less than or equal to",
            "in" => "is one of",
            "not_in" => "is not one of",
            "is_null" => "is empty",
            "is_not_null" => "is not empty",
            _ => MatchPolicy
        };

        return MatchPolicy is "is_null" or "is_not_null"
            ? $"{fieldDisplay} {operatorDisplay}"
            : $"{fieldDisplay} {operatorDisplay} '{Value}'";
    }

    #endregion

    #region Events

    public static class Events
    {
        public record Created(Guid TaxonRuleId) : DomainEvent;
        public record Updated(Guid TaxonRuleId) : DomainEvent;
        public record Deleted(Guid TaxonRuleId) : DomainEvent;
    }

    #endregion
}