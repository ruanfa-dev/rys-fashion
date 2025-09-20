# External OAuth Testing Guide

## ?? **Testing Scenarios**

### **Scenario 1: New User Google Authentication**
1. **Setup**: User has Google account but no existing account in your system
2. **Flow**: 
   - User clicks "Sign in with Google" 
   - Redirected to Keycloak ? Google OAuth
   - Google redirects back to Keycloak
   - Keycloak creates new user and redirects to your app
   - Your app exchanges authorization code for tokens
3. **Expected Result**: New user created, authenticated successfully

### **Scenario 2: Existing User Google Login**
1. **Setup**: User has both Google account and existing account (already linked)
2. **Flow**: Same as Scenario 1 but user already exists in Keycloak
3. **Expected Result**: User authenticated successfully with existing account

### **Scenario 3: Account Linking**
1. **Setup**: User has existing account and wants to link Google
2. **Flow**:
   - User logs in normally first
   - Goes to account settings and clicks "Link Google"
   - Completes Google OAuth flow
   - Account gets linked
3. **Expected Result**: Google account linked to existing user

### **Scenario 4: Direct Token Exchange**
1. **Setup**: Frontend gets Google token directly (via Google SDK)
2. **Flow**:
   - Frontend calls Google SDK, gets access token
   - Frontend calls your API's `/auth/external/exchange` endpoint
   - API exchanges Google token for Keycloak token
3. **Expected Result**: Keycloak tokens returned

## ?? **Manual Testing Steps**

### **Step 1: Setup Keycloak Identity Providers**
```bash
# Ensure Keycloak is running
docker-compose -f docker-compose.keycloak.yml up -d

# Access admin console
# http://localhost:8080/admin
# Username: admin, Password: admin123

# Configure Google and Facebook identity providers as per setup guides
```

### **Step 2: Test Authorization URLs**
```bash
# Test Google authorize URL
curl "http://localhost:7001/api/auth/external/authorize-url/google?redirectUri=http://localhost:3000/auth/callback&state=test123"

# Expected response:
# {
#   "authorizeUrl": "http://localhost:8080/realms/rys-fashion/broker/google/login?redirect_uri=...",
#   "provider": "google",
#   "redirectUri": "http://localhost:3000/auth/callback",
#   "state": "test123"
# }
```

### **Step 3: Test Complete OAuth Flow**
1. **Manual Browser Test**:
   - Navigate to the `authorizeUrl` from Step 2
   - Complete Google authentication
   - Note the authorization code in the callback URL
   
2. **Exchange Code for Tokens**:
   ```bash
   # Use the authorization code to get tokens
   curl -X POST "http://localhost:8080/realms/rys-fashion/protocol/openid-connect/token" \
     -H "Content-Type: application/x-www-form-urlencoded" \
     -d "grant_type=authorization_code" \
     -d "client_id=rys-fashion-api" \
     -d "client_secret=YOUR_CLIENT_SECRET" \
     -d "code=AUTHORIZATION_CODE_FROM_CALLBACK" \
     -d "redirect_uri=http://localhost:3000/auth/callback"
   ```

### **Step 4: Test Direct Token Exchange**
```bash
# If you have a Google access token, test direct exchange
curl -X POST "http://localhost:7001/api/auth/external/exchange" \
  -H "Content-Type: application/json" \
  -d '{
    "provider": "google",
    "accessToken": "YOUR_GOOGLE_ACCESS_TOKEN"
  }'
```

### **Step 5: Test Account Linking**
```bash
# First, create or get an existing user ID
# Then link Google account
curl -X POST "http://localhost:7001/api/auth/external/exchange" \
  -H "Content-Type: application/json" \
  -d '{
    "provider": "google",
    "accessToken": "YOUR_GOOGLE_ACCESS_TOKEN",
    "linkToExistingUser": true,
    "existingUserId": "EXISTING_USER_GUID"
  }'
```

### **Step 6: Verify Authentication**
```bash
# Use the returned access token to test protected endpoints
curl -H "Authorization: Bearer YOUR_KEYCLOAK_ACCESS_TOKEN" \
  "http://localhost:7001/api/users/profile"
```

## ?? **Common Issues & Solutions**

### **Issue 1: "Token exchange failed: 404"**
- **Cause**: User doesn't exist in Keycloak yet
- **Solution**: User needs to complete initial Keycloak broker login flow first
- **Test**: Navigate manually to authorization URL and complete Google login

### **Issue 2: "Invalid redirect URI"**
- **Cause**: Redirect URI not configured in Google/Facebook app
- **Solution**: Add `http://localhost:8080/realms/rys-fashion/broker/google/endpoint` to allowed redirect URIs

### **Issue 3: "Client secret not found"**
- **Cause**: Keycloak client secret not properly configured
- **Solution**: Check Keycloak admin console ? Clients ? rys-fashion-api ? Credentials

### **Issue 4: "Account already linked"**
- **Cause**: Trying to link an external account that's already linked
- **Solution**: Check existing user's federated identities in Keycloak admin

### **Issue 5: CORS errors in frontend**
- **Cause**: Frontend making cross-origin requests to Keycloak
- **Solution**: Configure CORS in Keycloak admin console

## ?? **Test Data**

### **Test Google Account**
- Create a test Google account for testing
- Enable Google+ API access
- Get test access tokens using Google OAuth Playground

### **Test Facebook Account**
- Create Facebook developer account
- Create test app with limited permissions
- Use Facebook Graph Explorer for test tokens

### **Sample Test Users**
```json
{
  "testUser1": {
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "email": "test1@example.com",
    "firstName": "Test",
    "lastName": "User1"
  },
  "testUser2": {
    "id": "550e8400-e29b-41d4-a716-446655440002", 
    "email": "test2@example.com",
    "firstName": "Test",
    "lastName": "User2"
  }
}
```

## ?? **Automated Testing**

### **Unit Tests**
```csharp
[Test]
public async Task ExchangeExternalToken_WithValidGoogleToken_ShouldReturnKeycloakTokens()
{
    // Arrange
    var command = new ExchangeExternalTokenCommand
    {
        Provider = "google",
        AccessToken = "valid_google_token"
    };
    
    // Mock external token validation
    _mockKeycloakService
        .Setup(x => x.ExchangeExternalTokenAsync(It.IsAny<ExternalTokenExchangeRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new TokenResponse { AccessToken = "keycloak_token" });
    
    // Act
    var result = await _handler.Handle(command, CancellationToken.None);
    
    // Assert
    result.IsError.Should().BeFalse();
    result.Value.AccessToken.Should().Be("keycloak_token");
}
```

### **Integration Tests**
```csharp
[Test]
public async Task ExternalAuth_EndToEnd_ShouldWork()
{
    // This would require actual OAuth setup and test credentials
    // Use test environment with real external providers
}
```

### **Frontend E2E Tests**
```javascript
// Cypress test example
describe('External Authentication', () => {
  it('should authenticate with Google successfully', () => {
    cy.visit('/login');
    cy.get('[data-cy=google-login]').click();
    
    // Handle OAuth redirect flow
    cy.origin('accounts.google.com', () => {
      // Complete Google authentication
      cy.get('#identifierId').type('test@example.com');
      cy.get('#passwordNext').click();
    });
    
    // Verify successful authentication
    cy.url().should('include', '/dashboard');
    cy.get('[data-cy=user-menu]').should('be.visible');
  });
});
```

## ?? **Monitoring & Analytics**

### **Key Metrics to Track**
- External authentication success rate
- Token exchange failure rate
- Account linking success rate
- Provider-specific conversion rates
- Error distribution by provider

### **Logging Strategy**
- Log all token exchange attempts
- Track user journeys through external auth
- Monitor provider API response times
- Alert on high error rates

### **Performance Monitoring**
- Monitor Keycloak response times
- Track external provider API latency
- Measure complete authentication flow duration
- Monitor token refresh patterns