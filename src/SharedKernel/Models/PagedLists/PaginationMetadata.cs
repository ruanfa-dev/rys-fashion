namespace SharedKernel.Models.PagedLists;

/// <summary>
/// Pagination metadata for API responses containing paginated data.
/// Provides comprehensive information about the current page state and navigation options.
/// </summary>
public sealed class PaginationMetadata
{
    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPrevious { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNext { get; set; }

    /// <summary>
    /// Index of the first item on the current page (1-based)
    /// </summary>
    public int FirstItemIndex { get; set; }

    /// <summary>
    /// Index of the last item on the current page (1-based)
    /// </summary>
    public int LastItemIndex { get; set; }

    /// <summary>
    /// Gets a value indicating whether this is the first page
    /// </summary>
    public bool IsFirstPage => CurrentPage == 1;

    /// <summary>
    /// Gets a value indicating whether this is the last page
    /// </summary>
    public bool IsLastPage => CurrentPage >= TotalPages;

    /// <summary>
    /// Gets a value indicating whether the result set is empty
    /// </summary>
    public bool IsEmpty => TotalItems == 0;

    /// <summary>
    /// Creates pagination metadata from a PagedList
    /// </summary>
    /// <typeparam name="T">Type of items in the paged list</typeparam>
    /// <param name="pagedList">The paged list</param>
    /// <returns>Pagination metadata</returns>
    public static PaginationMetadata FromPagedList<T>(PagedList<T> pagedList)
    {
        return new PaginationMetadata
        {
            CurrentPage = pagedList.PageNumber,
            PageSize = pagedList.PageSize,
            TotalItems = pagedList.TotalCount,
            TotalPages = pagedList.TotalPages,
            HasPrevious = pagedList.HasPreviousPage,
            HasNext = pagedList.HasNextPage,
            FirstItemIndex = pagedList.StartIndex,
            LastItemIndex = pagedList.EndIndex
        };
    }

    /// <summary>
    /// Creates pagination metadata manually
    /// </summary>
    /// <param name="currentPage">Current page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="totalItems">Total number of items</param>
    /// <returns>Pagination metadata</returns>
    public static PaginationMetadata Create(int currentPage, int pageSize, int totalItems)
    {
        var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalItems / (double)pageSize) : 0;
        var firstItemIndex = totalItems > 0 ? (currentPage - 1) * pageSize + 1 : 0;
        var lastItemIndex = Math.Min(currentPage * pageSize, totalItems);

        return new PaginationMetadata
        {
            CurrentPage = currentPage,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            HasPrevious = currentPage > 1,
            HasNext = currentPage < totalPages && totalPages > 0,
            FirstItemIndex = firstItemIndex,
            LastItemIndex = lastItemIndex
        };
    }
}
