using System.Collections.Frozen;

using ErrorOr;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using SharedKernel.Models;
using SharedKernel.Models.PagedLists;

namespace UseCases.Common.Extensions;

/// <summary>
/// Extension methods for converting ErrorOr results to standardized ApiResponse format.
/// Provides seamless integration between ErrorOr library and the ApiResponse wrapper with proper HTTP status codes.
/// </summary>
/// <remarks>
/// This class focuses specifically on converting ErrorOr results to the standardized ApiResponse format
/// while preserving HTTP status codes for proper client handling.
/// 
/// Key Features:
/// - Automatic error-to-HTTP status code mapping preserved in ApiResponse.StatusCode
/// - Consistent ApiResponse wrapper for all API responses
/// - Support for pagination with PaginationMetadata
/// - HATEOAS links support
/// - Additional metadata support
/// - E-commerce specific response types (Product, Cart, etc.)
/// - Performance optimized with frozen dictionaries
/// </remarks>
/// <example>
/// Basic usage in Controllers:
/// <code>
/// [HttpGet("{id:int}")]
/// public async Task&lt;ActionResult&lt;ApiResponse&lt;Product&gt;&gt;&gt; GetProduct(int id)
/// {
///     var result = await _productService.GetProductAsync(id);
///     var apiResponse = result.ToApiResponse("Product retrieved successfully");
///     return Ok(apiResponse); // Always returns 200 OK with ApiResponse wrapper
/// }
/// </code>
/// </example>
public static class ErrorOrApiResponseExtensions
{
    // Pre-computed frozen dictionary for better performance
    private static readonly FrozenDictionary<ErrorType, int> ErrorTypeToStatusCode =
        new Dictionary<ErrorType, int>
        {
            [ErrorType.Validation] = StatusCodes.Status400BadRequest,
            [ErrorType.Unauthorized] = StatusCodes.Status401Unauthorized,
            [ErrorType.Forbidden] = StatusCodes.Status403Forbidden,
            [ErrorType.NotFound] = StatusCodes.Status404NotFound,
            [ErrorType.Conflict] = StatusCodes.Status409Conflict,
            [ErrorType.Failure] = StatusCodes.Status500InternalServerError,
            [ErrorType.Unexpected] = StatusCodes.Status422UnprocessableEntity
        }.ToFrozenDictionary();

    private const int DefaultStatusCode = StatusCodes.Status500InternalServerError;

    #region Core ApiResponse Extensions

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an ApiResponse&lt;T&gt; with preserved HTTP status codes.
    /// Returns a successful ApiResponse with data on success or error ApiResponse on failure with appropriate StatusCode.
    /// </summary>
    /// <typeparam name="T">The type of the success result</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="message">Optional success message</param>
    /// <param name="requestId">Optional request identifier for tracing</param>
    /// <returns>ApiResponse&lt;T&gt; representing either success or error with proper StatusCode</returns>
    /// <example>
    /// <code>
    /// public async Task&lt;ActionResult&lt;ApiResponse&lt;Product&gt;&gt;&gt; GetProduct(int id)
    /// {
    ///     var result = await _productService.GetProductAsync(id);
    ///     var apiResponse = result.ToApiResponse("Product retrieved successfully");
    ///     return Ok(apiResponse); // StatusCode preserved in apiResponse.StatusCode
    /// }
    /// </code>
    /// </example>
    public static ApiResponse<T> ToApiResponse<T>(this ErrorOr<T> result, string? message = null, string? requestId = null)
        => result.Match(
            value => ApiResponse<T>.Success(value, message, requestId),
            errors => ConvertErrorsToApiResponse<T>(errors, requestId));

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an ApiResponse&lt;T&gt; with Created status (201).
    /// Returns a successful Created ApiResponse with data on success or error ApiResponse on failure with appropriate StatusCode.
    /// </summary>
    /// <typeparam name="T">The type of the created resource</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="message">Optional creation message</param>
    /// <param name="requestId">Optional request identifier for tracing</param>
    /// <returns>ApiResponse&lt;T&gt; with Created status (201) or error details with appropriate StatusCode</returns>
    /// <example>
    /// <code>
    /// public async Task&lt;ActionResult&lt;ApiResponse&lt;Product&gt;&gt;&gt; CreateProduct(CreateProductRequest request)
    /// {
    ///     var result = await _productService.CreateProductAsync(request);
    ///     var apiResponse = result.ToApiResponseCreated("Product created successfully");
    ///     return Ok(apiResponse); // apiResponse.StatusCode will be 201 on success
    /// }
    /// </code>
    /// </example>
    public static ApiResponse<T> ToApiResponseCreated<T>(this ErrorOr<T> result, string? message = null, string? requestId = null)
        => result.Match(
            value => ApiResponse<T>.Created(value, message, requestId),
            errors => ConvertErrorsToApiResponse<T>(errors, requestId));

    /// <summary>
    /// Converts an ErrorOr&lt;Updated&gt; result to a non-generic ApiResponse with appropriate StatusCode.
    /// Returns a successful no-data ApiResponse on success or error ApiResponse on failure with preserved StatusCode.
    /// </summary>
    /// <param name="result">The ErrorOr&lt;Updated&gt; result to convert</param>
    /// <param name="message">Optional success message</param>
    /// <param name="requestId">Optional request identifier for tracing</param>
    /// <returns>ApiResponse representing either success (200) or error with appropriate StatusCode</returns>
    /// <example>
    /// <code>
    /// public async Task&lt;ActionResult&lt;ApiResponse&gt;&gt; UpdateProduct(int id, UpdateProductRequest request)
    /// {
    ///     var result = await _productService.UpdateProductAsync(id, request);
    ///     var apiResponse = result.ToApiResponseUpdated("Product updated successfully");
    ///     return Ok(apiResponse); // apiResponse.StatusCode will be 200 on success
    /// }
    /// </code>
    /// </example>
    public static ApiResponse ToApiResponseUpdated(this ErrorOr<Updated> result, string? message = null, string? requestId = null)
        => result.Match(
            _ => ApiResponse.Success(message ?? "Resource updated successfully", requestId),
            errors => ConvertErrorsToApiResponse(errors, requestId));

    /// <summary>
    /// Converts an ErrorOr&lt;Deleted&gt; result to a non-generic ApiResponse with appropriate StatusCode.
    /// Returns a successful no-data ApiResponse on success or error ApiResponse on failure with preserved StatusCode.
    /// </summary>
    /// <param name="result">The ErrorOr&lt;Deleted&gt; result to convert</param>
    /// <param name="message">Optional success message</param>
    /// <param name="requestId">Optional request identifier for tracing</param>
    /// <returns>ApiResponse representing either success (200) or error with appropriate StatusCode</returns>
    /// <example>
    /// <code>
    /// public async Task&lt;ActionResult&lt;ApiResponse&gt;&gt; DeleteProduct(int id)
    /// {
    ///     var result = await _productService.DeleteProductAsync(id);
    ///     var apiResponse = result.ToApiResponseDeleted("Product deleted successfully");
    ///     return Ok(apiResponse); // apiResponse.StatusCode will be 200 on success
    /// }
    /// </code>
    /// </example>
    public static ApiResponse ToApiResponseDeleted(this ErrorOr<Deleted> result, string? message = null, string? requestId = null)
        => result.Match(
            _ => ApiResponse.Success(message ?? "Resource deleted successfully", requestId),
            errors => ConvertErrorsToApiResponse(errors, requestId));

    #endregion

    #region Advanced ApiResponse Extensions

    /// <summary>
    /// Converts an ErrorOr&lt;PagedList&lt;T&gt;&gt; result to an ApiResponse&lt;List&lt;T&gt;&gt; with pagination metadata and appropriate StatusCode.
    /// Returns a successful paginated ApiResponse on success or error ApiResponse on failure with preserved StatusCode.
    /// </summary>
    /// <typeparam name="T">The type of items in the paged list</typeparam>
    /// <param name="result">The ErrorOr&lt;PagedList&lt;T&gt;&gt; result to convert</param>
    /// <param name="message">Optional success message</param>
    /// <param name="requestId">Optional request identifier for tracing</param>
    /// <returns>ApiResponse&lt;List&lt;T&gt;&gt; with pagination metadata or error details with appropriate StatusCode</returns>
    /// <example>
    /// <code>
    /// public async Task&lt;ActionResult&lt;ApiResponse&lt;List&lt;Product&gt;&gt;&gt;&gt; GetProducts(int page, int pageSize)
    /// {
    ///     var result = await _productService.GetProductsPagedAsync(page, pageSize);
    ///     var apiResponse = result.ToApiResponsePaged("Products retrieved successfully");
    ///     return Ok(apiResponse); // Includes pagination metadata
    /// }
    /// </code>
    /// </example>
    public static ApiResponse<List<T>> ToApiResponsePaged<T>(this ErrorOr<PagedList<T>> result, string? message = null, string? requestId = null)
        => result.Match(
            pagedList => ApiResponse<List<T>>.Paginated(
                pagedList.Items.ToList(),
                PaginationMetadata.FromPagedList(pagedList),
                message,
                requestId),
            errors => ConvertErrorsToApiResponse<List<T>>(errors, requestId));

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an ApiResponse&lt;T&gt; with additional HATEOAS links and appropriate StatusCode.
    /// Returns a successful ApiResponse with links on success or error ApiResponse on failure with preserved StatusCode.
    /// </summary>
    /// <typeparam name="T">The type of the success result</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="links">Dictionary of HATEOAS links to include</param>
    /// <param name="message">Optional success message</param>
    /// <param name="requestId">Optional request identifier for tracing</param>
    /// <returns>ApiResponse&lt;T&gt; with HATEOAS links or error details with appropriate StatusCode</returns>
    /// <example>
    /// <code>
    /// public async Task&lt;ActionResult&lt;ApiResponse&lt;Product&gt;&gt;&gt; GetProductWithLinks(int id)
    /// {
    ///     var result = await _productService.GetProductAsync(id);
    ///     var links = new Dictionary&lt;string, string&gt;
    ///     {
    ///         ["self"] = $"/api/products/{id}",
    ///         ["edit"] = $"/api/products/{id}",
    ///         ["delete"] = $"/api/products/{id}"
    ///     };
    ///     var apiResponse = result.ToApiResponseWithLinks(links, "Product retrieved with links");
    ///     return Ok(apiResponse);
    /// }
    /// </code>
    /// </example>
    public static ApiResponse<T> ToApiResponseWithLinks<T>(this ErrorOr<T> result, Dictionary<string, string> links, string? message = null, string? requestId = null)
        => result.Match(
            value => ApiResponse<T>.Success(value, message, requestId).WithLinks(links),
            errors => ConvertErrorsToApiResponse<T>(errors, requestId));

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an ApiResponse&lt;T&gt; with additional metadata and appropriate StatusCode.
    /// Returns a successful ApiResponse with metadata on success or error ApiResponse on failure with preserved StatusCode.
    /// </summary>
    /// <typeparam name="T">The type of the success result</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="metadata">Dictionary of metadata to include</param>
    /// <param name="message">Optional success message</param>
    /// <param name="requestId">Optional request identifier for tracing</param>
    /// <returns>ApiResponse&lt;T&gt; with metadata or error details with appropriate StatusCode</returns>
    /// <example>
    /// <code>
    /// public async Task&lt;ActionResult&lt;ApiResponse&lt;Product&gt;&gt;&gt; GetProductWithMetadata(int id)
    /// {
    ///     var result = await _productService.GetProductAsync(id);
    ///     var metadata = new Dictionary&lt;string, object&gt;
    ///     {
    ///         ["cached"] = true,
    ///         ["cacheExpiry"] = DateTime.UtcNow.AddMinutes(15),
    ///         ["version"] = "1.2.0"
    ///     };
    ///     var apiResponse = result.ToApiResponseWithMetadata(metadata, "Product retrieved with metadata");
    ///     return Ok(apiResponse);
    /// }
    /// </code>
    /// </example>
    public static ApiResponse<T> ToApiResponseWithMetadata<T>(this ErrorOr<T> result, Dictionary<string, object> metadata, string? message = null, string? requestId = null)
        => result.Match(
            value =>
            {
                ApiResponse<T> response = ApiResponse<T>.Success(value, message, requestId);
                foreach ((string key, object val) in metadata)
                {
                    response.WithMetadata(key, val);
                }
                return response;
            },
            errors => ConvertErrorsToApiResponse<T>(errors, requestId));

    #endregion

    #region E-Commerce Specific Extensions

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an ApiResponse&lt;T&gt; with product-specific metadata and appropriate StatusCode.
    /// Includes stock information and inventory details with preserved StatusCode.
    /// </summary>
    /// <typeparam name="T">The type of the product result</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="inStock">Whether the product is in stock</param>
    /// <param name="stockCount">Current stock count</param>
    /// <param name="message">Optional success message</param>
    /// <param name="requestId">Optional request identifier for tracing</param>
    /// <returns>ApiResponse&lt;T&gt; with product metadata or error details with appropriate StatusCode</returns>
    /// <example>
    /// <code>
    /// public async Task&lt;ActionResult&lt;ApiResponse&lt;Product&gt;&gt;&gt; GetProductWithStock(int id)
    /// {
    ///     var result = await _productService.GetProductWithStockAsync(id);
    ///     var apiResponse = result.ToApiResponseProduct(
    ///         inStock: result.Value?.Stock > 0,
    ///         stockCount: result.Value?.Stock,
    ///         "Product retrieved with stock information");
    ///     return Ok(apiResponse);
    /// }
    /// </code>
    /// </example>
    public static ApiResponse<T> ToApiResponseProduct<T>(this ErrorOr<T> result, bool inStock = true, int? stockCount = null, string? message = null, string? requestId = null)
        => result.Match(
            value =>
            {
                ApiResponse<T> response = ApiResponse<T>.ProductSuccess(value, inStock, stockCount, requestId);
                if (!string.IsNullOrWhiteSpace(message))
                    response.Message = message;
                return response;
            },
            errors => ConvertErrorsToApiResponse<T>(errors, requestId));

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an ApiResponse&lt;T&gt; with cart-specific metadata and appropriate StatusCode.
    /// Includes cart totals and item count with preserved StatusCode.
    /// </summary>
    /// <typeparam name="T">The type of the cart result</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="subtotal">Cart subtotal</param>
    /// <param name="total">Cart total including taxes and fees</param>
    /// <param name="itemCount">Number of items in cart</param>
    /// <param name="message">Optional success message</param>
    /// <param name="requestId">Optional request identifier for tracing</param>
    /// <returns>ApiResponse&lt;T&gt; with cart metadata or error details with appropriate StatusCode</returns>
    /// <example>
    /// <code>
    /// public async Task&lt;ActionResult&lt;ApiResponse&lt;Cart&gt;&gt;&gt; GetCart(int customerId)
    /// {
    ///     var result = await _cartService.GetCartAsync(customerId);
    ///     var apiResponse = result.ToApiResponseCart(
    ///         subtotal: result.Value?.Subtotal,
    ///         total: result.Value?.Total,
    ///         itemCount: result.Value?.Items.Count,
    ///         "Cart retrieved successfully");
    ///     return Ok(apiResponse);
    /// }
    /// </code>
    /// </example>
    public static ApiResponse<T> ToApiResponseCart<T>(this ErrorOr<T> result, decimal? subtotal = null, decimal? total = null, int? itemCount = null, string? message = null, string? requestId = null)
        => result.Match(
            value =>
            {
                ApiResponse<T> response = ApiResponse<T>.CartSuccess(value, subtotal, total, itemCount, requestId);
                if (!string.IsNullOrWhiteSpace(message))
                    response.Message = message;
                return response;
            },
            errors => ConvertErrorsToApiResponse<T>(errors, requestId));

    #endregion

    #region Minimal API Integration

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an IResult with ApiResponse wrapper for minimal APIs with appropriate StatusCode.
    /// Returns Ok(ApiResponse) on success or error ApiResponse wrapped in Ok with StatusCode preserved in the response.
    /// </summary>
    /// <typeparam name="T">The type of the success result</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="message">Optional success message</param>
    /// <param name="requestId">Optional request identifier for tracing</param>
    /// <returns>IResult with ApiResponse wrapper and preserved StatusCode</returns>
    /// <example>
    /// <code>
    /// app.MapGet("/products/{id}", async (int id, IProductService service) =>
    /// {
    ///     var result = await service.GetProductAsync(id);
    ///     return result.ToTypedApiResponse("Product retrieved successfully");
    ///     // Always returns 200 OK, but StatusCode is preserved in apiResponse.StatusCode
    /// });
    /// </code>
    /// </example>
    public static IResult ToTypedApiResponse<T>(this ErrorOr<T> result, string? message = null, string? requestId = null)
    {
        ApiResponse<T> apiResponse = result.ToApiResponse(message, requestId);
        return TypedResults.Ok(apiResponse);
    }

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an IResult with ApiResponse wrapper for minimal APIs with Created response.
    /// Returns Ok(ApiResponse) with Created status preserved in ApiResponse.StatusCode.
    /// </summary>
    /// <typeparam name="T">The type of the created resource</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="message">Optional creation message</param>
    /// <param name="requestId">Optional request identifier for tracing</param>
    /// <returns>IResult with ApiResponse wrapper and Created status</returns>
    /// <example>
    /// <code>
    /// app.MapPost("/products", async (CreateProductRequest request, IProductService service) =>
    /// {
    ///     var result = await service.CreateProductAsync(request);
    ///     return result.ToTypedApiResponseCreated("Product created successfully");
    ///     // Returns 200 OK, but apiResponse.StatusCode will be 201
    /// });
    /// </code>
    /// </example>
    public static IResult ToTypedApiResponseCreated<T>(this ErrorOr<T> result, string? message = null, string? requestId = null)
    {
        ApiResponse<T> apiResponse = result.ToApiResponseCreated(message, requestId);
        return TypedResults.Ok(apiResponse);
    }

    #endregion

    #region Private Helper Methods

    private static ApiResponse<T> ConvertErrorsToApiResponse<T>(IReadOnlyList<Error> errors, string? requestId = null)
    {
        if (errors.Count == 0)
            return ApiResponse<T>.Error(new Dictionary<string, string[]> { ["General"] = ["An unknown error occurred"] }, "An unknown error occurred", requestId);

        Error firstError = errors[0];
        int statusCode = GetStatusCode(firstError.Type);

        // Group errors by full error code (not just category)
        Dictionary<string, string[]> errorGroups = errors
            .GroupBy(e => GetErrorCode(e))
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Description).ToArray() // Use only description, not formatted message
            );

        return firstError.Type switch
        {
            ErrorType.Validation => CreateValidationApiResponse<T>(errorGroups, requestId),
            ErrorType.NotFound => CreateNotFoundApiResponse<T>(firstError, requestId),
            ErrorType.Unauthorized => CreateUnauthorizedApiResponse<T>(firstError, requestId),
            ErrorType.Conflict => CreateConflictApiResponse<T>(firstError, errorGroups, requestId),
            ErrorType.Forbidden => CreateForbiddenApiResponse<T>(firstError, errorGroups, requestId),
            ErrorType.Failure => CreateFailureApiResponse<T>(firstError, errorGroups, requestId),
            ErrorType.Unexpected => CreateUnexpectedApiResponse<T>(firstError, errorGroups, requestId),
            _ => CreateGenericErrorApiResponse<T>(firstError, errorGroups, requestId)
        };
    }

    private static ApiResponse ConvertErrorsToApiResponse(IReadOnlyList<Error> errors, string? requestId = null)
    {
        if (errors.Count == 0)
            return ApiResponse.Error(new Dictionary<string, string[]> { ["General"] = ["An unknown error occurred"] }, "An unknown error occurred", requestId);

        Error firstError = errors[0];

        // Group errors by full error code (not just category)
        Dictionary<string, string[]> errorGroups = errors
            .GroupBy(e => GetErrorCode(e))
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Description).ToArray() // Use only description, not formatted message
            );

        return firstError.Type switch
        {
            ErrorType.NotFound => CreateNotFoundApiResponse(firstError, requestId),
            ErrorType.Unauthorized => CreateUnauthorizedApiResponse(firstError, requestId),
            _ => CreateGenericErrorApiResponse(firstError, errorGroups, requestId)
        };
    }

    #region Specific Error Response Creators

    private static ApiResponse<T> CreateValidationApiResponse<T>(Dictionary<string, string[]> errorGroups, string? requestId)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = errorGroups,
            Message = "Validation failed",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Validation Failed",
            Status = 422,
            Detail = "One or more validation errors occurred",
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse<T> CreateNotFoundApiResponse<T>(Error error, string? requestId)
    {
        string title = GetErrorCode(error);
        string detail = error.Description;

        return new ApiResponse<T>
        {
            IsSuccess = false,
            Message = detail,
            Type = "https://httpstatuses.com/404",
            Title = title,
            Status = 404,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse CreateNotFoundApiResponse(Error error, string? requestId)
    {
        string title = GetErrorCode(error);
        string detail = error.Description;

        return new ApiResponse
        {
            IsSuccess = false,
            Message = detail,
            Type = "https://httpstatuses.com/404",
            Title = title,
            Status = 404,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse<T> CreateUnauthorizedApiResponse<T>(Error error, string? requestId)
    {
        string title = GetErrorCode(error);
        string detail = error.Description;

        return new ApiResponse<T>
        {
            IsSuccess = false,
            Message = detail,
            Type = "https://httpstatuses.com/401",
            Title = title,
            Status = 401,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse CreateUnauthorizedApiResponse(Error error, string? requestId)
    {
        string title = GetErrorCode(error);
        string detail = error.Description;

        return new ApiResponse
        {
            IsSuccess = false,
            Message = detail,
            Type = "https://httpstatuses.com/401",
            Title = title,
            Status = 401,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse<T> CreateConflictApiResponse<T>(Error firstError, Dictionary<string, string[]> errorGroups, string? requestId)
    {
        string title = GetErrorCode(firstError);
        string detail = firstError.Description;

        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = errorGroups,
            Message = "A conflict occurred",
            Type = "https://httpstatuses.com/409",
            Title = title,
            Status = 409,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse<T> CreateForbiddenApiResponse<T>(Error firstError, Dictionary<string, string[]> errorGroups, string? requestId)
    {
        string title = GetErrorCode(firstError);
        string detail = firstError.Description;

        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = errorGroups,
            Message = "Access forbidden",
            Type = "https://httpstatuses.com/403",
            Title = title,
            Status = 403,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse<T> CreateFailureApiResponse<T>(Error firstError, Dictionary<string, string[]> errorGroups, string? requestId)
    {
        string title = GetErrorCode(firstError);
        string detail = firstError.Description;

        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = errorGroups,
            Message = "An internal error occurred",
            Type = "https://httpstatuses.com/500",
            Title = title,
            Status = 500,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse<T> CreateUnexpectedApiResponse<T>(Error firstError, Dictionary<string, string[]> errorGroups, string? requestId)
    {
        string title = GetErrorCode(firstError);
        string detail = firstError.Description;

        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = errorGroups,
            Message = "An unexpected error occurred",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.22",
            Title = title,
            Status = 422,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse<T> CreateGenericErrorApiResponse<T>(Error firstError, Dictionary<string, string[]> errorGroups, string? requestId)
    {
        int statusCode = GetStatusCode(firstError.Type);
        string title = GetErrorCode(firstError);
        string detail = firstError.Description;

        return new ApiResponse<T>
        {
            IsSuccess = false,
            Errors = errorGroups,
            Message = "An error occurred",
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static ApiResponse CreateGenericErrorApiResponse(Error firstError, Dictionary<string, string[]> errorGroups, string? requestId)
    {
        int statusCode = GetStatusCode(firstError.Type);
        string title = GetErrorCode(firstError);
        string detail = firstError.Description;

        return new ApiResponse
        {
            IsSuccess = false,
            Errors = errorGroups,
            Message = "An error occurred",
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    #endregion

    /// <summary>
    /// Gets the full error code to use as the key in errors dictionary.
    /// If no code is provided, falls back to error type.
    /// Examples: "Role.AlreadyExists" -> "Role.AlreadyExists", "" -> "Validation"
    /// </summary>
    private static string GetErrorCode(Error error)
    {
        if (string.IsNullOrWhiteSpace(error.Code))
            return error.Type.ToString();

        return error.Code;
    }

    private static int GetStatusCode(ErrorType type)
        => ErrorTypeToStatusCode.GetValueOrDefault(type, DefaultStatusCode);

    #endregion
}

#region Usage Examples

/// <summary>
/// Example service layer that returns ErrorOr results for ApiResponse conversion
/// </summary>
public sealed class ApiResponseWrappedExampleService
{
    public async Task<ErrorOr<Order>> GetOrderByIdAsync(int id)
    {
        if (id <= 0)
            return Error.Validation("Order.InvalidId", "Order ID must be greater than 0");

        Order? order = await FindOrderInDatabaseAsync(id);
        if (order == null)
            return Error.NotFound("Order.NotFound", $"Order with ID {id} was not found");

        return order;
    }

    public async Task<ErrorOr<Order>> CreateOrderAsync(CreateOrderRequest request)
    {
        List<Error> validationErrors = ValidateCreateOrderRequest(request);
        if (validationErrors.Any())
            return validationErrors;

        Customer? customer = await FindCustomerAsync(request.CustomerId);
        if (customer == null)
            return Error.NotFound("Customer.NotFound", "Customer not found");

        Order order = new Order(request.CustomerId, request.Items);
        await SaveOrderAsync(order);
        return order;
    }

    public async Task<ErrorOr<Updated>> UpdateOrderStatusAsync(int id, string status)
    {
        ErrorOr<Order> getOrderResult = await GetOrderByIdAsync(id);
        if (getOrderResult.IsError)
            return getOrderResult.Errors;

        Order order = getOrderResult.Value;
        order.UpdateStatus(status);
        await SaveOrderAsync(order);
        return Result.Updated;
    }

    public async Task<ErrorOr<Deleted>> CancelOrderAsync(int id)
    {
        ErrorOr<Order> getOrderResult = await GetOrderByIdAsync(id);
        if (getOrderResult.IsError)
            return getOrderResult.Errors;

        Order order = getOrderResult.Value;
        if (order.Status == "Shipped")
            return Error.Conflict("Order.CannotCancel", "Cannot cancel a shipped order");

        await DeleteOrderAsync(id);
        return Result.Deleted;
    }

    public async Task<ErrorOr<PagedList<Order>>> GetOrdersPagedAsync(int page, int pageSize)
    {
        if (page <= 0)
            return Error.Validation("Pagination.InvalidPage", "Page number must be greater than 0");

        if (pageSize <= 0 || pageSize > 100)
            return Error.Validation("Pagination.InvalidPageSize", "Page size must be between 1 and 100");

        PagedList<Order> orders = await GetOrdersFromDatabaseAsync(page, pageSize);
        return orders;
    }

    private List<Error> ValidateCreateOrderRequest(CreateOrderRequest request)
    {
        List<Error> errors = [];

        if (request.CustomerId <= 0)
            errors.Add(Error.Validation("Customer.InvalidId", "Customer ID is required"));

        if (request.Items == null || !request.Items.Any())
            errors.Add(Error.Validation("Order.ItemsRequired", "At least one order item is required"));

        return errors;
    }

    // Placeholder methods - implement with your actual data access
    private Task<Order?> FindOrderInDatabaseAsync(int id) => throw new NotImplementedException();
    private Task<Customer?> FindCustomerAsync(int customerId) => throw new NotImplementedException();
    private Task SaveOrderAsync(Order order) => throw new NotImplementedException();
    private Task DeleteOrderAsync(int id) => throw new NotImplementedException();
    private Task<PagedList<Order>> GetOrdersFromDatabaseAsync(int page, int pageSize) => throw new NotImplementedException();
}

/// <summary>
/// Comprehensive MVC Controller using ErrorOr ApiResponse extensions
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
internal sealed class OrdersApiResponseController(ApiResponseWrappedExampleService orderService) : ControllerBase
{
    /// <summary>
    /// Get order by ID - Returns ApiResponse wrapper with preserved status codes.
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <returns>ApiResponse containing order details</returns>
    /// <response code="200">Always returns 200 OK with ApiResponse wrapper</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<Order>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Order>>> GetOrder(int id)
    {
        ErrorOr<Order> result = await orderService.GetOrderByIdAsync(id);
        ApiResponse<Order> apiResponse = result.ToApiResponse("Order retrieved successfully");

        // Note: Always returns 200 OK, but the actual status is in apiResponse.Status
        // For a 404 error, apiResponse.Status will be 404, but HTTP response is 200
        return Ok(apiResponse);
    }

    /// <summary>
    /// Create a new order - Returns ApiResponse wrapper with Created status preserved
    /// </summary>
    /// <param name="request">Order creation request</param>
    /// <returns>ApiResponse containing created order</returns>
    /// <response code="200">Always returns 200 OK with ApiResponse wrapper</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Order>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Order>>> CreateOrder(CreateOrderRequest request)
    {
        ErrorOr<Order> result = await orderService.CreateOrderAsync(request);
        ApiResponse<Order> apiResponse = result.ToApiResponseCreated("Order created successfully");

        // Note: Returns 200 OK, but apiResponse.Status will be 201 on success
        return Ok(apiResponse);
    }

    /// <summary>
    /// Update order status - Returns ApiResponse wrapper without data
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <param name="status">New order status</param>
    /// <returns>ApiResponse without data</returns>
    /// <response code="200">Always returns 200 OK with ApiResponse wrapper</response>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> UpdateOrderStatus(int id, [FromBody] string status)
    {
        ErrorOr<Updated> result = await orderService.UpdateOrderStatusAsync(id, status);
        ApiResponse apiResponse = result.ToApiResponseUpdated("Order status updated successfully");

        return Ok(apiResponse);
    }

    /// <summary>
    /// Cancel an order - Returns ApiResponse wrapper without data
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <returns>ApiResponse without data</returns>
    /// <response code="200">Always returns 200 OK with ApiResponse wrapper</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> CancelOrder(int id)
    {
        ErrorOr<Deleted> result = await orderService.CancelOrderAsync(id);
        ApiResponse apiResponse = result.ToApiResponseDeleted("Order cancelled successfully");

        return Ok(apiResponse);
    }

    /// <summary>
    /// Get paginated orders with pagination metadata
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>ApiResponse with paginated orders and metadata</returns>
    /// <response code="200">Always returns 200 OK with ApiResponse wrapper</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<Order>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<Order>>>> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        ErrorOr<PagedList<Order>> result = await orderService.GetOrdersPagedAsync(page, pageSize);
        ApiResponse<List<Order>> apiResponse = result.ToApiResponsePaged("Orders retrieved successfully");

        return Ok(apiResponse);
    }

    /// <summary>
    /// Get order with HATEOAS links
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <returns>ApiResponse with order and navigation links</returns>
    /// <response code="200">Always returns 200 OK with ApiResponse wrapper</response>
    [HttpGet("{id:int}/with-links")]
    [ProducesResponseType(typeof(ApiResponse<Order>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Order>>> GetOrderWithLinks(int id)
    {
        ErrorOr<Order> result = await orderService.GetOrderByIdAsync(id);
        Dictionary<string, string> links = new Dictionary<string, string>
        {
            ["self"] = $"/api/orders/{id}",
            ["update-status"] = $"/api/orders/{id}/status",
            ["cancel"] = $"/api/orders/{id}",
            ["customer"] = $"/api/customers/{result.Value?.CustomerId}"
        };

        ApiResponse<Order> apiResponse = result.ToApiResponseWithLinks(links, "Order retrieved with navigation links");
        return Ok(apiResponse);
    }

    /// <summary>
    /// Get order with additional metadata
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <returns>ApiResponse with order and metadata</returns>
    /// <response code="200">Always returns 200 OK with ApiResponse wrapper</response>
    [HttpGet("{id:int}/with-metadata")]
    [ProducesResponseType(typeof(ApiResponse<Order>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<Order>>> GetOrderWithMetadata(int id)
    {
        ErrorOr<Order> result = await orderService.GetOrderByIdAsync(id);
        Dictionary<string, object> metadata = new Dictionary<string, object>
        {
            ["cached"] = true,
            ["cacheExpiry"] = DateTime.UtcNow.AddMinutes(15),
            ["source"] = "database",
            ["processingTimeMs"] = 125
        };

        ApiResponse<Order> apiResponse = result.ToApiResponseWithMetadata(metadata, "Order retrieved with metadata");
        return Ok(apiResponse);
    }
}

/// <summary>
/// Minimal API endpoints using ErrorOr ApiResponse conversions
/// </summary>
public static class ApiResponseMinimalApiExamples
{
    public static void MapOrderApiResponseEndpoints(this WebApplication app)
    {
        RouteGroupBuilder orders = app.MapGroup("/api/orders-wrapped")
            .WithTags("Orders with ApiResponse")
            .WithOpenApi();

        // GET /api/orders-wrapped/{id} - Returns ApiResponse wrapper
        orders.MapGet("/{id:int}", async (int id, ApiResponseWrappedExampleService orderService) =>
        {
            ErrorOr<Order> result = await orderService.GetOrderByIdAsync(id);
            return result.ToTypedApiResponse("Order retrieved successfully");
        })
        .WithName("GetOrderWrapped")
        .WithSummary("Get order with ApiResponse wrapper")
        .Produces<ApiResponse<Order>>(StatusCodes.Status200OK);

        // POST /api/orders-wrapped - Returns ApiResponse with Created status
        orders.MapPost("/", async (CreateOrderRequest request, ApiResponseWrappedExampleService orderService) =>
        {
            ErrorOr<Order> result = await orderService.CreateOrderAsync(request);
            return result.ToTypedApiResponseCreated("Order created successfully");
        })
        .WithName("CreateOrderWrapped")
        .WithSummary("Create order with ApiResponse wrapper")
        .Produces<ApiResponse<Order>>(StatusCodes.Status200OK);

        // PATCH /api/orders-wrapped/{id}/status - Returns ApiResponse without data
        orders.MapPatch("/{id:int}/status", async (int id, string status, ApiResponseWrappedExampleService orderService) =>
        {
            ErrorOr<Updated> result = await orderService.UpdateOrderStatusAsync(id, status);
            ApiResponse apiResponse = result.ToApiResponseUpdated("Order status updated successfully");
            return TypedResults.Ok(apiResponse);
        })
        .WithName("UpdateOrderStatusWrapped")
        .WithSummary("Update order status with ApiResponse wrapper")
        .Produces<ApiResponse>(StatusCodes.Status200OK);

        // DELETE /api/orders-wrapped/{id} - Returns ApiResponse without data
        orders.MapDelete("/{id:int}", async (int id, ApiResponseWrappedExampleService orderService) =>
        {
            ErrorOr<Deleted> result = await orderService.CancelOrderAsync(id);
            ApiResponse apiResponse = result.ToApiResponseDeleted("Order cancelled successfully");
            return TypedResults.Ok(apiResponse);
        })
        .WithName("CancelOrderWrapped")
        .WithSummary("Cancel order with ApiResponse wrapper")
        .Produces<ApiResponse>();

        // GET /api/orders-wrapped - Returns paginated ApiResponse
        orders.MapGet("/", async (int page, int pageSize, ApiResponseWrappedExampleService orderService) =>
        {
            ErrorOr<PagedList<Order>> result = await orderService.GetOrdersPagedAsync(page, pageSize);
            return result.ToTypedApiResponse("Orders retrieved successfully");
        })
        .WithName("GetOrdersPagedWrapped")
        .WithSummary("Get paginated orders with ApiResponse wrapper")
        .Produces<ApiResponse<List<Order>>>(StatusCodes.Status200OK);
    }
}

/// <summary>
/// Example DTOs for the order endpoints
/// </summary>
public record Order(int Id, int CustomerId, List<OrderItem> Items, string Status, decimal Total, DateTime CreatedAt)
{
    public Order(int customerId, List<OrderItem> items)
        : this(0, customerId, items, "Pending", items.Sum(i => i.Price * i.Quantity), DateTime.UtcNow) { }

    public Order UpdateStatus(string status) => this with { Status = status };
}

public record OrderItem(int ProductId, string ProductName, decimal Price, int Quantity);
public record CreateOrderRequest(int CustomerId, List<OrderItem> Items);

/// <summary>
/// Example of expected ApiResponse JSON outputs with RFC 7807 Problem Details and full error codes
/// </summary>
public static class ApiResponseJsonExamples
{
    /*
    Successful GET /api/orders-wrapped/1:
    HTTP 200 OK
    Content-Type: application/json
    {
        "isSuccess": true,
        "data": {
            "id": 1,
            "customerId": 123,
            "items": [
                { "productId": 1, "productName": "iPhone 15", "price": 999.99, "quantity": 1 }
            ],
            "status": "Pending",
            "total": 999.99,
            "createdAt": "2024-01-15T10:30:00Z"
        },
        "message": "Order retrieved successfully",
        "status": 200,
        "timestamp": "2024-01-15T10:30:00Z",
        "apiVersion": "1.0",
        "requestId": "abc123"
    }
    
    Not Found GET /api/orders-wrapped/999:
    HTTP 200 OK (Note: Always 200, but status shows the real status)
    Content-Type: application/json
    {
        "isSuccess": false,
        "data": null,
        "message": "Order with ID 999 was not found",
        "type": "https://httpstatuses.com/404",
        "title": "Order.NotFound",
        "status": 404,
        "detail": "Order with ID 999 was not found",
        "timestamp": "2024-01-15T10:30:00Z",
        "apiVersion": "1.0",
        "requestId": "def456"
    }
    
    Validation Error POST /api/orders-wrapped:
    HTTP 200 OK (Note: Always 200, but status shows the real status)
    Content-Type: application/json
    {
        "isSuccess": false,
        "data": null,
        "message": "Validation failed",
        "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        "title": "Validation Failed",
        "status": 422,
        "detail": "One or more validation errors occurred",
        "errors": {
            "Customer.InvalidId": ["Customer ID is required"],
            "Order.ItemsRequired": ["At least one order item is required"]
        },
        "timestamp": "2024-01-15T10:30:00Z",
        "apiVersion": "1.0",
        "requestId": "ghi789"
    }
    
    Successful Creation POST /api/orders-wrapped:
    HTTP 200 OK (Note: Always 200, but status shows Created)
    Content-Type: application/json
    {
        "isSuccess": true,
        "data": {
            "id": 2,
            "customerId": 123,
            "items": [
                { "productId": 1, "productName": "iPhone 15", "price": 999.99, "quantity": 1 }
            ],
            "status": "Pending",
            "total": 999.99,
            "createdAt": "2024-01-15T10:30:00Z"
        },
        "message": "Order created successfully",
        "status": 201,
        "timestamp": "2024-01-15T10:30:00Z",
        "apiVersion": "1.0",
        "requestId": "jkl012"
    }
    
    Conflict Error DELETE /api/orders-wrapped/1:
    HTTP 200 OK (Note: Always 200, but status shows Conflict)
    Content-Type: application/json
    {
        "isSuccess": false,
        "data": null,
        "message": "A conflict occurred",
        "type": "https://httpstatuses.com/409",
        "title": "Order.CannotCancel",
        "status": 409,
        "detail": "Cannot cancel a shipped order",
        "errors": {
            "Order.CannotCancel": ["Cannot cancel a shipped order"]
        },
        "timestamp": "2024-01-15T10:30:00Z",
        "apiVersion": "1.0",
        "requestId": "mno345"
    }
    
    Paginated Response GET /api/orders-wrapped?page_index=1&page_size=10:
    HTTP 200 OK
    Content-Type: application/json
    {
        "isSuccess": true,
        "data": [
            { "id": 1, "customerId": 123, "status": "Pending", "total": 999.99 },
            { "id": 2, "customerId": 124, "status": "Shipped", "total": 1499.99 }
        ],
        "message": "Orders retrieved successfully",
        "status": 200,
        "pagination": {
            "currentPage": 1,
            "pageSize": 10,
            "totalItems": 25,
            "totalPages": 3,
            "hasPrevious": false,
            "hasNext": true,
            "firstItemIndex": 1,
            "lastItemIndex": 10
        },
        "links": {
            "self": "/api/orders-wrapped?page_index=1&page_size=10",
            "next": "/api/orders-wrapped?page_index=2&page_size=10",
            "first": "/api/orders-wrapped?page_index=1&page_size=10",
            "last": "/api/orders-wrapped?page_index=3&page_size=10"
        },
        "timestamp": "2024-01-15T10:30:00Z",
        "apiVersion": "1.0",
        "requestId": "pqr678"
    }

    Enhanced Error Response with Multiple Categories:
    HTTP 200 OK
    Content-Type: application/json
    {
        "isSuccess": false,
        "data": null,
        "message": "Validation failed",
        "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        "title": "Validation Failed",
        "status": 422,
        "detail": "One or more validation errors occurred",
        "errors": {
            "Product.NameRequired": ["Product name is required"],
            "Product.PriceInvalid": ["Product price must be greater than 0"],
            "Customer.EmailInvalid": ["Email format is invalid"],
            "Address.ZipCodeRequired": ["ZIP code is required for shipping"]
        },
        "timestamp": "2024-01-15T10:30:00Z",
        "apiVersion": "1.0",
        "requestId": "stu901"
    }

    Response with HATEOAS Links and Metadata:
    HTTP 200 OK
    Content-Type: application/json
    {
        "isSuccess": true,
        "data": {
            "id": 1,
            "customerId": 123,
            "status": "Processing",
            "total": 999.99
        },
        "message": "Order retrieved with navigation links",
        "status": 200,
        "links": {
            "self": "/api/orders/1",
            "update-status": "/api/orders/1/status",
            "cancel": "/api/orders/1",
            "customer": "/api/customers/123"
        },
        "metadata": {
            "cached": true,
            "cacheExpiry": "2024-01-15T10:45:00Z",
            "source": "database",
            "processingTimeMs": 125
        },
        "timestamp": "2024-01-15T10:30:00Z",
        "apiVersion": "1.0",
        "requestId": "vwx234"
    }
    */
}

#endregion