namespace SharedKernel.Models.Filter;
/// <summary>
/// Defines how multiple filters should be combined.
/// </summary>
public enum FilterLogicalOperator
{
    /// <summary>
    /// All filters must match (AND logic).
    /// </summary>
    All,

    /// <summary>
    /// Any filter can match (OR logic).
    /// </summary>
    Any
}