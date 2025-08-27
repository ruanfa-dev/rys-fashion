using System.Text.Json.Serialization;

using SharedKernel.Models.PagedLists;

namespace SharedKernel.Models;

/// <summary>
/// Standard API response wrapper that provides consistent structure for all API responses.
/// Follows industry standards and best practices for REST API responses.
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
/// <remarks>
/// This response model provides:
/// - Consistent response structure across all endpoints
/// - Metadata for API versioning, timestamps, and request tracking
/// - Support for pagination information
/// - Links for HATEOAS compliance
/// - Clear success/error indication
/// </remarks>
/// <example>
/// Success response:
/// <code>
/// {
///   "success": true,
///   "data": { "id": 1, "name": "John Doe" },
///   "message": "User retrieved successfully",
///   "timestamp": "2025-08-13T10:30:00Z",
///   "apiVersion": "1.0",
///   "requestId": "abc123"
/// }
/// </code>
/// 
/// Error response:
/// <code>
/// {
///   "success": false,
///   "data": null,
///   "message": "Validation failed",
///   "errors": ["Name is required", "Email is invalid"],
///   "timestamp": "2025-08-13T10:30:00Z",
///   "apiVersion": "1.0",
///   "requestId": "abc123"
/// }
/// </code>
/// </example>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the request was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// The main data payload of the response
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Human-readable message describing the result
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// List of error messages (populated when Success is false)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<string>? Errors { get; set; }

    /// <summary>
    /// Timestamp when the response was generated (ISO 8601 format)
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// API version for this response
    /// </summary>
    public string ApiVersion { get; set; } = "1.0";

    /// <summary>
    /// Unique identifier for request tracing and debugging
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RequestId { get; set; }

    /// <summary>
    /// Pagination information (only included for paginated responses)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaginationMetadata? Pagination { get; set; }

    /// <summary>
    /// HATEOAS links for related resources and actions
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Links { get; set; }

    /// <summary>
    /// Additional metadata that may be relevant for the response
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; set; }

    #region Factory Methods

    /// <summary>
    /// Creates a successful response with data
    /// </summary>
    /// <param name="data">The data to include in the response</param>
    /// <param name="message">Optional success message</param>
    /// <param name="requestId">Optional request identifier</param>
    /// <returns>A successful API response</returns>
    public static ApiResponse<T> Success(T data, string? message = null, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message,
            RequestId = requestId
        };
    }

    /// <summary>
    /// Creates a successful response without data (for operations like delete)
    /// </summary>
    /// <param name="message">Success message</param>
    /// <param name="requestId">Optional request identifier</param>
    /// <returns>A successful API response without data</returns>
    public static ApiResponse<T> SuccessWithoutData(string message, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Message = message,
            RequestId = requestId
        };
    }

    /// <summary>
    /// Creates an error response with multiple error messages
    /// </summary>
    /// <param name="errors">List of error messages</param>
    /// <param name="message">General error message</param>
    /// <param name="requestId">Optional request identifier</param>
    /// <returns>An error API response</returns>
    public static ApiResponse<T> Error(IEnumerable<string> errors, string? message = null, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = errors,
            Message = message ?? "An error occurred",
            RequestId = requestId
        };
    }

    /// <summary>
    /// Creates an error response with a single error message
    /// </summary>
    /// <param name="error">Single error message</param>
    /// <param name="message">General error message</param>
    /// <param name="requestId">Optional request identifier</param>
    /// <returns>An error API response</returns>
    public static ApiResponse<T> Error(string error, string? message = null, string? requestId = null)
    {
        return Error([error], message, requestId);
    }

    /// <summary>
    /// Creates a paginated response with data and pagination metadata
    /// </summary>
    /// <param name="data">The paginated data</param>
    /// <param name="pagination">Pagination metadata</param>
    /// <param name="message">Optional success message</param>
    /// <param name="requestId">Optional request identifier</param>
    /// <returns>A successful paginated API response</returns>
    public static ApiResponse<T> Paginated(T data, PaginationMetadata pagination, string? message = null, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            Pagination = pagination,
            Message = message,
            RequestId = requestId
        };
    }

    #endregion

    #region Fluent Methods

    /// <summary>
    /// Adds HATEOAS links to the response
    /// </summary>
    /// <param name="linkName">The name/relation of the link</param>
    /// <param name="url">The URL for the link</param>
    /// <returns>The current response instance for method chaining</returns>
    public ApiResponse<T> WithLink(string linkName, string url)
    {
        Links ??= new Dictionary<string, string>();
        Links[linkName] = url;
        return this;
    }

    /// <summary>
    /// Adds multiple HATEOAS links to the response
    /// </summary>
    /// <param name="links">Dictionary of link names and URLs</param>
    /// <returns>The current response instance for method chaining</returns>
    public ApiResponse<T> WithLinks(Dictionary<string, string> links)
    {
        Links ??= new Dictionary<string, string>();
        foreach (var link in links)
        {
            Links[link.Key] = link.Value;
        }
        return this;
    }

    /// <summary>
    /// Adds metadata to the response
    /// </summary>
    /// <param name="key">Metadata key</param>
    /// <param name="value">Metadata value</param>
    /// <returns>The current response instance for method chaining</returns>
    public ApiResponse<T> WithMetadata(string key, object value)
    {
        Metadata ??= new Dictionary<string, object>();
        Metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Sets the API version for the response
    /// </summary>
    /// <param name="version">API version string</param>
    /// <returns>The current response instance for method chaining</returns>
    public ApiResponse<T> WithVersion(string version)
    {
        ApiVersion = version;
        return this;
    }

    #endregion
}

/// <summary>
/// Non-generic version of ApiResponse for responses without data
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// Creates an error response with multiple error messages
    /// </summary>
    /// <param name="errors">List of error messages</param>
    /// <param name="message">General error message</param>
    /// <param name="requestId">Optional request identifier</param>
    /// <returns>An error API response</returns>
    public static new ApiResponse Error(IEnumerable<string> errors, string? message = null, string? requestId = null)
    {
        return new ApiResponse
        {
            IsSuccess = false,
            Errors = errors,
            Message = message ?? "An error occurred",
            RequestId = requestId
        };
    }

    /// <summary>
    /// Creates an error response with a single error message
    /// </summary>
    /// <param name="error">Single error message</param>
    /// <param name="message">General error message</param>
    /// <param name="requestId">Optional request identifier</param>
    /// <returns>An error API response</returns>
    public static new ApiResponse Error(string error, string? message = null, string? requestId = null)
    {
        return Error([error], message, requestId);
    }
}

/// <summary>
/// Pagination metadata for paginated responses
/// </summary>
public class PaginationMetadata
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
    /// Index of the first item on the current page (0-based)
    /// </summary>
    public int FirstItemIndex { get; set; }

    /// <summary>
    /// Index of the last item on the current page (0-based)
    /// </summary>
    public int LastItemIndex { get; set; }

    /// <summary>
    /// Creates pagination metadata from IPagedList
    /// </summary>
    /// <typeparam name="T">Type of items in the paged list</typeparam>
    /// <param name="pagedList">The paged list</param>
    /// <returns>Pagination metadata</returns>
    public static PaginationMetadata FromPagedList<T>(PagedList<T> pagedList)
    {
        return new PaginationMetadata
        {
            CurrentPage = pagedList.PageIndex + 1, // Convert to 1-based
            PageSize = pagedList.PageSize,
            TotalItems = pagedList.TotalCount,
            TotalPages = pagedList.TotalPages,
            HasPrevious = pagedList.HasPreviousPage,
            HasNext = pagedList.HasNextPage,
            FirstItemIndex = pagedList.PageIndex * pagedList.PageSize,
            LastItemIndex = Math.Min((pagedList.PageIndex + 1) * pagedList.PageSize - 1, pagedList.TotalCount - 1)
        };
    }
}
