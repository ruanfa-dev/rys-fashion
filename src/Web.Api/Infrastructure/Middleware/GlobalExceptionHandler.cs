using System.Collections.Frozen;

using ErrorOr;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Infrastructure.Middleware;

/// <summary>
/// Global exception handler that converts unhandled exceptions to standardized ErrorOr-based ProblemDetails responses.
/// Integrates seamlessly with the ErrorOr extensions to provide consistent error handling across the application.
/// </summary>
/// <remarks>
/// This handler automatically maps common .NET exceptions to appropriate ErrorOr error types and HTTP status codes,
/// following RFC 7807 ProblemDetails standards. It provides special handling for:
/// - FluentValidation exceptions (converted to validation errors)
/// - Common .NET framework exceptions (mapped to appropriate error types)
/// - Unknown exceptions (mapped to unexpected errors)
/// 
/// The handler maintains consistency with the ErrorOr extensions by using the same error type mapping
/// and ProblemDetails creation patterns.
/// </remarks>
/// <example>
/// Register the handler in Program.cs:
/// <code>
/// builder.Services.AddExceptionHandler&lt;GlobalExceptionHandler&gt;();
/// 
/// // In the middleware pipeline:
/// app.UseExceptionHandler();
/// </code>
/// 
/// The handler will automatically convert exceptions like:
/// - ArgumentException → 400 Bad Request with validation error
/// - UnauthorizedAccessException → 401 Unauthorized
/// - KeyNotFoundException → 404 Not Found
/// - TimeoutException → 500 Internal Server Error
/// </example>
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    // Pre-computed frozen dictionary for better performance - matches ErrorOrExtensions
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
    private const string ValidationProblemType = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
    private const string ResponseContentType = "application/problem+json";

    /// <summary>
    /// Initializes a new instance of the GlobalExceptionHandler.
    /// </summary>
    /// <param name="logger">Logger for recording unhandled exceptions</param>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles unhandled exceptions by converting them to ErrorOr-based ProblemDetails responses.
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
        var problemDetails = CreateProblemDetails(errors, httpContext.Request.Path);

        await WriteResponseAsync(httpContext, problemDetails, cancellationToken);
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
    /// Creates ProblemDetails from ErrorOr errors using the same patterns as ErrorOrExtensions.
    /// </summary>
    /// <param name="errors">The errors to convert</param>
    /// <param name="requestPath">The request path for the Instance property</param>
    /// <returns>ProblemDetails object ready for HTTP response</returns>
    private static ProblemDetails CreateProblemDetails(IReadOnlyList<Error> errors, string requestPath)
    {
        if (errors.Count == 0)
            return CreateGenericProblemDetails(requestPath);

        var firstError = errors[0];

        if (firstError.Type == ErrorType.Validation)
            return CreateValidationProblemDetails(errors, requestPath);

        return CreateStandardProblemDetails(firstError, requestPath);
    }

    /// <summary>
    /// Writes the ProblemDetails response to the HTTP context.
    /// </summary>
    /// <param name="httpContext">The HTTP context</param>
    /// <param name="problemDetails">The ProblemDetails to write</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private static async Task WriteResponseAsync(
        HttpContext httpContext,
        ProblemDetails problemDetails,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = problemDetails.Status ?? DefaultStatusCode;
        httpContext.Response.ContentType = ResponseContentType;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
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
    /// <returns>List of validation errors</returns>
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
    /// <returns>Appropriate ErrorOr error</returns>
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

    #region ProblemDetails Creation

    /// <summary>
    /// Creates ValidationProblemDetails for validation errors using the same pattern as ErrorOrExtensions.
    /// </summary>
    /// <param name="errors">Validation errors</param>
    /// <param name="requestPath">Request path</param>
    /// <returns>ValidationProblemDetails</returns>
    private static ValidationProblemDetails CreateValidationProblemDetails(
        IReadOnlyList<Error> errors,
        string requestPath)
    {
        // Group validation errors by property name using ToLookup for better performance
        var errorsByProperty = errors
            .ToLookup(e => ExtractPropertyName(e.Code), e => e.Description)
            .ToDictionary(g => g.Key, g => g.ToArray());

        return new ValidationProblemDetails(errorsByProperty)
        {
            Title = "Validation Failed",
            Type = ValidationProblemType,
            Status = StatusCodes.Status400BadRequest,
            Instance = requestPath
        };
    }

    /// <summary>
    /// Creates standard ProblemDetails for non-validation errors.
    /// </summary>
    /// <param name="error">The error to convert</param>
    /// <param name="requestPath">Request path</param>
    /// <returns>ProblemDetails</returns>
    private static ProblemDetails CreateStandardProblemDetails(Error error, string requestPath)
    {
        var statusCode = GetStatusCode(error.Type);

        return new ProblemDetails
        {
            Title = error.Code,
            Detail = error.Description,
            Status = statusCode,
            Type = GetProblemTypeUri(statusCode),
            Instance = requestPath
        };
    }

    /// <summary>
    /// Creates generic ProblemDetails for unknown errors.
    /// </summary>
    /// <param name="requestPath">Request path</param>
    /// <returns>Generic ProblemDetails</returns>
    private static ProblemDetails CreateGenericProblemDetails(string requestPath)
    {
        return new ProblemDetails
        {
            Title = "Unknown Error",
            Detail = "An unexpected error occurred while processing your request.",
            Status = DefaultStatusCode,
            Type = GetProblemTypeUri(DefaultStatusCode),
            Instance = requestPath
        };
    }

    /// <summary>
    /// Extracts property name from validation error code (handles "PropertyName.ErrorCode" format).
    /// </summary>
    /// <param name="code">Error code</param>
    /// <returns>Property name</returns>
    private static string ExtractPropertyName(string code)
    {
        if (string.IsNullOrEmpty(code))
            return "Unknown";

        var dotIndex = code.IndexOf('.');
        return dotIndex > 0 ? code[..dotIndex] : code;
    }

    /// <summary>
    /// Maps ErrorType to HTTP status code using the same mapping as ErrorOrExtensions.
    /// </summary>
    /// <param name="type">ErrorOr error type</param>
    /// <returns>HTTP status code</returns>
    private static int GetStatusCode(ErrorType type) =>
        ErrorTypeToStatusCode.GetValueOrDefault(type, DefaultStatusCode);

    /// <summary>
    /// Gets problem type URI for HTTP status code.
    /// </summary>
    /// <param name="statusCode">HTTP status code</param>
    /// <returns>Problem type URI</returns>
    private static string GetProblemTypeUri(int statusCode) =>
        $"https://httpstatuses.com/{statusCode}";

    #endregion
}

#region Usage Examples and Integration

/// <summary>
/// Example of how to register and configure the GlobalExceptionHandler
/// </summary>
public static class ExceptionHandlerConfiguration
{
    /// <summary>
    /// Registers the GlobalExceptionHandler in the DI container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddGlobalExceptionHandler(this IServiceCollection services)
    {
        return services.AddExceptionHandler<GlobalExceptionHandler>();
    }

    /// <summary>
    /// Configures the exception handling middleware in the pipeline
    /// </summary>
    /// <param name="app">Web application</param>
    /// <returns>Web application for chaining</returns>
    public static WebApplication UseGlobalExceptionHandler(this WebApplication app)
    {
        // Add exception handler middleware
        app.UseExceptionHandler();

        // Optionally add status code pages for client errors
        app.UseStatusCodePages();

        return app;
    }
}

/// <summary>
/// Example Program.cs configuration
/// </summary>
public static class ProgramExample
{
    /*
    var builder = WebApplication.CreateBuilder(args);
    
    // Register services
    builder.Services.AddControllers();
    builder.Services.AddGlobalExceptionHandler(); // Add our exception handler
    
    var app = builder.Build();
    
    // Configure middleware pipeline
    app.UseGlobalExceptionHandler(); // Use our exception handler
    app.UseRouting();
    app.MapControllers();
    
    app.Run();
    */
}

/// <summary>
/// Examples of how different exceptions are handled
/// </summary>
public static class ExceptionMappingExamples
{
    /*
    Exception Type → ErrorOr Type → HTTP Status → Response Example
    
    ArgumentException → Validation → 400 Bad Request
    {
        "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        "title": "Validation Failed",
        "status": 400,
        "instance": "/api/users",
        "errors": {
            "userId": ["User ID must be greater than 0"]
        }
    }
    
    UnauthorizedAccessException → Unauthorized → 401 Unauthorized
    {
        "type": "https://httpstatuses.com/401",
        "title": "Api.UnauthorizedAccess",
        "status": 401,
        "detail": "Access denied",
        "instance": "/api/users/1"
    }
    
    KeyNotFoundException → NotFound → 404 Not Found
    {
        "type": "https://httpstatuses.com/404",
        "title": "Entity.NotFound",
        "status": 404,
        "detail": "User with ID 999 was not found",
        "instance": "/api/users/999"
    }
    
    TimeoutException → Failure → 500 Internal Server Error
    {
        "type": "https://httpstatuses.com/500",
        "title": "Operation.Timeout",
        "status": 500,
        "detail": "The operation timed out",
        "instance": "/api/users"
    }
    
    FluentValidation.ValidationException → Validation → 400 Bad Request
    {
        "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        "title": "Validation Failed",
        "status": 400,
        "instance": "/api/users",
        "errors": {
            "Name": ["Name is required"],
            "Email": ["Email format is invalid"]
        }
    }
    */
}

#endregion
