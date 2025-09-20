# ?? Keycloak API Quick Reference Guide

## ?? **Complete Endpoint Summary**

### **?? Authentication Endpoints (OpenID Connect)**
```
Base URL: http://localhost:8080/realms/rys-fashion/protocol/openid-connect
```

| **Endpoint** | **Method** | **Purpose** |
|--------------|------------|-------------|
| `/token` | POST | Login user with password |
| `/token` | POST | Refresh access token |
| `/token` | POST | Get admin token (client_credentials) |
| `/logout` | POST | Logout user session |
| `/userinfo` | GET | Get user info from token |

### **?? User Management Endpoints**
```
Base URL: http://localhost:8080/admin/realms/rys-fashion
Auth: Bearer {admin_token}
```

| **Endpoint** | **Method** | **Purpose** |
|--------------|------------|-------------|
| `/users` | GET | Get all users |
| `/users?search={term}&max={limit}` | GET | Search users |
| `/users/{id}` | GET | Get user by ID |
| `/users` | POST | Create user |
| `/users/{id}` | PUT | Update user |
| `/users/{id}` | DELETE | Delete user |
| `/users/{id}/reset-password` | PUT | Set user password |
| `/users/{id}/send-verify-email` | PUT | Send email verification |
| `/users/count` | GET | Get user count |

### **?? Role Management Endpoints**
```
Base URL: http://localhost:8080/admin/realms/rys-fashion
Auth: Bearer {admin_token}
```

| **Endpoint** | **Method** | **Purpose** |
|--------------|------------|-------------|
| `/roles` | GET | Get all roles |
| `/roles/{name}` | GET | Get role by name |
| `/roles` | POST | Create role |
| `/roles/{name}` | PUT | Update role |
| `/roles/{name}` | DELETE | Delete role |

### **?? User-Role Assignment Endpoints**
```
Base URL: http://localhost:8080/admin/realms/rys-fashion
Auth: Bearer {admin_token}
```

| **Endpoint** | **Method** | **Purpose** |
|--------------|------------|-------------|
| `/users/{id}/role-mappings/realm` | GET | Get user roles |
| `/users/{id}/role-mappings/realm` | POST | Assign roles |
| `/users/{id}/role-mappings/realm` | DELETE | Remove roles |
| `/users/{id}/role-mappings/realm/available` | GET | Get available roles |

---

## ?? **Quick Setup Commands**

### **1. Start Everything**
```powershell
# Complete setup (first time)
./scripts/Setup-Keycloak-Complete.ps1 -FullSetup

# Or step by step
./scripts/Setup-Keycloak-Complete.ps1 -StartKeycloak
./scripts/Setup-Keycloak-Complete.ps1 -ConfigureRealm
./scripts/Setup-Keycloak-Complete.ps1 -TestEndpoints -ClientSecret "your-secret"
```

### **2. Manual Docker Setup**
```bash
# Start services
docker-compose -f docker-compose.keycloak.yml up -d

# Check health
curl http://localhost:8080/health/ready

# Stop services
docker-compose -f docker-compose.keycloak.yml down
```

---

## ?? **Postman Quick Start**

### **1. Import Files**
1. **Collection**: `tests/postman/Keycloak-Complete-API-Collection.postman_collection.json`
2. **Environment**: `tests/postman/Keycloak-Local-Environment.postman_environment.json`

### **2. Configure Environment Variables**
```json
{
  "keycloak_url": "http://localhost:8080",
  "realm": "rys-fashion", 
  "client_id": "rys-fashion-api",
  "client_secret": "YOUR_CLIENT_SECRET_HERE"
}
```

### **3. Test Authentication Flow**
1. Run: **?? Authentication ? 1. Login User**
2. Check: Environment variables auto-populated
3. Run: **?? Authentication ? 2. Get User Info**

### **4. Test Admin Operations**  
1. Admin token acquired automatically
2. Run: **?? User Management ? 1. Get All Users**
3. Run: **?? Role Management ? 1. Get All Roles**

---

## ?? **Testing Scenarios**

### **Scenario 1: User Lifecycle**
```
1. ?? Create User
2. ?? Set User Password  
3. ?? Assign Role to User
4. ?? Login as User
5. ?? Get User Info
6. ?? Update User
7. ?? Remove Role from User
8. ?? Delete User
```

### **Scenario 2: Authentication Flow**
```
1. ?? Login User (get tokens)
2. ?? Get User Info (use access token)
3. ?? Refresh Token (use refresh token)
4. ?? Get User Info (use new access token)
5. ?? Logout (invalidate tokens)
```

### **Scenario 3: Admin Operations**
```
1. ?? Create Role
2. ?? Create User
3. ?? Assign Role to User
4. ?? Create Group
5. ?? Add User to Group
6. ??? Check User Sessions
7. ??? Logout User Sessions
```

---

## ?? **Sample API Calls**

### **Get Admin Token**
```bash
curl -X POST "http://localhost:8080/realms/rys-fashion/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=rys-fashion-api" \
  -d "client_secret=YOUR_CLIENT_SECRET"
```

### **Login User**
```bash  
curl -X POST "http://localhost:8080/realms/rys-fashion/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=rys-fashion-api" \
  -d "client_secret=YOUR_CLIENT_SECRET" \
  -d "username=admin@rys-fashion.com" \
  -d "password=Admin123!" \
  -d "scope=openid profile email"
```

### **Get All Users**
```bash
curl -X GET "http://localhost:8080/admin/realms/rys-fashion/users" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

### **Create User**
```bash
curl -X POST "http://localhost:8080/admin/realms/rys-fashion/users" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "test.user",
    "email": "test.user@example.com",
    "firstName": "Test",
    "lastName": "User", 
    "enabled": true,
    "emailVerified": false
  }'
```

### **Create Role**
```bash
curl -X POST "http://localhost:8080/admin/realms/rys-fashion/roles" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "test-role",
    "description": "Test role for API testing",
    "composite": false
  }'
```

---

## ? **PowerShell Testing Scripts**

### **Quick Token Test**
```powershell
# Get admin token
$tokenResponse = Invoke-RestMethod -Uri "http://localhost:8080/realms/rys-fashion/protocol/openid-connect/token" -Method Post -Body @{
    grant_type = "client_credentials"
    client_id = "rys-fashion-api"
    client_secret = "YOUR_CLIENT_SECRET"
} -ContentType "application/x-www-form-urlencoded"

$adminToken = $tokenResponse.access_token
Write-Host "Admin token acquired: $($adminToken.Substring(0, 20))..."

# Test API call
$headers = @{ Authorization = "Bearer $adminToken" }
$users = Invoke-RestMethod -Uri "http://localhost:8080/admin/realms/rys-fashion/users" -Headers $headers
Write-Host "Retrieved $($users.Count) users"
```

### **User Login Test**
```powershell
# Login user
$loginResponse = Invoke-RestMethod -Uri "http://localhost:8080/realms/rys-fashion/protocol/openid-connect/token" -Method Post -Body @{
    grant_type = "password"
    client_id = "rys-fashion-api"
    client_secret = "YOUR_CLIENT_SECRET"
    username = "admin@rys-fashion.com"
    password = "Admin123!"
    scope = "openid profile email"
} -ContentType "application/x-www-form-urlencoded"

$accessToken = $loginResponse.access_token
Write-Host "User login successful!"

# Get user info
$userHeaders = @{ Authorization = "Bearer $accessToken" }
$userInfo = Invoke-RestMethod -Uri "http://localhost:8080/realms/rys-fashion/protocol/openid-connect/userinfo" -Headers $userHeaders
Write-Host "User: $($userInfo.name) ($($userInfo.email))"
```

---

## ??? **Troubleshooting**

### **Common Issues & Solutions**

#### **? Connection Refused**
```bash
# Check if Keycloak is running
docker ps | grep keycloak

# Check health endpoint
curl http://localhost:8080/health/ready

# Restart if needed
docker-compose -f docker-compose.keycloak.yml restart keycloak
```

#### **? 401 Unauthorized**
```bash
# Verify client secret is correct
# Check in Keycloak Admin: Clients ? rys-fashion-api ? Credentials

# Test admin token acquisition
curl -X POST "http://localhost:8080/realms/rys-fashion/protocol/openid-connect/token" \
  -d "grant_type=client_credentials&client_id=rys-fashion-api&client_secret=YOUR_SECRET"
```

#### **? Realm Not Found**
```bash
# Verify realm exists
curl "http://localhost:8080/realms/rys-fashion/.well-known/openid_configuration"

# Should return OpenID configuration JSON
```

#### **? User Not Found**
```bash
# Check if user exists
curl -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  "http://localhost:8080/admin/realms/rys-fashion/users?username=admin@rys-fashion.com"
```

### **Debug Commands**
```bash
# View logs
docker-compose -f docker-compose.keycloak.yml logs keycloak
docker-compose -f docker-compose.keycloak.yml logs postgres

# Check containers
docker-compose -f docker-compose.keycloak.yml ps

# Restart specific service
docker-compose -f docker-compose.keycloak.yml restart keycloak
```

---

## ?? **API Endpoint Summary**

### **Total Endpoints: 33**
- ?? **Authentication**: 5 endpoints
- ?? **User Management**: 9 endpoints  
- ?? **Role Management**: 5 endpoints
- ?? **User-Role Assignment**: 4 endpoints
- ?? **Group Management**: 7 endpoints
- ??? **Session Management**: 2 endpoints
- ?? **Client Management**: 2 endpoints

### **Test Users Available**
- **admin@rys-fashion.com** / Admin123! (admin role)
- **user@rys-fashion.com** / User123! (user role)  
- **customer@rys-fashion.com** / Customer123! (customer role)

### **Available Roles**
- **admin** - Full system access
- **user** - Standard user access
- **customer** - Customer access
- **manager** - Management access
- **viewer** - Read-only access

---

## ?? **Success Criteria**

? **Setup Complete When:**
- Keycloak running on http://localhost:8080
- Realm 'rys-fashion' configured
- Client 'rys-fashion-api' created with secret
- Test users created with roles assigned
- Postman collection working
- All API endpoints responding correctly

? **Ready for Development When:**
- All 33 endpoints tested successfully
- Authentication flow working
- Admin operations functional
- User management operational
- Role assignment working

?? **You're ready to integrate with your Rys.Fashion application!**