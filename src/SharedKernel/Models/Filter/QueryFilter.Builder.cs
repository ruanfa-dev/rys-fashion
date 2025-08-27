namespace SharedKernel.Models.Filter;

/// <summary>
/// Builder class for constructing complex filter queries with fluent API.
/// </summary>
public sealed class QueryFilterBuilder
{
    private readonly List<QueryFilterParameter> _filters = [];
    private FilterLogicalOperator _defaultLogicalOperator = FilterLogicalOperator.All;
    private int _currentGroup = 0;

    /// <summary>
    /// Sets the default logical operator for subsequent filters.
    /// </summary>
    /// <param name="logicalOperator">The default logical operator.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryFilterBuilder WithDefaultLogic(FilterLogicalOperator logicalOperator)
    {
        _defaultLogicalOperator = logicalOperator;
        return this;
    }

    /// <summary>
    /// Sets the current group for subsequent filters.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryFilterBuilder InGroup(int groupId)
    {
        _currentGroup = groupId;
        return this;
    }

    /// <summary>
    /// Adds a filter with the default logical operator.
    /// </summary>
    /// <param name="field">The field name to filter on.</param>
    /// <param name="operator">The filter operator.</param>
    /// <param name="value">The value to filter by.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryFilterBuilder Add(string field, FilterOperator @operator, string value)
    {
        return Add(field, @operator, value, _defaultLogicalOperator);
    }

    /// <summary>
    /// Adds a filter with a specific logical operator.
    /// </summary>
    /// <param name="field">The field name to filter on.</param>
    /// <param name="operator">The filter operator.</param>
    /// <param name="value">The value to filter by.</param>
    /// <param name="logicalOperator">The logical operator for this filter.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryFilterBuilder Add(string field, FilterOperator @operator, string value, FilterLogicalOperator logicalOperator)
    {
        _filters.Add(new QueryFilterParameter
        {
            Field = field,
            Operator = @operator,
            Value = value,
            LogicalOperator = logicalOperator,
            Group = _currentGroup
        });
        return this;
    }

    /// <summary>
    /// Adds an AND filter.
    /// </summary>
    /// <param name="field">The field name to filter on.</param>
    /// <param name="operator">The filter operator.</param>
    /// <param name="value">The value to filter by.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryFilterBuilder And(string field, FilterOperator @operator, string value)
    {
        return Add(field, @operator, value, FilterLogicalOperator.All);
    }

    /// <summary>
    /// Adds an OR filter.
    /// </summary>
    /// <param name="field">The field name to filter on.</param>
    /// <param name="operator">The filter operator.</param>
    /// <param name="value">The value to filter by.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryFilterBuilder Or(string field, FilterOperator @operator, string value)
    {
        return Add(field, @operator, value, FilterLogicalOperator.Any);
    }

    /// <summary>
    /// Adds an equality filter.
    /// </summary>
    /// <param name="field">The field name to filter on.</param>
    /// <param name="value">The value to filter by.</param>
    /// <param name="logicalOperator">The logical operator (optional, uses default if not specified).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryFilterBuilder Equal(string field, string value, FilterLogicalOperator? logicalOperator = null)
    {
        return Add(field, FilterOperator.Equal, value, logicalOperator ?? _defaultLogicalOperator);
    }

    /// <summary>
    /// Adds a contains filter.
    /// </summary>
    /// <param name="field">The field name to filter on.</param>
    /// <param name="value">The value to filter by.</param>
    /// <param name="logicalOperator">The logical operator (optional, uses default if not specified).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryFilterBuilder Contains(string field, string value, FilterLogicalOperator? logicalOperator = null)
    {
        return Add(field, FilterOperator.Contains, value, logicalOperator ?? _defaultLogicalOperator);
    }

    /// <summary>
    /// Adds a greater than filter.
    /// </summary>
    /// <param name="field">The field name to filter on.</param>
    /// <param name="value">The value to filter by.</param>
    /// <param name="logicalOperator">The logical operator (optional, uses default if not specified).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryFilterBuilder GreaterThan(string field, string value, FilterLogicalOperator? logicalOperator = null)
    {
        return Add(field, FilterOperator.GreaterThan, value, logicalOperator ?? _defaultLogicalOperator);
    }

    /// <summary>
    /// Adds a less than filter.
    /// </summary>
    /// <param name="field">The field name to filter on.</param>
    /// <param name="value">The value to filter by.</param>
    /// <param name="logicalOperator">The logical operator (optional, uses default if not specified).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryFilterBuilder LessThan(string field, string value, FilterLogicalOperator? logicalOperator = null)
    {
        return Add(field, FilterOperator.LessThan, value, logicalOperator ?? _defaultLogicalOperator);
    }

    /// <summary>
    /// Adds an IN filter with multiple values.
    /// </summary>
    /// <param name="field">The field name to filter on.</param>
    /// <param name="values">Comma-separated values or array of values.</param>
    /// <param name="logicalOperator">The logical operator (optional, uses default if not specified).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryFilterBuilder In(string field, string values, FilterLogicalOperator? logicalOperator = null)
    {
        return Add(field, FilterOperator.In, values, logicalOperator ?? _defaultLogicalOperator);
    }

    /// <summary>
    /// Adds an IN filter with multiple values.
    /// </summary>
    /// <param name="field">The field name to filter on.</param>
    /// <param name="values">Array of values.</param>
    /// <param name="logicalOperator">The logical operator (optional, uses default if not specified).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryFilterBuilder In(string field, IEnumerable<string> values, FilterLogicalOperator? logicalOperator = null)
    {
        return Add(field, FilterOperator.In, string.Join(",", values), logicalOperator ?? _defaultLogicalOperator);
    }

    /// <summary>
    /// Adds a range filter.
    /// </summary>
    /// <param name="field">The field name to filter on.</param>
    /// <param name="minValue">The minimum value.</param>
    /// <param name="maxValue">The maximum value.</param>
    /// <param name="logicalOperator">The logical operator (optional, uses default if not specified).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryFilterBuilder Range(string field, string minValue, string maxValue, FilterLogicalOperator? logicalOperator = null)
    {
        return Add(field, FilterOperator.Range, $"{minValue},{maxValue}", logicalOperator ?? _defaultLogicalOperator);
    }

    /// <summary>
    /// Adds a null check filter.
    /// </summary>
    /// <param name="field">The field name to filter on.</param>
    /// <param name="logicalOperator">The logical operator (optional, uses default if not specified).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryFilterBuilder IsNull(string field, FilterLogicalOperator? logicalOperator = null)
    {
        return Add(field, FilterOperator.IsNull, string.Empty, logicalOperator ?? _defaultLogicalOperator);
    }

    /// <summary>
    /// Adds a not null check filter.
    /// </summary>
    /// <param name="field">The field name to filter on.</param>
    /// <param name="logicalOperator">The logical operator (optional, uses default if not specified).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public QueryFilterBuilder IsNotNull(string field, FilterLogicalOperator? logicalOperator = null)
    {
        return Add(field, FilterOperator.IsNotNull, string.Empty, logicalOperator ?? _defaultLogicalOperator);
    }

    /// <summary>
    /// Builds the final list of filter parameters.
    /// </summary>
    /// <returns>A list of QueryFilterParameter objects.</returns>
    public List<QueryFilterParameter> Build()
    {
        // Validate all filters before returning
        foreach (var filter in _filters)
        {
            filter.Validate();
        }

        return [.. _filters];
    }

    /// <summary>
    /// Builds and applies the filters to a queryable.
    /// </summary>
    /// <typeparam name="T">The type of the elements of source.</typeparam>
    /// <param name="query">The IQueryable to filter.</param>
    /// <returns>The filtered IQueryable.</returns>
    public IQueryable<T> ApplyTo<T>(IQueryable<T> query)
    {
        return query.ApplyFilters(Build());
    }

    /// <summary>
    /// Creates a new QueryFilterBuilder instance.
    /// </summary>
    /// <returns>A new builder instance.</returns>
    public static QueryFilterBuilder Create() => new();

    /// <summary>
    /// Creates a new QueryFilterBuilder instance with a default logical operator.
    /// </summary>
    /// <param name="defaultLogicalOperator">The default logical operator.</param>
    /// <returns>A new builder instance.</returns>
    public static QueryFilterBuilder Create(FilterLogicalOperator defaultLogicalOperator) =>
        new QueryFilterBuilder().WithDefaultLogic(defaultLogicalOperator);
}
