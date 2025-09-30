using System.Collections.Frozen;

using ErrorOr;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UseCases.Common.Extensions;

/// <summary>
/// Extension methods for converting ErrorOr results to ActionResult for MVC Controllers.
/// Provides seamless integration between ErrorOr library and ASP.NET Core MVC Controllers.
/// </summary>
/// <remarks>
/// This class focuses specifically on MVC Controller ActionResult conversion with proper HTTP status codes.
/// All methods return appropriate ActionResult types with correct status codes for API responses.
/// 
/// Key Features:
/// - Automatic error-to-HTTP status code mapping
/// - RFC 7807 compliant ProblemDetails responses
/// - Validation error handling with ValidationProblemDetails
/// - Consistent controller response patterns
/// - Performance optimized with frozen dictionaries
/// </remarks>
/// <example>
/// Basic usage in MVC Controllers:
/// <code>
/// [HttpGet("{id:int}")]
/// public async Task&lt;IActionResult&gt; GetProduct(int id)
/// {
///     var result = await _productService.GetProductAsync(id);
///     return result.ToActionResult(); // Returns Ok(product) or NotFound with problem details
/// }
/// </code>
/// </example>
public static class ErrorOrActionResultExtensions
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

    #region Core ActionResult Extensions

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an ActionResult for MVC controllers.
    /// Returns Ok(value) on success or appropriate error ActionResult with proper HTTP status codes.
    /// </summary>
    /// <typeparam name="T">The type of the success result</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <returns>ActionResult representing either success (200 OK) or error response with appropriate status code</returns>
    /// <example>
    /// <code>
    /// [HttpGet("{id:int}")]
    /// public async Task&lt;ActionResult&lt;Product&gt;&gt; GetProduct(int id)
    /// {
    ///     var result = await _productService.GetProductAsync(id);
    ///     return result.ToActionResult(); // Returns Ok(product) or NotFound with problem details
    /// }
    /// </code>
    /// </example>
    public static ActionResult<T> ToActionResult<T>(this ErrorOr<T> result)
    {
        if (result.IsError)
        {
            IActionResult problemResult = result.Errors.ToProblemDetailsActionResult();
            // Cast IActionResult to ActionResult to use the constructor that accepts ActionResult
            return (ActionResult)problemResult;
        }
        
        return new OkObjectResult(result.Value);
    }

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to a CreatedAtAction ActionResult for MVC controllers.
    /// Returns CreatedAtAction on success or appropriate error ActionResult with proper HTTP status codes.
    /// </summary>
    /// <typeparam name="T">The type of the created resource</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="actionName">The name of the action to generate URL for</param>
    /// <param name="routeValues">Route values for generating the URL</param>
    /// <returns>ActionResult representing either created response (201 Created) or error with appropriate status code</returns>
    /// <example>
    /// <code>
    /// [HttpPost]
    /// public async Task&lt;ActionResult&lt;Product&gt;&gt; CreateProduct(CreateProductRequest request)
    /// {
    ///     var result = await _productService.CreateProductAsync(request);
    ///     return result.ToCreatedAtActionResult(nameof(GetProduct), new { id = result.Value?.Id });
    /// }
    /// </code>
    /// </example>
    public static ActionResult<T> ToCreatedAtActionResult<T>(this ErrorOr<T> result, string actionName, object? routeValues = null)
    {
        if (result.IsError)
        {
            IActionResult problemResult = result.Errors.ToProblemDetailsActionResult();
            return (ActionResult)problemResult;
        }
        
        return new CreatedAtActionResult(actionName, controllerName: null, routeValues, result.Value);
    }

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to a CreatedAtRoute ActionResult for MVC controllers.
    /// Returns CreatedAtRoute on success or appropriate error ActionResult with proper HTTP status codes.
    /// </summary>
    /// <typeparam name="T">The type of the created resource</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="routeName">The name of the route to generate URL for</param>
    /// <param name="routeValues">Route values for generating the URL</param>
    /// <returns>ActionResult representing either created response (201 Created) or error with appropriate status code</returns>
    /// <example>
    /// <code>
    /// [HttpPost]
    /// public async Task&lt;ActionResult&lt;Product&gt;&gt; CreateProduct(CreateProductRequest request)
    /// {
    ///     var result = await _productService.CreateProductAsync(request);
    ///     return result.ToCreatedAtRouteResult("GetProduct", new { id = result.Value?.Id });
    /// }
    /// </code>
    /// </example>
    public static ActionResult<T> ToCreatedAtRouteResult<T>(this ErrorOr<T> result, string routeName, object? routeValues = null)
    {
        if (result.IsError)
        {
            IActionResult problemResult = result.Errors.ToProblemDetailsActionResult();
            return (ActionResult)problemResult;
        }
        
        return new CreatedAtRouteResult(routeName, routeValues, result.Value);
    }

    /// <summary>
    /// Converts an ErrorOr&lt;Updated&gt; result to a NoContent ActionResult for MVC controllers.
    /// Returns NoContent on success or appropriate error ActionResult with proper HTTP status codes.
    /// </summary>
    /// <param name="result">The ErrorOr&lt;Updated&gt; result to convert</param>
    /// <returns>ActionResult representing either no content (204 No Content) or error response</returns>
    /// <example>
    /// <code>
    /// [HttpPut("{id:int}")]
    /// public async Task&lt;IActionResult&gt; UpdateProduct(int id, UpdateProductRequest request)
    /// {
    ///     var result = await _productService.UpdateProductAsync(id, request);
    ///     return result.ToNoContentResult(); // Returns NoContent or error details
    /// }
    /// </code>
    /// </example>
    public static IActionResult ToNoContentResult(this ErrorOr<Updated> result)
        => result.Match(
            _ => new NoContentResult(),
            errors => errors.ToProblemDetailsActionResult());

    /// <summary>
    /// Converts an ErrorOr&lt;Deleted&gt; result to a NoContent ActionResult for MVC controllers.
    /// Returns NoContent on success or appropriate error ActionResult with proper HTTP status codes.
    /// </summary>
    /// <param name="result">The ErrorOr&lt;Deleted&gt; result to convert</param>
    /// <returns>ActionResult representing either no content (204 No Content) or error response</returns>
    /// <example>
    /// <code>
    /// [HttpDelete("{id:int}")]
    /// public async Task&lt;IActionResult&gt; DeleteProduct(int id)
    /// {
    ///     var result = await _productService.DeleteProductAsync(id);
    ///     return result.ToNoContentResult(); // Returns NoContent or error details
    /// }
    /// </code>
    /// </example>
    public static IActionResult ToNoContentResult(this ErrorOr<Deleted> result)
        => result.Match(
            _ => new NoContentResult(),
            errors => errors.ToProblemDetailsActionResult());

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an Accepted ActionResult for MVC controllers.
    /// Returns Accepted on success or appropriate error ActionResult with proper HTTP status codes.
    /// </summary>
    /// <typeparam name="T">The type of the accepted resource</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="location">Optional URL where the status of the operation can be monitored</param>
    /// <returns>ActionResult representing either accepted response (202 Accepted) or error</returns>
    /// <example>
    /// <code>
    /// [HttpPost("{id:int}/process")]
    /// public async Task&lt;ActionResult&lt;ProcessResult&gt;&gt; ProcessProduct(int id)
    /// {
    ///     var result = await _productService.ProcessProductAsync(id);
    ///     return result.ToAcceptedResult($"/api/products/{id}/status");
    /// }
    /// </code>
    /// </example>
    public static ActionResult<T> ToAcceptedResult<T>(this ErrorOr<T> result, string? location = null)
    {
        if (result.IsError)
        {
            IActionResult problemResult = result.Errors.ToProblemDetailsActionResult();
            return (ActionResult)problemResult;
        }
        
        return location != null 
            ? new AcceptedResult(location, result.Value)
            : new AcceptedResult(location: null, result.Value);
    }

    #endregion

    #region ProblemDetails Conversion

    /// <summary>
    /// Converts a list of errors to an IActionResult with appropriate ProblemDetails for MVC controllers.
    /// Handles validation errors specially by creating ValidationProblemDetails with proper HTTP status codes.
    /// </summary>
    /// <param name="errors">The list of errors to convert</param>
    /// <returns>IActionResult with appropriate HTTP status and ProblemDetails</returns>
    /// <remarks>
    /// Error types mapping:
    /// - Validation → 400 Bad Request with ValidationProblemDetails
    /// - NotFound → 404 Not Found with ProblemDetails
    /// - Unauthorized → 401 Unauthorized with ProblemDetails
    /// - Forbidden → 403 Forbidden with ProblemDetails
    /// - Conflict → 409 Conflict with ProblemDetails
    /// - Failure → 500 Internal Server Error with ProblemDetails
    /// - Unexpected → 422 Unprocessable Entity with ProblemDetails
    /// </remarks>
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

        Error firstError = errors[0];

        if (firstError.Type == ErrorType.Validation)
            return CreateValidationProblemActionResult(errors);

        return CreateProblemActionResult(firstError);
    }

    #endregion

    #region Private Helper Methods

    private static IActionResult CreateValidationProblemActionResult(IReadOnlyList<Error> errors)
    {
        Dictionary<string, string[]> errorsByProperty = errors
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
        int statusCode = GetStatusCode(error.Type);
        ProblemDetails problemDetails = new ProblemDetails
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
        ProblemDetails problemDetails = new ProblemDetails
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
/// Example service layer that returns ErrorOr results for MVC Controller usage
/// </summary>
public sealed class MvcControllerExampleService
{
    public async Task<ErrorOr<Customer>> GetCustomerByIdAsync(int id)
    {
        if (id <= 0)
            return Error.Validation("Customer.Id", "Customer ID must be greater than 0");

        Customer? customer = await FindCustomerInDatabaseAsync(id);
        if (customer == null)
            return Error.NotFound("Customer.NotFound", $"Customer with ID {id} was not found");

        return customer;
    }

    public async Task<ErrorOr<Customer>> CreateCustomerAsync(CreateCustomerRequest request)
    {
        List<Error> validationErrors = ValidateCreateCustomerRequest(request);
        if (validationErrors.Any())
            return validationErrors;

        Customer? existingCustomer = await FindCustomerByEmailAsync(request.Email);
        if (existingCustomer != null)
            return Error.Conflict("Customer.EmailExists", "A customer with this email already exists");

        Customer customer = new Customer(request.Name, request.Email);
        await SaveCustomerAsync(customer);
        return customer;
    }

    public async Task<ErrorOr<Updated>> UpdateCustomerAsync(int id, UpdateCustomerRequest request)
    {
        ErrorOr<Customer> getCustomerResult = await GetCustomerByIdAsync(id);
        if (getCustomerResult.IsError)
            return getCustomerResult.Errors;

        Customer customer = getCustomerResult.Value;
        customer.UpdateName(request.Name);
        await SaveCustomerAsync(customer);
        return Result.Updated;
    }

    public async Task<ErrorOr<Deleted>> DeleteCustomerAsync(int id)
    {
        ErrorOr<Customer> getCustomerResult = await GetCustomerByIdAsync(id);
        if (getCustomerResult.IsError)
            return getCustomerResult.Errors;

        await DeleteCustomerFromDatabaseAsync(id);
        return Result.Deleted;
    }

    public async Task<ErrorOr<List<Customer>>> GetCustomersPagedAsync(int page, int pageSize)
    {
        if (page <= 0)
            return Error.Validation("Page", "Page number must be greater than 0");
        
        if (pageSize <= 0 || pageSize > 100)
            return Error.Validation("PageSize", "Page size must be between 1 and 100");

        List<Customer> customers = await GetCustomersFromDatabaseAsync(page, pageSize);
        return customers;
    }

    private List<Error> ValidateCreateCustomerRequest(CreateCustomerRequest request)
    {
        List<Error> errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add(Error.Validation("Name", "Customer name is required"));

        if (string.IsNullOrWhiteSpace(request.Email))
            errors.Add(Error.Validation("Email", "Email is required"));
        else if (!IsValidEmail(request.Email))
            errors.Add(Error.Validation("Email", "Email format is invalid"));

        return errors;
    }

    // Placeholder methods - implement with your actual data access
    private Task<Customer?> FindCustomerInDatabaseAsync(int id) => throw new NotImplementedException();
    private Task<Customer?> FindCustomerByEmailAsync(string email) => throw new NotImplementedException();
    private Task SaveCustomerAsync(Customer customer) => throw new NotImplementedException();
    private Task DeleteCustomerFromDatabaseAsync(int id) => throw new NotImplementedException();
    private Task<List<Customer>> GetCustomersFromDatabaseAsync(int page, int pageSize) => throw new NotImplementedException();
    private bool IsValidEmail(string email) => throw new NotImplementedException();
}

/// <summary>
/// Comprehensive MVC Controller using ErrorOr ActionResult extensions
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ApiExplorerSettings(IgnoreApi = true)]
internal sealed class CustomersController(MvcControllerExampleService customerService) : ControllerBase
{
    /// <summary>
    /// Get customer by ID
    /// </summary>
    /// <param name="id">The customer ID</param>
    /// <returns>The customer details</returns>
    /// <response code="200">Customer found and returned</response>
    /// <response code="404">Customer not found</response>
    /// <response code="400">Invalid customer ID provided</response>
    [HttpGet("{id:int}", Name = "GetCustomer")]
    [ProducesResponseType(typeof(Customer), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Customer>> GetCustomer(int id)
    {
        ErrorOr<Customer> result = await customerService.GetCustomerByIdAsync(id);
        return result.ToActionResult();
    }

    /// <summary>
    /// Create a new customer
    /// </summary>
    /// <param name="request">Customer creation request</param>
    /// <returns>The created customer</returns>
    /// <response code="201">Customer created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="409">Customer with email already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(Customer), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Customer>> CreateCustomer(CreateCustomerRequest request)
    {
        ErrorOr<Customer> result = await customerService.CreateCustomerAsync(request);
        return result.ToCreatedAtActionResult(nameof(GetCustomer), new { id = result.Value?.Id });
    }

    /// <summary>
    /// Update an existing customer
    /// </summary>
    /// <param name="id">The customer ID</param>
    /// <param name="request">Customer update request</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Customer updated successfully</response>
    /// <response code="404">Customer not found</response>
    /// <response code="400">Invalid request data</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCustomer(int id, UpdateCustomerRequest request)
    {
        ErrorOr<Updated> result = await customerService.UpdateCustomerAsync(id, request);
        return result.ToNoContentResult();
    }

    /// <summary>
    /// Delete a customer
    /// </summary>
    /// <param name="id">The customer ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Customer deleted successfully</response>
    /// <response code="404">Customer not found</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        ErrorOr<Deleted> result = await customerService.DeleteCustomerAsync(id);
        return result.ToNoContentResult();
    }

    /// <summary>
    /// Get paginated list of customers
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of customers</returns>
    /// <response code="200">Customers retrieved successfully</response>
    /// <response code="400">Invalid pagination parameters</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<Customer>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<Customer>>> GetCustomers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        ErrorOr<List<Customer>> result = await customerService.GetCustomersPagedAsync(page, pageSize);
        return result.ToActionResult();
    }

    /// <summary>
    /// Alternative approach using explicit error handling
    /// </summary>
    [HttpGet("{id:int}/alternative")]
    public async Task<IActionResult> GetCustomerAlternative(int id)
    {
        ErrorOr<Customer> result = await customerService.GetCustomerByIdAsync(id);

        return result.Match(
            customer => Ok(customer),
            errors => errors.ToProblemDetailsActionResult()
        );
    }

    /// <summary>
    /// Example with custom validation and response handling
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateCustomer(CreateCustomerRequest request)
    {
        // Additional controller-level validation
        if (string.IsNullOrWhiteSpace(request.Email?.Trim()))
        {
            ModelState.AddModelError(nameof(request.Email), "Email cannot be empty or whitespace");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        ErrorOr<Customer> result = await customerService.CreateCustomerAsync(request);
        
        if (result.IsError)
            return result.Errors.ToProblemDetailsActionResult();

        return CreatedAtAction(nameof(GetCustomer), new { id = result.Value.Id }, result.Value);
    }
}

/// <summary>
/// Example DTOs for the customer endpoints
/// </summary>
public record Customer(int Id, string Name, string Email, DateTime CreatedAt)
{
    public Customer(string name, string email) : this(0, name, email, DateTime.UtcNow) { }
    public Customer UpdateName(string name) => this with { Name = name };
}

public record CreateCustomerRequest(string Name, string Email);
public record UpdateCustomerRequest(string Name);

/// <summary>
/// Example of expected HTTP responses for MVC Controllers
/// </summary>
public static class MvcControllerResponseExamples
{
    /*
    Successful GET /api/customers/1:
    HTTP 200 OK
    Content-Type: application/json
    {
        "id": 1,
        "name": "John Doe",
        "email": "john@example.com",
        "createdAt": "2024-01-15T10:30:00Z"
    }
    
    Not Found GET /api/customers/999:
    HTTP 404 Not Found
    Content-Type: application/problem+json
    {
        "type": "https://httpstatuses.com/404",
        "title": "Customer.NotFound",
        "status": 404,
        "detail": "Customer with ID 999 was not found"
    }
    
    Validation Error POST /api/customers:
    HTTP 400 Bad Request
    Content-Type: application/problem+json
    {
        "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        "title": "Validation Failed",
        "status": 400,
        "errors": {
            "Name": ["Customer name is required"],
            "Email": ["Email is required"]
        }
    }
    
    Successful Creation POST /api/customers:
    HTTP 201 Created
    Location: /api/customers/2
    Content-Type: application/json
    {
        "id": 2,
        "name": "Jane Smith",
        "email": "jane@example.com",
        "createdAt": "2024-01-15T10:30:00Z"
    }
    
    Successful Update PUT /api/customers/1:
    HTTP 204 No Content
    
    Conflict Error POST /api/customers:
    HTTP 409 Conflict
    Content-Type: application/problem+json
    {
        "type": "https://httpstatuses.com/409",
        "title": "Customer.EmailExists",
        "status": 409,
        "detail": "A customer with this email already exists"
    }
    */
}

#endregion