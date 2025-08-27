namespace SharedKernel.Models.Filter;

/// <summary>
/// Represents a group of filters with logical operations for complex filtering scenarios.
/// </summary>
public sealed class QueryFilterGroup
{
    /// <summary>
    /// The list of filters in this group.
    /// </summary>
    public List<QueryFilterParameter> Filters { get; set; } = [];

    /// <summary>
    /// Sub-groups for nested logical operations.
    /// </summary>
    public List<QueryFilterGroup> SubGroups { get; set; } = [];

    /// <summary>
    /// The logical operator used to combine filters and sub-groups within this group.
    /// </summary>
    public FilterLogicalOperator LogicalOperator { get; set; } = FilterLogicalOperator.All;

    /// <summary>
    /// Unique identifier for this group.
    /// </summary>
    public int GroupId { get; set; }

    /// <summary>
    /// Validates that the filter group is properly configured.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the group is invalid.</exception>
    public void Validate()
    {
        // Validate all filters in the group
        foreach (var filter in Filters)
        {
            filter.Validate();
        }

        // Validate all sub-groups recursively
        foreach (var subGroup in SubGroups)
        {
            subGroup.Validate();
        }

        // Ensure the group has at least some content
        if (!Filters.Any() && !SubGroups.Any())
        {
            throw new ArgumentException("Filter group must contain at least one filter or sub-group.", nameof(Filters));
        }
    }

    /// <summary>
    /// Gets the total count of filters in this group and all sub-groups.
    /// </summary>
    /// <returns>The total number of filters.</returns>
    public int GetTotalFilterCount()
    {
        return Filters.Count + SubGroups.Sum(sg => sg.GetTotalFilterCount());
    }

    /// <summary>
    /// Flattens all filters from this group and sub-groups into a single list.
    /// </summary>
    /// <returns>A flattened list of all filters.</returns>
    public List<QueryFilterParameter> GetAllFilters()
    {
        var allFilters = new List<QueryFilterParameter>(Filters);

        foreach (var subGroup in SubGroups)
        {
            allFilters.AddRange(subGroup.GetAllFilters());
        }

        return allFilters;
    }
}
