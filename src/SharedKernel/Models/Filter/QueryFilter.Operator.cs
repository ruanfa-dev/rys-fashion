namespace SharedKernel.Models.Filter;
/// <summary>
/// Defines available filter operations.
/// </summary>
public enum FilterOperator
{
    // Equality operations (most common, should be default)
    Equal,
    NotEqual,

    // Comparison operations
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,

    // Null checks
    IsNull,
    IsNotNull,

    // String operations
    Contains,
    NotContains,
    StartsWith,
    EndsWith,

    // Collection operations
    In,
    NotIn,

    // Range operations
    Range
}