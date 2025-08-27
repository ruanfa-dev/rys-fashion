using Microsoft.EntityFrameworkCore;

using SharedKernel.Models.Paging;

namespace SharedKernel.Models.PagedLists;

/// <summary>
/// Extension methods for creating PagedList instances from IQueryable and IEnumerable sources using PagingParams.
/// </summary>
public static class PagedListExtensions
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;
    private const int MaxAllItemsLimit = 1000;

    #region IQueryable Extensions (Async)

    /// <summary>
    /// Creates a PagedList from an IQueryable using PagingParams with specified page size.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="pagingParams">The paging parameters.</param>
    /// <param name="fallbackDefaultPageSize">The page size (defaults to 10, max 100).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A PagedList containing the requested page of items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when query is null.</exception>
    public static async Task<PagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> query,
        PagingParams? pagingParams,
        int fallbackDefaultPageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        // Use pagingParams.PageSize if provided, otherwise use fallback
        var effectivePageSize = NormalizePageSize(pagingParams?.PageSize ?? fallbackDefaultPageSize);
        var (effectivePageIndex, effectivePageNumber) = CalculatePaginationValues(pagingParams);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = CalculateTotalPages(totalCount, effectivePageSize);

        // Adjust page number if beyond total pages or if no data exists
        (effectivePageNumber, effectivePageIndex) = AdjustPageBounds(effectivePageNumber, effectivePageIndex, totalPages);

        var items = await query
            .Skip(effectivePageIndex * effectivePageSize)
            .Take(effectivePageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<T>(items, totalCount, effectivePageNumber, effectivePageSize);
    }

    /// <summary>
    /// Creates a PagedList from an IQueryable using default pagination or returns all items (capped) if no paging parameters are provided.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="pagingParams">The paging parameters.</param>
    /// <param name="maxAllItemsLimit">The maximum number of items to return when no pagination is applied.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A PagedList containing the requested page of items or all items (capped) if no paging is specified.</returns>
    /// <exception cref="ArgumentNullException">Thrown when query is null.</exception>
    public static async Task<PagedList<T>> ToPagedListOrAllAsync<T>(
        this IQueryable<T> query,
        PagingParams? pagingParams,
        int maxAllItemsLimit = MaxAllItemsLimit,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        var totalCount = await query.CountAsync(cancellationToken);

        if (pagingParams?.HasPagingValues() != true)
        {
            // Return all items but cap at max limit
            var effectiveLimit = Math.Min(Math.Max(maxAllItemsLimit, 1), Math.Min(MaxAllItemsLimit, totalCount));
            var allItems = await query.Take(effectiveLimit).ToListAsync(cancellationToken);

            // When returning all items (capped), use meaningful pagination metadata:
            // - If we returned all items (not capped): TotalPages = 1, PageSize = totalCount
            // - If we capped items: TotalPages = calculated based on maxAllItemsLimit as page size
            var isFullyReturned = effectiveLimit >= totalCount;
            var pageSize = isFullyReturned ? Math.Max(totalCount, 1) : effectiveLimit;
            var totalPagesInternal = isFullyReturned ? 1 : CalculateTotalPages(totalCount, effectiveLimit);

            return new PagedList<T>(allItems, totalCount, 1, pageSize, totalPagesInternal);
        }

        // Apply pagination - use pagingParams.PageSize if provided
        var effectivePageSize = NormalizePageSize(pagingParams.PageSize ?? DefaultPageSize);
        var (effectivePageIndex, effectivePageNumber) = CalculatePaginationValues(pagingParams);

        var totalPages = CalculateTotalPages(totalCount, effectivePageSize);

        // Adjust page number if beyond total pages or if no data exists
        (effectivePageNumber, effectivePageIndex) = AdjustPageBounds(effectivePageNumber, effectivePageIndex, totalPages);

        var items = await query
            .Skip(effectivePageIndex * effectivePageSize)
            .Take(effectivePageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<T>(items, totalCount, effectivePageNumber, effectivePageSize);
    }

    /// <summary>
    /// Creates a PagedList from an IQueryable using default pagination if no paging parameters are provided.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="pagingParams">The paging parameters.</param>
    /// <param name="defaultPageSize">The default page size to use when no paging params provided.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A PagedList containing the requested page of items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when query is null.</exception>
    public static async Task<PagedList<T>> ToPagedListOrDefaultAsync<T>(
        this IQueryable<T> query,
        PagingParams? pagingParams,
        int defaultPageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        // Use pagingParams.PageSize if provided, otherwise use defaultPageSize
        var effectivePageSize = NormalizePageSize(pagingParams?.PageSize ?? defaultPageSize);
        var (effectivePageIndex, effectivePageNumber) = CalculatePaginationValues(pagingParams);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = CalculateTotalPages(totalCount, effectivePageSize);

        // Adjust page number if beyond total pages or if no data exists
        (effectivePageNumber, effectivePageIndex) = AdjustPageBounds(effectivePageNumber, effectivePageIndex, totalPages);

        var items = await query
            .Skip(effectivePageIndex * effectivePageSize)
            .Take(effectivePageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<T>(items, totalCount, effectivePageNumber, effectivePageSize);
    }

    /// <summary>
    /// Creates a PagedList with pre-computed total count for better performance.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    /// <param name="query">The source queryable (already filtered and sorted).</param>
    /// <param name="pagingParams">The paging parameters.</param>
    /// <param name="totalCount">The pre-computed total count.</param>
    /// <param name="fallbackPageSize">The fallback page size (defaults to 10, max 100).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A PagedList containing the requested page of items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when query is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when totalCount is negative.</exception>
    public static async Task<PagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> query,
        PagingParams? pagingParams,
        int totalCount,
        int fallbackPageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query, nameof(query));

        if (totalCount < 0)
            throw new ArgumentOutOfRangeException(nameof(totalCount), "Total count cannot be negative.");

        // Use pagingParams.PageSize if provided, otherwise use fallbackPageSize
        var effectivePageSize = NormalizePageSize(pagingParams?.PageSize ?? fallbackPageSize);
        var (effectivePageIndex, effectivePageNumber) = CalculatePaginationValues(pagingParams);

        var totalPages = CalculateTotalPages(totalCount, effectivePageSize);

        // Adjust page number if beyond total pages or if no data exists
        (effectivePageNumber, effectivePageIndex) = AdjustPageBounds(effectivePageNumber, effectivePageIndex, totalPages);

        var items = await query
            .Skip(effectivePageIndex * effectivePageSize)
            .Take(effectivePageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<T>(items, totalCount, effectivePageNumber, effectivePageSize);
    }

    #endregion

    #region IEnumerable Extensions (Sync)

    /// <summary>
    /// Creates a PagedList from a collection using PagingParams.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="pagingParams">The paging parameters.</param>
    /// <param name="fallbackPageSize">The fallback page size (defaults to 10, max 100).</param>
    /// <returns>A PagedList containing the requested page of items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
    public static PagedList<T> ToPagedList<T>(
        this IEnumerable<T> source,
        PagingParams? pagingParams,
        int fallbackPageSize = DefaultPageSize)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));

        var sourceList = source as IList<T> ?? source.ToList();
        var totalCount = sourceList.Count;

        // Use pagingParams.PageSize if provided, otherwise use fallbackPageSize
        var effectivePageSize = NormalizePageSize(pagingParams?.PageSize ?? fallbackPageSize);
        var (effectivePageIndex, effectivePageNumber) = CalculatePaginationValues(pagingParams);

        // Validate page bounds and adjust if necessary
        var totalPages = CalculateTotalPages(totalCount, effectivePageSize);
        (effectivePageNumber, effectivePageIndex) = AdjustPageBounds(effectivePageNumber, effectivePageIndex, totalPages);

        var items = sourceList
            .Skip(effectivePageIndex * effectivePageSize)
            .Take(effectivePageSize);

        return new PagedList<T>(items, totalCount, effectivePageNumber, effectivePageSize);
    }

    /// <summary>
    /// Creates a PagedList from a collection using PagingParams, or returns all items (capped) if no paging parameters are provided.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    /// <param name="source">The source collection.</param>
    /// <param name="pagingParams">The paging parameters.</param>
    /// <param name="maxAllItemsLimit">The maximum number of items to return when no pagination is applied.</param>
    /// <returns>A PagedList containing the requested page of items or all items (capped) if no paging is specified.</returns>
    /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
    public static PagedList<T> ToPagedListOrAll<T>(
        this IEnumerable<T> source,
        PagingParams? pagingParams,
        int maxAllItemsLimit = MaxAllItemsLimit)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));

        var sourceList = source as IList<T> ?? source.ToList();
        var totalCount = sourceList.Count;

        if (pagingParams?.HasPagingValues() != true)
        {
            // Return all items but cap at max limit
            var effectiveLimit = Math.Min(Math.Max(maxAllItemsLimit, 1), Math.Min(MaxAllItemsLimit, totalCount));
            var cappedItems = sourceList.Take(effectiveLimit);

            // When returning all items (capped), use meaningful pagination metadata
            var isFullyReturned = effectiveLimit >= totalCount;
            var pageSize = isFullyReturned ? Math.Max(totalCount, 1) : effectiveLimit;
            var totalPages = isFullyReturned ? 1 : CalculateTotalPages(totalCount, effectiveLimit);

            return new PagedList<T>(cappedItems, totalCount, 1, pageSize, totalPages);
        }

        // Apply specified pagination - delegate to ToPagedList to ensure consistent behavior
        return sourceList.ToPagedList(pagingParams, DefaultPageSize);
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates an empty PagedList with the specified pagination metadata.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    /// <param name="pagingParams">The paging parameters.</param>
    /// <param name="totalCount">The total count of items across all pages.</param>
    /// <param name="fallbackPageSize">The fallback page size (defaults to 10, max 100).</param>
    /// <returns>An empty PagedList with the specified pagination metadata.</returns>
    public static PagedList<T> CreateEmpty<T>(
        PagingParams? pagingParams = null,
        int totalCount = 0,
        int fallbackPageSize = DefaultPageSize)
    {
        if (totalCount < 0)
            throw new ArgumentOutOfRangeException(nameof(totalCount), "Total count cannot be negative.");

        // Use pagingParams.PageSize if provided, otherwise use fallbackPageSize
        var effectivePageSize = NormalizePageSize(pagingParams?.PageSize ?? fallbackPageSize);
        var (effectivePageIndex, effectivePageNumber) = CalculatePaginationValues(pagingParams);

        var totalPages = CalculateTotalPages(totalCount, effectivePageSize);

        // Adjust page number if beyond total pages or if no data exists
        (effectivePageNumber, effectivePageIndex) = AdjustPageBounds(effectivePageNumber, effectivePageIndex, totalPages);

        return new PagedList<T>([], totalCount, effectivePageNumber, effectivePageSize);
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Normalizes the page size to ensure it's within acceptable bounds.
    /// </summary>
    /// <param name="pageSize">The requested page size.</param>
    /// <returns>A normalized page size between 1 and MaxPageSize.</returns>
    private static int NormalizePageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

    /// <summary>
    /// Calculates effective page index (0-based) and page number (1-based) from paging parameters.
    /// </summary>
    /// <param name="pagingParams">The paging parameters.</param>
    /// <returns>A tuple containing the effective page index and page number.</returns>
    private static (int EffectivePageIndex, int EffectivePageNumber) CalculatePaginationValues(PagingParams? pagingParams)
    {
        var effectivePageIndex = Math.Max(pagingParams?.EffectivePageIndex() ?? 0, 0);
        // Cap page index to prevent overflow when computing page number
        effectivePageIndex = Math.Min(effectivePageIndex, int.MaxValue - 1);
        var effectivePageNumber = effectivePageIndex + 1;
        return (effectivePageIndex, effectivePageNumber);
    }

    /// <summary>
    /// Calculates the total number of pages based on total count and page size.
    /// </summary>
    /// <param name="totalCount">The total number of items.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>The total number of pages.</returns>
    private static int CalculateTotalPages(int totalCount, int pageSize) =>
        pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;

    /// <summary>
    /// Adjusts page bounds to ensure valid page numbers and handles edge cases.
    /// </summary>
    /// <param name="requestedPageNumber">The requested page number (1-based).</param>
    /// <param name="requestedPageIndex">The requested page index (0-based).</param>
    /// <param name="totalPages">The total number of available pages.</param>
    /// <returns>A tuple containing the adjusted page number and page index.</returns>
    private static (int AdjustedPageNumber, int AdjustedPageIndex) AdjustPageBounds(
        int requestedPageNumber,
        int requestedPageIndex,
        int totalPages)
    {
        // If no data exists or no pages, always return page 1
        if (totalPages <= 0)
        {
            return (1, 0);
        }

        // If requested page is beyond available pages, return the last page
        if (requestedPageNumber > totalPages)
        {
            return (totalPages, totalPages - 1);
        }

        // If requested page is valid, use it as-is
        return (requestedPageNumber, requestedPageIndex);
    }

    #endregion
}