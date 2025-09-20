# Keycloak Integration Testing Guide

## Overview

This guide provides step-by-step instructions for setting up, testing, and troubleshooting the Keycloak integration in the Rys.Fashion system.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Local Development Setup](#local-development-setup)
3. [Keycloak Configuration](#keycloak-configuration)
4. [Application Configuration](#application-configuration)
5. [Testing the Integration](#testing-the-integration)
6. [Automated Testing](#automated-testing)
7. [Troubleshooting](#troubleshooting)
8. [Production Deployment](#production-deployment)

## Prerequisites

### Required Software
- Docker Desktop 4.0+
- .NET 9 SDK
- Visual Studio 2022 or VS Code
- Postman or similar API testing tool
- Git

### Recommended Tools
- Docker Compose
- PowerShell 7+
- Azure CLI (for cloud deployment)
- pgAdmin (for database management)

## Local Development Setup

### 1. Clone and Setup Project

```powershell
# Clone the repository
git clone https://github.com/ruanfa-dev/rys-fashion.git
cd rys-fashion

# Checkout the Keycloak feature branch
git checkout feat/migrate-keycloak

# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build
```

### 2. Start Infrastructure Services

```powershell
# Start Keycloak and PostgreSQL
docker-compose -f docker-compose.keycloak.yml up -d

# Verify services are running
docker-compose -f docker-compose.keycloak.yml ps
```

Expected output:
```
Name                    Command               State                     Ports
keycloak_postgres      docker-entrypoint.sh postgres   Up    0.0.0.0:5433->5432/tcp
keycloak_keycloak      /opt/keycloak/bin/kc.sh start   Up    0.0.0.0:8080->8080/tcp
```

### 3. Wait for Services to be Ready

```powershell
# Check Keycloak health
curl http://localhost:8080/health/ready

# Check PostgreSQL connection
docker exec keycloak_postgres psql -U keycloak -d keycloak -c "SELECT 1;"
```

## Keycloak Configuration

### 1. Access Keycloak Admin Console

1. Open browser to http://localhost:8080
2. Click "Administration Console"
3. Login with:
   - Username: `admin`
   - Password: `admin`

### 2. Create Realm

1. Click "Create Realm"
2. Enter realm name: `rys-fashion`
3. Click "Create"

### 3. Create Client

1. Go to "Clients" ? "Create client"
2. Enter client details:
   - Client type: `OpenID Connect`
   - Client ID: `rys-fashion-api`
   - Name: `Rys Fashion API`
3. Click "Next"
4. Configure capabilities:
   - Client authentication: `On`
   - Authorization: `Off`
   - Standard flow: `On`
   - Direct access grants: `On`
   - Service accounts roles: `On`
5. Click "Next"
6. Configure login settings:
   - Root URL: `https://localhost:7001`
   - Home URL: `https://localhost:7001`
   - Valid redirect URIs: `https://localhost:7001/*`
   - Valid post logout redirect URIs: `https://localhost:7001/*`
   - Web origins: `https://localhost:7001`
7. Click "Save"

### 4. Get Client Secret

1. Go to "Clients" ? "rys-fashion-api" ? "Credentials"
2. Copy the "Client secret" value
3. Save this for application configuration

### 5. Create Roles

1. Go to "Realm roles" ? "Create role"
2. Create the following roles:
   - `admin` - Administrator role
   - `user` - Standard user role
   - `customer` - Customer role
   - `manager` - Manager role

### 6. Create Test Users

1. Go to "Users" ? "Create new user"
2. Create admin user:
   - Username: `admin@rys-fashion.com`
   - Email: `admin@rys-fashion.com`
   - First name: `Admin`
   - Last name: `User`
   - Email verified: `Yes`
   - Enabled: `Yes`
3. Click "Create"
4. Go to "Credentials" tab
5. Set password: `Admin123!`
6. Set temporary: `Off`
7. Go to "Role mapping" tab
8. Assign "admin" role

Repeat for other test users:
- `user@rys-fashion.com` with "user" role
- `customer@rys-fashion.com` with "customer" role

## Application Configuration

### 1. Update appsettings.json

```json
{
  "Authentication": {
    "Keycloak": {
      "Authority": "http://localhost:8080/realms/rys-fashion",
      "ClientId": "rys-fashion-api",
      "ClientSecret": "YOUR_CLIENT_SECRET_HERE",
      "Realm": "rys-fashion",
      "AdminApiUrl": "http://localhost:8080",
      "RequireHttpsMetadata": false,
      "ValidateIssuer": true,
      "ValidateAudience": true,
      "ValidateLifetime": true,
      "ClockSkew": "00:05:00"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Infrastructure.Security.Authentication": "Debug"
    }
  }
}
```

### 2. Environment Variables (Optional)

```powershell
# Set environment variables for sensitive data
$env:Authentication__Keycloak__ClientSecret = "YOUR_CLIENT_SECRET"
$env:Authentication__Keycloak__Authority = "http://localhost:8080/realms/rys-fashion"
```

### 3. Update Docker Compose (if using)

```yaml
version: '3.8'
services:
  api:
    build:
      context: .
      dockerfile: src/Web.Api/Dockerfile
    ports:
      - "7001:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - Authentication__Keycloak__Authority=http://keycloak:8080/realms/rys-fashion
      - Authentication__Keycloak__ClientSecret=${KEYCLOAK_CLIENT_SECRET}
    depends_on:
      - keycloak
    networks:
      - rys-fashion-network
```

## Testing the Integration

### 1. Start the Application

```powershell
# Start the API
cd src/Web.Api
dotnet run
```

The API should start on https://localhost:7001

### 2. Test Authentication Endpoints

#### Login Test
```powershell
# Test login
$loginBody = @{
    username = "admin@rys-fashion.com"
    password = "Admin123!"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "https://localhost:7001/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"

# Save the access token
$accessToken = $response.data.accessToken
$refreshToken = $response.data.refreshToken

Write-Host "Login successful! Access token: $($accessToken.Substring(0, 50))..."
```

#### User Info Test
```powershell
# Test get user info
$headers = @{
    Authorization = "Bearer $accessToken"
}

$userInfo = Invoke-RestMethod -Uri "https://localhost:7001/api/auth/userinfo" -Method Get -Headers $headers
Write-Host "User info: $($userInfo.data | ConvertTo-Json -Depth 3)"
```

#### Refresh Token Test
```powershell
# Test refresh token
$refreshBody = @{
    refreshToken = $refreshToken
} | ConvertTo-Json

$refreshResponse = Invoke-RestMethod -Uri "https://localhost:7001/api/auth/refresh" -Method Post -Body $refreshBody -ContentType "application/json"
Write-Host "Token refreshed successfully!"
```

### 3. Test User Management Endpoints

#### Get Users
```powershell
# Test get users (requires admin role)
$headers = @{
    Authorization = "Bearer $accessToken"
}

$users = Invoke-RestMethod -Uri "https://localhost:7001/api/admin/users" -Method Get -Headers $headers
Write-Host "Users retrieved: $($users.data.Count)"
```

#### Create User
```powershell
# Test create user
$createUserBody = @{
    username = "test.user"
    email = "test.user@example.com"
    firstName = "Test"
    lastName = "User"
    enabled = $true
    emailVerified = $false
    password = "TestPass123!"
    temporaryPassword = $false
    roleNames = @("user")
} | ConvertTo-Json

$createResponse = Invoke-RestMethod -Uri "https://localhost:7001/api/admin/users" -Method Post -Body $createUserBody -ContentType "application/json" -Headers $headers
Write-Host "User created with ID: $($createResponse.data.id)"
```

#### Search Users
```powershell
# Test search users
$searchResponse = Invoke-RestMethod -Uri "https://localhost:7001/api/admin/users?search=test&max=10" -Method Get -Headers $headers
Write-Host "Search results: $($searchResponse.data.Count) users found"
```

### 4. Test Role Management Endpoints

#### Create Role
```powershell
# Test create role
$createRoleBody = @{
    name = "test-role"
    description = "Test role for integration testing"
} | ConvertTo-Json

$roleResponse = Invoke-RestMethod -Uri "https://localhost:7001/api/admin/roles" -Method Post -Body $createRoleBody -ContentType "application/json" -Headers $headers
Write-Host "Role created: $($roleResponse.data.name)"
```

#### Get Roles
```powershell
# Test get roles
$roles = Invoke-RestMethod -Uri "https://localhost:7001/api/admin/roles" -Method Get -Headers $headers
Write-Host "Roles retrieved: $($roles.data.Count)"
```

### 5. Test Role Assignment

```powershell
# Assign role to user
$assignRoleBody = @{
    roleNames = @("test-role")
} | ConvertTo-Json

$assignResponse = Invoke-RestMethod -Uri "https://localhost:7001/api/admin/users/$($createResponse.data.id)/roles" -Method Post -Body $assignRoleBody -ContentType "application/json" -Headers $headers
Write-Host "Role assigned successfully"
```

## Automated Testing

### 1. Run Unit Tests

```powershell
# Run all unit tests
dotnet test tests/UseCases.UnitTests/ --verbosity normal

# Run specific test category
dotnet test tests/UseCases.UnitTests/ --filter Category=KeycloakIntegration

# Run tests with coverage
dotnet test tests/UseCases.UnitTests/ --collect:"XPlat Code Coverage"
```

### 2. Run Integration Tests

```powershell
# Start test infrastructure
docker-compose -f docker-compose.keycloak.yml up -d

# Wait for services to be ready
Start-Sleep -Seconds 30

# Run integration tests
dotnet test tests/Infrastructure.IntegrationTests/ --verbosity normal

# Clean up
docker-compose -f docker-compose.keycloak.yml down
```

### 3. Run API Tests with Newman (if Postman collection exists)

```powershell
# Install Newman
npm install -g newman

# Run Postman collection
newman run tests/postman/Keycloak-Integration-Tests.postman_collection.json -e tests/postman/Local-Environment.postman_environment.json
```

### 4. Automated Test Script

Create `test-keycloak-integration.ps1`:

```powershell
#!/usr/bin/env pwsh

param(
    [switch]$SkipInfrastructure,
    [switch]$CleanupOnly,
    [string]$TestFilter = "*"
)

$ErrorActionPreference = "Stop"

function Write-Section($message) {
    Write-Host "`n=== $message ===" -ForegroundColor Cyan
}

function Test-ServiceHealth($url, $maxAttempts = 30) {
    Write-Host "Waiting for service at $url to be ready..." -ForegroundColor Yellow
    for ($i = 1; $i -le $maxAttempts; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $url -Method Get -TimeoutSec 5
            if ($response.StatusCode -eq 200) {
                Write-Host "Service is ready!" -ForegroundColor Green
                return $true
            }
        }
        catch {
            Write-Host "Attempt $i/$maxAttempts failed, retrying..." -ForegroundColor Yellow
            Start-Sleep -Seconds 2
        }
    }
    throw "Service at $url did not become ready within $($maxAttempts * 2) seconds"
}

try {
    if ($CleanupOnly) {
        Write-Section "Cleaning up test infrastructure"
        docker-compose -f docker-compose.keycloak.yml down -v
        exit 0
    }

    if (-not $SkipInfrastructure) {
        Write-Section "Starting test infrastructure"
        docker-compose -f docker-compose.keycloak.yml up -d
        
        Write-Section "Waiting for services to be ready"
        Test-ServiceHealth "http://localhost:8080/health/ready"
    }

    Write-Section "Running unit tests"
    dotnet test tests/UseCases.UnitTests/ --filter $TestFilter --verbosity normal --logger "console;verbosity=detailed"

    Write-Section "Running integration tests"
    dotnet test tests/Infrastructure.IntegrationTests/ --filter $TestFilter --verbosity normal --logger "console;verbosity=detailed"

    Write-Section "Building and starting API"
    Start-Process -FilePath "dotnet" -ArgumentList "run --project src/Web.Api" -PassThru -NoNewWindow
    Start-Sleep -Seconds 10

    Write-Section "Running API health check"
    Test-ServiceHealth "https://localhost:7001/health"

    Write-Section "Running integration scenarios"
    & .\tests\scripts\test-api-scenarios.ps1

    Write-Section "All tests completed successfully!"
}
catch {
    Write-Host "Test execution failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
finally {
    if (-not $SkipInfrastructure) {
        Write-Section "Cleaning up test infrastructure"
        docker-compose -f docker-compose.keycloak.yml down
    }
}
```

## Troubleshooting

### Common Issues and Solutions

#### 1. Keycloak Connection Issues

**Symptom**: `Failed to obtain admin token` or connection timeouts

**Solutions**:
```powershell
# Check if Keycloak is running
docker-compose -f docker-compose.keycloak.yml ps

# Check Keycloak logs
docker-compose -f docker-compose.keycloak.yml logs keycloak

# Restart Keycloak
docker-compose -f docker-compose.keycloak.yml restart keycloak

# Check network connectivity
curl http://localhost:8080/health/ready
```

#### 2. Authentication Failures

**Symptom**: 401 Unauthorized responses

**Solutions**:
```powershell
# Verify client configuration in Keycloak
# Check client secret matches configuration
# Ensure user exists and has correct password
# Verify realm name is correct

# Test direct token request
$body = @{
    grant_type = "password"
    client_id = "rys-fashion-api"
    client_secret = "YOUR_CLIENT_SECRET"
    username = "admin@rys-fashion.com"
    password = "Admin123!"
} | ConvertTo-Json

curl -X POST http://localhost:8080/realms/rys-fashion/protocol/openid-connect/token -H "Content-Type: application/json" -d $body
```

#### 3. Role Assignment Issues

**Symptom**: Users don't have expected permissions

**Solutions**:
```powershell
# Check user roles in Keycloak admin console
# Verify role mapping is correct
# Check JWT token claims

# Decode JWT token to inspect claims
[System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($tokenPayload))
```

#### 4. SSL/TLS Issues

**Symptom**: SSL certificate errors

**Solutions**:
```powershell
# For development, disable HTTPS redirect
# Update appsettings.json:
"Kestrel": {
  "EndpointDefaults": {
    "Protocols": "Http1AndHttp2AndHttp3"
  }
}

# Or use development certificates
dotnet dev-certs https --trust
```

#### 5. Database Connection Issues

**Symptom**: Keycloak fails to start with database errors

**Solutions**:
```powershell
# Check PostgreSQL container
docker logs keycloak_postgres

# Reset database
docker-compose -f docker-compose.keycloak.yml down -v
docker-compose -f docker-compose.keycloak.yml up -d

# Check database connectivity
docker exec keycloak_postgres psql -U keycloak -d keycloak -c "SELECT version();"
```

### Debugging Tips

#### 1. Enable Debug Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Infrastructure.Security.Authentication": "Debug",
      "Microsoft.AspNetCore.Authentication": "Debug",
      "Microsoft.AspNetCore.Authorization": "Debug"
    }
  }
}
```

#### 2. Use Postman for API Testing

Import the provided Postman collection:
- Set environment variables for base URL and tokens
- Use pre-request scripts for token refresh
- Add tests for response validation

#### 3. Monitor HTTP Traffic

Use tools like Fiddler or browser developer tools to inspect:
- Request/response headers
- JWT token contents
- HTTP status codes
- Response timing

#### 4. Check Keycloak Events

In Keycloak admin console:
1. Go to "Events" ? "Config"
2. Enable event logging
3. Check "Login events" and "Admin events"
4. Review failed authentication attempts

## Production Deployment

### 1. Security Checklist

- [ ] Use HTTPS everywhere
- [ ] Store client secrets securely (Azure Key Vault, etc.)
- [ ] Enable Keycloak security features
- [ ] Configure proper CORS settings
- [ ] Set up proper network security
- [ ] Enable audit logging
- [ ] Configure rate limiting
- [ ] Set up monitoring and alerting

### 2. Performance Optimization

- [ ] Configure connection pooling
- [ ] Enable response caching where appropriate
- [ ] Set up CDN for static assets
- [ ] Configure proper timeout values
- [ ] Enable compression
- [ ] Set up load balancing
- [ ] Configure database performance settings

### 3. Monitoring and Observability

- [ ] Set up application insights
- [ ] Configure health checks
- [ ] Set up log aggregation
- [ ] Create dashboards for key metrics
- [ ] Set up alerting for failures
- [ ] Monitor token usage patterns
- [ ] Track authentication success/failure rates

### 4. Backup and Recovery

- [ ] Set up database backups
- [ ] Test backup restoration procedures
- [ ] Document recovery procedures
- [ ] Set up monitoring for backup success
- [ ] Configure cross-region replication if needed

This comprehensive testing guide provides everything needed to successfully implement, test, and deploy the Keycloak integration. Follow the steps in order and use the troubleshooting section when issues arise.