# Keycloak Integration Setup Guide

## Prerequisites
- Docker and Docker Compose installed
- .NET 9 SDK
- Your existing API project

## Step 1: Start Keycloak with Docker

```bash
# Start Keycloak and PostgreSQL
docker-compose -f docker-compose.keycloak.yml up -d

# Check if services are running
docker-compose -f docker-compose.keycloak.yml ps
```

## Step 2: Configure Keycloak Realm

1. Open Keycloak Admin Console: http://localhost:8080
2. Login with admin/admin
3. Create a new realm called "rys-fashion"

### Create Realm Configuration:
- Name: `rys-fashion`
- Display name: `Rys Fashion E-commerce`
- Enabled: `true`

## Step 3: Create Client for API

1. Go to Clients ? Create Client
2. Configuration:
   - Client ID: `rys-fashion-api`
   - Client type: `OpenID Connect`
   - Client authentication: `On`
   - Standard flow: `Enabled`
   - Direct access grants: `Enabled`
   - Service accounts roles: `Enabled`

3. After creation, go to Settings tab:
   - Access Type: `confidential`
   - Valid redirect URIs: `http://localhost:5000/*`, `https://localhost:5001/*`
   - Web origins: `http://localhost:5000`, `https://localhost:5001`

4. Go to Credentials tab and copy the Client Secret

## Step 4: Create Client Scopes

Create the following client scopes:

### User Management Scope
- Name: `user-management`
- Description: `User management operations`
- Type: `Default`

### Role Management Scope  
- Name: `role-management`
- Description: `Role management operations`
- Type: `Default`

### Permission Management Scope
- Name: `permission-management` 
- Description: `Permission management operations`
- Type: `Default`

### Todo Management Scope
- Name: `todo-management`
- Description: `Todo operations`
- Type: `Default`

## Step 5: Create Roles

Create the following realm roles:

1. `admin` - Full system administrator
2. `user-manager` - Can manage users
3. `role-manager` - Can manage roles
4. `permission-manager` - Can manage permissions
5. `user` - Regular user with basic access

## Step 6: Create Custom User Attributes

Go to Realm Settings ? User Profile and add:
- `first_name` (String)
- `last_name` (String) 
- `date_of_birth` (String)
- `profile_image_path` (String)

## Step 7: Client Mappers

Add the following mappers to your client:

### Role Mapper
- Name: `realm-roles`
- Mapper Type: `User Realm Role`
- Token Claim Name: `roles`
- Claim JSON Type: `String`
- Add to ID token: `On`
- Add to access token: `On`

### Custom Claims Mappers
Create mappers for custom user attributes:
- `first_name` ? `first_name`
- `last_name` ? `last_name`
- `date_of_birth` ? `date_of_birth`
- `profile_image_path` ? `profile_image_path`

## Step 8: Test User Creation

Create a test user:
1. Go to Users ? Add User
2. Username: `testuser`
3. Email: `test@example.com`
4. First Name: `Test`
5. Last Name: `User`
6. Email Verified: `Yes`
7. Enabled: `Yes`

Set password:
1. Go to Credentials tab
2. Set password: `testuser123`
3. Temporary: `Off`

Assign roles:
1. Go to Role Mappings tab
2. Assign `user` role

## Environment Variables for Your API

Add these to your appsettings.json or environment:

```json
{
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/rys-fashion",
    "Audience": "rys-fashion-api",
    "ClientId": "rys-fashion-api",
    "ClientSecret": "your-client-secret-from-keycloak",
    "RequireHttpsMetadata": false,
    "ValidateAudience": true,
    "ValidateIssuer": true,
    "ClockSkew": "00:05:00"
  }
}
```

## API Endpoints You'll Need to Call

The following Keycloak Admin REST API endpoints will be integrated into your service:

### Authentication
- `POST /realms/{realm}/protocol/openid-connect/token` - Get access token
- `POST /realms/{realm}/protocol/openid-connect/logout` - Logout user

### User Management  
- `GET /admin/realms/{realm}/users` - Get all users
- `POST /admin/realms/{realm}/users` - Create user
- `GET /admin/realms/{realm}/users/{id}` - Get user by ID
- `PUT /admin/realms/{realm}/users/{id}` - Update user
- `DELETE /admin/realms/{realm}/users/{id}` - Delete user

### Role Management
- `GET /admin/realms/{realm}/roles` - Get all roles
- `POST /admin/realms/{realm}/roles` - Create role
- `GET /admin/realms/{realm}/roles/{role-name}` - Get role by name
- `PUT /admin/realms/{realm}/roles/{role-name}` - Update role
- `DELETE /admin/realms/{realm}/roles/{role-name}` - Delete role

### User Role Assignment
- `GET /admin/realms/{realm}/users/{id}/role-mappings/realm` - Get user roles
- `POST /admin/realms/{realm}/users/{id}/role-mappings/realm` - Assign roles to user
- `DELETE /admin/realms/{realm}/users/{id}/role-mappings/realm` - Remove roles from user

## Next Steps

1. Run the Docker Compose to start Keycloak
2. Configure the realm as described above
3. Update your .NET API with the Keycloak integration code
4. Test the authentication flow