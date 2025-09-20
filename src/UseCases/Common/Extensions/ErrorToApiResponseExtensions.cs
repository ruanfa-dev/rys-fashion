using ErrorOr;

using Microsoft.AspNetCore.Http;

using SharedKernel.Models;

namespace UseCases.Common.Extensions;

/// <summary>
/// Extension methods for converting ErrorOr Error objects to IResult for minimal APIs
/// </summary>
public static class ErrorToApiResponseExtensions
{
    /// <summary>
    /// Converts an Error object to an IResult with ApiResponse wrapper
    /// </summary>
    /// <param name="error">The Error to convert</param>
    /// <param name="requestId">Optional request identifier for tracing</param>
    /// <returns>IResult with ApiResponse wrapper</returns>
    public static IResult ToApiResponse(this Error error, string? requestId = null)
    {
        var statusCode = GetStatusCode(error.Type);
        var apiResponse = CreateApiResponseFromError(error, statusCode, requestId);
        return Results.Ok(apiResponse);
    }

    /// <summary>
    /// Converts a list of errors to an IResult with ApiResponse wrapper
    /// </summary>
    /// <param name="errors">The errors to convert</param>
    /// <param name="requestId">Optional request identifier for tracing</param>
    /// <returns>IResult with ApiResponse wrapper</returns>
    public static IResult ToApiResponse(this IReadOnlyList<Error> errors, string? requestId = null)
    {
        if (errors.Count == 0)
        {
            var unknownError = Error.Failure("Unknown.Error", "An unknown error occurred");
            return unknownError.ToApiResponse(requestId);
        }

        var firstError = errors[0];
        var statusCode = GetStatusCode(firstError.Type);

        // Group errors by error code
        var errorGroups = errors
            .GroupBy(e => GetErrorCode(e))
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Description).ToArray()
            );

        var apiResponse = CreateApiResponseFromErrors(firstError, errorGroups, statusCode, requestId);
        return Results.Ok(apiResponse);
    }

    private static ApiResponse CreateApiResponseFromError(Error error, int statusCode, string? requestId)
    {
        var errorCode = GetErrorCode(error);
        var errorGroups = new Dictionary<string, string[]>
        {
            [errorCode] = [error.Description]
        };

        return CreateApiResponseFromErrors(error, errorGroups, statusCode, requestId);
    }

    private static ApiResponse CreateApiResponseFromErrors(Error firstError, Dictionary<string, string[]> errorGroups, int statusCode, string? requestId)
    {
        var errorCode = GetErrorCode(firstError);
        var detail = firstError.Description;
        var typeUri = GetProblemTypeUri(statusCode);
        var title = GetProblemTitle(firstError.Type, errorCode);

        return new ApiResponse
        {
            IsSuccess = false,
            Errors = errorGroups,
            Message = GetErrorMessage(firstError.Type),
            Type = typeUri,
            Title = title,
            Status = statusCode,
            Detail = detail,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private static string GetErrorCode(Error error)
    {
        return string.IsNullOrWhiteSpace(error.Code) ? error.Type.ToString() : error.Code;
    }

    private static int GetStatusCode(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            ErrorType.Unexpected => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };
    }

    private static string GetProblemTypeUri(int statusCode)
    {
        return statusCode switch
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
    }

    private static string GetProblemTitle(ErrorType errorType, string errorCode)
    {
        return errorType switch
        {
            ErrorType.Validation => "Validation Failed",
            ErrorType.Unauthorized => "Unauthorized",
            ErrorType.Forbidden => "Forbidden",
            ErrorType.NotFound => errorCode.Contains('.') ? errorCode : "Not Found",
            ErrorType.Conflict => errorCode.Contains('.') ? errorCode : "Conflict",
            ErrorType.Failure => "Internal Server Error",
            ErrorType.Unexpected => "Unprocessable Entity",
            _ => "Error"
        };
    }

    private static string GetErrorMessage(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.Validation => "Validation failed",
            ErrorType.Unauthorized => "Unauthorized access",
            ErrorType.Forbidden => "Access forbidden",
            ErrorType.NotFound => "Resource not found",
            ErrorType.Conflict => "A conflict occurred",
            ErrorType.Failure => "An internal error occurred",
            ErrorType.Unexpected => "An unexpected error occurred",
            _ => "An error occurred"
        };
    }
}