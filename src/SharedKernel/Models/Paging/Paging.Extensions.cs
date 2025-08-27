namespace SharedKernel.Models.Paging;


/// <summary>
/// Extension methods for applying pagination to IQueryable sources.
/// </summary>
public static class PagingExtensions
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;
    private const int MaxAllItemsLimit = 1000;

    /// <summary>
    /// Applies pagination to a queryable using default page size if no paging parameters are provided.
    /// </summary>
    /// <typeparam name="T">The type of items in the queryable.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="pagingParams">The paging parameters (optional).</param>
    /// <param name="fallbackDefaultPageSize">The default page size to use when no paging params provided.</param>
    /// <returns>A queryable with pagination applied.</returns>
    /// <exception cref="ArgumentNullException">Thrown when query is null.</exception>
    public static IQueryable<T> ApplyPagingOrDefault<T>(
        this IQueryable<T> query,
        PagingParams? pagingParams,
        int fallbackDefaultPageSize = DefaultPageSize)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        if (pagingParams?.HasPagingValues() != true)
        {
            // No paging parameters provided, use default pagination
            var pageSize = NormalizePageSize(fallbackDefaultPageSize);
            return query.Take(pageSize);
        }

        // Apply specified pagination
        var pageIndex = Math.Max(pagingParams.EffectivePageIndex(), 0);
        var effectivePageSize = NormalizePageSize(pagingParams.PageSize ?? DefaultPageSize);

        return query
            .Skip(pageIndex * effectivePageSize)
            .Take(effectivePageSize);
    }

    /// <summary>
    /// Applies pagination to a queryable or returns all items (capped at max limit) if no paging parameters are provided.
    /// </summary>
    /// <typeparam name="T">The type of items in the queryable.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="pagingParams">The paging parameters (optional).</param>
    /// <param name="maxAllItemsLimit">The maximum number of items to return when no pagination is applied.</param>
    /// <returns>A queryable with pagination applied or all items (capped).</returns>
    /// <exception cref="ArgumentNullException">Thrown when query is null.</exception>
    public static IQueryable<T> ApplyPagingOrAll<T>(
        this IQueryable<T> query,
        PagingParams? pagingParams,
        int maxAllItemsLimit = MaxAllItemsLimit)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        if (pagingParams?.HasPagingValues() != true)
        {
            // No paging parameters provided, return all items but cap at max limit
            var effectiveLimit = Math.Min(Math.Max(maxAllItemsLimit, 1), MaxAllItemsLimit);
            return query.Take(effectiveLimit);
        }

        // Apply specified pagination
        var pageIndex = Math.Max(pagingParams.EffectivePageIndex(), 0);
        var effectivePageSize = NormalizePageSize(pagingParams.PageSize ?? DefaultPageSize);

        return query
            .Skip(pageIndex * effectivePageSize)
            .Take(effectivePageSize);
    }

    /// <summary>
    /// Gets the effective page index (0-based) from paging parameters.
    /// </summary>
    /// <param name="pagingParams">The paging parameters.</param>
    /// <returns>The effective page index (0-based).</returns>
    public static int EffectivePageIndex(this PagingParams pagingParams)
    {
        ArgumentNullException.ThrowIfNull(pagingParams, nameof(pagingParams));
        return Math.Max(pagingParams.PageIndex ?? 0, 0);
    }

    /// <summary>
    /// Gets the effective page number (1-based) from paging parameters.
    /// </summary>
    /// <param name="pagingParams">The paging parameters.</param>
    /// <returns>The effective page number (1-based).</returns>
    public static int EffectivePageNumber(this PagingParams pagingParams)
    {
        ArgumentNullException.ThrowIfNull(pagingParams, nameof(pagingParams));
        return Math.Max(pagingParams.PageIndex ?? 0, 0) + 1;
    }

    /// <summary>
    /// Checks if paging parameters contain valid paging values.
    /// </summary>
    /// <param name="pagingParams">The paging parameters to check.</param>
    /// <returns>True if paging parameters contain valid paging values; otherwise, false.</returns>
    public static bool HasPagingValues(this PagingParams? pagingParams)
    {
        return pagingParams?.PageSize.HasValue == true || pagingParams?.PageIndex.HasValue == true;
    }

    /// <summary>
    /// Normalizes the page size to ensure it's within acceptable bounds.
    /// </summary>
    /// <param name="pageSize">The requested page size.</param>
    /// <returns>A normalized page size between 1 and MaxPageSize.</returns>
    private static int NormalizePageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}