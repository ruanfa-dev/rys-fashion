// Pre-request Script for Admin Endpoints
// Automatically manages admin token acquisition and refresh

// Token Manager Class (copy this to collection-level pre-request script)
class TokenManager {
    constructor() {
        this.keycloakUrl = pm.environment.get("keycloak_url");
        this.realm = pm.environment.get("realm");
        this.clientId = pm.environment.get("client_id");
        this.clientSecret = pm.environment.get("client_secret");
        this.tokenEndpoint = `${this.keycloakUrl}/realms/${this.realm}/protocol/openid-connect/token`;
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
    getAdminToken() {
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
                    console.error('? Error getting admin token:', err);
                    reject(err);
                    return;
                }
                
                if (response.code !== 200) {
                    console.error('? Failed to get admin token:', response.status, response.text());
                    reject(new Error(`Token request failed: ${response.status}`));
                    return;
                }
                
                try {
                    const jsonData = response.json();
                    const now = Math.floor(Date.now() / 1000);
                    const expiresAt = now + (jsonData.expires_in || 300);
                    
                    pm.environment.set("admin_token", jsonData.access_token);
                    pm.environment.set("admin_token_expiry", expiresAt.toString());
                    
                    console.log('? Admin token acquired, expires at:', new Date(expiresAt * 1000).toISOString());
                    resolve(jsonData.access_token);
                } catch (parseError) {
                    console.error('? Error parsing token response:', parseError);
                    reject(parseError);
                }
            });
        });
    }
    
    // Refresh user token using refresh token
    refreshUserToken() {
        const refreshToken = pm.environment.get("refresh_token");
        
        if (!refreshToken) {
            return Promise.reject(new Error('No refresh token available'));
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
                    console.error('? Error refreshing token:', err);
                    reject(err);
                    return;
                }
                
                if (response.code !== 200) {
                    console.error('? Failed to refresh token:', response.status, response.text());
                    // Clear invalid tokens
                    pm.environment.set("access_token", "");
                    pm.environment.set("refresh_token", "");
                    pm.environment.set("user_token_expiry", "0");
                    reject(new Error(`Token refresh failed: ${response.status}`));
                    return;
                }
                
                try {
                    const jsonData = response.json();
                    const now = Math.floor(Date.now() / 1000);
                    const expiresAt = now + (jsonData.expires_in || 300);
                    
                    pm.environment.set("access_token", jsonData.access_token);
                    pm.environment.set("user_token_expiry", expiresAt.toString());
                    
                    if (jsonData.refresh_token) {
                        pm.environment.set("refresh_token", jsonData.refresh_token);
                    }
                    
                    console.log('?? Token refreshed, expires at:', new Date(expiresAt * 1000).toISOString());
                    resolve(jsonData.access_token);
                } catch (parseError) {
                    console.error('? Error parsing refresh response:', parseError);
                    reject(parseError);
                }
            });
        });
    }
    
    // Ensure admin token is valid
    ensureAdminToken() {
        const currentToken = pm.environment.get("admin_token");
        
        if (!currentToken || this.isTokenExpired('admin')) {
            console.log('?? Admin token missing or expired, acquiring new token...');
            return this.getAdminToken();
        }
        
        console.log('? Admin token is still valid');
        return Promise.resolve(currentToken);
    }
    
    // Ensure user token is valid
    ensureUserToken() {
        const currentToken = pm.environment.get("access_token");
        
        if (!currentToken || this.isTokenExpired('user')) {
            console.log('?? User token missing or expired, attempting refresh...');
            return this.refreshUserToken().catch(error => {
                console.log('? Token refresh failed, user needs to login again');
                throw error;
            });
        }
        
        console.log('? User token is still valid');
        return Promise.resolve(currentToken);
    }
}

// For Admin Endpoints - Ensure admin token is valid
const tokenManager = new TokenManager();

// Check if required environment variables are set
const requiredVars = ['keycloak_url', 'realm', 'client_id', 'client_secret'];
const missingVars = requiredVars.filter(varName => !pm.environment.get(varName));

if (missingVars.length > 0) {
    console.error('? Missing required environment variables:', missingVars.join(', '));
    throw new Error(`Missing environment variables: ${missingVars.join(', ')}`);
}

// Ensure admin token before request
tokenManager.ensureAdminToken()
    .then(token => {
        console.log('? Admin token ready for request');
        // Token is now available in environment variable "admin_token"
    })
    .catch(error => {
        console.error('? Failed to ensure admin token:', error.message);
        // This will cause the request to fail, which is what we want
        throw error;
    });