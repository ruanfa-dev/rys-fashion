# ApiResponse Format Examples

This document shows examples of the updated `ApiResponse<T>` format with RFC 7807 Problem Details compliance and full error codes.

## Key Changes

1. **Removed `StatusCode` field** - Now only using `Status` field for RFC 7807 compliance
2. **Full error codes in errors dictionary** - Using complete error codes like `"Role.AlreadyExists"` instead of just categories like `"Role"`
3. **RFC 7807 Problem Details fields** - Added `type`, `title`, `status`, and `detail` fields for standards compliance

## Success Response Examples

### Simple Success Response
```json
{
  "isSuccess": true,
  "data": {
    "id": 1,
    "name": "iPhone 15",
    "price": 999.99
  },
  "message": "Product retrieved successfully",
  "status": 200,
  "timestamp": "2024-01-15T10:30:00Z",
  "apiVersion": "1.0"
}
```

### Created Resource Response
```json
{
  "isSuccess": true,
  "data": {
    "id": 2,
    "name": "Admin Role",
    "permissions": ["create_users", "delete_users"]
  },
  "message": "Role created successfully",
  "status": 201,
  "timestamp": "2024-01-15T10:30:00Z",
  "apiVersion": "1.0",
  "links": {
    "self": "/api/admin/roles/2",
    "update": "/api/admin/roles/2",
    "delete": "/api/admin/roles/2"
  }
}
```

## Error Response Examples

### Not Found Error (RFC 7807 Compatible)
```json
{
  "isSuccess": false,
  "data": null,
  "message": "Role with ID 999 was not found",
  "type": "https://httpstatuses.com/404",
  "title": "Role.NotFound",
  "status": 404,
  "detail": "Role with ID 999 was not found",
  "timestamp": "2024-01-15T10:30:00Z",
  "apiVersion": "1.0"
}
```

### Conflict Error with Full Error Code
```json
{
  "isSuccess": false,
  "data": null,
  "message": "A conflict occurred",
  "type": "https://httpstatuses.com/409",
  "title": "Role.AlreadyExists",
  "status": 409,
  "detail": "A role with this name already exists",
  "errors": {
    "Role.AlreadyExists": [
      "A role with this name already exists"
    ]
  },
  "timestamp": "2024-01-15T10:30:00Z",
  "apiVersion": "1.0"
}
```

### Validation Error with Multiple Full Error Codes
```json
{
  "isSuccess": false,
  "data": null,
  "message": "Validation failed",
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Failed",
  "status": 422,
  "detail": "One or more validation errors occurred",
  "errors": {
    "User.EmailRequired": [
      "Email address is required"
    ],
    "User.EmailInvalid": [
      "Email format is invalid"
    ],
    "Password.TooShort": [
      "Password must be at least 8 characters"
    ],
    "Password.MissingUppercase": [
      "Password must contain at least one uppercase letter"
    ]
  },
  "timestamp": "2024-01-15T10:30:00Z",
  "apiVersion": "1.0"
}
```

### Unauthorized Error
```json
{
  "isSuccess": false,
  "data": null,
  "message": "Authentication token is required",
  "type": "https://httpstatuses.com/401",
  "title": "Authentication.Required",
  "status": 401,
  "detail": "Authentication token is required",
  "timestamp": "2024-01-15T10:30:00Z",
  "apiVersion": "1.0"
}
```

## Paginated Response Example
```json
{
  "isSuccess": true,
  "data": [
    {
      "id": 1,
      "name": "Admin Role",
      "permissions": ["create_users"]
    },
    {
      "id": 2,
      "name": "User Role", 
      "permissions": ["view_profile"]
    }
  ],
  "message": "Roles retrieved successfully",
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
    "self": "/api/admin/roles?page=1&pageSize=10",
    "next": "/api/admin/roles?page=2&pageSize=10",
    "first": "/api/admin/roles?page=1&pageSize=10",
    "last": "/api/admin/roles?page=3&pageSize=10"
  },
  "timestamp": "2024-01-15T10:30:00Z",
  "apiVersion": "1.0"
}
```

## Benefits of This Format

### 1. RFC 7807 Compliance
- Standard `type` field with URI reference for problem identification
- Standard `title` field with error code (e.g., "Role.AlreadyExists")
- Standard `status` field with HTTP status code
- Standard `detail` field with human-readable explanation

### 2. Better Error Categorization
- **Before:** `"errors": { "Role": ["A role with this name already exists"] }`
- **After:** `"errors": { "Role.AlreadyExists": ["A role with this name already exists"] }`

The full error code provides:
- **Specificity**: `Role.AlreadyExists` vs just `Role`
- **Programmatic handling**: Frontend can handle specific error types
- **Better debugging**: Clear identification of exact error condition
- **API evolution**: Can add new error types without breaking existing clients

### 3. Consistent Structure
- Always returns HTTP 200 OK at transport layer
- Business logic status preserved in `status` field
- Consistent parsing for frontend applications
- Rich metadata and HATEOAS links support

### 4. Backward Compatibility
- Maintained all existing functionality
- Added new RFC 7807 fields without breaking existing clients
- Enhanced error information while preserving simplicity for basic use cases