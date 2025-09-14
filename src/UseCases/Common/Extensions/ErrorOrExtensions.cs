using ErrorOr;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using SharedKernel.Models;
using SharedKernel.Models.PagedLists;

namespace UseCases.Common.Extensions;

/// <summary>
/// Main entry point for ErrorOr extensions. This class provides a unified API surface
/// by combining specialized extension classes for different response types.
/// </summary>
/// <remarks>
/// This class serves as the main entry point for all ErrorOr conversion extensions.
/// For specific implementations, see:
/// - <see cref="ErrorOrTypedResultsExtensions"/> for Minimal API TypedResults
/// - <see cref="ErrorOrActionResultExtensions"/> for MVC Controller ActionResults  
/// - <see cref="ErrorOrApiResponseExtensions"/> for standardized ApiResponse wrappers
/// 
/// All extensions preserve HTTP status codes appropriately for their target response type.
/// </remarks>
/// <example>
/// Quick reference for all three approaches:
/// 
/// Minimal API (TypedResults):
/// <code>
/// app.MapGet("/products/{id}", async (int id, IProductService service) =>
/// {
///     var result = await service.GetProductAsync(id);
///     return result.ToTypedResult(); // Returns appropriate HTTP status directly
/// });
/// </code>
/// 
/// MVC Controller (ActionResult):
/// <code>
/// [HttpGet("{id:int}")]
/// public async Task&lt;ActionResult&lt;Product&gt;&gt; GetProduct(int id)
/// {
///     var result = await _productService.GetProductAsync(id);
///     return result.ToActionResult(); // Returns appropriate HTTP status directly
/// }
/// </code>
/// 
/// Standardized API Response (ApiResponse):
/// <code>
/// [HttpGet("{id:int}")]
/// public async Task&lt;ActionResult&lt;ApiResponse&lt;Product&gt;&gt;&gt; GetProduct(int id)
/// {
///     var result = await _productService.GetProductAsync(id);
///     var apiResponse = result.ToApiResponse("Product retrieved successfully");
///     return Ok(apiResponse); // Always 200 OK, status preserved in apiResponse.StatusCode
/// }
/// </code>
/// </example>
public static class ErrorOrExtensions
{
    // This class serves as a documentation and entry point hub.
    // All actual implementations are in the specialized extension files:
    // - ErrorOrTypedResultsExtensions.cs
    // - ErrorOrActionResultExtensions.cs  
    // - ErrorOrApiResponseExtensions.cs
}

#region Documentation and Usage Guide

/// <summary>
/// Comprehensive usage guide for ErrorOr extensions in different scenarios.
/// </summary>
/// <remarks>
/// Choose the appropriate extension method based on your API design:
/// 
/// ## 1. Minimal API with Direct HTTP Status Codes
/// Use when you want HTTP response codes to directly reflect the operation result.
/// Client receives actual HTTP status (200, 404, 400, etc.).
/// 
/// ## 2. MVC Controllers with Direct HTTP Status Codes  
/// Use when you want HTTP response codes to directly reflect the operation result.
/// Client receives actual HTTP status (200, 404, 400, etc.).
/// 
/// ## 3. Standardized API Response Wrapper
/// Use when you want consistent response structure across all endpoints.
/// Client always receives 200 OK, but can check apiResponse.StatusCode and apiResponse.IsSuccess.
/// 
/// ### When to Use Each Approach:
/// 
/// **Direct HTTP Status (TypedResults/ActionResult):**
/// - RESTful APIs following HTTP semantics strictly
/// - APIs consumed by HTTP clients that rely on status codes
/// - Simple APIs without complex error handling requirements
/// - Integration with existing systems expecting standard HTTP responses
/// 
/// **ApiResponse Wrapper:**
/// - Enterprise APIs requiring consistent response structure
/// - APIs with complex error scenarios and metadata
/// - Single Page Applications (SPAs) that prefer consistent parsing
/// - APIs requiring additional metadata (pagination, links, timing info)
/// - Mobile applications that benefit from structured responses
/// - APIs requiring request tracing and debugging information
/// 
/// ### Performance Considerations:
/// - All extension methods use frozen dictionaries for optimal performance
/// - ApiResponse approach has slightly more overhead due to wrapper object
/// - Direct HTTP status approach is more lightweight
/// - Choose based on your specific requirements and client needs
/// </remarks>
public static class ErrorOrExtensionsUsageGuide
{
    /*
    ## Example Responses for Each Approach:

    ### 1. Direct HTTP Status (Minimal API/Controller)
    
    Success: GET /api/products/1
    HTTP 200 OK
    {
        "id": 1,
        "name": "iPhone 15",
        "price": 999.99
    }
    
    Error: GET /api/products/999  
    HTTP 404 Not Found
    {
        "type": "https://httpstatuses.com/404",
        "title": "Product.NotFound", 
        "status": 404,
        "detail": "Product with ID 999 was not found"
    }

    ### 2. ApiResponse Wrapper
    
    Success: GET /api/products/1
    HTTP 200 OK
    {
        "isSuccess": true,
        "data": {
            "id": 1,
            "name": "iPhone 15", 
            "price": 999.99
        },
        "message": "Product retrieved successfully",
        "timestamp": "2024-01-15T10:30:00Z",
        "apiVersion": "1.0",
        "statusCode": 200
    }
    
    Error: GET /api/products/999
    HTTP 200 OK  
    {
        "isSuccess": false,
        "data": null,
        "message": "Product with ID 999 was not found",
        "timestamp": "2024-01-15T10:30:00Z", 
        "apiVersion": "1.0",
        "statusCode": 404
    }

    ## Migration Path:
    
    1. Start with direct HTTP status approach for simple scenarios
    2. Gradually adopt ApiResponse wrapper for complex scenarios
    3. Both approaches can coexist in the same application
    4. Use route prefixes to distinguish API styles (e.g., /api/v1/ vs /api/v2/)
    */
}

#endregion
