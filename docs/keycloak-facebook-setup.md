# Keycloak Facebook Identity Provider Setup

## 1. Create Facebook App

1. Go to [Facebook Developers](https://developers.facebook.com/)
2. Create a new app ? "Consumer" type
3. Add Facebook Login product
4. Configure Facebook Login settings:
   - Valid OAuth Redirect URIs: `http://localhost:8080/realms/rys-fashion/broker/facebook/endpoint`
   - Client OAuth Login: YES
   - Web OAuth Login: YES

## 2. Configure in Keycloak Admin Console

1. Go to "Identity Providers" ? "Add provider" ? "Facebook"
2. Configure:
   - **Alias**: `facebook`
   - **Client ID**: Your Facebook App ID
   - **Client Secret**: Your Facebook App Secret
   - **Default Scopes**: `email public_profile`
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
- **Claim**: `first_name`
- **User Attribute**: `firstName`

### Last Name Mapper
- **Name**: `lastName`
- **Mapper Type**: `Attribute Importer`
- **Claim**: `last_name`
- **User Attribute**: `lastName`

### Profile Picture Mapper
- **Name**: `avatar`
- **Mapper Type**: `Attribute Importer`
- **Claim**: `picture.data.url`
- **User Attribute**: `picture`

## 4. Facebook Permissions

Request these permissions in your Facebook app:
- `email`: User's email address
- `public_profile`: Basic profile information

## 5. Facebook App Review

For production, submit your app for review to access:
- Email permission
- Public profile access
- Any additional permissions needed