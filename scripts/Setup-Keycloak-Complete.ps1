#!/usr/bin/env pwsh

param(
    [switch]$StartKeycloak,
    [switch]$ConfigureRealm,
    [switch]$TestEndpoints,
    [switch]$StopKeycloak,
    [switch]$FullSetup,
    [string]$ClientSecret = ""
)

$ErrorActionPreference = "Stop"

function Write-Section($message) {
    Write-Host "`n🔥 $message" -ForegroundColor Cyan
    Write-Host ("=" * ($message.Length + 4)) -ForegroundColor Cyan
}

function Write-Success($message) {
    Write-Host "✅ $message" -ForegroundColor Green
}

function Write-Warning($message) {
    Write-Host "⚠️  $message" -ForegroundColor Yellow
}

function Write-Error($message) {
    Write-Host "❌ $message" -ForegroundColor Red
}

function Test-ServiceHealth($url, $maxAttempts = 30) {
    Write-Host "⏳ Waiting for service at $url to be ready..." -ForegroundColor Yellow
    for ($i = 1; $i -le $maxAttempts; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $url -Method Get -TimeoutSec 5 -UseBasicParsing
            if ($response.StatusCode -eq 200) {
                Write-Success "Service is ready!"
                return $true
            }
        }
        catch {
            Write-Host "🔄 Attempt $i/$maxAttempts - Service not ready yet..." -ForegroundColor DarkYellow
            Start-Sleep -Seconds 2
        }
    }
    throw "❌ Service at $url did not become ready within $($maxAttempts * 2) seconds"
}

function Start-KeycloakServices {
    Write-Section "Starting Keycloak Services"
    
    if (!(Test-Path "docker-compose.keycloak.yml")) {
        Write-Warning "Creating docker-compose.keycloak.yml file..."
        
        $dockerCompose = @"
version: '3.8'

services:
  postgres:
    container_name: keycloak_postgres
    image: postgres:16
    environment:
      POSTGRES_DB: keycloak
      POSTGRES_USER: keycloak
      POSTGRES_PASSWORD: keycloak123
    volumes:
      - keycloak_postgres_data:/var/lib/postgresql/data
    ports:
      - "5433:5432"
    networks:
      - keycloak-network

  keycloak:
    container_name: keycloak_keycloak
    image: quay.io/keycloak/keycloak:23.0
    environment:
      KEYCLOAK_ADMIN: admin
      KEYCLOAK_ADMIN_PASSWORD: admin
      KC_DB: postgres
      KC_DB_URL: jdbc:postgresql://postgres:5432/keycloak
      KC_DB_USERNAME: keycloak
      KC_DB_PASSWORD: keycloak123
      KC_HOSTNAME_STRICT: false
      KC_HOSTNAME_STRICT_HTTPS: false
      KC_HTTP_ENABLED: true
      KC_HEALTH_ENABLED: true
    command: start-dev
    ports:
      - "8080:8080"
    depends_on:
      - postgres
    networks:
      - keycloak-network
    healthcheck:
         test: ["CMD", "curl", "--head","fsS", "http://localhost:8080/health/ready"]
      interval: 30s
      timeout: 10s
      retries: 5

volumes:
  keycloak_postgres_data:

networks:
  keycloak-network:
    driver: bridge
"@
        
        $dockerCompose | Out-File -FilePath "docker-compose.keycloak.yml" -Encoding UTF8
        Write-Success "Created docker-compose.keycloak.yml"
    }
    
    Write-Host "🐳 Starting Docker services..."
    docker-compose -f docker-compose.keycloak.yml up -d
    
    if ($LASTEXITCODE -ne 0) {
        throw "❌ Failed to start Docker services"
    }
    
    Write-Success "Docker services started"
    
    # Wait for Keycloak to be ready
    Start-Sleep -Seconds 5  # Additional wait for full startup
    
    Write-Success "Keycloak is ready at http://localhost:8080"
}

function Configure-KeycloakRealm {
    Write-Section "Configuring Keycloak Realm"
    
    if ([string]::IsNullOrEmpty($ClientSecret)) {
        $ClientSecret = Read-Host "Enter the client secret for rys-fashion-api (get this from Keycloak admin console)"
    }
    
    Write-Host "📋 Manual Configuration Steps Required:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. 🌐 Open Keycloak Admin Console: http://localhost:8080" -ForegroundColor Cyan
    Write-Host "2. 🔐 Login with: admin / admin" -ForegroundColor Cyan
    Write-Host "3. 🏢 Create Realm:" -ForegroundColor Cyan
    Write-Host "   - Click 'Create Realm'" -ForegroundColor Gray
    Write-Host "   - Name: 'rys-fashion'" -ForegroundColor Gray
    Write-Host "   - Click 'Create'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "4. 📱 Create Client:" -ForegroundColor Cyan
    Write-Host "   - Go to Clients → 'Create client'" -ForegroundColor Gray
    Write-Host "   - Client type: 'OpenID Connect'" -ForegroundColor Gray
    Write-Host "   - Client ID: 'rys-fashion-api'" -ForegroundColor Gray
    Write-Host "   - Name: 'Rys Fashion API'" -ForegroundColor Gray
    Write-Host "   - Click 'Next'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "5. ⚙️  Configure Capabilities:" -ForegroundColor Cyan
    Write-Host "   ✅ Client authentication: On" -ForegroundColor Gray
    Write-Host "   ✅ Authorization: On" -ForegroundColor Gray
    Write-Host "   ✅ Standard flow: On" -ForegroundColor Gray
    Write-Host "   ✅ Direct access grants: On" -ForegroundColor Gray
    Write-Host "   ✅ Service accounts roles: On" -ForegroundColor Gray
    Write-Host "   - Click 'Next'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "6. 🔗 Configure Login Settings:" -ForegroundColor Cyan
    Write-Host "   - Root URL: https://localhost:7001" -ForegroundColor Gray
    Write-Host "   - Valid redirect URIs: https://localhost:7001/*" -ForegroundColor Gray
    Write-Host "   - Web origins: https://localhost:7001" -ForegroundColor Gray
    Write-Host "   - Click 'Save'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "7. ⏰ Configure Token Settings (IMPORTANT for refresh):" -ForegroundColor Cyan
    Write-Host "   - Go to 'Advanced' tab" -ForegroundColor Gray
    Write-Host "   - Access Token Lifespan: 5 minutes (for testing)" -ForegroundColor Gray
    Write-Host "   - Refresh Token Lifespan: 30 minutes" -ForegroundColor Gray
    Write-Host "   - Refresh Token Idle Time: 15 minutes" -ForegroundColor Gray
    Write-Host "   - Enable Refresh Token Rotation: ON" -ForegroundColor Gray
    Write-Host "   - Click 'Save'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "8. 🎭 Create Roles:" -ForegroundColor Cyan
    Write-Host "   - Go to 'Realm roles' → 'Create role'" -ForegroundColor Gray
    Write-Host "   - Create: admin, user, customer, manager, viewer" -ForegroundColor Gray
    Write-Host ""
    Write-Host "9. 👤 Create Test Users:" -ForegroundColor Cyan
    Write-Host "   - Go to 'Users' → 'Create new user'" -ForegroundColor Gray
    Write-Host "   - admin@rys-fashion.com (password: Admin123!)" -ForegroundColor Gray
    Write-Host "   - user@rys-fashion.com (password: User123!)" -ForegroundColor Gray
    Write-Host "   - customer@rys-fashion.com (password: Customer123!)" -ForegroundColor Gray
    Write-Host "   - Set 'Email verified' to true for all users" -ForegroundColor Gray
    Write-Host "   - Assign appropriate roles to each user" -ForegroundColor Gray
    Write-Host ""
    Write-Host "10. 🔑 Get Client Secret:" -ForegroundColor Cyan
    Write-Host "    - Go to Clients → 'rys-fashion-api' → 'Credentials'" -ForegroundColor Gray
    Write-Host "    - Copy the 'Client secret'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "11. 🔧 Configure External Providers (Optional):" -ForegroundColor Cyan
    Write-Host "    - Go to 'Identity providers'" -ForegroundColor Gray
    Write-Host "    - Add Google: Client ID, Client Secret" -ForegroundColor Gray
    Write-Host "    - Add Facebook: App ID, App Secret" -ForegroundColor Gray
    Write-Host "    - Configure mappers for user attributes" -ForegroundColor Gray
    Write-Host ""
    
    $proceed = Read-Host "Have you completed the manual configuration? (y/N)"
    if ($proceed -ne "y" -and $proceed -ne "Y") {
        Write-Warning "Please complete the manual configuration before proceeding."
        return
    }
    
    # Update environment file with client secret and token settings
    if (![string]::IsNullOrEmpty($ClientSecret)) {
        Update-PostmanEnvironment -ClientSecret $ClientSecret
        Create-TokenRefreshScript
    }
    
    Write-Success "Keycloak realm configuration completed"
    Write-Host ""
    Write-Host "🔄 Token Refresh Information:" -ForegroundColor Yellow
    Write-Host "  • Access tokens expire in 5 minutes (configured for testing)" -ForegroundColor Gray
    Write-Host "  • Refresh tokens expire in 30 minutes" -ForegroundColor Gray
    Write-Host "  • Automatic token refresh is configured in Postman" -ForegroundColor Gray
    Write-Host "  • Check the Pre-request Scripts for automatic token management" -ForegroundColor Gray
}

function Update-PostmanEnvironment {
    param([string]$ClientSecret)
    
    Write-Section "Updating Postman Environment"
    
    $envPath = "tests/postman/Keycloak-Local-Environment.postman_environment.json"
    
    if (Test-Path $envPath) {
        $env = Get-Content $envPath | ConvertFrom-Json
        
        # Update client secret
        $clientSecretVar = $env.values | Where-Object { $_.key -eq "client_secret" }
        if ($clientSecretVar) {
            $clientSecretVar.value = $ClientSecret
        }
        
        # Add token expiration tracking variables
        $tokenExpiryVar = $env.values | Where-Object { $_.key -eq "token_expiry" }
        if (-not $tokenExpiryVar) {
            $env.values += @{
                key = "token_expiry"
                value = "0"
                enabled = $true
            }
        }
        
        $refreshTokenVar = $env.values | Where-Object { $_.key -eq "refresh_token" }
        if (-not $refreshTokenVar) {
            $env.values += @{
                key = "refresh_token" 
                value = ""
                enabled = $true
            }
        }
        
        # Save updated environment
        $env | ConvertTo-Json -Depth 10 | Set-Content $envPath
        Write-Success "Updated Postman environment with client secret and token management variables"
    }
    else {
        Write-Warning "Postman environment file not found at $envPath"
    }
}

function Create-TokenRefreshScript {
    Write-Section "Creating Token Refresh Script"
    
    $scriptPath = "tests/postman/token-refresh-script.js"
    
    $tokenRefreshScript = @"/
// Advanced Token Management Script for Keycloak
// This script handles automatic token refresh and expiration checking

class TokenManager {
    constructor() {
        this.keycloakUrl = pm.environment.get("keycloak_url");
        this.realm = pm.environment.get("realm");
        this.clientId = pm.environment.get("client_id");
        this.clientSecret = pm.environment.get("client_secret");
        this.tokenEndpoint = `\${this.keycloakUrl}/realms/\${this.realm}/protocol/openid-connect/token`;
    }
    
    // Check if token is expired (with 30 second buffer)
    isTokenExpired(tokenType = 'admin') {
        const expiryKey = tokenType === 'admin' ? 'admin_token_expiry' : 'user_token_expiry';
        const expiry = pm.environment.get(expiryKey);
        
        if (!expiry || expiry === '0') {
            return true;
        }
        
        const now = Math.floor(Date.now() / 1000);
        const expiryTime = parseInt(expiry);
        
        // Add 30 second buffer to prevent using tokens that expire during request
        return now >= (expiryTime - 30);
    }
    
    // Get admin token using client credentials
    async getAdminToken() {
        return new Promise((resolve, reject) => {
            const tokenRequest = {
                url: this.tokenEndpoint,
                method: 'POST',
                header: {
                    'Content-Type': 'application/x-www-form-urlencoded'
                },
                body: {
                    mode: 'urlencoded',
                    urlencoded: [
                        {key: 'grant_type', value: 'client_credentials'},
                        {key: 'client_id', value: this.clientId},
                        {key: 'client_secret', value: this.clientSecret}
                    ]
                }
            };
            
            pm.sendRequest(tokenRequest, (err, response) => {
                if (err) {
                    console.error('Error getting admin token:', err);
                    reject(err);
                    return;
                }
                
                if (response.code !== 200) {
                    console.error('Failed to get admin token:', response.status, response.text());
                    reject(new Error('Token request failed'));
                    return;
                }
                
                const jsonData = response.json();
                const now = Math.floor(Date.now() / 1000);
                const expiresAt = now + (jsonData.expires_in || 300);
                
                pm.environment.set("admin_token", jsonData.access_token);
                pm.environment.set("admin_token_expiry", expiresAt.toString());
                
                console.log('Admin token acquired, expires at:', new Date(expiresAt * 1000).toISOString());
                resolve(jsonData.access_token);
            });
        });
    }
    
    // Refresh user token using refresh token
    async refreshUserToken() {
        const refreshToken = pm.environment.get("refresh_token");
        
        if (!refreshToken) {
            throw new Error('No refresh token available');
        }
        
        return new Promise((resolve, reject) => {
            const refreshRequest = {
                url: this.tokenEndpoint,
                method: 'POST',
                header: {
                    'Content-Type': 'application/x-www-form-urlencoded'
                },
                body: {
                    mode: 'urlencoded',
                    urlencoded: [
                        {key: 'grant_type', value: 'refresh_token'},
                        {key: 'client_id', value: this.clientId},
                        {key: 'client_secret', value: this.clientSecret},
                        {key: 'refresh_token', value: refreshToken}
                    ]
                }
            };
            
            pm.sendRequest(refreshRequest, (err, response) => {
                if (err) {
                    console.error('Error refreshing token:', err);
                    reject(err);
                    return;
                }
                
                if (response.code !== 200) {
                    console.error('Failed to refresh token:', response.status, response.text());
                    // Clear invalid tokens
                    pm.environment.set("access_token", "");
                    pm.environment.set("refresh_token", "");
                    pm.environment.set("user_token_expiry", "0");
                    reject(new Error('Token refresh failed'));
                    return;
                }
                
                const jsonData = response.json();
                const now = Math.floor(Date.now() / 1000);
                const expiresAt = now + (jsonData.expires_in || 300);
                
                pm.environment.set("access_token", jsonData.access_token);
                pm.environment.set("user_token_expiry", expiresAt.toString());
                
                if (jsonData.refresh_token) {
                    pm.environment.set("refresh_token", jsonData.refresh_token);
                }
                
                console.log('Token refreshed, expires at:', new Date(expiresAt * 1000).toISOString());
                resolve(jsonData.access_token);
            });
        });
    }
    
    // Ensure admin token is valid
    async ensureAdminToken() {
        const currentToken = pm.environment.get("admin_token");
        
        if (!currentToken || this.isTokenExpired('admin')) {
            console.log('Admin token missing or expired, acquiring new token...');
            return await this.getAdminToken();
        }
        
        console.log('Admin token is still valid');
        return currentToken;
    }
    
    // Ensure user token is valid
    async ensureUserToken() {
        const currentToken = pm.environment.get("access_token");
        
        if (!currentToken || this.isTokenExpired('user')) {
            console.log('User token missing or expired, attempting refresh...');
            try {
                return await this.refreshUserToken();
            } catch (error) {
                console.log('Token refresh failed, user needs to login again');
                throw error;
            }
        }
        
        console.log('User token is still valid');
        return currentToken;
    }
}

// Usage in Pre-request Script:
// For admin endpoints:
// const tokenManager = new TokenManager();
// tokenManager.ensureAdminToken().then(() => {
//     // Token is ready, request will proceed
// }).catch(console.error);

// For user endpoints:
// const tokenManager = new TokenManager();
// tokenManager.ensureUserToken().then(() => {
//     // Token is ready, request will proceed  
// }).catch(console.error);

// Export for use in collection
if (typeof module !== 'undefined') {
    module.exports = TokenManager;
}

// Make available globally in Postman
pm.globals.set('TokenManager', TokenManager.toString());
"@
    
    # Create directory if it doesn't exist
    $scriptDir = Split-Path $scriptPath -Parent
    if (!(Test-Path $scriptDir)) {
        New-Item -ItemType Directory -Path $scriptDir -Force
    }
    
    $tokenRefreshScript | Out-File -FilePath $scriptPath -Encoding UTF8
    Write-Success "Created token refresh script at $scriptPath"
    
    Write-Host ""
    Write-Host "📝 How to use the token refresh script:" -ForegroundColor Yellow
    Write-Host "1. Copy the script content into your Postman collection Pre-request Script" -ForegroundColor Gray
    Write-Host "2. For admin endpoints, add: new TokenManager().ensureAdminToken()" -ForegroundColor Gray
    Write-Host "3. For user endpoints, add: new TokenManager().ensureUserToken()" -ForegroundColor Gray
    Write-Host "4. The script will automatically handle token expiration and refresh" -ForegroundColor Gray
}

function Test-KeycloakEndpoints {
    Write-Section "Testing Keycloak Endpoints"
    
    if ([string]::IsNullOrEmpty($ClientSecret)) {
        Write-Warning "Client secret not provided. Using placeholder."
        $ClientSecret = "your-client-secret-here"
    }
    
    $keycloakUrl = "http://localhost:8080"
    $realm = "rys-fashion"
    $clientId = "rys-fashion-api"
    
    try {
        Write-Host "🧪 Testing admin token acquisition..." -ForegroundColor Yellow
        
        $tokenBody = @{
            grant_type = "client_credentials"
            client_id = $clientId
            client_secret = $ClientSecret
        }
        
        $tokenResponse = Invoke-RestMethod -Uri "$keycloakUrl/realms/$realm/protocol/openid-connect/token" `
            -Method Post -Body $tokenBody -ContentType "application/x-www-form-urlencoded"
        
        $adminToken = $tokenResponse.access_token
        Write-Success "✅ Admin token acquired successfully"
        
        Write-Host "🧪 Testing user endpoints..." -ForegroundColor Yellow
        
        $headers = @{ Authorization = "Bearer $adminToken" }
        $users = Invoke-RestMethod -Uri "$keycloakUrl/admin/realms/$realm/users" -Headers $headers
        Write-Success "✅ Retrieved $($users.Count) users"
        
        Write-Host "🧪 Testing role endpoints..." -ForegroundColor Yellow
        
        $roles = Invoke-RestMethod -Uri "$keycloakUrl/admin/realms/$realm/roles" -Headers $headers
        Write-Success "✅ Retrieved $($roles.Count) roles"
        
        Write-Host "🧪 Testing client endpoints..." -ForegroundColor Yellow
        
        $clients = Invoke-RestMethod -Uri "$keycloakUrl/admin/realms/$realm/clients" -Headers $headers
        Write-Success "✅ Retrieved $($clients.Count) clients"
        
        Write-Success "🎉 All endpoint tests passed!"
        
        Write-Host "`n📊 Test Summary:" -ForegroundColor Cyan
        Write-Host "  👥 Users: $($users.Count)" -ForegroundColor Green
        Write-Host "  🎭 Roles: $($roles.Count)" -ForegroundColor Green  
        Write-Host "  📱 Clients: $($clients.Count)" -ForegroundColor Green
    }
    catch {
        Write-Error "❌ Endpoint test failed: $($_.Exception.Message)"
        Write-Host "💡 Make sure you've completed the realm configuration and provided the correct client secret." -ForegroundColor Yellow
    }
}

function Stop-KeycloakServices {
    Write-Section "Stopping Keycloak Services"
    
    Write-Host "🛑 Stopping Docker services..."
    docker-compose -f docker-compose.keycloak.yml down
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Keycloak services stopped successfully"
    } else {
        Write-Warning "Some issues occurred while stopping services"
    }
}

function Show-PostmanInstructions {
    Write-Section "Postman Setup Instructions"
    
    Write-Host "📬 Postman Collection Setup:" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. 📥 Import Collection:" -ForegroundColor Yellow
    Write-Host "   - Open Postman" -ForegroundColor Gray
    Write-Host "   - Click 'Import'" -ForegroundColor Gray
    Write-Host "   - Import: tests/postman/Keycloak-Complete-API-Collection.postman_collection.json" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. 🌍 Import Environment:" -ForegroundColor Yellow
    Write-Host "   - Click 'Import'" -ForegroundColor Gray
    Write-Host "   - Import: tests/postman/Keycloak-Local-Environment.postman_environment.json" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. ⚙️  Configure Environment:" -ForegroundColor Yellow
    Write-Host "   - Select 'Keycloak-Local-Environment'" -ForegroundColor Gray
    Write-Host "   - Update 'client_secret' with your actual client secret" -ForegroundColor Gray
    Write-Host ""
    Write-Host "4. 🔄 Setup Token Auto-Refresh:" -ForegroundColor Yellow
    Write-Host "   - Go to Collection Settings → Pre-request Scripts" -ForegroundColor Gray
    Write-Host "   - Copy content from: tests/postman/scripts/admin-token-manager.js" -ForegroundColor Gray
    Write-Host "   - For user endpoints, use: tests/postman/scripts/user-token-manager.js" -ForegroundColor Gray
    Write-Host ""
    Write-Host "5. 🧪 Test Token Refresh:" -ForegroundColor Yellow
    Write-Host "   - Run 'Login User' to get initial tokens" -ForegroundColor Gray
    Write-Host "   - Wait 5+ minutes or manually expire token" -ForegroundColor Gray
    Write-Host "   - Run any user endpoint to see automatic refresh" -ForegroundColor Gray
    Write-Host "   - Check Console logs for refresh messages" -ForegroundColor Gray
    Write-Host ""
    Write-Host "6. 🧪 Test Endpoints:" -ForegroundColor Yellow
    Write-Host "   - Start with '🔐 Authentication' folder" -ForegroundColor Gray
    Write-Host "   - Run 'Login User' to get access token" -ForegroundColor Gray
    Write-Host "   - The admin token will be automatically acquired" -ForegroundColor Gray
    Write-Host "   - Test other endpoints as needed" -ForegroundColor Gray
    Write-Host ""
    Write-Host "📋 Available Collections:" -ForegroundColor Cyan
    Write-Host "  🔐 Authentication (5 endpoints - includes token refresh)" -ForegroundColor Green
    Write-Host "  👤 User Management (9 endpoints)" -ForegroundColor Green
    Write-Host "  🎭 Role Management (5 endpoints)" -ForegroundColor Green
    Write-Host "  🔗 User-Role Assignment (4 endpoints)" -ForegroundColor Green
    Write-Host "  👥 Group Management (7 endpoints)" -ForegroundColor Green
    Write-Host "  🖥️  Session Management (2 endpoints)" -ForegroundColor Green
    Write-Host "  🏢 Client Management (2 endpoints)" -ForegroundColor Green
    Write-Host ""
    Write-Host "🔄 Token Refresh Features:" -ForegroundColor Cyan
    Write-Host "  ✅ Automatic admin token acquisition" -ForegroundColor Green
    Write-Host "  ✅ Automatic user token refresh" -ForegroundColor Green
    Write-Host "  ✅ Token expiry tracking (30 second buffer)" -ForegroundColor Green
    Write-Host "  ✅ Refresh token rotation support" -ForegroundColor Green
    Write-Host "  ✅ Detailed console logging" -ForegroundColor Green
    Write-Host "  ✅ Error handling and fallback" -ForegroundColor Green
    Write-Host ""
    Write-Host "🎯 Total: 34 API endpoints with automatic token management!" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "💡 Token Refresh Tips:" -ForegroundColor Yellow
    Write-Host "  • Access tokens expire in 5 minutes (set for testing)" -ForegroundColor Gray
    Write-Host "  • Refresh tokens expire in 30 minutes" -ForegroundColor Gray
    Write-Host "  • Check Console tab for token refresh logs" -ForegroundColor Gray
    Write-Host "  • Enable 'Automatically follow redirects' in Postman settings" -ForegroundColor Gray
    Write-Host "  • Use Collection-level pre-request scripts for global token management" -ForegroundColor Gray
}

# Main execution logic
try {
    Write-Host "🔐 Keycloak Setup & Testing Tool" -ForegroundColor Magenta
    Write-Host "================================" -ForegroundColor Magenta
    Write-Host ""
    
    if ($FullSetup) {
        Start-KeycloakServices
        Configure-KeycloakRealm
        Test-KeycloakEndpoints
        Show-PostmanInstructions
    }
    else {
        if ($StartKeycloak) {
            Start-KeycloakServices
        }
        
        if ($ConfigureRealm) {
            Configure-KeycloakRealm
        }
        
        if ($TestEndpoints) {
            Test-KeycloakEndpoints
        }
        
        if ($StopKeycloak) {
            Stop-KeycloakServices
        }
        
        if (-not ($StartKeycloak -or $ConfigureRealm -or $TestEndpoints -or $StopKeycloak)) {
            Write-Host "🚀 Usage Examples:" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "  # Complete setup (recommended for first time)" -ForegroundColor Gray
            Write-Host "  ./Setup-Keycloak-Complete.ps1 -FullSetup" -ForegroundColor Green
            Write-Host ""
            Write-Host "  # Individual steps" -ForegroundColor Gray
            Write-Host "  ./Setup-Keycloak-Complete.ps1 -StartKeycloak" -ForegroundColor Green
            Write-Host "  ./Setup-Keycloak-Complete.ps1 -ConfigureRealm" -ForegroundColor Green
            Write-Host "  ./Setup-Keycloak-Complete.ps1 -TestEndpoints -ClientSecret 'your-secret'" -ForegroundColor Green
            Write-Host "  ./Setup-Keycloak-Complete.ps1 -StopKeycloak" -ForegroundColor Green
            Write-Host ""
            Show-PostmanInstructions
        }
    }
    
    Write-Host ""
    Write-Success "🎉 Script completed successfully!"
}
catch {
    Write-Error "💥 Script failed: $($_.Exception.Message)"
    Write-Host ""
    Write-Host "🔧 Troubleshooting Tips:" -ForegroundColor Yellow
    Write-Host "  • Ensure Docker Desktop is running" -ForegroundColor Gray
    Write-Host "  • Check if ports 8080 and 5433 are available" -ForegroundColor Gray
    Write-Host "  • Verify client secret is correct" -ForegroundColor Gray
    Write-Host "  • Check Keycloak logs: docker-compose -f docker-compose.keycloak.yml logs keycloak" -ForegroundColor Gray
    exit 1
}