# Keycloak Resource-Scope Authorization Usage Guide

## ?? **Overview**

This guide demonstrates how to use the new resource and scope-based authorization system with Keycloak integration.

## ?? **Key Concepts**

### **Resources**
Resources represent the entities/objects that can be protected:
- `users` - User management
- `products` - Product management  
- `orders` - Order management
- `roles` - Role management
- `system` - System administration
- `reports` - Reports and analytics

### **Scopes**
Scopes represent the actions that can be performed on resources:
- `create` - Create new entities
- `read` - View entities
- `update` - Modify entities
- `delete` - Remove entities
- `manage` - Full management access
- `approve` - Approval workflows
- `export` - Data export

### **Permissions**
Permissions combine resources and scopes in the format: `resource:scope`
- `users:create` - Create users
- `products:read` - View products
- `orders:manage` - Full order management

## ??? **Implementation**

### **1. Endpoint Authorization**

#### **Using Resource Extensions**
```csharp
// User management endpoints
group.MapPost("/users", CreateUser)
    .RequireUserCreate(); // Requires "users:create"

group.MapGet("/users", GetUsers)
    .RequireUserRead(); // Requires "users:read"

group.MapPut("/users/{id}", UpdateUser)
    .RequireUserUpdate(); // Requires "users:update"

group.MapDelete("/users/{id}", DeleteUser)
    .RequireUserDelete(); // Requires "users:delete"
```

#### **Using Generic Resource-Scope**
```csharp
// Product management endpoints
group.MapPost("/products", CreateProduct)
    .RequireResourcePermission("products", "create");

group.MapGet("/products", GetProducts)
    .RequireResourcePermission("products", "read");

group.MapPut("/products/{id}", UpdateProduct)
    .RequireResourcePermission("products", "update");
```

#### **Multiple Permissions**
```csharp
// Complex operations requiring multiple permissions
group.MapPost("/orders/{id}/approve", ApproveOrder)
    .RequireResourcePermissions(
        "orders:approve",
        "orders:update",
        "order-payments:read"
    );
```

### **2. Controller/Handler Authorization**

#### **Using Attributes**
```csharp
[RequestAuthorize(permissions: "users:create,users:update")]
public class CreateUserHandler : ICommandHandler<CreateUserCommand, Result>
{
    public async Task<ErrorOr<Result>> Handle(CreateUserCommand request, CancellationToken ct)
    {
        // Handler implementation
    }
}
```

#### **Using Service Authorization**
```csharp
public class UserService
{
    private readonly IUserAuthorizationProvider _authProvider;
    private readonly IUserContext _userContext;

    public async Task<Result> CreateUserAsync(CreateUserRequest request)
    {
        // Check permission programmatically
        var userAuth = await _authProvider.GetUserAuthorizationAsync(_userContext.UserId!.Value);
        
        if (!userAuth.Permissions.Contains("users:create"))
        {
            return Error.Forbidden("Insufficient permissions to create users");
        }

        // Implementation
    }
}
```

### **3. Role-Based Templates**

The system includes predefined role templates:

#### **Super Admin**
- All permissions across all resources and scopes

#### **Admin**
- User management: All operations
- Role management: All operations  
- System administration: All operations
- Reports: Read, view, export

#### **Manager**
- Product management: All operations
- Order management: All operations
- Customer service: All operations
- Dashboard: Read, view

#### **Staff**
- Products: Read, update, view
- Orders: Read, update, view
- Customer support: Read, create, update
- Limited operational access

#### **Customer**
- User profiles: Read, update (own only)
- Orders: Create, read (own only)
- Products: Read, browse
- Support tickets: Create, view (own only)

#### **Viewer**
- Read-only access across all resources

## ?? **Seeding Keycloak**

### **Automatic Seeding**
```csharp
// In Program.cs or Startup
public static async Task Main(string[] args)
{
    var app = builder.Build();
    
    // Seed Keycloak with roles and permissions
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<KeycloakAuthorizationSeeder>();
    await seeder.SeedAuthorizationDataAsync();
    
    app.Run();
}
```

### **Manual Role Management**
```csharp
// Check role permissions
var adminPermissions = KeycloakAuthorizationSeeder.GetRolePermissions("admin");

// Validate role permission
bool canCreateUsers = KeycloakAuthorizationSeeder.RoleHasPermission("admin", "users:create");

// Get resources for scope
var readableResources = KeycloakAuthorizationSeeder.GetRoleResourcesForScope("staff", "read");
```

## ?? **Advanced Usage**

### **Dynamic Permission Checking**
```csharp
public class ResourceAuthorizationService
{
    private readonly IUserAuthorizationProvider _authProvider;

    public async Task<bool> CanAccessResourceAsync(Guid userId, string resource, string scope)
    {
        var provider = _authProvider as KeycloakUserAuthorizationProvider;
        return await provider!.HasResourcePermissionAsync(userId, resource, scope);
    }

    public async Task<IEnumerable<string>> GetAccessibleResourcesAsync(Guid userId, string scope)
    {
        var provider = _authProvider as KeycloakUserAuthorizationProvider;
        return await provider!.GetUserResourcesForScopeAsync(userId, scope);
    }
}
```

### **Custom Resource Definitions**
```csharp
// Add custom resources in Resources.cs
public static class Resources
{
    // Existing resources...
    
    // Custom business resources
    public const string Campaigns = "campaigns";
    public const string Promotions = "promotions";
    public const string Suppliers = "suppliers";
}

// Update ResourceScopeMapping.cs
public static readonly Dictionary<string, string[]> Mappings = new()
{
    // Existing mappings...
    
    [Resources.Campaigns] = [
        Scopes.Create, Scopes.Read, Scopes.Update, Scopes.Delete,
        Scopes.Publish, Scopes.Approve, Scopes.Manage
    ]
};
```

## ?? **Configuration Example**

### **appsettings.json**
```json
{
  "Authentication": {
    "Keycloak": {
      "Authority": "http://localhost:8080/realms/rys-fashion",
      "ClientId": "rys-fashion-api",
      "ClientSecret": "your-client-secret",
      "Realm": "rys-fashion",
      "RequireHttpsMetadata": false,
      "ValidateIssuer": true,
      "ValidateAudience": true
    }
  },
  "Authorization": {
    "AuthUserCache": {
      "UserAuthCacheExpiryInMinutes": 30,
      "UserAuthCacheSlidingInMinutes": 15
    }
  }
}
```

## ?? **Testing**

### **Unit Testing**
```csharp
[Test]
public void RoleTemplate_Admin_Should_Have_User_Management_Permissions()
{
    // Arrange
    var adminRole = RolePermissionTemplates.Admin;
    
    // Act & Assert
    Assert.That(adminRole.Permissions, Contains.Item("users:create"));
    Assert.That(adminRole.Permissions, Contains.Item("users:read"));
    Assert.That(adminRole.Permissions, Contains.Item("users:update"));
    Assert.That(adminRole.Permissions, Contains.Item("users:delete"));
}
```

### **Integration Testing**
```csharp
[Test]
public async Task Endpoint_Should_Require_Correct_Permission()
{
    // Arrange
    var client = _factory.CreateClient();
    var token = await GetTokenWithPermissions("users:create");
    client.DefaultRequestHeaders.Authorization = new("Bearer", token);
    
    // Act
    var response = await client.PostAsync("/api/users", content);
    
    // Assert
    Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
}
```

This resource-scope based authorization system provides:
- ? Fine-grained permissions
- ? Role-based templates
- ? Keycloak integration
- ? Easy endpoint configuration
- ? Programmatic authorization
- ? Caching for performance
- ? Hierarchical roles