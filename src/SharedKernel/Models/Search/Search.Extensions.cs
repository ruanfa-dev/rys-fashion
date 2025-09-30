using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace SharedKernel.Models.Search;

public static class SearchParamsExtensions
{
    // Cache compiled expressions for better performance
    private static readonly ConcurrentDictionary<string, Func<object, string?, bool>> CompiledSearchExpressions = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> StringPropertiesCache = new();
    private static readonly ConcurrentDictionary<string, Dictionary<string, PropertyInfo>> PropertyMappingCache = new();

    /// <summary>
    /// Applies search with configurable options for SPA scenarios.
    /// </summary>
    public static IQueryable<T> ApplySearch<T>(this IQueryable<T> query, SearchParams searchParams)
    {
        if (string.IsNullOrWhiteSpace(searchParams.SearchTerm))
            return query;

        // Create options from SearchParams
        var options = new SearchOptions
        {
            StartsWith = searchParams.StartsWith,
            ExactMatch = searchParams.ExactMatch,
            CaseSensitive = searchParams.CaseSensitive
        };

        string searchTerm = options.CaseSensitive.HasValue && options.CaseSensitive.Value
            ? searchParams.SearchTerm
            : searchParams.SearchTerm.ToLower(System.Globalization.CultureInfo.CurrentCulture);

        if (searchParams.SearchFields?.Length > 0)
        {
            return query.ApplySearchInFields(searchTerm, searchParams.SearchFields, options);
        }
        else
        {
            return query.ApplyFullTextSearch(searchTerm, options);
        }
    }

    /// <summary>
    /// Applies search with configurable options for SPA scenarios.
    /// </summary>
    public static IQueryable<T> ApplySearch<T>(this IQueryable<T> query, SearchParameter searchParams, SearchOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(searchParams.SearchTerm))
            return query;

        options ??= new SearchOptions();
        string searchTerm = options.CaseSensitive ?? false ? searchParams.SearchTerm : searchParams.SearchTerm.ToLower(System.Globalization.CultureInfo.CurrentCulture);

        if (searchParams.SearchFields?.Length > 0)
        {
            return query.ApplySearchInFields(searchTerm, searchParams.SearchFields, options);
        }
        else
        {
            return query.ApplyFullTextSearch(searchTerm, options);
        }
    }

    /// <summary>
    /// Fluent API for typed search with lambda expressions.
    /// </summary>
    public static SearchBuilder<T> Search<T>(this IQueryable<T> query, string searchTerm)
    {
        return new SearchBuilder<T>(query, searchTerm);
    }

    /// <summary>
    /// Quick search for single field.
    /// </summary>
    public static IQueryable<T> SearchIn<T>(this IQueryable<T> query, string searchTerm, Expression<Func<T, string>> field)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        return query.ApplySearch(searchTerm, field);
    }

    /// <summary>
    /// Search with multiple lambda expressions.
    /// </summary>
    public static IQueryable<T> ApplySearch<T>(this IQueryable<T> query, string searchTerm, params Expression<Func<T, string>>[] searchExpressions)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchExpressions.Length == 0)
            return query;

        ParameterExpression param = Expression.Parameter(typeof(T), "x");
        ConstantExpression searchConstant = Expression.Constant(searchTerm.ToLower(System.Globalization.CultureInfo.CurrentCulture));
        Expression? combinedExpression = null;

        foreach (Expression<Func<T, string>> searchExpression in searchExpressions)
        {
            Expression? propertyExpression = ReplaceParameter(searchExpression.Body, searchExpression.Parameters[0], param);
            BinaryExpression? searchCondition = CreateSearchCondition(propertyExpression, searchConstant);

            combinedExpression = combinedExpression == null
                ? searchCondition
                : Expression.OrElse(combinedExpression, searchCondition);
        }

        if (combinedExpression != null)
        {
            Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, param);
            query = query.Where(lambda);
        }

        return query;
    }

    private static IQueryable<T> ApplySearchInFields<T>(this IQueryable<T> query, string searchTerm, string[] searchFields, SearchOptions options)
    {
        ParameterExpression param = Expression.Parameter(typeof(T), "x");
        ConstantExpression searchConstant = Expression.Constant(searchTerm);
        Expression? combinedExpression = null;

        Type entityType = typeof(T);
        Dictionary<string, PropertyInfo> propertyMapping = GetPropertyMapping<T>();

        foreach (string fieldName in searchFields)
        {
            PropertyInfo? property = FindProperty(propertyMapping, fieldName);
            if (property == null || property.PropertyType != typeof(string))
                continue;

            MemberExpression propertyAccess = Expression.Property(param, property);
            BinaryExpression searchCondition = CreateSearchCondition(propertyAccess, searchConstant, options);

            combinedExpression = combinedExpression == null
                ? searchCondition
                : Expression.OrElse(combinedExpression, searchCondition);
        }

        if (combinedExpression != null)
        {
            Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, param);
            query = query.Where(lambda);
        }

        return query;
    }

    private static IQueryable<T> ApplyFullTextSearch<T>(this IQueryable<T> query, string searchTerm, SearchOptions options)
    {
        PropertyInfo[] stringProperties = GetStringProperties<T>();
        if (stringProperties.Length == 0)
            return query;

        ParameterExpression param = Expression.Parameter(typeof(T), "x");
        ConstantExpression searchConstant = Expression.Constant(searchTerm);
        Expression? combinedExpression = null;

        foreach (PropertyInfo property in stringProperties)
        {
            MemberExpression propertyAccess = Expression.Property(param, property);
            BinaryExpression searchCondition = CreateSearchCondition(propertyAccess, searchConstant, options);

            combinedExpression = combinedExpression == null
                ? searchCondition
                : Expression.OrElse(combinedExpression, searchCondition);
        }

        if (combinedExpression != null)
        {
            Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, param);
            query = query.Where(lambda);
        }

        return query;
    }

    /// <summary>
    /// Creates a mapping of field names to properties, supporting multiple naming conventions.
    /// </summary>
    private static Dictionary<string, PropertyInfo> GetPropertyMapping<T>()
    {
        string cacheKey = typeof(T).FullName ?? typeof(T).Name;

        return PropertyMappingCache.GetOrAdd(cacheKey, _ =>
        {
            var mapping = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            PropertyInfo[] properties = typeof(T).GetProperties()
                .Where(p => p.PropertyType == typeof(string) && p.CanRead)
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

                // Add kebab-case version (bonus)
                string kebabCaseName = ToKebabCase(propertyName);
                mapping[kebabCaseName] = property;
            }

            return mapping;
        });
    }

    /// <summary>
    /// Finds a property using flexible field name matching.
    /// </summary>
    private static PropertyInfo? FindProperty(Dictionary<string, PropertyInfo> propertyMapping, string fieldName)
    {
        if (propertyMapping.TryGetValue(fieldName, out PropertyInfo? property))
        {
            return property;
        }

        // Try with normalized field name (remove underscores, hyphens, make lowercase)
        string normalizedFieldName = fieldName.Replace("_", "").Replace("-", "").ToLower();

        foreach (var kvp in propertyMapping)
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

        var result = new StringBuilder();
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

        var result = new StringBuilder();
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

    private static BinaryExpression CreateSearchCondition(Expression propertyExpression, ConstantExpression searchConstant, SearchOptions? options = null)
    {
        options ??= new SearchOptions();

        // Null check
        BinaryExpression nullCheck = Expression.NotEqual(propertyExpression, Expression.Constant(null, typeof(string)));

        Expression searchExpression;

        if (options.CaseSensitive.HasValue && options.CaseSensitive.Value)
        {
            searchExpression = propertyExpression;
        }
        else
        {
            MethodInfo? toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes);
            searchExpression = Expression.Call(propertyExpression, toLowerMethod!);
        }

        // Choose search method
        Expression searchCall;
        if (options.ExactMatch.HasValue && options.ExactMatch.Value)
        {
            MethodInfo? equalsMethod = typeof(string).GetMethod("Equals", [typeof(string)]);
            searchCall = Expression.Call(searchExpression, equalsMethod!, searchConstant);
        }
        else if (options.StartsWith.HasValue && options.StartsWith.Value)
        {
            MethodInfo? startsWithMethod = typeof(string).GetMethod("StartsWith", [typeof(string)]);
            searchCall = Expression.Call(searchExpression, startsWithMethod!, searchConstant);
        }
        else
        {
            MethodInfo? containsMethod = typeof(string).GetMethod("Contains", [typeof(string)]);
            searchCall = Expression.Call(searchExpression, containsMethod!, searchConstant);
        }

        return Expression.AndAlso(nullCheck, searchCall);
    }

    private static PropertyInfo[] GetStringProperties<T>()
    {
        return StringPropertiesCache.GetOrAdd(typeof(T), type =>
            type.GetProperties()
                .Where(p => p.PropertyType == typeof(string) && p.CanRead)
                .ToArray());
    }

    private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        return new ParameterReplacer(oldParameter, newParameter).Visit(expression);
    }

    private class ParameterReplacer(
        ParameterExpression oldParameter,
        ParameterExpression newParameter)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == oldParameter ? newParameter : base.VisitParameter(node);
        }
    }
}