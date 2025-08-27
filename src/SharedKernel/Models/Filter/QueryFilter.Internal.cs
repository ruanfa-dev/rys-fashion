namespace SharedKernel.Models.Filter;

/// <summary>
/// Internal helper class for filter criteria used by QueryFilter system.
/// This is used internally for backwards compatibility and expression building.
/// </summary>
internal sealed class FilterCriteria
{
    /// <summary>
    /// Property name to filter on (supports dot notation for nested properties).
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Filter operation to perform.
    /// </summary>
    public FilterOperator Operator { get; init; }

    /// <summary>
    /// Value to filter by (can be null for IsNull/IsNotNull operations).
    /// </summary>
    public object? Value { get; init; }
}
