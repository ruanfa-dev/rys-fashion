using System.Collections.Frozen;

using ErrorOr;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace UseCases.Common.Extensions;

/// <summary>
/// Extension methods for converting ErrorOr results to TypedResults for Minimal APIs.
/// Provides seamless integration between ErrorOr library and ASP.NET Core Minimal APIs.
/// </summary>
/// <remarks>
/// This class focuses specifically on Minimal API TypedResults conversion with proper HTTP status codes.
/// All methods return appropriate IResult types with correct status codes for API responses.
/// 
/// Key Features:
/// - Automatic error-to-HTTP status code mapping
/// - RFC 7807 compliant ProblemDetails responses
/// - Validation error handling with structured ValidationProblemDetails
/// - Performance optimized with frozen dictionaries
/// </remarks>
/// <example>
/// Basic usage in Minimal APIs:
/// <code>
/// app.MapGet("/products/{id}", async (int id, IProductService service) =>
/// {
///     var result = await service.GetProductAsync(id);
///     return result.ToTypedResult(); // Returns 200 OK with product or 404 Not Found
/// });
/// </code>
/// </example>
public static class ErrorOrTypedResultsExtensions
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

    #region Core TypedResults Extensions

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an IResult for minimal APIs.
    /// Returns Ok(value) on success or ProblemDetails on error with appropriate HTTP status codes.
    /// </summary>
    /// <typeparam name="T">The type of the success result</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <returns>IResult representing either success (200 OK) or error response with appropriate status code</returns>
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
    /// Returns Created(location, value) on success or ProblemDetails on error with appropriate HTTP status codes.
    /// </summary>
    /// <typeparam name="T">The type of the created resource</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="locationUrl">The URL of the created resource</param>
    /// <returns>IResult representing either created response (201 Created) or error with appropriate status code</returns>
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
    /// Returns 204 No Content on success or ProblemDetails on error with appropriate HTTP status codes.
    /// </summary>
    /// <param name="result">The ErrorOr&lt;Updated&gt; result to convert</param>
    /// <returns>IResult representing either no content (204 No Content) or error response</returns>
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
    /// Returns 204 No Content on success or ProblemDetails on error with appropriate HTTP status codes.
    /// </summary>
    /// <param name="result">The ErrorOr&lt;Deleted&gt; result to convert</param>
    /// <returns>IResult representing either no content (204 No Content) or error response</returns>
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

    /// <summary>
    /// Converts an ErrorOr&lt;T&gt; result to an Accepted response for minimal APIs.
    /// Returns 202 Accepted on success or ProblemDetails on error with appropriate HTTP status codes.
    /// </summary>
    /// <typeparam name="T">The type of the accepted resource</typeparam>
    /// <param name="result">The ErrorOr result to convert</param>
    /// <param name="locationUrl">Optional URL where the status of the operation can be monitored</param>
    /// <returns>IResult representing either accepted response (202 Accepted) or error</returns>
    /// <example>
    /// <code>
    /// app.MapPost("/users/{id}/process", async (int id, IUserService service) =>
    /// {
    ///     var result = await service.ProcessUserAsync(id);
    ///     return result.ToTypedResultAccepted($"/users/{id}/status");
    /// });
    /// </code>
    /// </example>
    public static IResult ToTypedResultAccepted<T>(this ErrorOr<T> result, string? locationUrl = null)
        => result.Match(
            value => locationUrl != null 
                ? TypedResults.Accepted(locationUrl, value)
                : TypedResults.Accepted(uri: (string?)null, value),
            ToProblemDetails);

    #endregion

    #region ProblemDetails Conversion

    /// <summary>
    /// Converts a list of errors to an IResult with appropriate ProblemDetails and HTTP status codes.
    /// Handles validation errors specially by grouping them by property name.
    /// </summary>
    /// <param name="errors">The list of errors to convert</param>
    /// <returns>IResult with appropriate HTTP status and ProblemDetails</returns>
    /// <remarks>
    /// Error types mapping:
    /// - Validation → 400 Bad Request with ValidationProblemDetails
    /// - NotFound → 404 Not Found
    /// - Unauthorized → 401 Unauthorized
    /// - Forbidden → 403 Forbidden
    /// - Conflict → 409 Conflict
    /// - Failure → 500 Internal Server Error
    /// - Unexpected → 422 Unprocessable Entity
    /// </remarks>
    public static IResult ToProblemDetails(IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0)
            return Results.Problem("An unknown error occurred.", statusCode: DefaultStatusCode);

        Error firstError = errors[0];

        if (firstError.Type == ErrorType.Validation)
            return CreateValidationProblem(errors);

        int statusCode = GetStatusCode(firstError.Type);
        return Results.Problem(
            title: firstError.Code,
            detail: firstError.Description,
            statusCode: statusCode,
            type: GetProblemTypeUri(statusCode));
    }

    #endregion

    #region Private Helper Methods

    private static IResult CreateValidationProblem(IReadOnlyList<Error> errors)
    {
        Dictionary<string, string[]> errorsByProperty = errors
            .ToLookup(e => e.Code, e => e.Description)
            .ToDictionary(g => g.Key, g => g.ToArray());

        return Results.ValidationProblem(
            errorsByProperty,
            title: "Validation Failed",
            type: ValidationProblemType);
    }

    private static int GetStatusCode(ErrorType type)
        => ErrorTypeToStatusCode.GetValueOrDefault(type, DefaultStatusCode);

    private static string GetProblemTypeUri(int statusCode)
        => $"https://httpstatuses.com/{statusCode}";

    #endregion
}

#region Usage Examples

/// <summary>
/// Example service layer that returns ErrorOr results for Minimal API usage
/// </summary>
public sealed class TypedResultsExampleService
{
    public async Task<ErrorOr<TestProductModel>> GetProductByIdAsync(int id)
    {
        if (id <= 0)
            return Error.Validation("Product.Id", "Product ID must be greater than 0");

        TestProductModel? product = await FindProductInDatabaseAsync(id);
        if (product == null)
            return Error.NotFound("Product.NotFound", $"Product with ID {id} was not found");

        return product;
    }

    public async Task<ErrorOr<TestProductModel>> CreateProductAsync(CreateProductRequest request)
    {
        List<Error> validationErrors = ValidateCreateProductRequest(request);
        if (validationErrors.Any())
            return validationErrors;

        TestProductModel? existingProduct = await FindProductByNameAsync(request.Name);
        if (existingProduct != null)
            return Error.Conflict("Product.NameExists", "A product with this name already exists");

        TestProductModel product = new TestProductModel(request.Name, request.Price);
        await SaveProductAsync(product);
        return product;
    }

    public async Task<ErrorOr<Updated>> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        ErrorOr<TestProductModel> getProductResult = await GetProductByIdAsync(id);
        if (getProductResult.IsError)
            return getProductResult.Errors;

        TestProductModel product = getProductResult.Value;
        product.UpdateName(request.Name);
        await SaveProductAsync(product);
        return Result.Updated;
    }

    public async Task<ErrorOr<Deleted>> DeleteProductAsync(int id)
    {
        ErrorOr<TestProductModel> getProductResult = await GetProductByIdAsync(id);
        if (getProductResult.IsError)
            return getProductResult.Errors;

        await DeleteProductFromDatabaseAsync(id);
        return Result.Deleted;
    }

    private List<Error> ValidateCreateProductRequest(CreateProductRequest request)
    {
        List<Error> errors = [];

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add(Error.Validation("Name", "Product name is required"));

        if (request.Price <= 0)
            errors.Add(Error.Validation("Price", "Product price must be greater than 0"));

        return errors;
    }

    // Placeholder methods - implement with your actual data access
    private Task<TestProductModel?> FindProductInDatabaseAsync(int id) => throw new NotImplementedException();
    private Task<TestProductModel?> FindProductByNameAsync(string name) => throw new NotImplementedException();
    private Task SaveProductAsync(TestProductModel product) => throw new NotImplementedException();
    private Task DeleteProductFromDatabaseAsync(int id) => throw new NotImplementedException();
}

/// <summary>
/// Comprehensive Minimal API endpoints using ErrorOr TypedResults extensions
/// </summary>
public static class TypedResultsApiExamples
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        RouteGroupBuilder products = app.MapGroup("/api/products")
            .WithTags("Products")
            .WithOpenApi();

        // GET /api/products/{id} - Returns 200 OK with product or 404 Not Found
        products.MapGet("/{id:int}", async (int id, TypedResultsExampleService productService) =>
        {
            ErrorOr<TestProductModel> result = await productService.GetProductByIdAsync(id);
            return result.ToTypedResult();
        })
        .WithName("GetProduct")
        .WithSummary("Get product by ID")
        .WithDescription("Retrieves a product by its unique identifier")
        .Produces<TestProductModel>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();

        // POST /api/products - Returns 201 Created or 400/409 for errors
        products.MapPost("/", async (CreateProductRequest request, TypedResultsExampleService productService) =>
        {
            ErrorOr<TestProductModel> result = await productService.CreateProductAsync(request);
            return result.ToTypedResultCreated($"/api/products/{result.Value?.Id}");
        })
        .WithName("CreateProduct")
        .WithSummary("Create a new product")
        .WithDescription("Creates a new product in the system")
        .Produces<TestProductModel>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesValidationProblem();

        // PUT /api/products/{id} - Returns 204 No Content or error details
        products.MapPut("/{id:int}", async (int id, UpdateProductRequest request, TypedResultsExampleService productService) =>
        {
            ErrorOr<Updated> result = await productService.UpdateProductAsync(id, request);
            return result.ToTypedResultNoContent();
        })
        .WithName("UpdateProduct")
        .WithSummary("Update an existing product")
        .WithDescription("Updates an existing product's information")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();

        // DELETE /api/products/{id} - Returns 204 No Content or error details
        products.MapDelete("/{id:int}", async (int id, TypedResultsExampleService productService) =>
        {
            ErrorOr<Deleted> result = await productService.DeleteProductAsync(id);
            return result.ToTypedResultDeleted();
        })
        .WithName("DeleteProduct")
        .WithSummary("Delete a product")
        .WithDescription("Permanently deletes a product from the system")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // GET /api/products/search - Example with query parameters (synchronous)
        products.MapGet("/search", (string? name, decimal? minPrice, decimal? maxPrice, TypedResultsExampleService productService) =>
        {
            // Simulate search logic
            if (string.IsNullOrWhiteSpace(name) && !minPrice.HasValue && !maxPrice.HasValue)
            {
                Error emptySearchError = Error.Validation("Search.Empty", "At least one search parameter is required");
                return Results.BadRequest(emptySearchError);
            }

            // Your search implementation here
            List<TestProductModel> searchResults = [];
            return Results.Ok(searchResults);
        })
        .WithName("SearchProducts")
        .WithSummary("Search products")
        .WithDescription("Search products by name and price range")
        .Produces<List<TestProductModel>>(StatusCodes.Status200OK)
        .ProducesValidationProblem();
    }
}

/// <summary>
/// Example DTOs for the product endpoints
/// </summary>
public record TestProductModel(int Id, string Name, decimal Price)
{
    public TestProductModel(string name, decimal price) : this(0, name, price) { }
    public TestProductModel UpdateName(string name) => this with { Name = name };
}

public record CreateProductRequest(string Name, decimal Price);
public record UpdateProductRequest(string Name, decimal Price);

/// <summary>
/// Example of expected HTTP responses for Minimal APIs
/// </summary>
public static class TypedResultsResponseExamples
{
    /*
    Successful GET /api/products/1:
    HTTP 200 OK
    Content-Type: application/json
    {
        "id": 1,
        "name": "iPhone 15",
        "price": 999.99
    }
    
    Not Found GET /api/products/999:
    HTTP 404 Not Found
    Content-Type: application/problem+json
    {
        "type": "https://httpstatuses.com/404",
        "title": "Product.NotFound",
        "status": 404,
        "detail": "Product with ID 999 was not found"
    }
    
    Validation Error POST /api/products:
    HTTP 400 Bad Request
    Content-Type: application/problem+json
    {
        "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        "title": "Validation Failed",
        "status": 400,
        "errors": {
            "Name": ["Product name is required"],
            "Price": ["Product price must be greater than 0"]
        }
    }
    
    Successful Creation POST /api/products:
    HTTP 201 Created
    Location: /api/products/2
    Content-Type: application/json
    {
        "id": 2,
        "name": "MacBook Pro",
        "price": 2499.99
    }
    
    Successful Update PUT /api/products/1:
    HTTP 204 No Content
    
    Conflict Error POST /api/products:
    HTTP 409 Conflict
    Content-Type: application/problem+json
    {
        "type": "https://httpstatuses.com/409",
        "title": "Product.NameExists",
        "status": 409,
        "detail": "A product with this name already exists"
    }
    */
}

#endregion