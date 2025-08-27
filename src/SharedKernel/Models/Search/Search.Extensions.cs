using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace SharedKernel.Models.Search;

public static class SearchParamsExtensions
{
    // Cache compiled expressions for better performance
    private static readonly ConcurrentDictionary<string, Func<object, string?, bool>> CompiledSearchExpressions = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> StringPropertiesCache = new();


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

        foreach (string fieldName in searchFields)
        {
            PropertyInfo? property = entityType.GetProperty(fieldName);
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
            MethodInfo? equalsMethod = typeof(string).GetMethod("Equals", new[] { typeof(string) });
            searchCall = Expression.Call(searchExpression, equalsMethod!, searchConstant);
        }
        else if (options.StartsWith.HasValue && options.StartsWith.Value)
        {
            MethodInfo? startsWithMethod = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
            searchCall = Expression.Call(searchExpression, startsWithMethod!, searchConstant);
        }
        else
        {
            MethodInfo? containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
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

    private class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ParameterReplacer(
            ParameterExpression oldParameter,
            ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }
}