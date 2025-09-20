# Keycloak Google Identity Provider Setup

## 1. Create Google OAuth Application

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing
3. Enable Google+ API
4. Go to "Credentials" ? "Create Credentials" ? "OAuth 2.0 Client ID"
5. Configure OAuth consent screen
6. Create OAuth 2.0 credentials:
   - Application type: Web application
   - Authorized redirect URIs: `http://localhost:8080/realms/rys-fashion/broker/google/endpoint`

## 2. Configure in Keycloak Admin Console

1. Login to Keycloak Admin Console
2. Go to "Identity Providers" ? "Add provider" ? "Google"
3. Configure:
   - **Alias**: `google`
   - **Client ID**: Your Google Client ID
   - **Client Secret**: Your Google Client Secret
   - **Default Scopes**: `openid profile email`
   - **Store Tokens**: ON
   - **Stored Tokens Readable**: ON
   - **Trust Email**: ON
   - **Account Linking Only**: OFF
   - **Hide on Login Page**: OFF

## 3. Mappers Configuration

Add these mappers to sync user attributes:

### Email Mapper
- **Name**: `email`
- **Mapper Type**: `Attribute Importer`
- **Claim**: `email`
- **User Attribute**: `email`

### First Name Mapper
- **Name**: `firstName`
- **Mapper Type**: `Attribute Importer`
- **Claim**: `given_name`
- **User Attribute**: `firstName`

### Last Name Mapper
- **Name**: `lastName`
- **Mapper Type**: `Attribute Importer`
- **Claim**: `family_name`
- **User Attribute**: `lastName`

### Profile Picture Mapper
- **Name**: `avatar`
- **Mapper Type**: `Attribute Importer`
- **Claim**: `picture`
- **User Attribute**: `picture`

## 4. First Broker Login Flow

Configure first broker login flow to handle new users:
1. Go to "Authentication" ? "Flows" ? "First Broker Login"
2. Ensure the flow includes:
   - Review Profile
   - Create User If Unique
   - Automatically Link Brokered Account