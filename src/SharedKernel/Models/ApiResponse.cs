using System.Text.Json.Serialization;
using SharedKernel.Models.PagedLists;

namespace SharedKernel.Models;

/// <summary>
/// Standard API response wrapper that provides consistent structure for all API responses.
/// Follows industry standards and best practices for REST API responses with RFC 7807 Problem Details compatibility.
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
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
    /// RFC 7807 Problem Details: A URI reference that identifies the problem type
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; set; }

    /// <summary>
    /// RFC 7807 Problem Details: A short, human-readable summary of the problem type
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }

    /// <summary>
    /// RFC 7807 Problem Details: The HTTP status code for this occurrence of the problem
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Status { get; set; }

    /// <summary>
    /// RFC 7807 Problem Details: A human-readable explanation specific to this occurrence of the problem
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; set; }

    /// <summary>
    /// Structured error messages grouped by error code (RFC 7807 compatible)
    /// Key: Full error code (e.g., "Role.AlreadyExists", "User.NotFound")
    /// Value: Array of error messages for that error code
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string[]>? Errors { get; set; }

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

    #region Factory Methods - Core Operations

    /// <summary>
    /// Creates a successful response with data
    /// </summary>
    public static ApiResponse<T> Success(T data, string? message = null, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message,
            RequestId = requestId,
            Status = 200
        };
    }

    /// <summary>
    /// Creates a successful response for creation operations
    /// </summary>
    public static ApiResponse<T> Created(T data, string? message = null, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message ?? "Resource created successfully",
            RequestId = requestId,
            Status = 201
        };
    }

    /// <summary>
    /// Creates a successful response without data (for operations like delete)
    /// </summary>
    public static ApiResponse<T> SuccessWithoutData(string? message = null, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Message = message ?? "Operation completed successfully",
            RequestId = requestId,
            Status = 204
        };
    }

    /// <summary>
    /// Creates an error response with structured error messages following RFC 7807
    /// </summary>
    public static ApiResponse<T> Error(Dictionary<string, string[]> errors, string? message = null, string? requestId = null, int statusCode = 400)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = errors,
            Message = message ?? "An error occurred",
            Type = GetProblemTypeUri(statusCode),
            Title = GetProblemTitle(statusCode),
            Status = statusCode,
            Detail = message ?? "An error occurred",
            RequestId = requestId
        };
    }

    /// <summary>
    /// Creates an error response with a single error message
    /// </summary>
    public static ApiResponse<T> Error(string error, string? message = null, string? requestId = null, int statusCode = 400)
    {
        return Error(new Dictionary<string, string[]> { ["General"] = [error] }, message, requestId, statusCode);
    }

    /// <summary>
    /// Creates an error response with multiple errors under a single category
    /// </summary>
    public static ApiResponse<T> Error(string category, string[] errors, string? message = null, string? requestId = null, int statusCode = 400)
    {
        return Error(new Dictionary<string, string[]> { [category] = errors }, message, requestId, statusCode);
    }

    /// <summary>
    /// Creates a not found error response following RFC 7807
    /// </summary>
    public static ApiResponse<T> NotFound(string? message = null, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Message = message ?? "Resource not found",
            Type = "https://httpstatuses.com/404",
            Title = "Not Found",
            Status = 404,
            Detail = message ?? "The requested resource was not found",
            RequestId = requestId
        };
    }

    /// <summary>
    /// Creates a not found error response with specific error code following RFC 7807
    /// </summary>
    public static ApiResponse<T> NotFound(string errorCode, string message, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Message = message,
            Type = "https://httpstatuses.com/404",
            Title = errorCode,
            Status = 404,
            Detail = message,
            RequestId = requestId
        };
    }

    /// <summary>
    /// Creates an unauthorized error response following RFC 7807
    /// </summary>
    public static ApiResponse<T> Unauthorized(string? message = null, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Message = message ?? "Unauthorized access",
            Type = "https://httpstatuses.com/401",
            Title = "Unauthorized",
            Status = 401,
            Detail = message ?? "Authentication is required to access this resource",
            RequestId = requestId
        };
    }

    /// <summary>
    /// Creates a validation error response with structured field errors following RFC 7807
    /// </summary>
    public static ApiResponse<T> ValidationError(Dictionary<string, string[]> validationErrors, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = validationErrors,
            Message = "Validation failed",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Validation Failed",
            Status = 422,
            Detail = "One or more validation errors occurred",
            RequestId = requestId
        };
    }

    /// <summary>
    /// Creates a validation error response from a list of error messages
    /// </summary>
    public static ApiResponse<T> ValidationError(IEnumerable<string> validationErrors, string? requestId = null)
    {
        var errors = new Dictionary<string, string[]> { ["Validation"] = validationErrors.ToArray() };
        return ValidationError(errors, requestId);
    }

    /// <summary>
    /// Creates a paginated response with data and pagination metadata
    /// </summary>
    public static ApiResponse<T> Paginated(T data, PaginationMetadata pagination, string? message = null, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            Pagination = pagination,
            Message = message,
            RequestId = requestId,
            Status = 200
        };
    }

    #endregion

    #region E-Commerce Specific Factory Methods

    /// <summary>
    /// Creates a response for successful product operations with inventory info
    /// </summary>
    public static ApiResponse<T> ProductSuccess(T data, bool inStock = true, int? stockCount = null, string? requestId = null)
    {
        var response = Success(data, null, requestId);
        
        if (stockCount.HasValue || !inStock)
        {
            response.WithMetadata("inStock", inStock);
            if (stockCount.HasValue)
                response.WithMetadata("stockCount", stockCount.Value);
        }
        
        return response;
    }

    /// <summary>
    /// Creates a response for cart operations with totals
    /// </summary>
    public static ApiResponse<T> CartSuccess(T data, decimal? subtotal = null, decimal? total = null, int? itemCount = null, string? requestId = null)
    {
        var response = Success(data, null, requestId);
        
        if (subtotal.HasValue)
            response.WithMetadata("subtotal", subtotal.Value);
        if (total.HasValue)
            response.WithMetadata("total", total.Value);
        if (itemCount.HasValue)
            response.WithMetadata("itemCount", itemCount.Value);
        
        return response;
    }

    /// <summary>
    /// Creates a response indicating out of stock following RFC 7807
    /// </summary>
    public static ApiResponse<T> OutOfStock(string? message = null, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Message = message ?? "Product is out of stock",
            Type = "https://httpstatuses.com/409",
            Title = "Product.OutOfStock",
            Status = 409,
            Detail = message ?? "The requested product is currently out of stock",
            RequestId = requestId
        }.WithMetadata("errorType", "STOCK_UNAVAILABLE");
    }

    /// <summary>
    /// Creates a response for payment errors following RFC 7807
    /// </summary>
    public static ApiResponse<T> PaymentError(string error, string? requestId = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = new Dictionary<string, string[]> { ["Payment"] = [error] },
            Message = "Payment processing failed",
            Type = "https://httpstatuses.com/402",
            Title = "Payment.Failed",
            Status = 402,
            Detail = error,
            RequestId = requestId
        }.WithMetadata("errorType", "PAYMENT_FAILED");
    }

    #endregion

    #region Fluent Methods

    /// <summary>
    /// Adds HATEOAS links to the response
    /// </summary>
    public ApiResponse<T> WithLink(string linkName, string url)
    {
        Links ??= new Dictionary<string, string>();
        Links[linkName] = url;
        return this;
    }

    /// <summary>
    /// Adds multiple HATEOAS links to the response
    /// </summary>
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
    public ApiResponse<T> WithMetadata(string key, object value)
    {
        Metadata ??= new Dictionary<string, object>();
        Metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Adds structured errors to the response
    /// </summary>
    public ApiResponse<T> WithError(string category, string[] errors)
    {
        Errors ??= new Dictionary<string, string[]>();
        Errors[category] = errors;
        return this;
    }

    /// <summary>
    /// Adds a single error to a category
    /// </summary>
    public ApiResponse<T> WithError(string category, string error)
    {
        return WithError(category, [error]);
    }

    /// <summary>
    /// Sets the API version for the response
    /// </summary>
    public ApiResponse<T> WithVersion(string version)
    {
        ApiVersion = version;
        return this;
    }

    /// <summary>
    /// Sets the status code for the response
    /// </summary>
    public ApiResponse<T> WithStatusCode(int statusCode)
    {
        Status = statusCode;
        return this;
    }

    /// <summary>
    /// Sets RFC 7807 Problem Details fields
    /// </summary>
    public ApiResponse<T> WithProblemDetails(string type, string title, string? detail = null)
    {
        Type = type;
        Title = title;
        Detail = detail ?? Message;
        return this;
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Gets the Problem Details type URI for a given status code
    /// </summary>
    private static string GetProblemTypeUri(int statusCode) => statusCode switch
    {
        400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        401 => "https://httpstatuses.com/401",
        403 => "https://httpstatuses.com/403",
        404 => "https://httpstatuses.com/404",
        409 => "https://httpstatuses.com/409",
        422 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        500 => "https://httpstatuses.com/500",
        _ => $"https://httpstatuses.com/{statusCode}"
    };

    /// <summary>
    /// Gets the Problem Details title for a given status code
    /// </summary>
    private static string GetProblemTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        422 => "Validation Failed",
        500 => "Internal Server Error",
        _ => "Error"
    };

    #endregion
}

/// <summary>
/// Non-generic version of ApiResponse for responses without data
/// </summary>
public sealed class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// Creates a successful response without data
    /// </summary>
    public static ApiResponse Success(string? message = null, string? requestId = null)
        => new ApiResponse
        {
            IsSuccess = true,
            Message = message ?? "Operation completed successfully",
            RequestId = requestId,
            Status = 200
        };

    /// <summary>
    /// Creates an error response with structured error messages
    /// </summary>
    public static new ApiResponse Error(Dictionary<string, string[]> errors, string? message = null, string? requestId = null, int statusCode = 400)
        => new ApiResponse
        {
            IsSuccess = false,
            Errors = errors,
            Message = message ?? "An error occurred",
            Type = GetProblemTypeUri(statusCode),
            Title = GetProblemTitle(statusCode),
            Status = statusCode,
            Detail = message ?? "An error occurred",
            RequestId = requestId
        };

    /// <summary>
    /// Creates an error response with a single error message
    /// </summary>
    public static new ApiResponse Error(string error, string? message = null, string? requestId = null, int statusCode = 400)
        => Error(new Dictionary<string, string[]> { ["General"] = [error] }, message, requestId, statusCode);

    /// <summary>
    /// Creates a not found error response
    /// </summary>
    public static new ApiResponse NotFound(string? message = null, string? requestId = null)
        => new ApiResponse
        {
            IsSuccess = false,
            Message = message ?? "Resource not found",
            Type = "https://httpstatuses.com/404",
            Title = "Not Found",
            Status = 404,
            Detail = message ?? "The requested resource was not found",
            RequestId = requestId
        };

    /// <summary>
    /// Creates an unauthorized error response
    /// </summary>
    public static new ApiResponse Unauthorized(string? message = null, string? requestId = null)
        => new ApiResponse
        {
            IsSuccess = false,
            Message = message ?? "Unauthorized access",
            Type = "https://httpstatuses.com/401",
            Title = "Unauthorized",
            Status = 401,
            Detail = message ?? "Authentication is required to access this resource",
            RequestId = requestId
        };

    #region Private Helper Methods

    /// <summary>
    /// Gets the Problem Details type URI for a given status code
    /// </summary>
    private static string GetProblemTypeUri(int statusCode) => statusCode switch
    {
        400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        401 => "https://httpstatuses.com/401",
        403 => "https://httpstatuses.com/403",
        404 => "https://httpstatuses.com/404",
        409 => "https://httpstatuses.com/409",
        422 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        500 => "https://httpstatuses.com/500",
        _ => $"https://httpstatuses.com/{statusCode}"
    };

    /// <summary>
    /// Gets the Problem Details title for a given status code
    /// </summary>
    private static string GetProblemTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        422 => "Validation Failed",
        500 => "Internal Server Error",
        _ => "Error"
    };

    #endregion
}
