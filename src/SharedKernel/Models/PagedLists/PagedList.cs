namespace SharedKernel.Models.PagedLists;

/// <summary>
/// Represents a paginated list of items with metadata about pagination state.
/// </summary>
/// <typeparam name="T">The type of items in the list.</typeparam>
public class PagedList<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PagedList{T}"/> class.
    /// </summary>
    /// <param name="items">The collection of items for the current page.</param>
    /// <param name="totalCount">The total number of items across all pages.</param>
    /// <param name="pageNumber">The current page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are invalid.</exception>
    public PagedList(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
        : this(items, totalCount, pageNumber, pageSize, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PagedList{T}"/> class with an optional total pages override.
    /// </summary>
    /// <param name="items">The collection of items for the current page.</param>
    /// <param name="totalCount">The total number of items across all pages.</param>
    /// <param name="pageNumber">The current page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="totalPages">Optional override for the total number of pages. If null, calculated based on totalCount and pageSize.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are invalid.</exception>
    public PagedList(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize, int? totalPages)
    {
        ArgumentNullException.ThrowIfNull(items, nameof(items));

        if (totalCount < 0)
            throw new ArgumentOutOfRangeException(nameof(totalCount), "Total count cannot be negative.");
        if (pageNumber < 0)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than 0.");
        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than 0.");
        if (totalPages.HasValue && totalPages < 0)
            throw new ArgumentOutOfRangeException(nameof(totalPages), "Total pages cannot be negative.");

        var itemsList = items.ToList();
        Items = itemsList.AsReadOnly();
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = totalPages ?? (pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0);

        // Calculate additional metadata
        PageIndex = pageNumber - 1; // 0-based index
        StartIndex = PageIndex * pageSize + 1;
        EndIndex = Math.Min(StartIndex + itemsList.Count - 1, totalCount);
        IsFirstPage = pageNumber == 1;
        IsLastPage = pageNumber >= TotalPages;
    }

    /// <summary>
    /// Gets the collection of items for the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// Gets the current page number (1-based).
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// Gets the current page index (0-based).
    /// </summary>
    public int PageIndex { get; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages { get; }

    /// <summary>
    /// Gets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Gets the count of items in the current page.
    /// </summary>
    public int Count => Items.Count;

    /// <summary>
    /// Gets the 1-based start index of items in the current page relative to the total collection.
    /// </summary>
    public int StartIndex { get; }

    /// <summary>
    /// Gets the 1-based end index of items in the current page relative to the total collection.
    /// </summary>
    public int EndIndex { get; }

    /// <summary>
    /// Gets a value indicating whether this is the first page.
    /// </summary>
    public bool IsFirstPage { get; }

    /// <summary>
    /// Gets a value indicating whether this is the last page.
    /// </summary>
    public bool IsLastPage { get; }

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages && TotalPages > 0;

    /// <summary>
    /// Gets a value indicating whether the list is empty.
    /// </summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// Maps the items in the current page to a new type while preserving pagination metadata.
    /// </summary>
    /// <typeparam name="TResult">The target type to map to.</typeparam>
    /// <param name="mapper">A function that transforms each item from type T to type TResult.</param>
    /// <returns>A new <see cref="PagedList{TResult}"/> with mapped items and the same pagination metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="mapper"/> is null.</exception>
    public PagedList<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper, nameof(mapper));
        var mappedItems = Items.Select(mapper);
        return new PagedList<TResult>(mappedItems, TotalCount, PageNumber, PageSize, TotalPages);
    }

    /// <summary>
    /// Maps the items in the current page to a new type with access to the item index while preserving pagination metadata.
    /// </summary>
    /// <typeparam name="TResult">The target type to map to.</typeparam>
    /// <param name="mapper">A function that transforms each item from type T to type TResult, with access to the zero-based index.</param>
    /// <returns>A new <see cref="PagedList{TResult}"/> with mapped items and the same pagination metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="mapper"/> is null.</exception>
    public PagedList<TResult> Map<TResult>(Func<T, int, TResult> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper, nameof(mapper));
        var mappedItems = Items.Select(mapper);
        return new PagedList<TResult>(mappedItems, TotalCount, PageNumber, PageSize, TotalPages);
    }

    /// <summary>
    /// Creates an empty PagedList with specified pagination metadata.
    /// </summary>
    /// <param name="totalCount">The total count of items across all pages.</param>
    /// <param name="pageNumber">The current page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>An empty PagedList with the specified pagination metadata.</returns>
    public static PagedList<T> Empty(int totalCount = 0, int pageNumber = 1, int pageSize = 10)
    {
        return new PagedList<T>([], totalCount, pageNumber, pageSize);
    }
}