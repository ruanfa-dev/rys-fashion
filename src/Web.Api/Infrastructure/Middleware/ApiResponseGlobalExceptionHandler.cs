using System.Collections.Frozen;

using ErrorOr;

using Microsoft.AspNetCore.Diagnostics;

using SharedKernel.Models;

namespace Web.Api.Infrastructure.Middleware;

/// <summary>
/// Global exception handler that converts unhandled exceptions to standardized ApiResponse wrapper format.
/// Integrates seamlessly with the ErrorOr extensions and ApiResponse pattern used throughout the application.
/// </summary>
/// <remarks>
/// This handler automatically maps common .NET exceptions to appropriate ErrorOr error types and wraps them
/// in the consistent ApiResponse format, following RFC 7807 Problem Details standards while maintaining
/// the ApiResponse wrapper pattern. It provides special handling for:
/// - FluentValidation exceptions (converted to structured validation errors)
/// - Common .NET framework exceptions (mapped to appropriate error types with full error codes)
/// - Unknown exceptions (mapped to unexpected errors with proper categorization)
/// 
/// The handler maintains consistency with the ErrorOr extensions by using the same error type mapping
/// and ApiResponse creation patterns, ensuring all responses follow the same structure.
/// 
/// Key Features:
/// - Always returns HTTP 200 OK with ApiResponse wrapper
/// - RFC 7807 Problem Details fields (type, title, status, detail)
/// - Full error codes in errors dictionary (e.g., "Argument.UserId" instead of just "Argument")
/// - Consistent error categorization and structured error information
/// - Request tracing and debugging support
/// </remarks>
/// <example>
/// Register the handler in Program.cs:
/// <code>
/// builder.Services.AddExceptionHandler&lt;ApiResponseGlobalExceptionHandler&gt;();
/// 
/// // In the middleware pipeline:
/// app.UseExceptionHandler();
/// </code>
/// 
/// The handler will automatically convert exceptions to ApiResponse format:
/// - ArgumentException → ApiResponse with status 400 and structured errors
/// - UnauthorizedAccessException → ApiResponse with status 401 
/// - KeyNotFoundException → ApiResponse with status 404
/// - TimeoutException → ApiResponse with status 500
/// </example>
internal sealed class ApiResponseGlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiResponseGlobalExceptionHandler> _logger;

    // Pre-computed frozen dictionary for better performance - matches ErrorOrApiResponseExtensions
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
    private const string ResponseContentType = "application/json";

    /// <summary>
    /// Initializes a new instance of the ApiResponseGlobalExceptionHandler.
    /// </summary>
    /// <param name="logger">Logger for recording unhandled exceptions</param>
    public ApiResponseGlobalExceptionHandler(ILogger<ApiResponseGlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles unhandled exceptions by converting them to ApiResponse wrapper format.
    /// </summary>
    /// <param name="httpContext">The HTTP context for the current request</param>
    /// <param name="exception">The unhandled exception to process</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True indicating the exception was handled</returns>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        LogException(exception);

        var errors = MapExceptionToErrors(exception);
        var apiResponse = CreateApiResponseFromErrors(errors, httpContext.Request.Path);

        await WriteApiResponseAsync(httpContext, apiResponse, cancellationToken);
        return true;
    }

    #region Private Helper Methods

    /// <summary>
    /// Logs the exception with appropriate detail level based on error type.
    /// </summary>
    /// <param name="exception">The exception to log</param>
    private void LogException(Exception exception)
    {
        // Log with different levels based on exception type
        var logLevel = GetLogLevelForException(exception);

        _logger.Log(logLevel, exception,
            "Unhandled exception occurred: {ExceptionType} - {ExceptionMessage}",
            exception.GetType().Name,
            exception.Message);
    }

    /// <summary>
    /// Determines appropriate log level based on exception type.
    /// </summary>
    /// <param name="exception">The exception to evaluate</param>
    /// <returns>Appropriate log level</returns>
    private static LogLevel GetLogLevelForException(Exception exception) => exception switch
    {
        UnauthorizedAccessException => LogLevel.Warning,
        ArgumentException => LogLevel.Warning,
        KeyNotFoundException => LogLevel.Information,
        FileNotFoundException => LogLevel.Information,
        InvalidOperationException => LogLevel.Warning,
        TimeoutException => LogLevel.Warning,
        _ when IsFluentValidationException(exception) => LogLevel.Information,
        _ => LogLevel.Error
    };

    /// <summary>
    /// Maps exceptions to ErrorOr errors with consistent error codes and descriptions.
    /// </summary>
    /// <param name="exception">The exception to map</param>
    /// <returns>List of ErrorOr errors representing the exception</returns>
    private static IReadOnlyList<Error> MapExceptionToErrors(Exception exception)
    {
        // Handle FluentValidation exceptions with reflection to avoid hard dependency
        if (IsFluentValidationException(exception))
        {
            var validationErrors = ExtractFluentValidationErrors(exception);
            if (validationErrors.Count > 0)
                return validationErrors;
        }

        // Map common .NET exceptions to appropriate ErrorOr error types
        var error = CreateErrorFromException(exception);
        return new[] { error };
    }

    /// <summary>
    /// Creates ApiResponse from ErrorOr errors using the same patterns as ErrorOrApiResponseExtensions.
    /// </summary>
    /// <param name="errors">The errors to convert</param>
    /// <param name="requestPath">The request path for tracing</param>
    /// <returns>ApiResponse object ready for HTTP response</returns>
    private static ApiResponse<object> CreateApiResponseFromErrors(IReadOnlyList<Error> errors, string requestPath)
    {
        if (errors.Count == 0)
            return CreateGenericErrorApiResponse(requestPath);

        var firstError = errors[0];
        var statusCode = GetStatusCode(firstError.Type);

        // Group errors by full error code (not just category)
        var errorGroups = errors
            .GroupBy(e => GetErrorCode(e))
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Description).ToArray()
            );

        return firstError.Type switch
        {
            ErrorType.Validation => CreateValidationApiResponse(errorGroups, requestPath),
            ErrorType.NotFound => CreateNotFoundApiResponse(firstError, requestPath),
            ErrorType.Unauthorized => CreateUnauthorizedApiResponse(firstError, requestPath),
            ErrorType.Conflict => CreateConflictApiResponse(firstError, errorGroups, requestPath),
            ErrorType.Forbidden => CreateForbiddenApiResponse(firstError, errorGroups, requestPath),
            ErrorType.Failure => CreateFailureApiResponse(firstError, errorGroups, requestPath),
            ErrorType.Unexpected => CreateUnexpectedApiResponse(firstError, errorGroups, requestPath),
            _ => CreateGenericErrorApiResponse(firstError, errorGroups, requestPath)
        };
    }

    /// <summary>
    /// Writes the ApiResponse to the HTTP context.
    /// </summary>
    /// <param name="httpContext">The HTTP context</param>
    /// <param name="apiResponse">The ApiResponse to write</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private static async Task WriteApiResponseAsync(
        HttpContext httpContext,
        ApiResponse<object> apiResponse,
        CancellationToken cancellationToken)
    {
        // Always return HTTP 200 OK for ApiResponse wrapper pattern
        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        httpContext.Response.ContentType = ResponseContentType;
        await httpContext.Response.WriteAsJsonAsync(apiResponse, cancellationToken);
    }

    #endregion

    #region FluentValidation Support

    /// <summary>
    /// Checks if an exception is a FluentValidation ValidationException using reflection.
    /// </summary>
    /// <param name="exception">The exception to check</param>
    /// <returns>True if it's a FluentValidation exception</returns>
    private static bool IsFluentValidationException(Exception exception) =>
        exception.GetType().Name == "ValidationException" &&
        exception.GetType().Namespace == "FluentValidation";

    /// <summary>
    /// Extracts validation errors from FluentValidation exception using reflection.
    /// </summary>
    /// <param name="exception">The FluentValidation exception</param>
    /// <returns>List of validation errors with full error codes</returns>
    private static List<Error> ExtractFluentValidationErrors(Exception exception)
    {
        var errors = new List<Error>();

        try
        {
            var errorsProperty = exception.GetType().GetProperty("Errors");
            if (errorsProperty?.GetValue(exception) is not IEnumerable<object> validationFailures)
                return errors;

            foreach (var failure in validationFailures)
            {
                var error = CreateValidationErrorFromFailure(failure);
                if (error.HasValue)
                    errors.Add(error.Value);
            }
        }
        catch
        {
            // If reflection fails, fall back to a generic validation error
            errors.Add(Error.Validation(
                "Validation.ReflectionFailed",
                $"Validation failed: {exception.Message}"));
        }

        return errors;
    }

    /// <summary>
    /// Creates a validation error from a FluentValidation failure object using reflection.
    /// </summary>
    /// <param name="failure">The validation failure object</param>
    /// <returns>ErrorOr validation error if successful</returns>
    private static Error? CreateValidationErrorFromFailure(object failure)
    {
        try
        {
            var type = failure.GetType();
            var propertyName = GetPropertyValue<string>(failure, type, "PropertyName") ?? "Unknown";
            var errorMessage = GetPropertyValue<string>(failure, type, "ErrorMessage") ?? "Validation failed";
            var errorCode = GetPropertyValue<string>(failure, type, "ErrorCode") ?? "ValidationError";

            return Error.Validation(
                code: $"{propertyName}.{errorCode}",
                description: errorMessage);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Helper method to get property values using reflection with type safety.
    /// </summary>
    /// <typeparam name="T">Expected return type</typeparam>
    /// <param name="obj">Object to get property from</param>
    /// <param name="type">Type of the object</param>
    /// <param name="propertyName">Name of the property</param>
    /// <returns>Property value or null</returns>
    private static T? GetPropertyValue<T>(object obj, Type type, string propertyName) where T : class
    {
        var property = type.GetProperty(propertyName);
        return property?.GetValue(obj) as T;
    }

    #endregion

    #region Exception Mapping

    /// <summary>
    /// Creates ErrorOr error from standard .NET exceptions with consistent error codes.
    /// </summary>
    /// <param name="exception">The exception to convert</param>
    /// <returns>Appropriate ErrorOr error with full error codes</returns>
    private static Error CreateErrorFromException(Exception exception) => exception switch
    {
        ArgumentNullException argNullEx => Error.Validation(
            $"Argument.{argNullEx.ParamName ?? "Null"}",
            exception.Message),

        ArgumentException argEx => Error.Validation(
            $"Argument.{argEx.ParamName ?? "Invalid"}",
            exception.Message),

        UnauthorizedAccessException => Error.Unauthorized(
            "Api.UnauthorizedAccess",
            exception.Message),

        FileNotFoundException => Error.NotFound(
            "Resource.FileNotFound",
            exception.Message),

        KeyNotFoundException => Error.NotFound(
            "Entity.NotFound",
            exception.Message),

        InvalidOperationException => Error.Validation(
            "Operation.Invalid",
            exception.Message),

        TimeoutException => Error.Failure(
            "Operation.Timeout",
            exception.Message),

        NotImplementedException => Error.Failure(
            "Feature.NotImplemented",
            exception.Message),

        // Default case for unexpected exceptions - don't leak internal details
        _ => Error.Unexpected(
            "InternalServerError.UnknownError",
            "An unexpected error occurred while processing your request.")
    };

    #endregion

    #region ApiResponse Creation Methods

    /// <summary>
    /// Creates ApiResponse for validation errors with structured field errors.
    /// </summary>
    /// <param name="errorGroups">Grouped validation errors by full error code</param>
    /// <param name="requestPath">Request path for tracing</param>
    /// <returns>ApiResponse with validation error structure</returns>
    private static ApiResponse<object> CreateValidationApiResponse(Dictionary<string, string[]> errorGroups, string requestPath)
    {
        return new ApiResponse<object>
        {
            IsSuccess = false,
            Data = null,
            Errors = errorGroups,
            Message = "Validation failed",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Validation Failed",
            Status = 422,
            Detail = "One or more validation errors occurred",
            RequestId = GenerateRequestId(),
            Timestamp = DateTimeOffset.UtcNow
        }.WithMetadata("requestPath", requestPath)
         .WithMetadata("source", "global-exception-handler");
    }

    /// <summary>
    /// Creates ApiResponse for not found errors.
    /// </summary>
    /// <param name="error">The not found error</param>
    /// <param name="requestPath">Request path for tracing</param>
    /// <returns>ApiResponse with not found error structure</returns>
    private static ApiResponse<object> CreateNotFoundApiResponse(Error error, string requestPath)
    {
        var title = GetErrorCode(error);
        var detail = error.Description;

        return new ApiResponse<object>
        {
            IsSuccess = false,
            Data = null,
            Message = detail,
            Type = "https://httpstatuses.com/404",
            Title = title,
            Status = 404,
            Detail = detail,
            RequestId = GenerateRequestId(),
            Timestamp = DateTimeOffset.UtcNow
        }.WithMetadata("requestPath", requestPath)
         .WithMetadata("source", "global-exception-handler");
    }

    /// <summary>
    /// Creates ApiResponse for unauthorized errors.
    /// </summary>
    /// <param name="error">The unauthorized error</param>
    /// <param name="requestPath">Request path for tracing</param>
    /// <returns>ApiResponse with unauthorized error structure</returns>
    private static ApiResponse<object> CreateUnauthorizedApiResponse(Error error, string requestPath)
    {
        var title = GetErrorCode(error);
        var detail = error.Description;

        return new ApiResponse<object>
        {
            IsSuccess = false,
            Data = null,
            Message = detail,
            Type = "https://httpstatuses.com/401",
            Title = title,
            Status = 401,
            Detail = detail,
            RequestId = GenerateRequestId(),
            Timestamp = DateTimeOffset.UtcNow
        }.WithMetadata("requestPath", requestPath)
         .WithMetadata("source", "global-exception-handler");
    }

    /// <summary>
    /// Creates ApiResponse for conflict errors.
    /// </summary>
    /// <param name="firstError">The primary conflict error</param>
    /// <param name="errorGroups">Grouped errors by full error code</param>
    /// <param name="requestPath">Request path for tracing</param>
    /// <returns>ApiResponse with conflict error structure</returns>
    private static ApiResponse<object> CreateConflictApiResponse(Error firstError, Dictionary<string, string[]> errorGroups, string requestPath)
    {
        var title = GetErrorCode(firstError);
        var detail = firstError.Description;

        return new ApiResponse<object>
        {
            IsSuccess = false,
            Data = null,
            Errors = errorGroups,
            Message = "A conflict occurred",
            Type = "https://httpstatuses.com/409",
            Title = title,
            Status = 409,
            Detail = detail,
            RequestId = GenerateRequestId(),
            Timestamp = DateTimeOffset.UtcNow
        }.WithMetadata("requestPath", requestPath)
         .WithMetadata("source", "global-exception-handler");
    }

    /// <summary>
    /// Creates ApiResponse for forbidden errors.
    /// </summary>
    /// <param name="firstError">The primary forbidden error</param>
    /// <param name="errorGroups">Grouped errors by full error code</param>
    /// <param name="requestPath">Request path for tracing</param>
    /// <returns>ApiResponse with forbidden error structure</returns>
    private static ApiResponse<object> CreateForbiddenApiResponse(Error firstError, Dictionary<string, string[]> errorGroups, string requestPath)
    {
        var title = GetErrorCode(firstError);
        var detail = firstError.Description;

        return new ApiResponse<object>
        {
            IsSuccess = false,
            Data = null,
            Errors = errorGroups,
            Message = "Access forbidden",
            Type = "https://httpstatuses.com/403",
            Title = title,
            Status = 403,
            Detail = detail,
            RequestId = GenerateRequestId(),
            Timestamp = DateTimeOffset.UtcNow
        }.WithMetadata("requestPath", requestPath)
         .WithMetadata("source", "global-exception-handler");
    }

    /// <summary>
    /// Creates ApiResponse for internal server errors.
    /// </summary>
    /// <param name="firstError">The primary failure error</param>
    /// <param name="errorGroups">Grouped errors by full error code</param>
    /// <param name="requestPath">Request path for tracing</param>
    /// <returns>ApiResponse with server error structure</returns>
    private static ApiResponse<object> CreateFailureApiResponse(Error firstError, Dictionary<string, string[]> errorGroups, string requestPath)
    {
        var title = GetErrorCode(firstError);
        var detail = firstError.Description;

        return new ApiResponse<object>
        {
            IsSuccess = false,
            Data = null,
            Errors = errorGroups,
            Message = "An internal error occurred",
            Type = "https://httpstatuses.com/500",
            Title = title,
            Status = 500,
            Detail = detail,
            RequestId = GenerateRequestId(),
            Timestamp = DateTimeOffset.UtcNow
        }.WithMetadata("requestPath", requestPath)
         .WithMetadata("source", "global-exception-handler");
    }

    /// <summary>
    /// Creates ApiResponse for unexpected errors.
    /// </summary>
    /// <param name="firstError">The primary unexpected error</param>
    /// <param name="errorGroups">Grouped errors by full error code</param>
    /// <param name="requestPath">Request path for tracing</param>
    /// <returns>ApiResponse with unexpected error structure</returns>
    private static ApiResponse<object> CreateUnexpectedApiResponse(Error firstError, Dictionary<string, string[]> errorGroups, string requestPath)
    {
        var title = GetErrorCode(firstError);
        var detail = firstError.Description;

        return new ApiResponse<object>
        {
            IsSuccess = false,
            Data = null,
            Errors = errorGroups,
            Message = "An unexpected error occurred",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.22",
            Title = title,
            Status = 422,
            Detail = detail,
            RequestId = GenerateRequestId(),
            Timestamp = DateTimeOffset.UtcNow
        }.WithMetadata("requestPath", requestPath)
         .WithMetadata("source", "global-exception-handler");
    }

    /// <summary>
    /// Creates ApiResponse for generic errors with grouped error structure.
    /// </summary>
    /// <param name="firstError">The primary error</param>
    /// <param name="errorGroups">Grouped errors by full error code</param>
    /// <param name="requestPath">Request path for tracing</param>
    /// <returns>ApiResponse with generic error structure</returns>
    private static ApiResponse<object> CreateGenericErrorApiResponse(Error firstError, Dictionary<string, string[]> errorGroups, string requestPath)
    {
        var statusCode = GetStatusCode(firstError.Type);
        var title = GetErrorCode(firstError);
        var detail = firstError.Description;

        return new ApiResponse<object>
        {
            IsSuccess = false,
            Data = null,
            Errors = errorGroups,
            Message = "An error occurred",
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            RequestId = GenerateRequestId(),
            Timestamp = DateTimeOffset.UtcNow
        }.WithMetadata("requestPath", requestPath)
         .WithMetadata("source", "global-exception-handler");
    }

    /// <summary>
    /// Creates ApiResponse for unknown errors without specific error information.
    /// </summary>
    /// <param name="requestPath">Request path for tracing</param>
    /// <returns>ApiResponse with generic unknown error structure</returns>
    private static ApiResponse<object> CreateGenericErrorApiResponse(string requestPath)
    {
        return new ApiResponse<object>
        {
            IsSuccess = false,
            Data = null,
            Errors = new Dictionary<string, string[]>
            {
                ["InternalServerError.UnknownError"] = ["An unexpected error occurred while processing your request."]
            },
            Message = "An unexpected error occurred",
            Type = "https://httpstatuses.com/500",
            Title = "Internal Server Error",
            Status = 500,
            Detail = "An unexpected error occurred while processing your request.",
            RequestId = GenerateRequestId(),
            Timestamp = DateTimeOffset.UtcNow
        }.WithMetadata("requestPath", requestPath)
         .WithMetadata("source", "global-exception-handler");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the full error code to use as the key in errors dictionary.
    /// If no code is provided, falls back to error type.
    /// </summary>
    /// <param name="error">The error to extract code from</param>
    /// <returns>Full error code or fallback</returns>
    private static string GetErrorCode(Error error)
    {
        if (string.IsNullOrWhiteSpace(error.Code))
            return error.Type.ToString();

        return error.Code;
    }

    /// <summary>
    /// Maps ErrorType to HTTP status code using the same mapping as ErrorOrApiResponseExtensions.
    /// </summary>
    /// <param name="type">ErrorOr error type</param>
    /// <returns>HTTP status code</returns>
    private static int GetStatusCode(ErrorType type) =>
        ErrorTypeToStatusCode.GetValueOrDefault(type, DefaultStatusCode);

    /// <summary>
    /// Generates a unique request ID for tracing purposes.
    /// </summary>
    /// <returns>Unique request identifier</returns>
    private static string GenerateRequestId() =>
        Guid.NewGuid().ToString("N")[..8];

    #endregion
}

#region Usage Examples and Integration

/// <summary>
/// Extension methods for registering and configuring the ApiResponseGlobalExceptionHandler
/// </summary>
public static class ApiResponseExceptionHandlerConfiguration
{
    /// <summary>
    /// Registers the ApiResponseGlobalExceptionHandler in the DI container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddApiResponseGlobalExceptionHandler(this IServiceCollection services)
    {
        return services.AddExceptionHandler<ApiResponseGlobalExceptionHandler>();
    }

    /// <summary>
    /// Configures the ApiResponse exception handling middleware in the pipeline
    /// </summary>
    /// <param name="app">Web application</param>
    /// <returns>Web application for chaining</returns>
    public static WebApplication UseApiResponseGlobalExceptionHandler(this WebApplication app)
    {
        // Add exception handler middleware
        app.UseExceptionHandler();

        return app;
    }
}

/// <summary>
/// Example Program.cs configuration for ApiResponse pattern
/// </summary>
public static class ApiResponseProgramExample
{
    /*
    var builder = WebApplication.CreateBuilder(args);
    
    // Register services
    builder.Services.AddControllers();
    builder.Services.AddApiResponseGlobalExceptionHandler(); // Add ApiResponse exception handler
    
    var app = builder.Build();
    
    // Configure middleware pipeline
    app.UseApiResponseGlobalExceptionHandler(); // Use ApiResponse exception handler
    app.UseRouting();
    app.MapControllers();
    
    app.Run();
    */
}

/// <summary>
/// Examples of how different exceptions are handled with ApiResponse wrapper
/// </summary>
public static class ApiResponseExceptionMappingExamples
{
    /*
    Exception Type → ErrorOr Type → ApiResponse Examples (Always HTTP 200 OK)
    
    ArgumentException → Validation → HTTP 200 OK
    {
        "isSuccess": false,
        "data": null,
        "message": "Validation failed",
        "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        "title": "Validation Failed",
        "status": 400,
        "detail": "One or more validation errors occurred",
        "errors": {
            "Argument.userId": ["User ID must be greater than 0"]
        },
        "timestamp": "2024-01-15T10:30:00Z",
        "requestId": "abc12345",
        "metadata": {
            "requestPath": "/api/users",
            "source": "global-exception-handler"
        }
    }
    
    UnauthorizedAccessException → Unauthorized → HTTP 200 OK
    {
        "isSuccess": false,
        "data": null,
        "message": "Access denied",
        "type": "https://httpstatuses.com/401",
        "title": "Api.UnauthorizedAccess",
        "status": 401,
        "detail": "Access denied",
        "timestamp": "2024-01-15T10:30:00Z",
        "requestId": "def67890",
        "metadata": {
            "requestPath": "/api/users/1",
            "source": "global-exception-handler"
        }
    }
    
    KeyNotFoundException → NotFound → HTTP 200 OK
    {
        "isSuccess": false,
        "data": null,
        "message": "User with ID 999 was not found",
        "type": "https://httpstatuses.com/404",
        "title": "Entity.NotFound",
        "status": 404,
        "detail": "User with ID 999 was not found",
        "timestamp": "2024-01-15T10:30:00Z",
        "requestId": "ghi11223",
        "metadata": {
            "requestPath": "/api/users/999",
            "source": "global-exception-handler"
        }
    }
    
    TimeoutException → Failure → HTTP 200 OK
    {
        "isSuccess": false,
        "data": null,
        "message": "An internal error occurred",
        "type": "https://httpstatuses.com/500",
        "title": "Operation.Timeout",
        "status": 500,
        "detail": "The operation timed out",
        "errors": {
            "Operation.Timeout": ["The operation timed out"]
        },
        "timestamp": "2024-01-15T10:30:00Z",
        "requestId": "jkl44556",
        "metadata": {
            "requestPath": "/api/users",
            "source": "global-exception-handler"
        }
    }
    
    FluentValidation.ValidationException → Validation → HTTP 200 OK
    {
        "isSuccess": false,
        "data": null,
        "message": "Validation failed",
        "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        "title": "Validation Failed",
        "status": 422,
        "detail": "One or more validation errors occurred",
        "errors": {
            "Name.Required": ["Name is required"],
            "Email.Invalid": ["Email format is invalid"],
            "Password.TooShort": ["Password must be at least 8 characters"]
        },
        "timestamp": "2024-01-15T10:30:00Z",
        "requestId": "mno78901",
        "metadata": {
            "requestPath": "/api/users",
            "source": "global-exception-handler"
        }
    }
    */
}

#endregion