// Pre-request Script for User Endpoints
// Automatically manages user token refresh

// Token Manager Class (same as admin, but focused on user tokens)
class TokenManager {
    constructor() {
        this.keycloakUrl = pm.environment.get("keycloak_url");
        this.realm = pm.environment.get("realm");
        this.clientId = pm.environment.get("client_id");
        this.clientSecret = pm.environment.get("client_secret");
        this.tokenEndpoint = `${this.keycloakUrl}/realms/${this.realm}/protocol/openid-connect/token`;
    }
    
    isTokenExpired(tokenType = 'user') {
        const expiryKey = tokenType === 'admin' ? 'admin_token_expiry' : 'user_token_expiry';
        const expiry = pm.environment.get(expiryKey);
        
        if (!expiry || expiry === '0') {
            return true;
        }
        
        const now = Math.floor(Date.now() / 1000);
        const expiryTime = parseInt(expiry);
        
        return now >= (expiryTime - 30);
    }
    
    refreshUserToken() {
        const refreshToken = pm.environment.get("refresh_token");
        
        if (!refreshToken) {
            return Promise.reject(new Error('No refresh token available - please login again'));
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
                    console.error('? Network error refreshing token:', err);
                    reject(err);
                    return;
                }
                
                if (response.code !== 200) {
                    console.error('? Failed to refresh token:', response.status);
                    console.error('Response:', response.text());
                    
                    // Clear invalid tokens
                    pm.environment.set("access_token", "");
                    pm.environment.set("refresh_token", "");
                    pm.environment.set("user_token_expiry", "0");
                    
                    reject(new Error(`Token refresh failed with status ${response.status}. Please login again.`));
                    return;
                }
                
                try {
                    const jsonData = response.json();
                    const now = Math.floor(Date.now() / 1000);
                    const expiresAt = now + (jsonData.expires_in || 300);
                    
                    // Update tokens
                    pm.environment.set("access_token", jsonData.access_token);
                    pm.environment.set("user_token_expiry", expiresAt.toString());
                    
                    // Update refresh token if provided (token rotation)
                    if (jsonData.refresh_token) {
                        pm.environment.set("refresh_token", jsonData.refresh_token);
                        console.log('?? Refresh token rotated');
                    }
                    
                    console.log('? User token refreshed successfully');
                    console.log('   Expires at:', new Date(expiresAt * 1000).toISOString());
                    
                    resolve(jsonData.access_token);
                } catch (parseError) {
                    console.error('? Error parsing refresh response:', parseError);
                    reject(parseError);
                }
            });
        });
    }
    
    ensureUserToken() {
        const currentToken = pm.environment.get("access_token");
        
        if (!currentToken || this.isTokenExpired('user')) {
            console.log('?? User token missing or expired, attempting refresh...');
            return this.refreshUserToken();
        }
        
        console.log('? User token is still valid');
        return Promise.resolve(currentToken);
    }
}

// For User Endpoints - Ensure user token is valid
const tokenManager = new TokenManager();

// Check if user is logged in (has refresh token)
const refreshToken = pm.environment.get("refresh_token");
const accessToken = pm.environment.get("access_token");

if (!refreshToken && !accessToken) {
    console.error('? No tokens available - please login first');
    console.log('?? Run the "Login User" request first to get tokens');
    throw new Error('Authentication required: No tokens available');
}

// Ensure user token before request
tokenManager.ensureUserToken()
    .then(token => {
        console.log('? User token ready for request');
        // Token is now available in environment variable "access_token"
    })
    .catch(error => {
        console.error('? Failed to ensure user token:', error.message);
        console.log('?? Please run the "Login User" request again to re-authenticate');
        throw error;
    });