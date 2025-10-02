using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace SharedKernel.Models.Filter;

/// <summary>
/// Provides extension methods for applying query filters to IQueryable with enhanced query parameter support.
/// </summary>
public static class QueryFilterExtensions
{
    // Cache for reflection lookups to improve performance
    private static readonly ConcurrentDictionary<string, MethodInfo> MethodCache = new();

    // Cache for property mappings with flexible naming support
    private static readonly ConcurrentDictionary<string, Dictionary<string, PropertyInfo>> PropertyMappingCache = new();

    // Cached method references
    private static readonly MethodInfo StringContainsMethod = GetCachedMethod(typeof(string), "Contains", typeof(string), typeof(StringComparison));
    private static readonly MethodInfo StringStartsWithMethod = GetCachedMethod(typeof(string), "StartsWith", typeof(string), typeof(StringComparison));
    private static readonly MethodInfo StringEndsWithMethod = GetCachedMethod(typeof(string), "EndsWith", typeof(string), typeof(StringComparison));
    private static readonly MethodInfo StringEqualsMethod = GetCachedMethod(typeof(string), "Equals", typeof(string), typeof(StringComparison));

    /// <summary>
    /// Operator mapping for query parameter parsing.
    /// </summary>
    private static readonly Dictionary<string, FilterOperator> OperatorMap = new()
    {
        { "eq", FilterOperator.Equal },
        { "ne", FilterOperator.NotEqual },
        { "gt", FilterOperator.GreaterThan },
        { "gte", FilterOperator.GreaterThanOrEqual },
        { "lt", FilterOperator.LessThan },
        { "lte", FilterOperator.LessThanOrEqual },
        { "contains", FilterOperator.Contains },
        { "notcontains", FilterOperator.NotContains },
        { "startswith", FilterOperator.StartsWith },
        { "endswith", FilterOperator.EndsWith },
        { "in", FilterOperator.In },
        { "notin", FilterOperator.NotIn },
        { "isnull", FilterOperator.IsNull },
        { "isnotnull", FilterOperator.IsNotNull },
        { "range", FilterOperator.Range }
    };

    /// <summary>
    /// Applies filters from HTTP query parameters using key-value pairs with operator notation.
    /// Supports formats like: field[operator]=value, or_field[operator]=value, field_operator=value
    /// </summary>
    /// <typeparam name="T">The type of the elements of source.</typeparam>
    /// <param name="query">The IQueryable to filter.</param>
    /// <param name="queryParams">Dictionary of query parameters from HTTP request.</param>
    /// <returns>An IQueryable that represents the filtered query.</returns>
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query,
        Dictionary<string, string> queryParams)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(queryParams);

        if (!queryParams.Any())
            return query;

        List<QueryFilterParameter> filters = ParseQueryParameters(queryParams);
        return query.ApplyFilters(filters);
    }


    /// <summary>
    /// Applies filters from a query string directly to an IQueryable.
    /// Supports the same formats as the dictionary overload: field[operator]=value, or_field[operator]=value
    /// </summary>
    /// <typeparam name="T">The type of the elements of source.</typeparam>
    /// <param name="query">The IQueryable to filter.</param>
    /// <param name="filterParams">The FilterParams containing filter parameters.</param>
    /// <returns>An IQueryable that represents the filtered query.</returns>
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, QueryFilterParams filterParams)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(filterParams);

        // Guard: Empty or whitespace query string
        if (string.IsNullOrWhiteSpace(filterParams.Filters))
            return query;

        // Parse: Query string into dictionary format
        Dictionary<string, string> queryParams = ParseQueryString(filterParams.Filters);

        // Apply: Filters using existing dictionary method
        return query.ApplyFilters(queryParams);
    }

    /// <summary>
    /// Applies filters from a query string directly to an IQueryable.
    /// Supports the same formats as the dictionary overload: field[operator]=value, or_field[operator]=value
    /// </summary>
    /// <typeparam name="T">The type of the elements of source.</typeparam>
    /// <param name="query">The IQueryable to filter.</param>
    /// <param name="queryString">The query string containing filter parameters.</param>
    /// <returns>An IQueryable that represents the filtered query.</returns>
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, string queryString)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Guard: Empty or whitespace query string
        if (string.IsNullOrWhiteSpace(queryString))
            return query;

        // Parse: Query string into dictionary format
        Dictionary<string, string> queryParams = ParseQueryString(queryString);

        // Apply: Filters using existing dictionary method
        return query.ApplyFilters(queryParams);
    }

    /// <summary>
    /// Applies filters from a list of QueryFilterParameter objects with support for complex logical operations.
    /// </summary>
    /// <typeparam name="T">The type of the elements of source.</typeparam>
    /// <param name="query">The IQueryable to filter.</param>
    /// <param name="filters">List of filter parameters with logical operators and grouping.</param>
    /// <returns>An IQueryable that represents the filtered query.</returns>
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query,
        List<QueryFilterParameter> filters)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (filters is null || !filters.Any())
            return query;

        try
        {
            QueryFilterGroup filterGroups = BuildFilterGroups(filters);
            Expression<Func<T, bool>>? expression = BuildGroupExpression<T>(filterGroups);

            if (expression != null)
            {
                query = query.Where(expression);
            }

            return query;
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException("Invalid filter criteria.", ex);
        }
        catch (NotSupportedException ex)
        {
            throw new InvalidOperationException("An unsupported filter operation was requested.", ex);
        }
    }

    /// <summary>
    /// Parses HTTP query parameters into QueryFilterParameter objects.
    /// Supports multiple formats:
    /// - field[operator]=value
    /// - or_field[operator]=value (for OR logic)
    /// - and_field[operator]=value (for AND logic)  
    /// - field_operator=value (alternative format)
    /// </summary>
    /// <param name="queryParams">Dictionary of query parameter key-value pairs.</param>
    /// <returns>List of parsed filter parameters.</returns>
    public static List<QueryFilterParameter> ParseQueryParameters(Dictionary<string, string> queryParams)
    {
        if (queryParams is null || !queryParams.Any())
            return [];

        List<QueryFilterParameter> filters = [];

        // Extract global settings first
        FilterLogicalOperator globalLogic = FilterLogicalOperator.All; // Default to AND
        if (queryParams.TryGetValue("logic", out string? logicValue) &&
            string.Equals(logicValue, "or", StringComparison.OrdinalIgnoreCase))
        {
            globalLogic = FilterLogicalOperator.Any;
        }

        // Process parameters in order to handle group assignments
        int currentGroup = 0; // Default group

        foreach (KeyValuePair<string, string> param in queryParams)
        {
            if (string.IsNullOrWhiteSpace(param.Key))
                continue;

            // Allow empty values for null check operators
            if (string.IsNullOrWhiteSpace(param.Value) &&
                !param.Key.Contains("[isnull]", StringComparison.OrdinalIgnoreCase) &&
                !param.Key.Contains("[isnotnull]", StringComparison.OrdinalIgnoreCase))
                continue;

            // Check for group assignment parameters like "group1", "group2", etc.
            if (param.Key.StartsWith("group", StringComparison.OrdinalIgnoreCase) &&
                param.Key.Length > 5 &&
                int.TryParse(param.Value, out int groupId))
            {
                currentGroup = groupId;
                continue;
            }

            // Skip special parameters
            if (param.Key.Equals("logic", StringComparison.OrdinalIgnoreCase))
                continue;

            QueryFilterParameter? filter = ParseSingleQueryParameter(param.Key, param.Value, globalLogic, currentGroup);
            if (filter != null)
            {
                filters.Add(filter);
            }
        }

        return filters;
    }

    private static QueryFilterParameter? ParseSingleQueryParameter(string key, string value, FilterLogicalOperator globalLogic, int currentGroup)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        FilterLogicalOperator logicalOp = globalLogic; // Use global default
        bool isLogicalOpExplicit = false;
        string originalKey = key;

        // Check for logical operator prefix: or_name[contains]=john
        if (key.StartsWith("or_", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            logicalOp = FilterLogicalOperator.Any;
            isLogicalOpExplicit = true;
            key = key.Substring(3);
        }
        else if (key.StartsWith("and_", StringComparison.OrdinalIgnoreCase))
        {
            logicalOp = FilterLogicalOperator.All;
            isLogicalOpExplicit = true;
            key = key.Substring(4);
        }

        QueryFilterParameter? filter = null;

        // Expected format: field[operator]=value
        if (key.Contains('[') && key.Contains(']'))
        {
            string fieldName = key.Substring(0, key.IndexOf('['));
            string operatorStr = key.Substring(key.IndexOf('[') + 1,
                key.IndexOf(']') - key.IndexOf('[') - 1);

            if (OperatorMap.TryGetValue(operatorStr.ToLower(), out FilterOperator filterOperator))
            {
                filter = new QueryFilterParameter
                {
                    Field = fieldName,
                    Operator = filterOperator,
                    Value = value,
                    LogicalOperator = logicalOp,
                    IsLogicalOperatorExplicit = isLogicalOpExplicit,
                    Group = currentGroup
                };
            }
        }
        // Alternative format: field_operator=value
        else if (key.Contains('_'))
        {
            string[] parts = key.Split('_');
            if (parts.Length >= 2)
            {
                string fieldName = string.Join("_", parts.Take(parts.Length - 1));
                string operatorStr = parts.Last();

                if (OperatorMap.TryGetValue(operatorStr.ToLower(), out FilterOperator filterOperator))
                {
                    filter = new QueryFilterParameter
                    {
                        Field = fieldName,
                        Operator = filterOperator,
                        Value = value,
                        LogicalOperator = logicalOp,
                        IsLogicalOperatorExplicit = isLogicalOpExplicit,
                        Group = currentGroup
                    };
                }
            }
        }

        return filter;
    }

    private static QueryFilterGroup BuildFilterGroups(List<QueryFilterParameter> filters)
    {
        QueryFilterGroup rootGroup = new QueryFilterGroup { GroupId = 0 };
        Dictionary<int, QueryFilterGroup> groups = new Dictionary<int, QueryFilterGroup> { { 0, rootGroup } };

        foreach (QueryFilterParameter filter in filters)
        {
            int groupId = filter.Group ?? 0;

            if (!groups.ContainsKey(groupId))
            {
                QueryFilterGroup newGroup = new QueryFilterGroup { GroupId = groupId };
                groups[groupId] = newGroup;

                if (groupId != 0)
                {
                    rootGroup.SubGroups.Add(newGroup);
                }
            }

            groups[groupId].Filters.Add(filter);
        }

        return rootGroup;
    }

    private static Expression<Func<T, bool>>? BuildGroupExpression<T>(QueryFilterGroup group)
    {
        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        Expression? expression = BuildGroupExpressionRecursive<T>(parameter, group);

        return expression != null ? Expression.Lambda<Func<T, bool>>(expression, parameter) : null;
    }

    private static Expression? BuildGroupExpressionRecursive<T>(ParameterExpression parameter,
        QueryFilterGroup group)
    {
        Expression? groupExpression = null;

        // Check if any filter in the group has OR logic
        bool hasOrFilter = group.Filters.Any(f => f.LogicalOperator == FilterLogicalOperator.Any);

        // If we have mixed operators, we need to handle them carefully
        if (hasOrFilter && group.Filters.Any(f => f.LogicalOperator == FilterLogicalOperator.All))
        {
            // Check if any AND filters are explicit (have and_ prefix)
            bool hasExplicitAndFilter = group.Filters.Any(f => f.LogicalOperator == FilterLogicalOperator.All && f.IsLogicalOperatorExplicit);

            // If no explicit AND filters, treat everything as OR (simple case)
            if (!hasExplicitAndFilter)
            {
                // Treat everything as OR
                foreach (QueryFilterParameter filter in group.Filters)
                {
                    Expression? filterExpression = BuildQueryFilterExpression<T>(parameter, filter);
                    if (filterExpression != null)
                    {
                        groupExpression = groupExpression == null
                            ? filterExpression
                            : Expression.OrElse(groupExpression, filterExpression);
                    }
                }
            }
            else
            {
                // True mixed operators: group OR filters separately from AND filters
                List<QueryFilterParameter> orFilters = group.Filters.Where(f => f.LogicalOperator == FilterLogicalOperator.Any).ToList();
                List<QueryFilterParameter> andFilters = group.Filters.Where(f => f.LogicalOperator == FilterLogicalOperator.All).ToList();

                Expression? orExpression = null;
                Expression? andExpression = null;

                // Build OR expression from OR filters
                foreach (QueryFilterParameter filter in orFilters)
                {
                    Expression? filterExpression = BuildQueryFilterExpression<T>(parameter, filter);
                    if (filterExpression != null)
                    {
                        orExpression = orExpression == null
                            ? filterExpression
                            : Expression.OrElse(orExpression, filterExpression);
                    }
                }

                // Build AND expression from AND filters  
                foreach (QueryFilterParameter filter in andFilters)
                {
                    Expression? filterExpression = BuildQueryFilterExpression<T>(parameter, filter);
                    if (filterExpression != null)
                    {
                        andExpression = andExpression == null
                            ? filterExpression
                            : Expression.AndAlso(andExpression, filterExpression);
                    }
                }

                // Combine OR and AND expressions
                if (orExpression != null && andExpression != null)
                {
                    groupExpression = Expression.AndAlso(orExpression, andExpression);
                }
                else if (orExpression != null)
                {
                    groupExpression = orExpression;
                }
                else if (andExpression != null)
                {
                    groupExpression = andExpression;
                }
            }
        }
        else
        {
            // All filters have the same logical operator
            FilterLogicalOperator logicalOp = group.Filters.FirstOrDefault()?.LogicalOperator ?? FilterLogicalOperator.All;

            foreach (QueryFilterParameter filter in group.Filters)
            {
                Expression? filterExpression = BuildQueryFilterExpression<T>(parameter, filter);
                if (filterExpression != null)
                {
                    if (groupExpression == null)
                    {
                        groupExpression = filterExpression;
                    }
                    else
                    {
                        groupExpression = logicalOp == FilterLogicalOperator.All
                            ? Expression.AndAlso(groupExpression, filterExpression)
                            : Expression.OrElse(groupExpression, filterExpression);
                    }
                }
            }
        }

        // Handle sub-groups
        foreach (QueryFilterGroup subGroup in group.SubGroups)
        {
            Expression? subExpression = BuildGroupExpressionRecursive<T>(parameter, subGroup);
            if (subExpression != null)
            {
                groupExpression = groupExpression == null
                    ? subExpression
                    : Expression.AndAlso(groupExpression, subExpression);
            }
        }

        return groupExpression;
    }

    private static Expression? BuildQueryFilterExpression<T>(ParameterExpression parameter,
        QueryFilterParameter filter)
    {
        try
        {
            // Use the enhanced property handling with flexible naming
            Expression propertyExpression = GetPropertyExpression<T>(parameter, filter.Field);
            Type propertyType = propertyExpression.Type;

            // Handle nullable types
            Type underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            // Convert the filter to FilterCriteria to reuse existing logic
            FilterCriteria criteria = new FilterCriteria
            {
                PropertyName = filter.Field,
                Operator = filter.Operator,
                Value = ConvertQueryValue(filter.Value, filter.Operator, underlyingType)
            };

            return BuildFilterExpression(criteria, propertyExpression);
        }
        catch
        {
            return null; // Skip invalid filters
        }
    }

    /// <summary>
    /// Creates a mapping of field names to properties, supporting multiple naming conventions.
    /// </summary>
    private static Dictionary<string, PropertyInfo> GetPropertyMapping<T>()
    {
        string cacheKey = typeof(T).FullName ?? typeof(T).Name;

        return PropertyMappingCache.GetOrAdd(cacheKey, _ =>
        {
            Dictionary<string, PropertyInfo> mapping = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            PropertyInfo[] properties = typeof(T).GetProperties()
                .Where(p => p.CanRead)
                .ToArray();

            foreach (PropertyInfo property in properties)
            {
                string propertyName = property.Name;

                // Add original property name
                mapping[propertyName] = property;

                // Add lowercase version
                mapping[propertyName.ToLower()] = property;

                // Add snake_case version
                string snakeCaseName = ToSnakeCase(propertyName);
                mapping[snakeCaseName] = property;

                // Add kebab-case version
                string kebabCaseName = ToKebabCase(propertyName);
                mapping[kebabCaseName] = property;
            }

            return mapping;
        });
    }

    /// <summary>
    /// Finds a property using flexible field name matching for nested properties.
    /// Supports formats like: user.first_name, user.firstName, User.FirstName
    /// </summary>
    private static PropertyInfo? FindProperty<T>(string fieldPath, out Type? containerType)
    {
        containerType = typeof(T);
        PropertyInfo? property = null;

        string[] segments = fieldPath.Split('.');
        Dictionary<string, PropertyInfo> propertyMapping = GetPropertyMapping<T>();

        // Handle single property (no nesting)
        if (segments.Length == 1)
        {
            return FindSingleProperty(propertyMapping, segments[0]);
        }

        // Handle nested properties
        Type currentType = typeof(T);
        for (int i = 0; i < segments.Length; i++)
        {
            string segment = segments[i];
            Dictionary<string, PropertyInfo> currentMapping = GetPropertyMappingForType(currentType);
            property = FindSingleProperty(currentMapping, segment);

            if (property == null)
                return null;

            if (i < segments.Length - 1) // Not the last segment
            {
                currentType = property.PropertyType;
                // Handle nullable types
                currentType = Nullable.GetUnderlyingType(currentType) ?? currentType;
                containerType = currentType;
            }
        }

        return property;
    }

    private static Dictionary<string, PropertyInfo> GetPropertyMappingForType(Type type)
    {
        string cacheKey = type.FullName ?? type.Name;

        return PropertyMappingCache.GetOrAdd(cacheKey, _ =>
        {
            Dictionary<string, PropertyInfo> mapping = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            PropertyInfo[] properties = type.GetProperties()
                .Where(p => p.CanRead)
                .ToArray();

            foreach (PropertyInfo property in properties)
            {
                string propertyName = property.Name;

                // Add original property name
                mapping[propertyName] = property;

                // Add lowercase version
                mapping[propertyName.ToLower()] = property;

                // Add snake_case version
                string snakeCaseName = ToSnakeCase(propertyName);
                mapping[snakeCaseName] = property;

                // Add kebab-case version
                string kebabCaseName = ToKebabCase(propertyName);
                mapping[kebabCaseName] = property;
            }

            return mapping;
        });
    }

    /// <summary>
    /// Finds a property using flexible field name matching.
    /// </summary>
    private static PropertyInfo? FindSingleProperty(Dictionary<string, PropertyInfo> propertyMapping, string fieldName)
    {
        if (propertyMapping.TryGetValue(fieldName, out PropertyInfo? property))
        {
            return property;
        }

        // Try with normalized field name (remove underscores, hyphens, make lowercase)
        string normalizedFieldName = fieldName.Replace("_", "").Replace("-", "").ToLower();

        foreach (KeyValuePair<string, PropertyInfo> kvp in propertyMapping)
        {
            string normalizedMappingKey = kvp.Key.Replace("_", "").Replace("-", "").ToLower();
            if (normalizedMappingKey == normalizedFieldName)
            {
                return kvp.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Converts PascalCase/camelCase to snake_case.
    /// </summary>
    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        StringBuilder result = new StringBuilder();
        result.Append(char.ToLower(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsUpper(c))
            {
                result.Append('_');
                result.Append(char.ToLower(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Converts PascalCase/camelCase to kebab-case.
    /// </summary>
    private static string ToKebabCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        StringBuilder result = new StringBuilder();
        result.Append(char.ToLower(input[0]));

        for (int i = 1; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsUpper(c))
            {
                result.Append('-');
                result.Append(char.ToLower(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Enhanced property expression builder with flexible field name support.
    /// </summary>
    private static Expression GetPropertyExpression<T>(Expression parameter, string propertyName)
    {
        Expression body = parameter;
        string[] segments = propertyName.Split('.');
        Type currentType = typeof(T);

        foreach (string segment in segments)
        {
            try
            {
                Dictionary<string, PropertyInfo> propertyMapping = GetPropertyMappingForType(currentType);
                PropertyInfo? property = FindSingleProperty(propertyMapping, segment);

                if (property == null)
                {
                    throw new ArgumentException($"Property '{segment}' not found on type '{currentType.Name}'. Available properties: {string.Join(", ", propertyMapping.Keys)}");
                }

                MemberExpression propertyAccess = Expression.Property(body, property);

                // Add null check for reference types (except for the root parameter)
                if (!body.Type.IsValueType && body != parameter)
                {
                    // Create null check: body != null ? body.property : default(PropertyType)
                    BinaryExpression nullCheck = Expression.Equal(body, Expression.Constant(null, body.Type));
                    DefaultExpression defaultValue = Expression.Default(propertyAccess.Type);

                    body = Expression.Condition(nullCheck, defaultValue, propertyAccess);
                }
                else
                {
                    body = propertyAccess;
                }

                // Update current type for next iteration
                currentType = property.PropertyType;
                currentType = Nullable.GetUnderlyingType(currentType) ?? currentType;
            }
            catch (ArgumentException)
            {
                // Re-throw with enhanced error message
                IEnumerable<string> availableProperties = GetPropertyMappingForType(currentType).Keys.Take(10);
                throw new ArgumentException($"Property path '{propertyName}' is invalid. Segment '{segment}' not found on type '{currentType.Name}'. Available properties: {string.Join(", ", availableProperties)}...");
            }
        }

        return body;
    }

    private static Expression BuildFilterExpression(FilterCriteria criterion, Expression property)
    {
        return criterion.Operator switch
        {
            FilterOperator.IsNull => BuildNullCheckExpression(criterion, property),
            FilterOperator.IsNotNull => Expression.Not(BuildNullCheckExpression(criterion, property)),
            FilterOperator.In => BuildInExpression(criterion, property),
            FilterOperator.NotIn => Expression.Not(BuildInExpression(criterion, property)),
            FilterOperator.Range => BuildRangeExpression(criterion, property),
            FilterOperator.Contains or FilterOperator.NotContains or FilterOperator.StartsWith or FilterOperator.EndsWith =>
                BuildStringExpression(criterion, property),
            _ => BuildComparisonExpression(criterion, property)
        };
    }

    private static Expression BuildNullCheckExpression(FilterCriteria criterion, Expression property)
    {
        // Handle value types vs reference types
        if (property.Type.IsValueType && Nullable.GetUnderlyingType(property.Type) == null)
        {
            // Non-nullable value type - never null
            return Expression.Constant(false);
        }

        return Expression.Equal(property, Expression.Constant(null, property.Type));
    }

    private static Expression BuildInExpression(FilterCriteria criterion, Expression property)
    {
        if (criterion.Value == null)
        {
            throw new ArgumentException("IN operator requires a non-null value.");
        }

        string valueString = criterion.Value.ToString()!;
        string[] values = valueString.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim())
            .ToArray();

        if (!values.Any())
        {
            return Expression.Constant(false);
        }

        Type propertyType = property.Type;
        Type underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        List<object?> convertedValues = [];
        foreach (string value in values)
        {
            try
            {
                object? convertedValue = ConvertValue(value, underlyingType);
                convertedValues.Add(convertedValue);
            }
            catch
            {
                // Skip invalid values
                continue;
            }
        }

        if (!convertedValues.Any())
        {
            return Expression.Constant(false);
        }

        // Create array of converted values
        Type arrayType = underlyingType.MakeArrayType();
        Array valueArray = Array.CreateInstance(underlyingType, convertedValues.Count);
        for (int i = 0; i < convertedValues.Count; i++)
        {
            valueArray.SetValue(convertedValues[i], i);
        }

        ConstantExpression arrayExpression = Expression.Constant(valueArray, arrayType);
        MethodInfo containsMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .MakeGenericMethod(underlyingType);

        return Expression.Call(containsMethod, arrayExpression, property);
    }

    private static BinaryExpression BuildRangeExpression(FilterCriteria criterion, Expression property)
    {
        if (criterion.Value == null)
        {
            throw new ArgumentException("Range operator requires a non-null value.");
        }

        string valueString = criterion.Value.ToString()!;
        string[] parts = valueString.Split(',', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2)
        {
            throw new ArgumentException("Range operator requires exactly two comma-separated values.");
        }

        Type propertyType = property.Type;
        Type underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        object? minValue = ConvertValue(parts[0].Trim(), underlyingType);
        object? maxValue = ConvertValue(parts[1].Trim(), underlyingType);

        ConstantExpression minExpression = Expression.Constant(minValue, propertyType);
        ConstantExpression maxExpression = Expression.Constant(maxValue, propertyType);

        BinaryExpression greaterThanOrEqual = Expression.GreaterThanOrEqual(property, minExpression);
        BinaryExpression lessThanOrEqual = Expression.LessThanOrEqual(property, maxExpression);

        return Expression.AndAlso(greaterThanOrEqual, lessThanOrEqual);
    }

    private static Expression BuildStringExpression(FilterCriteria criterion, Expression property)
    {
        if (criterion.Value == null)
        {
            return Expression.Constant(false);
        }

        ConstantExpression valueExpression = Expression.Constant(criterion.Value.ToString());
        ConstantExpression comparisonExpression = Expression.Constant(StringComparison.OrdinalIgnoreCase);

        Expression methodCall = (Expression)(criterion.Operator switch
        {
            FilterOperator.Contains => Expression.Call(property, StringContainsMethod, valueExpression, comparisonExpression),
            FilterOperator.StartsWith => Expression.Call(property, StringStartsWithMethod, valueExpression, comparisonExpression),
            FilterOperator.EndsWith => Expression.Call(property, StringEndsWithMethod, valueExpression, comparisonExpression),
            FilterOperator.NotContains => Expression.Not(Expression.Call(property, StringContainsMethod, valueExpression, comparisonExpression)),
            _ => throw new NotSupportedException($"String operator {criterion.Operator} is not supported.")
        });

        return methodCall;
    }

    private static BinaryExpression BuildComparisonExpression(FilterCriteria criterion, Expression property)
    {
        if (criterion.Value == null)
        {
            throw new ArgumentException("Comparison operators require a non-null value.");
        }

        Type propertyType = property.Type;
        Type underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        object? convertedValue = ConvertValue(criterion.Value, underlyingType);
        ConstantExpression valueExpression = Expression.Constant(convertedValue, propertyType);

        return criterion.Operator switch
        {
            FilterOperator.Equal => Expression.Equal(property, valueExpression),
            FilterOperator.NotEqual => Expression.NotEqual(property, valueExpression),
            FilterOperator.GreaterThan => Expression.GreaterThan(property, valueExpression),
            FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(property, valueExpression),
            FilterOperator.LessThan => Expression.LessThan(property, valueExpression),
            FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(property, valueExpression),
            _ => throw new NotSupportedException($"Operator {criterion.Operator} is not supported.")
        };
    }

    private static object? ConvertQueryValue(string value, FilterOperator op, Type targetType)
    {
        if (op == FilterOperator.IsNull || op == FilterOperator.IsNotNull)
            return null; // Null checks don't need values

        if (op == FilterOperator.Range || op == FilterOperator.In || op == FilterOperator.NotIn)
            return value; // These operators need the raw string value for parsing

        if (string.IsNullOrEmpty(value))
            return null;

        return ConvertValue(value, targetType);
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
            return null;

        string? stringValue = value.ToString();
        if (string.IsNullOrEmpty(stringValue))
            return null;

        try
        {
            // Handle common types
            if (targetType == typeof(string))
                return stringValue;

            if (targetType == typeof(int))
                return int.Parse(stringValue, CultureInfo.InvariantCulture);

            if (targetType == typeof(long))
                return long.Parse(stringValue, CultureInfo.InvariantCulture);

            if (targetType == typeof(decimal))
                return decimal.Parse(stringValue, CultureInfo.InvariantCulture);

            if (targetType == typeof(double))
                return double.Parse(stringValue, CultureInfo.InvariantCulture);

            if (targetType == typeof(float))
                return float.Parse(stringValue, CultureInfo.InvariantCulture);

            if (targetType == typeof(bool))
                return bool.Parse(stringValue);

            if (targetType == typeof(DateTime))
                return DateTime.Parse(stringValue, CultureInfo.InvariantCulture);

            if (targetType == typeof(DateTimeOffset))
                return DateTimeOffset.Parse(stringValue, CultureInfo.InvariantCulture);

            if (targetType == typeof(Guid))
                return Guid.Parse(stringValue);

            if (targetType.IsEnum)
                return Enum.Parse(targetType, stringValue, true);

            // Use Convert.ChangeType for other types
            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }

    private static MethodInfo GetCachedMethod(Type type, string methodName, params Type[] parameterTypes)
    {
        string key = $"{type.FullName}.{methodName}({string.Join(",", parameterTypes.Select(t => t.FullName))})";

        return MethodCache.GetOrAdd(key, _ =>
        {
            MethodInfo? method = type.GetMethod(methodName, parameterTypes);
            return method ?? throw new InvalidOperationException($"Method {methodName} not found on type {type.Name}");
        });
    }

    /// <summary>
    /// Parses a query string into a dictionary of key-value pairs.
    /// Handles URL decoding and supports both standard and custom query string formats.
    /// </summary>
    /// <param name="queryString">The query string to parse (with or without leading '?').</param>
    /// <returns>A dictionary of query parameters with case-insensitive keys.</returns>
    private static Dictionary<string, string> ParseQueryString(string queryString)
    {
        // Guard: Null or empty input
        if (string.IsNullOrWhiteSpace(queryString))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Normalize: Remove leading '?' if present
        if (queryString.StartsWith('?'))
            queryString = queryString[1..];

        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Parse: Standard query string using System.Web utilities if available
            NameValueCollection collection = System.Web.HttpUtility.ParseQueryString(queryString);

            foreach (string? key in collection.AllKeys)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    // Assign: First value if multiple values exist for same key
                    string value = collection[key] ?? string.Empty;
                    result[key] = value;
                }
            }
        }
        catch (Exception)
        {
            // Fallback: Manual parsing when System.Web is not available
            result = ParseQueryStringManually(queryString);
        }

        return result;
    }

    /// <summary>
    /// Manual query string parsing fallback when System.Web.HttpUtility is not available.
    /// </summary>
    /// <param name="queryString">The query string to parse.</param>
    /// <returns>A dictionary of query parameters.</returns>
    private static Dictionary<string, string> ParseQueryStringManually(string queryString)
    {
        Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Guard: Empty query string
        if (string.IsNullOrWhiteSpace(queryString))
            return result;

        // Split: Query string into key-value pairs
        string[] pairs = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (string pair in pairs)
        {
            // Parse: Each key-value pair
            string[] keyValue = pair.Split('=', 2);
            if (keyValue.Length >= 1)
            {
                // Decode: URL-encoded key and value
                string key = Uri.UnescapeDataString(keyValue[0]);
                string value = keyValue.Length == 2 ? Uri.UnescapeDataString(keyValue[1]) : string.Empty;

                // Assign: Non-empty keys only
                if (!string.IsNullOrEmpty(key))
                {
                    result[key] = value;
                }
            }
        }

        return result;
    }
}