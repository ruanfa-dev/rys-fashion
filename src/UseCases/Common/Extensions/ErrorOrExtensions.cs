using System.Collections.Frozen;

using ErrorOr;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UseCases.Common.Extensions;

/// <summary>
/// Extension methods for converting ErrorOr results to HTTP responses in ASP.NET Core applications.
/// Provides seamless integration between ErrorOr library and ASP.NET Core's response types.
/// </summary>
/// <remarks>
/// This class provides two main categories of extensions:
/// 1. TypedResults extensions for minimal APIs
/// 2. ActionResult extensions for MVC controllers
/// 
/// The extensions automatically handle error-to-HTTP status code mapping and create appropriate
/// ProblemDetails responses following RFC 7807 standards.
/// </remarks>
/// <example>
/// Basic usage in a minimal API:
/// <code>
/// app.MapGet("/users/{id}", async (int id, IUserService userService) =>
/// {
///     var result = await userService.GetUserByIdAsync(id);
///     return result.ToTypedResult(); // Automatically converts to Ok(user) or Problem(errors)
/// });
/// </code>
/// 
/// Usage in an MVC controller:
/// <code>
/// [HttpPost]
/// public async Task&lt;IActionResult&gt; CreateUser(CreateUserRequest request)
/// {
///     var result = await _userService.CreateUserAsync(request);
///     return result.Match(
///         user => Ok(user),
///         errors => errors.ToProblemDetailsActionResult()
///     );
/// }
/// </code>
/// </example>

public static class ErrorOrExtensions
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
    private const string ValidationProblemType = "https://tools.ietf.org/html/rfc7231#section-6.5.1";

    #region TypedResults Extensions

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an IResult for minimal APIs.
    /// Returns Ok(value) on success or ProblemDetails on error.
    /// </summary>
    /// <typeparam name="T">The type of the success result</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <returns>IResult representing either success or error response</returns>
    /// <example>
    /// <code>
    /// app.MapGet("/products/{id}", async (int id, IProductService service) =>
    /// {
    ///     var result = await service.GetProductAsync(id);
    ///     return result.ToTypedResult(); // Returns 200 OK with product or 404 Not Found
    /// });
    /// </code>
    /// </example>
    public static IResult ToTypedResult<T>(this ErrorOr<T> result)
        => result.Match(TypedResults.Ok, ToProblemDetails);

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to a Created response for minimal APIs.
    /// Returns Created(location, value) on success or ProblemDetails on error.
    /// </summary>
    /// <typeparam name="T">The type of the created resource</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="locationUrl">The URL of the created resource</param>
    /// <returns>IResult representing either created response or error</returns>
    /// <example>
    /// <code>
    /// app.MapPost("/users", async (CreateUserRequest request, IUserService service) =>
    /// {
    ///     var result = await service.CreateUserAsync(request);
    ///     return result.ToTypedResultCreated($"/users/{result.Value?.Id}");
    /// });
    /// </code>
    /// </example>
    public static IResult ToTypedResultCreated<T>(this ErrorOr<T> result, string locationUrl)
        => result.Match(
            value => TypedResults.Created(locationUrl, value),
            ToProblemDetails);

    /// <summary>
    /// Converts an ErrorOr&lt;Updated&gt; result to a NoContent response for minimal APIs.
    /// Returns 204 No Content on success or ProblemDetails on error.
    /// </summary>
    /// <param name="result">The ErrorOr&lt;Updated&gt; result to convert</param>
    /// <returns>IResult representing either no content or error response</returns>
    /// <example>
    /// <code>
    /// app.MapPut("/users/{id}", async (int id, UpdateUserRequest request, IUserService service) =>
    /// {
    ///     var result = await service.UpdateUserAsync(id, request);
    ///     return result.ToTypedResultNoContent(); // Returns 204 or error details
    /// });
    /// </code>
    /// </example>
    public static IResult ToTypedResultNoContent(this ErrorOr<Updated> result)
        => result.Match(_ => TypedResults.NoContent(), ToProblemDetails);

    /// <summary>
    /// Converts an ErrorOr&lt;Deleted&gt; result to a NoContent response for minimal APIs.
    /// Returns 204 No Content on success or ProblemDetails on error.
    /// </summary>
    /// <param name="result">The ErrorOr&lt;Deleted&gt; result to convert</param>
    /// <returns>IResult representing either no content or error response</returns>
    /// <example>
    /// <code>
    /// app.MapDelete("/users/{id}", async (int id, IUserService service) =>
    /// {
    ///     var result = await service.DeleteUserAsync(id);
    ///     return result.ToTypedResultDeleted(); // Returns 204 or error details
    /// });
    /// </code>
    /// </example>
    public static IResult ToTypedResultDeleted(this ErrorOr<Deleted> result)
        => result.Match(_ => TypedResults.NoContent(), ToProblemDetails);

    #endregion

    #region ProblemDetails Conversions

    /// <summary>
    /// Converts a list of errors to an IResult with appropriate ProblemDetails.
    /// Handles validation errors specially by grouping them by property name.
    /// </summary>
    /// <param name="errors">The list of errors to convert</param>
    /// <returns>IResult with appropriate HTTP status and ProblemDetails</returns>
    /// <example>
    /// Error types mapping:
    /// - Validation → 400 Bad Request with ValidationProblemDetails
    /// - NotFound → 404 Not Found
    /// - Unauthorized → 401 Unauthorized
    /// - Forbidden → 403 Forbidden
    /// - Conflict → 409 Conflict
    /// - Failure → 500 Internal Server Error
    /// - Unexpected → 422 Unprocessable Entity
    /// </example>
    public static IResult ToProblemDetails(IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0)
            return Results.Problem("An unknown error occurred.", statusCode: DefaultStatusCode);

        var firstError = errors[0];

        if (firstError.Type == ErrorType.Validation)
            return CreateValidationProblem(errors);

        var statusCode = GetStatusCode(firstError.Type);
        return Results.Problem(
            title: firstError.Code,
            detail: firstError.Description,
            statusCode: statusCode,
            type: GetProblemTypeUri(statusCode));
    }

    /// <summary>
    /// Converts a list of errors to an IActionResult with appropriate ProblemDetails for MVC controllers.
    /// Handles validation errors specially by creating ValidationProblemDetails.
    /// </summary>
    /// <param name="errors">The list of errors to convert</param>
    /// <returns>IActionResult with appropriate HTTP status and ProblemDetails</returns>
    /// <example>
    /// <code>
    /// [HttpPost]
    /// public async Task&lt;IActionResult&gt; CreateProduct(CreateProductRequest request)
    /// {
    ///     var result = await _productService.CreateProductAsync(request);
    ///     
    ///     if (result.IsError)
    ///         return result.Errors.ToProblemDetailsActionResult();
    ///         
    ///     return CreatedAtAction(nameof(GetProduct), new { id = result.Value.Id }, result.Value);
    /// }
    /// </code>
    /// </example>
    public static IActionResult ToProblemDetailsActionResult(this IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0)
            return CreateGenericProblemActionResult();

        var firstError = errors[0];

        if (firstError.Type == ErrorType.Validation)
            return CreateValidationProblemActionResult(errors);

        return CreateProblemActionResult(firstError);
    }

    #endregion

    #region Private Helper Methods

    private static IResult CreateValidationProblem(IReadOnlyList<Error> errors)
    {
        var errorsByProperty = errors
            .ToLookup(e => e.Code, e => e.Description)
            .ToDictionary(g => g.Key, g => g.ToArray());

        return Results.ValidationProblem(
            errorsByProperty,
            title: "Validation Failed",
            type: ValidationProblemType);
    }

    private static IActionResult CreateValidationProblemActionResult(IReadOnlyList<Error> errors)
    {
        var errorsByProperty = errors
            .ToLookup(e => e.Code, e => e.Description)
            .ToDictionary(g => g.Key, g => g.ToArray());

        return new BadRequestObjectResult(new ValidationProblemDetails(errorsByProperty)
        {
            Title = "Validation Failed",
            Type = ValidationProblemType,
            Status = StatusCodes.Status400BadRequest
        });
    }

    private static IActionResult CreateProblemActionResult(Error error)
    {
        var statusCode = GetStatusCode(error.Type);
        var problemDetails = new ProblemDetails
        {
            Title = error.Code,
            Detail = error.Description,
            Status = statusCode,
            Type = GetProblemTypeUri(statusCode)
        };

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }

    private static IActionResult CreateGenericProblemActionResult()
    {
        var problemDetails = new ProblemDetails
        {
            Title = "Unknown Error",
            Detail = "An unknown error occurred.",
            Status = DefaultStatusCode,
            Type = GetProblemTypeUri(DefaultStatusCode)
        };

        return new ObjectResult(problemDetails) { StatusCode = DefaultStatusCode };
    }

    private static int GetStatusCode(ErrorType type)
        => ErrorTypeToStatusCode.GetValueOrDefault(type, DefaultStatusCode);

    private static string GetProblemTypeUri(int statusCode)
        => $"https://httpstatuses.com/{statusCode}";

    #endregion
}

#region Usage Examples

/// <summary>
/// Example service layer that returns ErrorOr results
/// </summary>
public class UserService
{
    public async Task<ErrorOr<UserExample>> GetUserByIdAsync(int id)
    {
        if (id <= 0)
            return Error.Validation("User.Id", "User ID must be greater than 0");

        var user = await FindUserInDatabaseAsync(id);
        if (user == null)
            return Error.NotFound("User.NotFound", $"User with ID {id} was not found");

        return user;
    }

    public async Task<ErrorOr<UserExample>> CreateUserAsync(CreateUserRequest request)
    {
        var validationErrors = ValidateCreateUserRequest(request);
        if (validationErrors.Any())
            return validationErrors;

        var existingUser = await FindUserByEmailAsync(request.Email);
        if (existingUser != null)
            return Error.Conflict("User.EmailExists", "A user with this email already exists");

        var user = new UserExample(request.Name, request.Email);
        await SaveUserAsync(user);
        return user;
    }

    public async Task<ErrorOr<Updated>> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var getUserResult = await GetUserByIdAsync(id);
        if (getUserResult.IsError)
            return getUserResult.Errors;

        var user = getUserResult.Value;
        user.UpdateName(request.Name);
        await SaveUserAsync(user);
        return Result.Updated;
    }

    public async Task<ErrorOr<Deleted>> DeleteUserAsync(int id)
    {
        var getUserResult = await GetUserByIdAsync(id);
        if (getUserResult.IsError)
            return getUserResult.Errors;

        await DeleteUserFromDatabaseAsync(id);
        return Result.Deleted;
    }

    private List<Error> ValidateCreateUserRequest(CreateUserRequest request)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add(Error.Validation("Name", "Name is required"));

        if (string.IsNullOrWhiteSpace(request.Email))
            errors.Add(Error.Validation("Email", "Email is required"));
        else if (!IsValidEmail(request.Email))
            errors.Add(Error.Validation("Email", "Email format is invalid"));

        return errors;
    }

    // Placeholder methods - implement with your actual data access
    private Task<UserExample?> FindUserInDatabaseAsync(int id) => throw new NotImplementedException();
    private Task<UserExample?> FindUserByEmailAsync(string email) => throw new NotImplementedException();
    private Task SaveUserAsync(UserExample user) => throw new NotImplementedException();
    private Task DeleteUserFromDatabaseAsync(int id) => throw new NotImplementedException();
    private bool IsValidEmail(string email) => throw new NotImplementedException();
}

/// <summary>
/// Example Minimal API endpoints using ErrorOr extensions
/// </summary>
public static class MinimalApiExamples
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var users = app.MapGroup("/api/users").WithTags("Users");

        // GET /api/users/{id} - Returns 200 OK with user or 404 Not Found
        users.MapGet("/{id:int}", async (int id, UserService userService) =>
        {
            var result = await userService.GetUserByIdAsync(id);
            return result.ToTypedResult();
        });

        // POST /api/users - Returns 201 Created or 400 Bad Request for validation errors
        users.MapPost("/", async (CreateUserRequest request, UserService userService) =>
        {
            var result = await userService.CreateUserAsync(request);
            return result.ToTypedResultCreated($"/api/users/{result.Value?.Id}");
        });

        // PUT /api/users/{id} - Returns 204 No Content or error details
        users.MapPut("/{id:int}", async (int id, UpdateUserRequest request, UserService userService) =>
        {
            var result = await userService.UpdateUserAsync(id, request);
            return result.ToTypedResultNoContent();
        });

        // DELETE /api/users/{id} - Returns 204 No Content or error details
        users.MapDelete("/{id:int}", async (int id, UserService userService) =>
        {
            var result = await userService.DeleteUserAsync(id);
            return result.ToTypedResultDeleted();
        });
    }
}

/// <summary>
/// Example MVC Controller using ErrorOr extensions
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(IgnoreApi = true)]
public class UsersExampleController : ControllerBase
{
    private readonly UserService _userService;

    public UsersExampleController(UserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var result = await _userService.GetUserByIdAsync(id);

        return result.Match(
            user => Ok(user),
            errors => errors.ToProblemDetailsActionResult()
        );
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        var result = await _userService.CreateUserAsync(request);

        if (result.IsError)
            return result.Errors.ToProblemDetailsActionResult();

        return CreatedAtAction(
            nameof(GetUser),
            new { id = result.Value.Id },
            result.Value
        );
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserRequest request)
    {
        var result = await _userService.UpdateUserAsync(id, request);

        if (result.IsError)
            return result.Errors.ToProblemDetailsActionResult();

        return NoContent();
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var result = await _userService.DeleteUserAsync(id);

        return result.Match(
            _ => NoContent(),
            errors => errors.ToProblemDetailsActionResult()
        );
    }
}

/// <summary>
/// Example DTOs for the user endpoints
/// </summary>
public record UserExample(int Id, string Name, string Email)
{
    public UserExample(string name, string email)
        : this(0, name, email)
    {
    }

    public UserExample UpdateName(string name) => this with { Name = name };
}

public record CreateUserRequest(string Name, string Email);
public record UpdateUserRequest(string Name);

/// <summary>
/// Example of expected HTTP responses
/// </summary>
public static class ResponseExamples
{
    /*
    Successful GET /api/users/1:
    HTTP 200 OK
    {
        "id": 1,
        "name": "John Doe",
        "email": "john@example.com"
    }
    
    Not Found GET /api/users/999:
    HTTP 404 Not Found
    {
        "type": "https://httpstatuses.com/404",
        "title": "User.NotFound",
        "status": 404,
        "detail": "User with ID 999 was not found"
    }
    
    Validation Error POST /api/users:
    HTTP 400 Bad Request
    {
        "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        "title": "Validation Failed",
        "status": 400,
        "errors": {
            "Name": ["Name is required"],
            "Email": ["Email is required"]
        }
    }
    
    Conflict POST /api/users:
    HTTP 409 Conflict
    {
        "type": "https://httpstatuses.com/409",
        "title": "User.EmailExists",
        "status": 409,
        "detail": "A user with this email already exists"
    }
    */
}

#endregion
