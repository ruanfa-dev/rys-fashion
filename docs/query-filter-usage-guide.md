# QueryFilter System - Usage Guide

A comprehensive guide for using the QueryFilter system in Rys.Fashion for building dynamic, type-safe filtering capabilities in Minimal APIs and Controllers.

## Table of Contents

1. [Overview](#overview)
2. [Core Components](#core-components)
3. [Basic Usage](#basic-usage)
4. [Minimal API Integration](#minimal-api-integration)
5. [Controller Integration](#controller-integration)
6. [Dependency Injection Setup](#dependency-injection-setup)
7. [Query Parameter Formats](#query-parameter-formats)
8. [Fluent Builder API](#fluent-builder-api)
9. [Advanced Features](#advanced-features)
10. [SPA Integration](#spa-integration)
11. [Best Practices](#best-practices)
12. [Examples](#examples)

## Overview

The QueryFilter system provides a powerful, flexible way to handle dynamic filtering in your APIs. It supports:

- **Multiple Filter Operators**: Equal, Contains, GreaterThan, LessThan, In, Range, IsNull, etc.
- **Logical Operations**: AND/OR combinations with grouping support
- **Type Safety**: Expression-based filtering with compile-time safety
- **Multiple Input Formats**: Support for different query parameter formats
- **Fluent Builder API**: Programmatic filter construction
- **Performance**: Cached reflection and optimized expression building

## Core Components

### FilterOperator Enum
```csharp
public enum FilterOperator
{
    Equal, NotEqual,
    LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual,
    IsNull, IsNotNull,
    Contains, NotContains, StartsWith, EndsWith,
    In, NotIn,
    Range
}
```

### FilterLogicalOperator Enum
```csharp
public enum FilterLogicalOperator
{
    All,  // AND logic
    Any   // OR logic
}
```

### QueryFilterParameter Class
```csharp
public sealed class QueryFilterParameter
{
    public required string Field { get; set; }
    public FilterOperator Operator { get; set; }
    public required string Value { get; set; }
    public FilterLogicalOperator LogicalOperator { get; set; }
    public int? Group { get; set; }
}
```

## Basic Usage

### 1. Using Extension Methods Directly

```csharp
// Apply filters from HTTP query parameters dictionary
var filteredUsers = users.ApplyFilters(queryParams);

// Apply filters from query string directly
var queryString = "name[contains]=john&age[gte]=25&department[eq]=IT";
var filteredUsers = users.ApplyFilters(queryString);

// Apply filters from parameter list
var filters = new List<QueryFilterParameter>
{
    new() { Field = "Name", Operator = FilterOperator.Contains, Value = "John" },
    new() { Field = "Age", Operator = FilterOperator.GreaterThan, Value = "25" }
};
var filteredUsers = users.ApplyFilters(filters);
```

### 2. Using Fluent Builder API

```csharp
var filteredUsers = QueryFilterBuilder.Create()
    .Equal("Department", "IT")
    .And("IsActive", FilterOperator.Equal, "true")
    .Or("Age", FilterOperator.GreaterThan, "30")
    .ApplyTo(users);
```

## Minimal API Integration

### Simple Filtering Endpoint

```csharp
// Program.cs or endpoint configuration
app.MapGet("/api/users", async (
    [FromServices] IUserService userService,
    [AsParameters] UserFilterRequest request) =>
{
    var users = await userService.GetUsersAsync();
    
    // Apply filters from query parameters
    var filteredUsers = users.ApplyFilters(request.ToQueryParams());
    
    return Results.Ok(filteredUsers);
});

// Request model for binding query parameters
public class UserFilterRequest
{
    public string? Name { get; set; }
    public string? Department { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public bool? IsActive { get; set; }
    
    public Dictionary<string, string> ToQueryParams()
    {
        var queryParams = new Dictionary<string, string>();
        
        if (!string.IsNullOrEmpty(Name))
            queryParams["name[contains]"] = Name;
            
        if (!string.IsNullOrEmpty(Department))
            queryParams["department[eq]"] = Department;
            
        if (MinAge.HasValue)
            queryParams["age[gte]"] = MinAge.Value.ToString();
            
        if (MaxAge.HasValue)
            queryParams["age[lte]"] = MaxAge.Value.ToString();
            
        if (IsActive.HasValue)
            queryParams["isActive[eq]"] = IsActive.Value.ToString();
            
        return queryParams;
    }
}
```

### Advanced Minimal API with Direct Query Parameter Parsing

```csharp
app.MapGet("/api/products", async (
    HttpContext context,
    [FromServices] IProductService productService) =>
{
    // Extract query parameters from HttpContext
    var queryParams = context.Request.Query
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
    
    var products = await productService.GetProductsAsync();
    
    // Apply filters using query parameter parsing
    var filteredProducts = products.ApplyFilters(queryParams);
    
    return Results.Ok(filteredProducts);
});
```

### Direct Query String Filtering

```csharp
app.MapGet("/api/products/filter", async (
    [FromServices] IProductService productService,
    string? filter) =>
{
    var products = await productService.GetProductsAsync();
    
    // Apply filters directly from query string
    if (!string.IsNullOrEmpty(filter))
    {
        var filteredProducts = products.ApplyFilters(filter);
        return Results.Ok(filteredProducts);
    }
    
    return Results.Ok(products);
});

// Alternative approach using HttpContext.Request.QueryString
app.MapGet("/api/users/search", async (
    HttpContext context,
    [FromServices] IUserService userService) =>
{
    var users = await userService.GetUsersAsync();
    
    // Get the raw query string and apply filters directly
    var queryString = context.Request.QueryString.Value?.TrimStart('?') ?? string.Empty;
    var filteredUsers = users.ApplyFilters(queryString);
    
    return Results.Ok(filteredUsers);
});
```

### Using QueryFilterBuilder in Minimal API

```csharp
app.MapGet("/api/orders/search", async (
    [FromServices] IOrderService orderService,
    string? customerName,
    DateTime? fromDate,
    DateTime? toDate,
    string? status) =>
{
    var orders = await orderService.GetOrdersAsync();
    
    var builder = QueryFilterBuilder.Create();
    
    if (!string.IsNullOrEmpty(customerName))
        builder.Contains("CustomerName", customerName);
        
    if (fromDate.HasValue)
        builder.GreaterThanOrEqual("OrderDate", fromDate.Value.ToString("yyyy-MM-dd"));
        
    if (toDate.HasValue)
        builder.LessThanOrEqual("OrderDate", toDate.Value.ToString("yyyy-MM-dd"));
        
    if (!string.IsNullOrEmpty(status))
        builder.Equal("Status", status);
    
    var filteredOrders = builder.ApplyTo(orders);
    
    return Results.Ok(filteredOrders);
});
```

## Controller Integration

### Basic Controller with Filtering

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] UserFilterRequest request)
    {
        var users = await _userService.GetUsersAsync();
        
        // Convert request to query parameters and apply filters
        var filteredUsers = users.ApplyFilters(request.ToQueryParams());
        
        return Ok(filteredUsers);
    }
    
    [HttpPost("search")]
    public async Task<IActionResult> SearchUsers([FromBody] UserSearchRequest request)
    {
        var users = await _userService.GetUsersAsync();
        
        // Use fluent builder for complex search
        var builder = QueryFilterBuilder.Create(request.LogicalOperator);
        
        foreach (var criteria in request.Criteria)
        {
            builder.Add(criteria.Field, criteria.Operator, criteria.Value);
        }
        
        var filteredUsers = builder.ApplyTo(users);
        
        return Ok(filteredUsers);
    }
}

// Request models
public class UserSearchRequest
{
    public FilterLogicalOperator LogicalOperator { get; set; } = FilterLogicalOperator.All;
    public List<SearchCriteria> Criteria { get; set; } = new();
}

public class SearchCriteria
{
    public string Field { get; set; } = string.Empty;
    public FilterOperator Operator { get; set; }
    public string Value { get; set; } = string.Empty;
}
```

### Advanced Controller with Custom Model Binding

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    
    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        // Parse query parameters directly from HttpContext
        var queryParams = HttpContext.Request.Query
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
        
        var products = await _productService.GetProductsAsync();
        var filteredProducts = products.ApplyFilters(queryParams);
        
        return Ok(filteredProducts);
    }
    
    [HttpGet("filter-by-string")]
    public async Task<IActionResult> GetProductsByString([FromQuery] string? filter)
    {
        var products = await _productService.GetProductsAsync();
        
        // Apply filters directly from query string parameter
        if (!string.IsNullOrEmpty(filter))
        {
            var filteredProducts = products.ApplyFilters(filter);
            return Ok(filteredProducts);
        }
        
        return Ok(products);
    }
    
    [HttpGet("filter-from-query")]
    public async Task<IActionResult> GetProductsFromQuery()
    {
        var products = await _productService.GetProductsAsync();
        
        // Use the raw query string directly
        var queryString = HttpContext.Request.QueryString.Value?.TrimStart('?') ?? string.Empty;
        var filteredProducts = products.ApplyFilters(queryString);
        
        return Ok(filteredProducts);
    }
    
    [HttpGet("advanced-search")]
    public async Task<IActionResult> AdvancedSearch(
        [FromQuery] string? category,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] bool? inStock,
        [FromQuery] string[]? tags)
    {
        var products = await _productService.GetProductsAsync();
        
        var builder = QueryFilterBuilder.Create();
        
        if (!string.IsNullOrEmpty(category))
            builder.Equal("Category", category);
            
        if (minPrice.HasValue && maxPrice.HasValue)
            builder.Range("Price", minPrice.Value.ToString(), maxPrice.Value.ToString());
        else if (minPrice.HasValue)
            builder.GreaterThanOrEqual("Price", minPrice.Value.ToString());
        else if (maxPrice.HasValue)
            builder.LessThanOrEqual("Price", maxPrice.Value.ToString());
            
        if (inStock.HasValue)
            builder.Equal("InStock", inStock.Value.ToString());
            
        if (tags?.Length > 0)
            builder.In("Tags", tags);
        
        var filteredProducts = builder.ApplyTo(products);
        
        return Ok(filteredProducts);
    }
}
```

## Dependency Injection Setup

### Service Registration

```csharp
// Program.cs or Startup.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddQueryFiltering(this IServiceCollection services)
    {
        // Register any filtering-related services
        services.AddScoped<IQueryFilterService, QueryFilterService>();
        
        // Configure JSON options for filter enums
        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        
        return services;
    }
}

// In Program.cs
builder.Services.AddQueryFiltering();
```

### Custom Filter Service

```csharp
public interface IQueryFilterService
{
    IQueryable<T> ApplyFilters<T>(IQueryable<T> query, Dictionary<string, string> queryParams);
    QueryFilterBuilder CreateBuilder(FilterLogicalOperator defaultOperator = FilterLogicalOperator.All);
}

public class QueryFilterService : IQueryFilterService
{
    public IQueryable<T> ApplyFilters<T>(IQueryable<T> query, Dictionary<string, string> queryParams)
    {
        return query.ApplyFilters(queryParams);
    }
    
    public QueryFilterBuilder CreateBuilder(FilterLogicalOperator defaultOperator = FilterLogicalOperator.All)
    {
        return QueryFilterBuilder.Create(defaultOperator);
    }
}
```

## Query Parameter Formats

The system supports multiple query parameter formats:

### 1. Bracket Notation (Recommended)
```
GET /api/users?name[contains]=john&age[gte]=25&department[eq]=IT
```

### 2. Underscore Notation
```
GET /api/users?name_contains=john&age_gte=25&department_eq=IT
```

### 3. Logical Operator Prefixes
```
GET /api/users?name[contains]=john&or_department[eq]=HR&or_department[eq]=IT
```

### 4. Global Logic Setting
```
GET /api/users?logic=or&name[contains]=john&age[gte]=30
```

### 5. Complex Grouping
```
GET /api/users?group1=1&name[contains]=john&age[gte]=25&group2=2&or_department[eq]=HR&or_department[eq]=IT
```

## Fluent Builder API

### Basic Usage
```csharp
var filters = QueryFilterBuilder.Create()
    .Equal("Status", "Active")
    .Contains("Name", "Smith")
    .GreaterThan("CreatedDate", "2024-01-01")
    .Build();
```

### Logical Operations
```csharp
var filters = QueryFilterBuilder.Create()
    .WithDefaultLogic(FilterLogicalOperator.Any) // Set default to OR
    .Equal("Department", "IT")
    .Or("Department", FilterOperator.Equal, "HR")
    .And("IsActive", FilterOperator.Equal, "true")
    .Build();
```

### Grouping
```csharp
var filters = QueryFilterBuilder.Create()
    .InGroup(1)
    .Equal("Department", "IT")
    .Equal("IsActive", "true")
    .InGroup(2)
    .Equal("Department", "HR")
    .Equal("IsActive", "true")
    .Build();
```

### Specialized Methods
```csharp
var filters = QueryFilterBuilder.Create()
    .Equal("Status", "Active")
    .Contains("Name", "john")
    .Range("Age", "25", "65")
    .In("Department", new[] { "IT", "HR", "Finance" })
    .IsNotNull("Email")
    .Build();
```

## Advanced Features

### 1. Custom Operators

```csharp
// Extend the operator mapping if needed
public static class CustomQueryFilterExtensions
{
    public static IQueryable<T> ApplyCustomFilters<T>(this IQueryable<T> query, 
        Dictionary<string, string> queryParams)
    {
        // Custom logic for special operators
        var customParams = queryParams.Where(kv => kv.Key.StartsWith("custom_"));
        
        foreach (var param in customParams)
        {
            // Handle custom filtering logic
            query = ApplyCustomFilter(query, param.Key, param.Value);
        }
        
        // Apply standard filters
        var standardParams = queryParams.Where(kv => !kv.Key.StartsWith("custom_"))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
            
        return query.ApplyFilters(standardParams);
    }
    
    private static IQueryable<T> ApplyCustomFilter<T>(IQueryable<T> query, string key, string value)
    {
        // Implement custom filtering logic
        return query;
    }
}
```

### 2. Validation and Error Handling

```csharp
public class FilterValidationService
{
    public void ValidateFilters(List<QueryFilterParameter> filters)
    {
        foreach (var filter in filters)
        {
            ValidateFilter(filter);
        }
    }
    
    private void ValidateFilter(QueryFilterParameter filter)
    {
        // Field validation
        if (string.IsNullOrWhiteSpace(filter.Field))
            throw new ArgumentException("Filter field cannot be empty");
            
        // Value validation based on operator
        switch (filter.Operator)
        {
            case FilterOperator.Range:
                if (!filter.Value.Contains(','))
                    throw new ArgumentException("Range operator requires comma-separated values");
                break;
                
            case FilterOperator.In:
            case FilterOperator.NotIn:
                if (string.IsNullOrWhiteSpace(filter.Value))
                    throw new ArgumentException("IN operator requires at least one value");
                break;
        }
    }
}
```

### 3. Performance Optimization

```csharp
public static class QueryFilterPerformanceExtensions
{
    // Cache for compiled expressions
    private static readonly ConcurrentDictionary<string, object> ExpressionCache = new();
    
    public static IQueryable<T> ApplyFiltersWithCache<T>(this IQueryable<T> query, 
        List<QueryFilterParameter> filters)
    {
        var cacheKey = GenerateCacheKey<T>(filters);
        
        if (ExpressionCache.TryGetValue(cacheKey, out var cachedExpression) &&
            cachedExpression is Expression<Func<T, bool>> expression)
        {
            return query.Where(expression);
        }
        
        // Build and cache the expression
        var newExpression = BuildFilterExpression<T>(filters);
        ExpressionCache.TryAdd(cacheKey, newExpression);
        
        return query.Where(newExpression);
    }
    
    private static string GenerateCacheKey<T>(List<QueryFilterParameter> filters)
    {
        // Generate a unique key based on type and filters
        var typeKey = typeof(T).FullName;
        var filterKey = string.Join("|", filters.Select(f => $"{f.Field}:{f.Operator}:{f.LogicalOperator}"));
        return $"{typeKey}#{filterKey}";
    }
}
```

### 4. String-Based Filter Extension

```csharp
// Extension method for applying filters directly from query string
public static class QueryFilterStringExtensions
{
    /// <summary>
    /// Applies filters from a query string directly to an IQueryable.
    /// </summary>
    /// <typeparam name="T">The type of the elements of source.</typeparam>
    /// <param name="query">The IQueryable to filter.</param>
    /// <param name="queryString">The query string containing filter parameters.</param>
    /// <returns>An IQueryable that represents the filtered query.</returns>
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, string queryString)
    {
        ArgumentNullException.ThrowIfNull(query);
        
        if (string.IsNullOrWhiteSpace(queryString))
            return query;

        // Parse the query string into a dictionary
        var queryParams = ParseQueryString(queryString);
        
        // Use the existing ApplyFilters method with dictionary
        return query.ApplyFilters(queryParams);
    }

    /// <summary>
    /// Parses a query string into a dictionary of key-value pairs.
    /// Supports both URL-encoded and plain query strings.
    /// </summary>
    /// <param name="queryString">The query string to parse.</param>
    /// <returns>A dictionary of query parameters.</returns>
    public static Dictionary<string, string> ParseQueryString(string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
            return new Dictionary<string, string>();

        // Remove leading '?' if present
        if (queryString.StartsWith('?'))
            queryString = queryString.Substring(1);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Use built-in query string parsing
            var collection = System.Web.HttpUtility.ParseQueryString(queryString);
            
            foreach (string key in collection.AllKeys)
            {
                if (!string.IsNullOrEmpty(key))
                {
                    // Take the first value if multiple values exist for the same key
                    var value = collection[key] ?? string.Empty;
                    result[key] = value;
                }
            }
        }
        catch (Exception)
        {
            // Fallback to manual parsing if System.Web is not available
            return ParseQueryStringManually(queryString);
        }

        return result;
    }

    /// <summary>
    /// Manual query string parsing fallback method.
    /// </summary>
    /// <param name="queryString">The query string to parse.</param>
    /// <returns>A dictionary of query parameters.</returns>
    private static Dictionary<string, string> ParseQueryStringManually(string queryString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        if (string.IsNullOrWhiteSpace(queryString))
            return result;

        var pairs = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=', 2);
            if (keyValue.Length >= 1)
            {
                var key = Uri.UnescapeDataString(keyValue[0]);
                var value = keyValue.Length == 2 ? Uri.UnescapeDataString(keyValue[1]) : string.Empty;
                
                if (!string.IsNullOrEmpty(key))
                {
                    result[key] = value;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Builds a query string from filter criteria.
    /// </summary>
    /// <param name="filters">The filter criteria to convert.</param>
    /// <returns>A properly formatted query string.</returns>
    public static string BuildQueryString(IEnumerable<QueryFilterParameter> filters)
    {
        if (filters == null || !filters.Any())
            return string.Empty;

        var queryParams = new List<string>();
        
        foreach (var filter in filters)
        {
            var prefix = filter.LogicalOperator == FilterLogicalOperator.Any ? "or_" : "";
            var key = $"{prefix}{filter.Field}[{GetOperatorString(filter.Operator)}]";
            var value = Uri.EscapeDataString(filter.Value);
            queryParams.Add($"{key}={value}");
        }

        return string.Join("&", queryParams);
    }

    /// <summary>
    /// Converts FilterOperator enum to string representation.
    /// </summary>
    /// <param name="operator">The filter operator.</param>
    /// <returns>String representation of the operator.</returns>
    private static string GetOperatorString(FilterOperator @operator)
    {
        return @operator switch
        {
            FilterOperator.Equal => "eq",
            FilterOperator.NotEqual => "ne",
            FilterOperator.GreaterThan => "gt",
            FilterOperator.GreaterThanOrEqual => "gte",
            FilterOperator.LessThan => "lt",
            FilterOperator.LessThanOrEqual => "lte",
            FilterOperator.Contains => "contains",
            FilterOperator.NotContains => "notcontains",
            FilterOperator.StartsWith => "startswith",
            FilterOperator.EndsWith => "endswith",
            FilterOperator.In => "in",
            FilterOperator.NotIn => "notin",
            FilterOperator.IsNull => "isnull",
            FilterOperator.IsNotNull => "isnotnull",
            FilterOperator.Range => "range",
            _ => "eq"
        };
    }
}
```

## SPA Integration

The QueryFilter system works seamlessly with Single Page Applications (SPAs). Here are integration patterns for popular SPA frameworks.

### React Integration

#### 1. React Hook for Query Filters

```tsx
// hooks/useQueryFilter.ts
import { useState, useCallback, useMemo } from 'react';

interface FilterCriteria {
  field: string;
  operator: string;
  value: string;
  logicalOperator?: 'All' | 'Any';
}

interface UseQueryFilterReturn {
  filters: FilterCriteria[];
  addFilter: (filter: FilterCriteria) => void;
  removeFilter: (index: number) => void;
  updateFilter: (index: number, filter: FilterCriteria) => void;
  clearFilters: () => void;
  toQueryString: () => string;
  toQueryParams: () => Record<string, string>;
}

export const useQueryFilter = (): UseQueryFilterReturn => {
  const [filters, setFilters] = useState<FilterCriteria[]>([]);

  const addFilter = useCallback((filter: FilterCriteria) => {
    setFilters(prev => [...prev, filter]);
  }, []);

  const removeFilter = useCallback((index: number) => {
    setFilters(prev => prev.filter((_, i) => i !== index));
  }, []);

  const updateFilter = useCallback((index: number, filter: FilterCriteria) => {
    setFilters(prev => prev.map((f, i) => i === index ? filter : f));
  }, []);

  const clearFilters = useCallback(() => {
    setFilters([]);
  }, []);

  const toQueryParams = useMemo(() => {
    return () => {
      const params: Record<string, string> = {};
      
      filters.forEach((filter, index) => {
        const prefix = filter.logicalOperator === 'Any' ? 'or_' : '';
        const key = `${prefix}${filter.field}[${filter.operator}]`;
        params[key] = filter.value;
      });
      
      return params;
    };
  }, [filters]);

  const toQueryString = useMemo(() => {
    return () => {
      const params = toQueryParams();
      return new URLSearchParams(params).toString();
    };
  }, [toQueryParams]);

  return {
    filters,
    addFilter,
    removeFilter,
    updateFilter,
    clearFilters,
    toQueryString,
    toQueryParams
  };
};
```

#### 2. React Filter Component

```tsx
// components/FilterBuilder.tsx
import React from 'react';
import { useQueryFilter } from '../hooks/useQueryFilter';

interface FilterBuilderProps {
  onFiltersChange: (queryString: string) => void;
  availableFields: Array<{ name: string; label: string; type: 'string' | 'number' | 'date' | 'boolean' }>;
}

const OPERATORS = {
  string: [
    { value: 'eq', label: 'Equals' },
    { value: 'contains', label: 'Contains' },
    { value: 'startswith', label: 'Starts With' },
    { value: 'endswith', label: 'Ends With' }
  ],
  number: [
    { value: 'eq', label: 'Equals' },
    { value: 'gt', label: 'Greater Than' },
    { value: 'gte', label: 'Greater Than or Equal' },
    { value: 'lt', label: 'Less Than' },
    { value: 'lte', label: 'Less Than or Equal' },
    { value: 'range', label: 'Range' }
  ],
  date: [
    { value: 'eq', label: 'Equals' },
    { value: 'gt', label: 'After' },
    { value: 'gte', label: 'On or After' },
    { value: 'lt', label: 'Before' },
    { value: 'lte', label: 'On or Before' },
    { value: 'range', label: 'Date Range' }
  ],
  boolean: [
    { value: 'eq', label: 'Equals' }
  ]
};

export const FilterBuilder: React.FC<FilterBuilderProps> = ({ 
  onFiltersChange, 
  availableFields 
}) => {
  const { 
    filters, 
    addFilter, 
    removeFilter, 
    updateFilter, 
    clearFilters, 
    toQueryString 
  } = useQueryFilter();

  React.useEffect(() => {
    onFiltersChange(toQueryString());
  }, [filters, onFiltersChange, toQueryString]);

  const handleAddFilter = () => {
    addFilter({
      field: availableFields[0]?.name || '',
      operator: 'eq',
      value: '',
      logicalOperator: 'All'
    });
  };

  return (
    <div className="filter-builder">
      <div className="filter-header">
        <h3>Filters</h3>
        <div className="filter-actions">
          <button onClick={handleAddFilter} className="btn btn-primary">
            Add Filter
          </button>
          <button onClick={clearFilters} className="btn btn-secondary">
            Clear All
          </button>
        </div>
      </div>

      <div className="filter-list">
        {filters.map((filter, index) => {
          const fieldConfig = availableFields.find(f => f.name === filter.field);
          const operators = fieldConfig ? OPERATORS[fieldConfig.type] : OPERATORS.string;

          return (
            <div key={index} className="filter-row">
              <select
                value={filter.logicalOperator}
                onChange={(e) => updateFilter(index, { 
                  ...filter, 
                  logicalOperator: e.target.value as 'All' | 'Any' 
                })}
                className="form-control"
              >
                <option value="All">AND</option>
                <option value="Any">OR</option>
              </select>

              <select
                value={filter.field}
                onChange={(e) => updateFilter(index, { ...filter, field: e.target.value })}
                className="form-control"
              >
                {availableFields.map(field => (
                  <option key={field.name} value={field.name}>
                    {field.label}
                  </option>
                ))}
              </select>

              <select
                value={filter.operator}
                onChange={(e) => updateFilter(index, { ...filter, operator: e.target.value })}
                className="form-control"
              >
                {operators.map(op => (
                  <option key={op.value} value={op.value}>
                    {op.label}
                  </option>
                ))}
              </select>

              <input
                type="text"
                value={filter.value}
                onChange={(e) => updateFilter(index, { ...filter, value: e.target.value })}
                placeholder="Enter value..."
                className="form-control"
              />

              <button
                onClick={() => removeFilter(index)}
                className="btn btn-danger"
              >
                Remove
              </button>
            </div>
          );
        })}
      </div>
    </div>
  );
};
```

#### 3. React Data Fetching with Filters

```tsx
// hooks/useFilteredData.ts
import { useState, useEffect, useCallback } from 'react';

interface UseFilteredDataOptions<T> {
  endpoint: string;
  initialFilters?: string;
  transform?: (data: any) => T[];
}

interface UseFilteredDataReturn<T> {
  data: T[];
  loading: boolean;
  error: string | null;
  refetch: (filters?: string) => Promise<void>;
}

export const useFilteredData = <T>({
  endpoint,
  initialFilters = '',
  transform
}: UseFilteredDataOptions<T>): UseFilteredDataReturn<T> => {
  const [data, setData] = useState<T[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async (filters: string = '') => {
    setLoading(true);
    setError(null);

    try {
      const url = filters 
        ? `${endpoint}?${filters}`
        : endpoint;
      
      const response = await fetch(url, {
        headers: {
          'Content-Type': 'application/json',
          // Add auth headers if needed
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        }
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const result = await response.json();
      const transformedData = transform ? transform(result) : result;
      setData(transformedData);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  }, [endpoint, transform]);

  useEffect(() => {
    fetchData(initialFilters);
  }, [fetchData, initialFilters]);

  return {
    data,
    loading,
    error,
    refetch: fetchData
  };
};

// Usage in component
const UserList: React.FC = () => {
  const [filterQuery, setFilterQuery] = useState('');
  
  const { data: users, loading, error, refetch } = useFilteredData({
    endpoint: '/api/users',
    initialFilters: filterQuery
  });

  const handleFiltersChange = useCallback((queryString: string) => {
    setFilterQuery(queryString);
    refetch(queryString);
  }, [refetch]);

  const userFields = [
    { name: 'name', label: 'Name', type: 'string' as const },
    { name: 'email', label: 'Email', type: 'string' as const },
    { name: 'age', label: 'Age', type: 'number' as const },
    { name: 'department', label: 'Department', type: 'string' as const },
    { name: 'isActive', label: 'Active', type: 'boolean' as const },
    { name: 'createdAt', label: 'Created Date', type: 'date' as const }
  ];

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <div>
      <FilterBuilder 
        availableFields={userFields}
        onFiltersChange={handleFiltersChange}
      />
      
      <div className="user-list">
        {users.map(user => (
          <div key={user.id} className="user-card">
            {/* Render user data */}
          </div>
        ))}
      </div>
    </div>
  );
};
```

### Vue.js Integration

#### 1. Vue Composable for Query Filters

```typescript
// composables/useQueryFilter.ts
import { ref, computed, Ref } from 'vue';

interface FilterCriteria {
  field: string;
  operator: string;
  value: string;
  logicalOperator?: 'All' | 'Any';
}

export const useQueryFilter = () => {
  const filters: Ref<FilterCriteria[]> = ref([]);

  const addFilter = (filter: FilterCriteria) => {
    filters.value.push(filter);
  };

  const removeFilter = (index: number) => {
    filters.value.splice(index, 1);
  };

  const updateFilter = (index: number, filter: FilterCriteria) => {
    filters.value[index] = filter;
  };

  const clearFilters = () => {
    filters.value = [];
  };

  const toQueryParams = computed(() => {
    const params: Record<string, string> = {};
    
    filters.value.forEach((filter) => {
      const prefix = filter.logicalOperator === 'Any' ? 'or_' : '';
      const key = `${prefix}${filter.field}[${filter.operator}]`;
      params[key] = filter.value;
    });
    
    return params;
  });

  const toQueryString = computed(() => {
    return new URLSearchParams(toQueryParams.value).toString();
  });

  return {
    filters: readonly(filters),
    addFilter,
    removeFilter,
    updateFilter,
    clearFilters,
    toQueryParams,
    toQueryString
  };
};
```

#### 2. Vue Filter Component

```vue
<!-- components/FilterBuilder.vue -->
<template>
  <div class="filter-builder">
    <div class="filter-header">
      <h3>Filters</h3>
      <div class="filter-actions">
        <button @click="handleAddFilter" class="btn btn-primary">
          Add Filter
        </button>
        <button @click="clearFilters" class="btn btn-secondary">
          Clear All
        </button>
      </div>
    </div>

    <div class="filter-list">
      <div 
        v-for="(filter, index) in filters" 
        :key="index" 
        class="filter-row"
      >
        <select
          v-model="filter.logicalOperator"
          @change="onFilterChange"
          class="form-control"
        >
          <option value="All">AND</option>
          <option value="Any">OR</option>
        </select>

        <select
          v-model="filter.field"
          @change="onFilterChange"
          class="form-control"
        >
          <option 
            v-for="field in availableFields" 
            :key="field.name" 
            :value="field.name"
          >
            {{ field.label }}
          </option>
        </select>

        <select
          v-model="filter.operator"
          @change="onFilterChange"
          class="form-control"
        >
          <option 
            v-for="op in getOperatorsForField(filter.field)" 
            :key="op.value" 
            :value="op.value"
          >
            {{ op.label }}
          </option>
        </select>

        <input
          v-model="filter.value"
          @input="onFilterChange"
          type="text"
          placeholder="Enter value..."
          class="form-control"
        />

        <button
          @click="removeFilter(index)"
          class="btn btn-danger"
        >
          Remove
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { watch } from 'vue';
import { useQueryFilter } from '../composables/useQueryFilter';

interface FieldConfig {
  name: string;
  label: string;
  type: 'string' | 'number' | 'date' | 'boolean';
}

interface Props {
  availableFields: FieldConfig[];
}

interface Emits {
  (e: 'filtersChange', queryString: string): void;
}

const props = defineProps<Props>();
const emit = defineEmits<Emits>();

const {
  filters,
  addFilter,
  removeFilter,
  clearFilters,
  toQueryString
} = useQueryFilter();

const OPERATORS = {
  string: [
    { value: 'eq', label: 'Equals' },
    { value: 'contains', label: 'Contains' },
    { value: 'startswith', label: 'Starts With' },
    { value: 'endswith', label: 'Ends With' }
  ],
  number: [
    { value: 'eq', label: 'Equals' },
    { value: 'gt', label: 'Greater Than' },
    { value: 'gte', label: 'Greater Than or Equal' },
    { value: 'lt', label: 'Less Than' },
    { value: 'lte', label: 'Less Than or Equal' },
    { value: 'range', label: 'Range' }
  ],
  date: [
    { value: 'eq', label: 'Equals' },
    { value: 'gt', label: 'After' },
    { value: 'gte', label: 'On or After' },
    { value: 'lt', label: 'Before' },
    { value: 'lte', label: 'On or Before' },
    { value: 'range', label: 'Date Range' }
  ],
  boolean: [
    { value: 'eq', label: 'Equals' }
  ]
};

const getOperatorsForField = (fieldName: string) => {
  const field = props.availableFields.find(f => f.name === fieldName);
  return field ? OPERATORS[field.type] : OPERATORS.string;
};

const handleAddFilter = () => {
  addFilter({
    field: props.availableFields[0]?.name || '',
    operator: 'eq',
    value: '',
    logicalOperator: 'All'
  });
};

const onFilterChange = () => {
  emit('filtersChange', toQueryString.value);
};

watch(toQueryString, (newQueryString) => {
  emit('filtersChange', newQueryString);
});
</script>
```

### Angular Integration

#### 1. Angular Service for Query Filters

```typescript
// services/query-filter.service.ts
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface FilterCriteria {
  field: string;
  operator: string;
  value: string;
  logicalOperator?: 'All' | 'Any';
}

@Injectable({
  providedIn: 'root'
})
export class QueryFilterService {
  private filtersSubject = new BehaviorSubject<FilterCriteria[]>([]);
  public filters$ = this.filtersSubject.asObservable();

  public queryString$ = this.filters$.pipe(
    map(filters => this.buildQueryString(filters))
  );

  addFilter(filter: FilterCriteria): void {
    const currentFilters = this.filtersSubject.value;
    this.filtersSubject.next([...currentFilters, filter]);
  }

  removeFilter(index: number): void {
    const currentFilters = this.filtersSubject.value;
    this.filtersSubject.next(currentFilters.filter((_, i) => i !== index));
  }

  updateFilter(index: number, filter: FilterCriteria): void {
    const currentFilters = this.filtersSubject.value;
    const updatedFilters = [...currentFilters];
    updatedFilters[index] = filter;
    this.filtersSubject.next(updatedFilters);
  }

  clearFilters(): void {
    this.filtersSubject.next([]);
  }

  private buildQueryString(filters: FilterCriteria[]): string {
    const params: Record<string, string> = {};
    
    filters.forEach(filter => {
      const prefix = filter.logicalOperator === 'Any' ? 'or_' : '';
      const key = `${prefix}${filter.field}[${filter.operator}]`;
      params[key] = filter.value;
    });
    
    return new URLSearchParams(params).toString();
  }
}
```

#### 2. Angular Filter Component

```typescript
// components/filter-builder.component.ts
import { Component, EventEmitter, Input, Output, OnInit, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { QueryFilterService, FilterCriteria } from '../services/query-filter.service';

interface FieldConfig {
  name: string;
  label: string;
  type: 'string' | 'number' | 'date' | 'boolean';
}

@Component({
  selector: 'app-filter-builder',
  template: `
    <div class="filter-builder">
      <div class="filter-header">
        <h3>Filters</h3>
        <div class="filter-actions">
          <button (click)="addFilter()" class="btn btn-primary">
            Add Filter
          </button>
          <button (click)="clearFilters()" class="btn btn-secondary">
            Clear All
          </button>
        </div>
      </div>

      <div class="filter-list">
        <div 
          *ngFor="let filter of filters; let i = index" 
          class="filter-row"
        >
          <select
            [(ngModel)]="filter.logicalOperator"
            (change)="updateFilter(i, filter)"
            class="form-control"
          >
            <option value="All">AND</option>
            <option value="Any">OR</option>
          </select>

          <select
            [(ngModel)]="filter.field"
            (change)="updateFilter(i, filter)"
            class="form-control"
          >
            <option 
              *ngFor="let field of availableFields" 
              [value]="field.name"
            >
              {{ field.label }}
            </option>
          </select>

          <select
            [(ngModel)]="filter.operator"
            (change)="updateFilter(i, filter)"
            class="form-control"
          >
            <option 
              *ngFor="let op of getOperatorsForField(filter.field)" 
              [value]="op.value"
            >
              {{ op.label }}
            </option>
          </select>

          <input
            [(ngModel)]="filter.value"
            (input)="updateFilter(i, filter)"
            type="text"
            placeholder="Enter value..."
            class="form-control"
          />

          <button
            (click)="removeFilter(i)"
            class="btn btn-danger"
          >
            Remove
          </button>
        </div>
      </div>
    </div>
  `
})
export class FilterBuilderComponent implements OnInit, OnDestroy {
  @Input() availableFields: FieldConfig[] = [];
  @Output() filtersChange = new EventEmitter<string>();

  filters: FilterCriteria[] = [];
  private destroy$ = new Subject<void>();

  private readonly OPERATORS = {
    string: [
      { value: 'eq', label: 'Equals' },
      { value: 'contains', label: 'Contains' },
      { value: 'startswith', label: 'Starts With' },
      { value: 'endswith', label: 'Ends With' }
    ],
    number: [
      { value: 'eq', label: 'Equals' },
      { value: 'gt', label: 'Greater Than' },
      { value: 'gte', label: 'Greater Than or Equal' },
      { value: 'lt', label: 'Less Than' },
      { value: 'lte', label: 'Less Than or Equal' },
      { value: 'range', label: 'Range' }
    ],
    date: [
      { value: 'eq', label: 'Equals' },
      { value: 'gt', label: 'After' },
      { value: 'gte', label: 'On or After' },
      { value: 'lt', label: 'Before' },
      { value: 'lte', label: 'On or Before' },
      { value: 'range', label: 'Date Range' }
    ],
    boolean: [
      { value: 'eq', label: 'Equals' }
    ]
  };

  constructor(private queryFilterService: QueryFilterService) {}

  ngOnInit(): void {
    this.queryFilterService.filters$
      .pipe(takeUntil(this.destroy$))
      .subscribe(filters => {
        this.filters = filters;
      });

    this.queryFilterService.queryString$
      .pipe(takeUntil(this.destroy$))
      .subscribe(queryString => {
        this.filtersChange.emit(queryString);
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  addFilter(): void {
    this.queryFilterService.addFilter({
      field: this.availableFields[0]?.name || '',
      operator: 'eq',
      value: '',
      logicalOperator: 'All'
    });
  }

  removeFilter(index: number): void {
    this.queryFilterService.removeFilter(index);
  }

  updateFilter(index: number, filter: FilterCriteria): void {
    this.queryFilterService.updateFilter(index, filter);
  }

  clearFilters(): void {
    this.queryFilterService.clearFilters();
  }

  getOperatorsForField(fieldName: string) {
    const field = this.availableFields.find(f => f.name === fieldName);
    return field ? this.OPERATORS[field.type] : this.OPERATORS.string;
  }
}
```

### JavaScript/TypeScript API Client

#### Universal API Client for Any SPA Framework

```typescript
// api/filterClient.ts
export interface FilterCriteria {
  field: string;
  operator: string;
  value: string;
  logicalOperator?: 'All' | 'Any';
}

export interface QueryFilterOptions {
  baseUrl: string;
  authToken?: string;
  defaultHeaders?: Record<string, string>;
}

export class QueryFilterClient {
  private baseUrl: string;
  private defaultHeaders: Record<string, string>;

  constructor(options: QueryFilterOptions) {
    this.baseUrl = options.baseUrl;
    this.defaultHeaders = {
      'Content-Type': 'application/json',
      ...options.defaultHeaders
    };

    if (options.authToken) {
      this.defaultHeaders['Authorization'] = `Bearer ${options.authToken}`;
    }
  }

  /**
   * Converts filter criteria to query parameters
   */
  static buildQueryParams(filters: FilterCriteria[]): Record<string, string> {
    const params: Record<string, string> = {};
    
    filters.forEach(filter => {
      const prefix = filter.logicalOperator === 'Any' ? 'or_' : '';
      const key = `${prefix}${filter.field}[${filter.operator}]`;
      params[key] = filter.value;
    });
    
    return params;
  }

  /**
   * Converts query parameters to URL search string
   */
  static buildQueryString(params: Record<string, string>): string {
    return new URLSearchParams(params).toString();
  }

  /**
   * Fetch data with filters applied via query parameters
   */
  async fetchWithFilters<T>(
    endpoint: string, 
    filters: FilterCriteria[] = []
  ): Promise<T> {
    const queryParams = QueryFilterClient.buildQueryParams(filters);
    const queryString = QueryFilterClient.buildQueryString(queryParams);
    
    const url = queryString 
      ? `${this.baseUrl}${endpoint}?${queryString}`
      : `${this.baseUrl}${endpoint}`;

    const response = await fetch(url, {
      method: 'GET',
      headers: this.defaultHeaders
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    return response.json();
  }

  /**
   * Post search request with complex filter criteria
   */
  async searchWithFilters<T>(
    endpoint: string,
    searchRequest: {
      defaultLogic?: 'All' | 'Any';
      filters: FilterCriteria[];
      pagination?: {
        page: number;
        pageSize: number;
      };
    }
  ): Promise<T> {
    const url = `${this.baseUrl}${endpoint}`;

    const response = await fetch(url, {
      method: 'POST',
      headers: this.defaultHeaders,
      body: JSON.stringify(searchRequest)
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    return response.json();
  }

  /**
   * Update authentication token
   */
  setAuthToken(token: string): void {
    this.defaultHeaders['Authorization'] = `Bearer ${token}`;
  }

  /**
   * Remove authentication token
   */
  clearAuthToken(): void {
    delete this.defaultHeaders['Authorization'];
  }
}

// Usage example
export const createFilterClient = (authToken?: string) => {
  return new QueryFilterClient({
    baseUrl: process.env.REACT_APP_API_URL || 'https://api.example.com',
    authToken,
    defaultHeaders: {
      'X-Client-Version': '1.0.0'
    }
  });
};
```

### Integration with State Management

#### Redux Toolkit Integration

```typescript
// store/filterSlice.ts
import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { QueryFilterClient, FilterCriteria } from '../api/filterClient';

interface FilterState {
  criteria: FilterCriteria[];
  loading: boolean;
  error: string | null;
  data: any[];
}

const initialState: FilterState = {
  criteria: [],
  loading: false,
  error: null,
  data: []
};

export const fetchFilteredData = createAsyncThunk(
  'filter/fetchData',
  async ({ endpoint, filters }: { endpoint: string; filters: FilterCriteria[] }) => {
    const client = new QueryFilterClient({ baseUrl: '/api' });
    return await client.fetchWithFilters(endpoint, filters);
  }
);

const filterSlice = createSlice({
  name: 'filter',
  initialState,
  reducers: {
    addFilter: (state, action: PayloadAction<FilterCriteria>) => {
      state.criteria.push(action.payload);
    },
    removeFilter: (state, action: PayloadAction<number>) => {
      state.criteria.splice(action.payload, 1);
    },
    updateFilter: (state, action: PayloadAction<{ index: number; filter: FilterCriteria }>) => {
      state.criteria[action.payload.index] = action.payload.filter;
    },
    clearFilters: (state) => {
      state.criteria = [];
    }
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchFilteredData.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchFilteredData.fulfilled, (state, action) => {
        state.loading = false;
        state.data = action.payload;
      })
      .addCase(fetchFilteredData.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to fetch data';
      });
  }
});

export const { addFilter, removeFilter, updateFilter, clearFilters } = filterSlice.actions;
export default filterSlice.reducer;
```

### Real-time Filtering with SignalR

```typescript
// services/realtimeFilter.ts
import * as signalR from '@microsoft/signalr';
import { FilterCriteria } from '../api/filterClient';

export class RealtimeFilterService {
  private connection: signalR.HubConnection;
  private onDataUpdated?: (data: any[]) => void;

  constructor(hubUrl: string) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build();

    this.setupConnection();
  }

  private async setupConnection(): Promise<void> {
    this.connection.on('FilteredDataUpdated', (data: any[]) => {
      if (this.onDataUpdated) {
        this.onDataUpdated(data);
      }
    });

    await this.connection.start();
  }

  async subscribeToFilteredData(
    dataType: string, 
    filters: FilterCriteria[],
    onDataUpdated: (data: any[]) => void
  ): Promise<void> {
    this.onDataUpdated = onDataUpdated;
    
    await this.connection.invoke('SubscribeToFilteredData', {
      dataType,
      filters
    });
  }

  async updateFilters(filters: FilterCriteria[]): Promise<void> {
    await this.connection.invoke('UpdateFilters', filters);
  }

  async disconnect(): Promise<void> {
    await this.connection.stop();
  }
}
```

These SPA integration examples show how to:
1. Build dynamic filter UIs in React, Vue, and Angular
2. Manage filter state effectively
3. Integrate with APIs using the QueryFilter system
4. Handle real-time updates
5. Work with state management libraries

The key is to convert the SPA filter UI state into the query parameter format that your QueryFilter system expects on the backend.

## Best Practices

### 1. Input Validation
```csharp
[HttpGet]
public async Task<IActionResult> GetUsers([FromQuery] UserFilterRequest request)
{
    // Validate input
    if (!ModelState.IsValid)
        return BadRequest(ModelState);
    
    try
    {
        var users = await _userService.GetUsersAsync();
        var filteredUsers = users.ApplyFilters(request.ToQueryParams());
        return Ok(filteredUsers);
    }
    catch (ArgumentException ex)
    {
        return BadRequest($"Invalid filter criteria: {ex.Message}");
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest($"Filter operation failed: {ex.Message}");
    }
}
```

### 2. Pagination with Filtering
```csharp
app.MapGet("/api/users", async (
    [FromServices] IUserService userService,
    [AsParameters] UserFilterRequest filterRequest,
    [AsParameters] PaginationRequest paginationRequest) =>
{
    var users = await userService.GetUsersAsync();
    
    // Apply filters first
    var filteredUsers = users.ApplyFilters(filterRequest.ToQueryParams());
    
    // Then apply pagination
    var pagedUsers = filteredUsers
        .Skip((paginationRequest.Page - 1) * paginationRequest.PageSize)
        .Take(paginationRequest.PageSize)
        .ToList();
    
    var totalCount = filteredUsers.Count();
    
    return Results.Ok(new PagedResult<User>
    {
        Data = pagedUsers,
        TotalCount = totalCount,
        Page = paginationRequest.Page,
        PageSize = paginationRequest.PageSize
    });
});
```

### 3. Security Considerations
```csharp
public class SecureFilterService
{
    private readonly HashSet<string> _allowedFields = new()
    {
        "Name", "Email", "Department", "IsActive", "CreatedDate"
    };
    
    public IQueryable<T> ApplySecureFilters<T>(IQueryable<T> query, 
        Dictionary<string, string> queryParams)
    {
        // Filter out non-allowed fields
        var secureParams = queryParams
            .Where(kv => IsFieldAllowed(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        
        return query.ApplyFilters(secureParams);
    }
    
    private bool IsFieldAllowed(string paramKey)
    {
        // Extract field name from parameter key (handle both formats)
        var fieldName = ExtractFieldName(paramKey);
        return _allowedFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase);
    }
}
```

## Examples

### Complete Minimal API Example

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IUserService, UserService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Basic filtering endpoint
app.MapGet("/api/users", async (
    [FromServices] IUserService userService,
    HttpContext context) =>
{
    var queryParams = context.Request.Query
        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
    
    var users = await userService.GetUsersAsync();
    var filteredUsers = users.ApplyFilters(queryParams);
    
    return Results.Ok(filteredUsers);
})
.WithName("GetUsers")
.WithOpenApi();

// Advanced search endpoint
app.MapPost("/api/users/search", async (
    [FromServices] IUserService userService,
    UserSearchRequest request) =>
{
    var users = await userService.GetUsersAsync();
    
    var builder = QueryFilterBuilder.Create(request.DefaultLogic);
    
    foreach (var filter in request.Filters)
    {
        builder.Add(filter.Field, filter.Operator, filter.Value, filter.LogicalOperator);
    }
    
    var filteredUsers = builder.ApplyTo(users);
    
    return Results.Ok(filteredUsers);
})
.WithName("SearchUsers")
.WithOpenApi();

app.Run();

// Supporting classes
public class UserSearchRequest
{
    public FilterLogicalOperator DefaultLogic { get; set; } = FilterLogicalOperator.All;
    public List<FilterCriteria> Filters { get; set; } = new();
}

public class FilterCriteria
{
    public string Field { get; set; } = string.Empty;
    public FilterOperator Operator { get; set; }
    public string Value { get; set; } = string.Empty;
    public FilterLogicalOperator LogicalOperator { get; set; } = FilterLogicalOperator.All;
}
```

### Sample API Calls

```bash
# Basic filtering
GET /api/users?name[contains]=john&age[gte]=25

# OR logic
GET /api/users?logic=or&department[eq]=IT&department[eq]=HR

# Complex filtering with grouping
GET /api/users?name[contains]=john&and_age[gte]=25&or_department[eq]=IT&or_department[eq]=HR

# Range filtering
GET /api/products?price[range]=100,500&category[eq]=Electronics

# Multiple values
GET /api/orders?status[in]=Pending,Processing,Shipped

# String-based filtering (passing query string as parameter)
GET /api/products/filter-by-string?filter=name[contains]=laptop&price[gte]=500&category[eq]=Electronics

# Using raw query string
GET /api/users/search?name[contains]=john&department[eq]=IT&isActive[eq]=true

# POST search with JSON
POST /api/users/search
{
  "defaultLogic": "All",
  "filters": [
    {
      "field": "Name",
      "operator": "Contains",
      "value": "john",
      "logicalOperator": "All"
    },
    {
      "field": "Department",
      "operator": "Equal",
      "value": "IT",
      "logicalOperator": "Any"
    }
  ]
}
```

### String-Based Filtering Usage Examples

```csharp
// Example 1: Direct string filtering
var queryString = "name[contains]=john&age[gte]=25&department[eq]=IT";
var filteredUsers = users.ApplyFilters(queryString);

// Example 2: Building query string from criteria
var filters = new List<QueryFilterParameter>
{
    new() { Field = "Name", Operator = FilterOperator.Contains, Value = "john" },
    new() { Field = "Age", Operator = FilterOperator.GreaterThanOrEqual, Value = "25" },
    new() { Field = "Department", Operator = FilterOperator.Equal, Value = "IT" }
};

var queryString = QueryFilterStringExtensions.BuildQueryString(filters);
var filteredUsers = users.ApplyFilters(queryString);

// Example 3: Service method with string parameter
public class UserService
{
    public async Task<List<User>> GetFilteredUsersAsync(string filterString)
    {
        var users = await GetAllUsersAsync();
        return users.ApplyFilters(filterString).ToList();
    }
}

// Example 4: Converting between formats
public static class FilterConverters
{
    public static string ConvertDictionaryToString(Dictionary<string, string> queryParams)
    {
        return string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
    }
    
    public static Dictionary<string, string> ConvertStringToDictionary(string queryString)
    {
        return QueryFilterStringExtensions.ParseQueryString(queryString);
    }
}
```

This comprehensive guide covers all aspects of using the QueryFilter system in your Rys.Fashion application, from basic filtering to advanced scenarios with proper dependency injection and best practices.
