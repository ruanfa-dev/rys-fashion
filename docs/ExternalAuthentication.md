# External Authentication with Identity EF Core Integration

## Overview

This implementation provides a comprehensive, production-ready external authentication system that integrates seamlessly with ASP.NET Core Identity and Entity Framework Core. The system supports Google and Facebook OAuth authentication with secure token exchange.

## Key Features

### 🔐 **Enhanced Security**
- **Token Validation**: Uses official provider SDKs (Google.Apis.Auth) and secure API validation
- **Scope Validation**: Ensures required permissions are granted by external providers
- **Email Verification**: Validates email addresses from external providers
- **Rate Limiting**: Supports rate limiting on authentication endpoints
- **Input Sanitization**: Sanitizes display names to prevent XSS attacks

### 🏗️ **Clean Architecture**
- **Separation of Concerns**: Token validators handle only token validation, separate service handles user management
- **Interface-Based Design**: Uses `IExternalUserService` for dependency inversion
- **Error Handling**: Comprehensive `ErrorOr<T>` pattern for robust error management
- **Logging**: Structured logging throughout the authentication flow

### 👥 **User Management**
- **Existing User Detection**: Finds users by external login or email
- **Account Linking**: Links external logins to existing accounts
- **User Creation**: Creates new users with external authentication
- **Profile Updates**: Updates user information from external providers
- **Login Tracking**: Records sign-in times and IP addresses

### 🔄 **Identity EF Core Integration**
- **UserManager Integration**: Full integration with ASP.NET Core Identity
- **External Login Management**: Add, remove, and manage external logins
- **Safety Checks**: Prevents removal of last authentication method
- **Database Persistence**: All operations persist to EF Core database

## API Endpoints

### External Authentication Endpoints
- `GET /auth/external/providers` - Get available external providers
- `GET /auth/external/config/{provider}` - Get OAuth configuration for frontend
- `POST /auth/external/token/exchange/{provider}` - Exchange external token for app tokens
- `POST /auth/external/token/verify/{provider}` - Verify external token without creating session
- `GET /auth/external/health` - Health check for external auth services

## Supported Providers

### ✅ **Google OAuth 2.0**
- **Scopes**: `openid`, `email`, `profile`
- **Features**: ID token validation with signature verification, PKCE support
- **Security**: Email verification required, proper audience validation

### ✅ **Facebook Login**
- **Scopes**: `email`, `public_profile`
- **Features**: Token debug validation, app ID verification
- **Security**: Scope validation, token expiration checks

### ❌ **Microsoft Account** (Removed)
- Removed for lean production setup focused on most popular providers

## Configuration

### Google Configuration
```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret"
    }
  }
}
```

### Facebook Configuration
```json
{
  "Authentication": {
    "Facebook": {
      "AppId": "your-facebook-app-id",
      "AppSecret": "your-facebook-app-secret"
    }
  }
}
```

## Usage Flow

### Frontend OAuth Flow
1. **Get Config**: Frontend calls `/auth/external/config/{provider}` to get OAuth URLs
2. **User Authorization**: User authorizes with external provider
3. **Token Exchange**: Frontend calls `/auth/external/token/exchange/{provider}` with authorization code
4. **App Tokens**: Receive JWT access and refresh tokens for your application

### Backend Processing
1. **Token Validation**: Validate external token with provider's official APIs
2. **User Resolution**: Find existing user or create new account
3. **Login Linking**: Link external login to user account in Identity database
4. **Profile Update**: Update user information from external provider
5. **Token Generation**: Generate application JWT tokens
6. **Sign-in Tracking**: Record sign-in information

## Security Considerations

### ✅ **Production Ready**
- HTTPS enforcement for all external authentication flows
- Secure cookie settings with `HttpOnly`, `Secure`, and `SameSite`
- Short-lived external authentication cookies (15 minutes)
- Reduced JWT clock skew (2 minutes) for tighter security
- PKCE support for Google OAuth for enhanced security

### ✅ **Data Protection**
- Client secrets never exposed to frontend
- Display name sanitization to prevent XSS
- Proper error handling without information disclosure
- Comprehensive input validation

### ✅ **Account Security**
- Cannot remove last external login without password
- Email verification from external providers
- User account linking based on verified email addresses
- Sign-in tracking for audit purposes

## Extension Points

### Custom Providers
To add new providers:
1. Create new `{Provider}TokenValidator` implementing `IExternalTokenValidator`
2. Add provider to `SupportedProviders` in `ExternalUserService`
3. Update `CompositeExternalTokenValidator` and configuration classes
4. Register in DI container

### Enhanced User Management
The `ExternalUserService` can be extended for:
- Custom user creation logic
- Additional profile synchronization
- Advanced account linking rules
- Custom authentication flows

## Database Schema

The system uses standard ASP.NET Core Identity tables:
- **AspNetUsers**: User accounts
- **AspNetUserLogins**: External login associations
- **AspNetUserTokens**: Token storage (if needed)

No additional database changes required - it works with existing Identity schema.

## Error Handling

### Comprehensive Error Types
- **Validation Errors**: Invalid input parameters
- **Authentication Errors**: Token validation failures
- **Authorization Errors**: Insufficient permissions
- **Network Errors**: External API communication issues
- **Database Errors**: User management operation failures

### Client-Friendly Responses
All errors follow consistent format:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Provider": ["Provider 'invalid' is not supported"]
  }
}
```

## Performance Optimizations

- **Cached OAuth Configurations**: Provider settings cached for performance
- **Efficient Database Queries**: Optimized user lookup and creation
- **HTTP Client Reuse**: Shared HTTP clients with proper configuration
- **Reduced Timeouts**: 15-second timeouts for better user experience

This implementation provides a robust, secure, and scalable foundation for external authentication in production e-commerce applications.