namespace SharedKernel.Models.Filter;

public sealed record QueryFilterParams(string? Filters = null);


/// <summary>
/// Represents a filter parameter parsed from HTTP query parameters with enhanced logical operations support.
/// </summary>
public sealed class QueryFilterParameter
{
    /// <summary>
    /// The field name to filter on (supports dot notation for nested properties).
    /// </summary>
    public required string Field { get; set; }

    /// <summary>
    /// The filter operator to apply.
    /// </summary>
    public FilterOperator Operator { get; set; }

    /// <summary>
    /// The value to filter by as a string (will be converted to appropriate type).
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// For IN operations, comma-separated values can be provided in Value property.
    /// </summary>
    public string? Values { get; set; }

    /// <summary>
    /// The logical operator for chaining this filter with others.
    /// </summary>
    public FilterLogicalOperator LogicalOperator { get; set; } = FilterLogicalOperator.All;

    /// <summary>
    /// Indicates whether the logical operator was explicitly set (via prefix) or defaulted.
    /// </summary>
    public bool IsLogicalOperatorExplicit { get; set; } = false;

    /// <summary>
    /// Optional group identifier for grouping conditions.
    /// </summary>
    public int? Group { get; set; }

    /// <summary>
    /// Validates that the filter parameter is properly configured.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the parameter is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Field))
        {
            throw new ArgumentException("Field cannot be null or whitespace.", nameof(Field));
        }

        // Validate that null check operators don't require values
        if (Operator is FilterOperator.IsNull or FilterOperator.IsNotNull)
        {
            // For null checks, reject non-empty values since they shouldn't have values
            if (!string.IsNullOrWhiteSpace(Value))
            {
                throw new ArgumentException($"Operator {Operator} should not have a value.", nameof(Value));
            }
            return; // Value is not required for null checks
        }

        // Validate IN operations have proper format
        if (Operator is FilterOperator.In or FilterOperator.NotIn)
        {
            if (string.IsNullOrWhiteSpace(Value) && string.IsNullOrWhiteSpace(Values))
            {
                throw new ArgumentException($"Operator {Operator} requires values in Value or Values property.", nameof(Value));
            }
            return; // Values are provided, validation passes
        }

        // Validate Range operations have proper format
        if (Operator == FilterOperator.Range)
        {
            if (string.IsNullOrWhiteSpace(Value) || !Value.Contains(','))
            {
                throw new ArgumentException("Range operator requires two comma-separated values.", nameof(Value));
            }
            return; // Range validation passes
        }

        // Validate that other operators have values
        if (string.IsNullOrWhiteSpace(Value))
        {
            throw new ArgumentException($"Operator {Operator} requires a value.", nameof(Value));
        }
    }
}
