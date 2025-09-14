# ErrorOr Extensions Organization

This document explains the organization of ErrorOr extension methods into specialized files for better maintainability and clarity.

## File Structure

```
src/UseCases/Common/Extensions/
??? ErrorOrExtensions.cs                    # Main entry point with quick access methods
??? ErrorOrTypedResultsExtensions.cs       # Minimal API TypedResults extensions  
??? ErrorOrActionResultExtensions.cs       # MVC Controller ActionResult extensions
??? ErrorOrApiResponseExtensions.cs        # Standardized ApiResponse wrapper extensions
```

## 1. ErrorOrTypedResultsExtensions.cs

**Purpose**: Minimal API extensions that return direct HTTP status codes.

**Key Features**:
- Returns appropriate HTTP status codes directly (200, 201, 204, 404, 400, etc.)
- RFC 7807 compliant ProblemDetails for errors
- Optimized for Minimal APIs using TypedResults
- Performance focused with frozen dictionaries

**When to Use**:
- Minimal APIs following REST principles strictly
- Simple APIs where HTTP status codes directly represent operation results
- Integration with HTTP clients that rely on status codes

**Example**:
```csharp
app.MapGet("/products/{id}", async (int id, IProductService service) =>
{
    var result = await service.GetProductAsync(id);
    return result.ToTypedResult(); // Returns 200 OK or 404 Not Found directly
});
```

**Response Example**:
```json
// Success: HTTP 200 OK
{
    "id": 1,
    "name": "iPhone 15",
    "price": 999.99
}

// Error: HTTP 404 Not Found
{
    "type": "https://httpstatuses.com/404",
    "title": "Product.NotFound",
    "status": 404,
    "detail": "Product with ID 1 was not found"
}
```

## 2. ErrorOrActionResultExtensions.cs

**Purpose**: MVC Controller extensions that return direct HTTP status codes.

**Key Features**:
- Returns ActionResult with appropriate HTTP status codes
- Supports CreatedAtAction and CreatedAtRoute patterns
- ValidationProblemDetails for validation errors
- Full MVC Controller integration

**When to Use**:
- MVC Controllers following REST principles
- Applications requiring ActionResult flexibility
- Complex routing scenarios with CreatedAtAction

**Example**:
```csharp
[HttpGet("{id:int}")]
public async Task<ActionResult<Product>> GetProduct(int id)
{
    var result = await _productService.GetProductAsync(id);
    return result.ToActionResult(); // Returns Ok(product) or NotFound with ProblemDetails
}

[HttpPost]
public async Task<ActionResult<Product>> CreateProduct(CreateProductRequest request)
{
    var result = await _productService.CreateProductAsync(request);
    return result.ToCreatedAtActionResult(nameof(GetProduct), new { id = result.Value?.Id });
}
```

**Response Example**:
```json
// Success: HTTP 200 OK
{
    "id": 1,
    "name": "iPhone 15",
    "price": 999.99
}

// Error: HTTP 404 Not Found  
{
    "type": "https://httpstatuses.com/404",
    "title": "Product.NotFound",
    "status": 404,
    "detail": "Product with ID 1 was not found"
}
```

## 3. ErrorOrApiResponseExtensions.cs

**Purpose**: Standardized ApiResponse wrapper extensions with preserved status codes.

**Key Features**:
- Always returns HTTP 200 OK with ApiResponse wrapper
- Preserves actual status codes in `apiResponse.StatusCode`
- Supports pagination metadata, HATEOAS links, and custom metadata
- E-commerce specific extensions (Product, Cart responses)
- Consistent response structure across all endpoints

**When to Use**:
- Enterprise APIs requiring consistent response structure
- SPAs that prefer structured responses for easier parsing
- APIs requiring additional metadata (pagination, request tracing)
- Mobile applications benefiting from consistent response format
- Complex error scenarios with detailed error information

**Example**:
```csharp
[HttpGet("{id:int}")]
public async Task<ActionResult<ApiResponse<Product>>> GetProduct(int id)
{
    var result = await _productService.GetProductAsync(id);
    var apiResponse = result.ToApiResponse("Product retrieved successfully");
    return Ok(apiResponse); // Always 200 OK, real status in apiResponse.StatusCode
}

[HttpGet]
public async Task<ActionResult<ApiResponse<List<Product>>>> GetProducts(int page, int pageSize)
{
    var result = await _productService.GetProductsPagedAsync(page, pageSize);
    var apiResponse = result.ToApiResponsePaged("Products retrieved successfully");
    return Ok(apiResponse); // Includes pagination metadata
}
```

**Response Example**:
```json
// Success: HTTP 200 OK
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
    "statusCode": 200,
    "requestId": "abc123"
}

// Error: HTTP 200 OK (Note: Always 200, but statusCode shows real status)
{
    "isSuccess": false,
    "data": null,
    "message": "Product with ID 1 was not found",
    "timestamp": "2024-01-15T10:30:00Z",
    "apiVersion": "1.0", 
    "statusCode": 404,
    "requestId": "def456"
}

// Paginated Response: HTTP 200 OK
{
    "isSuccess": true,
    "data": [
        {"id": 1, "name": "iPhone 15", "price": 999.99},
        {"id": 2, "name": "MacBook Pro", "price": 2499.99}
    ],
    "message": "Products retrieved successfully",
    "pagination": {
        "currentPage": 1,
        "pageSize": 10,
        "totalItems": 25,
        "totalPages": 3,
        "hasPrevious": false,
        "hasNext": true
    },
    "timestamp": "2024-01-15T10:30:00Z",
    "apiVersion": "1.0",
    "statusCode": 200
}
```

## 4. ErrorOrExtensions.cs

**Purpose**: Main entry point providing unified API surface and quick access methods.

**Key Features**:
- Provides quick access to all extension methods
- Serves as main entry point for developers
- Comprehensive documentation and usage examples
- Migration guidance between approaches

**Example**:
```csharp
// All these work through the main entry point:
return result.ToTypedResult();           // Minimal API
return result.ToActionResult();          // MVC Controller  
return result.ToApiResponse();           // ApiResponse wrapper
```

## Decision Matrix

| Scenario | Recommended Approach | File to Use |
|----------|---------------------|-------------|
| Simple REST API | Direct HTTP Status | TypedResults or ActionResult |
| Enterprise API | ApiResponse Wrapper | ApiResponse |
| Mobile App Backend | ApiResponse Wrapper | ApiResponse |
| SPA Backend | ApiResponse Wrapper | ApiResponse |
| Integration API | Direct HTTP Status | TypedResults or ActionResult |
| Microservice | Direct HTTP Status | TypedResults or ActionResult |
| Public API | Direct HTTP Status | TypedResults or ActionResult |
| Internal API | Either (based on team preference) | Any |

## Error Type Mapping

All approaches use consistent error type to HTTP status code mapping:

| ErrorType | HTTP Status Code | Description |
|-----------|------------------|-------------|
| Validation | 400 Bad Request | Input validation failed |
| Unauthorized | 401 Unauthorized | Authentication required |
| Forbidden | 403 Forbidden | Access denied |
| NotFound | 404 Not Found | Resource not found |
| Conflict | 409 Conflict | Resource conflict |
| Failure | 500 Internal Server Error | Server error |
| Unexpected | 422 Unprocessable Entity | Unexpected error |

## Migration Strategy

1. **Start Simple**: Begin with direct HTTP status approaches for straightforward APIs
2. **Identify Complexity**: Move to ApiResponse wrapper when you need:
   - Consistent response structure
   - Additional metadata
   - Complex error scenarios
   - Request tracing
3. **Coexistence**: Both approaches can coexist in the same application
4. **Gradual Migration**: Migrate endpoints incrementally based on requirements

## Performance Considerations

- **TypedResults**: Fastest, minimal overhead
- **ActionResult**: Slight overhead for MVC features
- **ApiResponse**: Additional overhead for wrapper object and metadata
- All approaches use frozen dictionaries for optimal lookup performance
- Choose based on feature requirements vs. performance needs

## Best Practices

1. **Consistency**: Pick one approach per API version/area
2. **Documentation**: Document which approach is used where
3. **Client Expectations**: Ensure clients understand the response format
4. **Error Handling**: Provide comprehensive error information
5. **Status Codes**: Use appropriate HTTP status codes consistently
6. **Request Tracing**: Use requestId parameter for debugging
7. **Versioning**: Consider using API versioning when migrating approaches

## Examples Repository

Each extension file includes comprehensive examples showing:
- Basic usage patterns
- Advanced scenarios
- Integration with services
- Complete controller/endpoint implementations
- Expected JSON responses
- Error handling patterns

This organization provides clear separation of concerns while maintaining ease of use through the unified entry point.