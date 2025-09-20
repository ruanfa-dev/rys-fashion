# Keycloak Integration API Documentation

## Overview

This document provides comprehensive documentation for the Keycloak integration APIs implemented in the Rys.Fashion system. The integration follows CQRS (Command Query Responsibility Segregation) pattern with MediatR for clean separation of concerns.

## Table of Contents

1. [Authentication APIs](#authentication-apis)
2. [User Management APIs](#user-management-apis)
3. [Role Management APIs](#role-management-apis)
4. [Group Management APIs](#group-management-apis)
5. [Session Management APIs](#session-management-apis)
6. [Statistics APIs](#statistics-apis)
7. [Error Handling](#error-handling)
8. [Security Considerations](#security-considerations)
9. [Testing](#testing)

## Authentication APIs

### 1. Login
**Endpoint:** `POST /api/auth/login`

Authenticates a user with Keycloak and returns JWT tokens.

#### Request Body
```json
{
  "username": "john.doe",
  "password": "secure-password"
}
```

#### Response
```json
{
  "data": {
    "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenType": "Bearer",
    "expiresIn": 3600
  },
  "success": true,
  "message": "Login successful",
  "metadata": {
    "source": "keycloak",
    "authenticatedAt": "2024-01-15T10:30:00Z",
    "tokenType": "Bearer",
    "scope": "rys-fashion-api"
  }
}
```

#### Error Responses
- **401 Unauthorized**: Invalid credentials
- **400 Bad Request**: Validation errors
- **500 Internal Server Error**: Keycloak service unavailable

### 2. Refresh Token
**Endpoint:** `POST /api/auth/refresh`

Refreshes an expired access token using the refresh token.

#### Request Body
```json
{
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

#### Response
```json
{
  "data": {
    "accessToken": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenType": "Bearer",
    "expiresIn": 3600
  },
  "success": true,
  "message": "Token refreshed successfully"
}
```

### 3. Logout
**Endpoint:** `POST /api/auth/logout`

Logs out the user and invalidates tokens.

#### Request Body
```json
{
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

#### Response
```json
{
  "data": {
    "success": true
  },
  "success": true,
  "message": "Logout successful"
}
```

### 4. Get User Info
**Endpoint:** `GET /api/auth/userinfo`

Retrieves user information from the access token.

#### Headers
```
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Response
```json
{
  "data": {
    "subject": "user-123-456",
    "username": "john.doe",
    "email": "john.doe@example.com",
    "emailVerified": true,
    "firstName": "John",
    "lastName": "Doe",
    "roles": ["user", "customer"]
  },
  "success": true,
  "message": "User information retrieved successfully"
}
```

## User Management APIs

### 1. Create User
**Endpoint:** `POST /api/admin/users`

Creates a new user in Keycloak.

#### Request Body
```json
{
  "username": "jane.smith",
  "email": "jane.smith@example.com",
  "firstName": "Jane",
  "lastName": "Smith",
  "enabled": true,
  "emailVerified": false,
  "password": "temp-password",
  "temporaryPassword": true,
  "roleNames": ["user", "customer"],
  "attributes": {
    "department": ["Sales"],
    "location": ["New York"]
  }
}
```

#### Response
```json
{
  "data": {
    "id": "user-789-012"
  },
  "success": true,
  "message": "User created successfully in Keycloak",
  "links": {
    "self": "/api/admin/users/user-789-012",
    "update": "/api/admin/users/user-789-012",
    "delete": "/api/admin/users/user-789-012",
    "assign-roles": "/api/admin/users/user-789-012/roles",
    "all-users": "/api/admin/users"
  }
}
```

#### Validation Rules
- **Username**: Required, max 100 characters
- **Email**: Required, valid email format, max 200 characters
- **FirstName**: Required, max 50 characters
- **LastName**: Required, max 50 characters
- **Password**: Min 8 characters (when provided)

### 2. List Users
**Endpoint:** `GET /api/admin/users`

Retrieves users from Keycloak with optional filtering.

#### Query Parameters
- `search` (optional): Search term for username, email, first name, or last name
- `max` (optional): Maximum number of results
- `first` (optional): Starting index for pagination

#### Example Request
```
GET /api/admin/users?search=john&max=10&first=0
```

#### Response
```json
{
  "data": [
    {
      "id": "user-123-456",
      "username": "john.doe",
      "email": "john.doe@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "enabled": true,
      "emailVerified": true,
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ],
  "success": true,
  "message": "Users retrieved successfully from Keycloak",
  "metadata": {
    "totalReturned": 1,
    "searchApplied": true
  }
}
```

### 3. Get User by ID
**Endpoint:** `GET /api/admin/users/{id}`

Retrieves a specific user by ID including their roles.

#### Response
```json
{
  "data": {
    "id": "user-123-456",
    "username": "john.doe",
    "email": "john.doe@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "enabled": true,
    "emailVerified": true,
    "createdAt": "2024-01-15T10:30:00Z",
    "roles": ["user", "customer", "vip"],
    "attributes": {
      "department": ["Sales"],
      "location": ["New York"]
    }
  },
  "success": true,
  "message": "User details retrieved successfully from Keycloak"
}
```

### 4. Update User
**Endpoint:** `PUT /api/admin/users/{id}`

Updates an existing user in Keycloak.

#### Request Body
```json
{
  "email": "john.doe.new@example.com",
  "firstName": "Jonathan",
  "lastName": "Doe",
  "enabled": false,
  "emailVerified": true,
  "attributes": {
    "department": ["Marketing"],
    "location": ["Boston"]
  }
}
```

#### Response
```json
{
  "data": {
    "id": "user-123-456",
    "success": true
  },
  "success": true,
  "message": "User updated successfully in Keycloak"
}
```

### 5. Delete User
**Endpoint:** `DELETE /api/admin/users/{id}`

Deletes a user from Keycloak.

#### Response
```json
{
  "data": {
    "id": "user-123-456",
    "success": true
  },
  "success": true,
  "message": "User deleted successfully from Keycloak"
}
```

### 6. Assign Roles to User
**Endpoint:** `POST /api/admin/users/{id}/roles`

Assigns roles to a user.

#### Request Body
```json
{
  "roleNames": ["admin", "moderator"]
}
```

#### Response
```json
{
  "data": {
    "userId": "user-123-456",
    "assignedRoles": ["admin", "moderator"],
    "success": true
  },
  "success": true,
  "message": "Roles assigned successfully in Keycloak"
}
```

### 7. Remove Roles from User
**Endpoint:** `DELETE /api/admin/users/{id}/roles`

Removes roles from a user.

#### Request Body
```json
{
  "roleNames": ["admin"]
}
```

### 8. Reset User Password
**Endpoint:** `POST /api/admin/users/{id}/password`

Resets a user's password.

#### Request Body
```json
{
  "password": "new-secure-password",
  "temporary": false
}
```

## Role Management APIs

### 1. Create Role
**Endpoint:** `POST /api/admin/roles`

Creates a new role in Keycloak.

#### Request Body
```json
{
  "name": "content-manager",
  "description": "Content management role with limited permissions"
}
```

#### Response
```json
{
  "data": {
    "name": "content-manager"
  },
  "success": true,
  "message": "Role created successfully in Keycloak",
  "links": {
    "self": "/api/admin/roles/content-manager",
    "update": "/api/admin/roles/content-manager",
    "delete": "/api/admin/roles/content-manager",
    "all-roles": "/api/admin/roles",
    "users": "/api/admin/users"
  }
}
```

#### Validation Rules
- **Name**: Required, max 100 characters, alphanumeric + hyphens/underscores only
- **Description**: Optional, max 500 characters

### 2. List Roles
**Endpoint:** `GET /api/admin/roles`

Retrieves all roles from Keycloak.

#### Response
```json
{
  "data": [
    {
      "id": "role-123",
      "name": "admin",
      "description": "Administrator role",
      "composite": false,
      "clientRole": false
    },
    {
      "id": "role-456",
      "name": "user",
      "description": "Standard user role",
      "composite": false,
      "clientRole": false
    }
  ],
  "success": true,
  "message": "Roles retrieved successfully from Keycloak"
}
```

### 3. Get Role by Name
**Endpoint:** `GET /api/admin/roles/{roleName}`

Retrieves a specific role by name.

#### Response
```json
{
  "data": {
    "id": "role-123",
    "name": "admin",
    "description": "Administrator role",
    "composite": false,
    "clientRole": false,
    "containerId": "realm-id"
  },
  "success": true,
  "message": "Role details retrieved successfully from Keycloak"
}
```

### 4. Update Role
**Endpoint:** `PUT /api/admin/roles/{roleName}`

Updates an existing role.

#### Request Body
```json
{
  "name": "super-admin",
  "description": "Super administrator with full permissions"
}
```

### 5. Delete Role
**Endpoint:** `DELETE /api/admin/roles/{roleName}`

Deletes a role from Keycloak.

#### Response
```json
{
  "data": {
    "name": "content-manager",
    "success": true
  },
  "success": true,
  "message": "Role deleted successfully from Keycloak"
}
```

## Group Management APIs

### 1. Create Group
**Endpoint:** `POST /api/admin/groups`

Creates a new group in Keycloak.

#### Request Body
```json
{
  "name": "Sales Team",
  "attributes": {
    "department": ["Sales"],
    "region": ["North America"]
  },
  "realmRoles": ["user", "customer"]
}
```

### 2. List Groups
**Endpoint:** `GET /api/admin/groups`

Retrieves all groups from Keycloak.

### 3. Add User to Group
**Endpoint:** `PUT /api/admin/users/{userId}/groups/{groupId}`

Adds a user to a group.

### 4. Remove User from Group
**Endpoint:** `DELETE /api/admin/users/{userId}/groups/{groupId}`

Removes a user from a group.

## Session Management APIs

### 1. Get User Sessions
**Endpoint:** `GET /api/admin/users/{userId}/sessions`

Retrieves active sessions for a user.

#### Response
```json
{
  "data": [
    {
      "id": "session-123",
      "username": "john.doe",
      "userId": "user-123-456",
      "ipAddress": "192.168.1.100",
      "start": 1705316400000,
      "lastAccess": 1705320000000,
      "clients": {
        "rys-fashion-web": "2024-01-15T11:00:00Z",
        "rys-fashion-mobile": "2024-01-15T10:30:00Z"
      }
    }
  ],
  "success": true,
  "message": "User sessions retrieved successfully"
}
```

### 2. Logout User Sessions
**Endpoint:** `POST /api/admin/users/{userId}/logout`

Logs out all active sessions for a user.

## Statistics APIs

### 1. Get Realm Statistics
**Endpoint:** `GET /api/admin/stats`

Retrieves comprehensive realm statistics.

#### Response
```json
{
  "data": {
    "totalUsers": 1250,
    "enabledUsers": 1180,
    "disabledUsers": 70,
    "totalRoles": 15,
    "totalGroups": 8,
    "totalClients": 5,
    "activeSessions": 320,
    "lastUpdated": "2024-01-15T12:00:00Z"
  },
  "success": true,
  "message": "Realm statistics retrieved successfully"
}
```

## Error Handling

All APIs follow a consistent error response format:

### Validation Error (400)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "detail": "Please refer to the errors property for additional details.",
  "instance": "/api/admin/users",
  "errors": {
    "Email": ["Email is required"],
    "Username": ["Username must not exceed 100 characters"]
  }
}
```

### Unauthorized Error (401)
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Login failed: Invalid credentials",
  "instance": "/api/auth/login"
}
```

### Not Found Error (404)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "User with ID user-999 not found",
  "instance": "/api/admin/users/user-999"
}
```

### Internal Server Error (500)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500,
  "detail": "Keycloak service is temporarily unavailable",
  "instance": "/api/admin/users"
}
```

## Security Considerations

### Authentication
- All admin endpoints require valid JWT tokens
- Tokens are validated against Keycloak
- Token expiration is enforced

### Authorization
- Role-based permissions are enforced
- Admin operations require appropriate permissions:
  - `admin.user.create` - Create users
  - `admin.user.read` - View users
  - `admin.user.update` - Update users
  - `admin.user.delete` - Delete users
  - `admin.role.create` - Create roles
  - `admin.role.read` - View roles
  - `admin.role.update` - Update roles
  - `admin.role.delete` - Delete roles

### Rate Limiting
- API endpoints are rate-limited to prevent abuse
- Different limits apply to different endpoint types

### Input Validation
- All inputs are validated using FluentValidation
- SQL injection and XSS prevention measures are in place
- File upload restrictions are enforced

### Audit Logging
- All administrative actions are logged
- User authentication events are tracked
- Failed attempts are monitored

## Testing

### Unit Tests
The integration includes comprehensive unit tests covering:

- **CQRS Commands and Queries**: Testing business logic
- **Validation Rules**: Testing input validation
- **Error Handling**: Testing error scenarios
- **Service Layer**: Testing Keycloak service interactions

### Integration Tests
Integration tests cover:

- **End-to-End API flows**: Complete request/response cycles
- **Keycloak Integration**: Real Keycloak interactions
- **Database Operations**: Data persistence validation
- **Authentication Flows**: Token validation and refresh

### Test Examples

#### Unit Test Example
```csharp
[Fact]
public async Task CreateUserCommand_ValidRequest_ReturnsUserId()
{
    // Arrange
    var param = new CreateUserCommand.CreateUserParam
    {
        Username = "testuser",
        Email = "test@example.com",
        FirstName = "Test",
        LastName = "User"
    };

    _keycloakServiceMock
        .Setup(x => x.CreateUserAsync(It.IsAny<CreateKeycloakUserRequest>(), 
                                     It.IsAny<CancellationToken>()))
        .ReturnsAsync("user-123");

    var command = new CreateUserCommand.Command(param);

    // Act
    var result = await _mediator.Send(command);

    // Assert
    result.IsError.Should().BeFalse();
    result.Value.Id.Should().Be("user-123");
}
```

#### Integration Test Example
```csharp
[Fact]
public async Task POST_CreateUser_ValidRequest_ReturnsCreatedUser()
{
    // Arrange
    var request = new CreateUserRequest
    {
        Username = "integration-test-user",
        Email = "test@integration.com",
        FirstName = "Integration",
        LastName = "Test"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/admin/users", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadFromJsonAsync<ApiResponse<CreateUserResult>>();
    content!.Success.Should().BeTrue();
    content.Data.Id.Should().NotBeEmpty();
}
```

### Testing Guidelines

1. **Mock External Dependencies**: Always mock Keycloak service in unit tests
2. **Test Error Scenarios**: Include tests for error conditions
3. **Validate Input**: Test validation rules thoroughly
4. **Test Authorization**: Verify permission requirements
5. **Performance Testing**: Include performance benchmarks
6. **Integration Testing**: Test with real Keycloak instance when possible

## Performance Considerations

### Caching
- Admin tokens are cached to reduce Keycloak calls
- User information is cached with appropriate TTL
- Role information is cached for performance

### Pagination
- Large result sets are paginated
- Default page sizes are configured
- Maximum page sizes are enforced

### Bulk Operations
- Batch operations are supported where possible
- Bulk user imports are optimized
- Role assignments can be batched

### Monitoring
- Response times are monitored
- Error rates are tracked
- Keycloak service health is monitored

This documentation provides a comprehensive guide to the Keycloak integration APIs. For implementation details, refer to the source code and unit tests.