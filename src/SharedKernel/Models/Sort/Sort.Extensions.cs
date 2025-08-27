using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace SharedKernel.Models.Sort;

public static class SortParamExtensions
{
    private static readonly ConcurrentDictionary<string, PropertyInfo?> _propertyCache = new();
    private static readonly ConcurrentDictionary<string, object> _methodCache = new();

    /// <summary>
    /// Applies sorting with caching and multiple sort criteria support.
    /// </summary>
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, SortParams? sortParams = null)
    {
        if (sortParams == null || sortParams?.SortBy == null)
            return query;

        return query.ApplySingleSort(sortParams.SortBy, sortParams.SortOrder);
    }

    /// <summary>
    /// Applies multiple sort criteria.
    /// </summary>
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, params SortParams[]? sortParams)
    {
        if (sortParams?.Length == 0)
            return query;

        IOrderedQueryable<T>? orderedQuery = null;

        foreach (var sort in sortParams?.Where(s => !string.IsNullOrWhiteSpace(s.SortBy)) ?? Enumerable.Empty<SortParams>())
        {
            if (orderedQuery == null)
            {
                orderedQuery = (IOrderedQueryable<T>)query.ApplySingleSort(sort.SortBy!, sort.SortOrder);
            }
            else
            {
                orderedQuery = orderedQuery.ApplyThenBy(sort.SortBy!, sort.SortOrder);
            }
        }

        return orderedQuery ?? query;
    }

    /// <summary>
    /// Type-safe sorting with lambda expressions.
    /// </summary>
    public static IOrderedQueryable<T> OrderBy<T, TKey>(this IQueryable<T> query, Expression<Func<T, TKey>> keySelector, bool descending = false)
    {
        return descending
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
    }

    /// <summary>
    /// Fluent sorting builder.
    /// </summary>
    public static SortBuilder<T> Sort<T>(this IQueryable<T> query)
    {
        return new SortBuilder<T>(query);
    }

    private static IQueryable<T> ApplySingleSort<T>(this IQueryable<T> query, string sortBy, string sortOrder)
    {
        var propertyKey = $"{typeof(T).Name}.{sortBy}";
        var propertyInfo = _propertyCache.GetOrAdd(propertyKey, _ =>
            typeof(T).GetProperty(sortBy, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));

        if (propertyInfo == null)
            return query;

        var isDescending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        return query.ApplyOrderBy(propertyInfo, isDescending);
    }

    private static IOrderedQueryable<T> ApplyThenBy<T>(this IOrderedQueryable<T> query, string sortBy, string sortOrder)
    {
        var propertyKey = $"{typeof(T).Name}.{sortBy}";
        var propertyInfo = _propertyCache.GetOrAdd(propertyKey, _ =>
            typeof(T).GetProperty(sortBy, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));

        if (propertyInfo == null)
            return query;

        var isDescending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        return query.ApplyThenByInternal(propertyInfo, isDescending) ?? query;
    }

    private static IQueryable<T> ApplyOrderBy<T>(this IQueryable<T> query, PropertyInfo propertyInfo, bool descending)
    {
        var methodKey = $"{typeof(T).Name}.{propertyInfo.Name}.{propertyInfo.PropertyType.Name}.{(descending ? "Desc" : "Asc")}";

        var method = _methodCache.GetOrAdd(methodKey, _ =>
        {
            var methodName = descending ? "OrderByDescending" : "OrderBy";
            return typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName &&
                           m.GetParameters().Length == 2 &&
                           m.GetParameters()[1].ParameterType.IsGenericType)
                .MakeGenericMethod(typeof(T), propertyInfo.PropertyType);
        });

        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, propertyInfo);
        var lambda = Expression.Lambda(property, parameter);

        return (IQueryable<T>)((MethodInfo)method).Invoke(null, new object[] { query, lambda })!;
    }

    private static IOrderedQueryable<T> ApplyThenByInternal<T>(this IOrderedQueryable<T> query, PropertyInfo propertyInfo, bool descending)
    {
        var methodKey = $"{typeof(T).Name}.{propertyInfo.Name}.{propertyInfo.PropertyType.Name}.Then{(descending ? "Desc" : "Asc")}";

        var method = _methodCache.GetOrAdd(methodKey, _ =>
        {
            var methodName = descending ? "ThenByDescending" : "ThenBy";
            return typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName &&
                           m.GetParameters().Length == 2 &&
                           m.GetParameters()[1].ParameterType.IsGenericType)
                .MakeGenericMethod(typeof(T), propertyInfo.PropertyType);
        });

        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, propertyInfo);
        var lambda = Expression.Lambda(property, parameter);

        return (IOrderedQueryable<T>)(((MethodInfo)method).Invoke(null, new object[] { query, lambda }) ?? query);
    }
}

