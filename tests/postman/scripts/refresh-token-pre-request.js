// Pre-request script for Token Refresh endpoint
// This prepares the refresh token request

console.log("?? Preparing token refresh request...");

// Check if we have a refresh token
const refreshToken = pm.environment.get("refresh_token");
const currentAccessToken = pm.environment.get("access_token");

if (!refreshToken) {
    console.error("? No refresh token available");
    console.log("?? Please run the 'Login User' request first to get a refresh token");
    throw new Error("No refresh token available - please login first");
}

// Store current access token for comparison in tests
if (currentAccessToken) {
    pm.environment.set("previous_access_token", currentAccessToken);
    console.log("?? Stored current access token for comparison");
}

// Check current token expiry
const tokenExpiry = pm.environment.get("user_token_expiry");
if (tokenExpiry && tokenExpiry !== "0") {
    const expiryTime = new Date(parseInt(tokenExpiry) * 1000);
    const now = new Date();
    const timeUntilExpiry = Math.floor((expiryTime.getTime() - now.getTime()) / 1000);
    
    console.log("? Current token status:");
    console.log("   Expires at:", expiryTime.toISOString());
    console.log("   Time until expiry:", timeUntilExpiry > 0 ? `${timeUntilExpiry} seconds` : "EXPIRED");
    
    if (timeUntilExpiry > 300) { // More than 5 minutes
        console.warn("??  Current token is still valid for more than 5 minutes");
        console.log("?? Consider waiting or manually expiring the token for testing");
    }
} else {
    console.log("? No token expiry information available");
}

// Verify required environment variables
const requiredVars = ['keycloak_url', 'realm', 'client_id', 'client_secret'];
const missingVars = requiredVars.filter(varName => !pm.environment.get(varName));

if (missingVars.length > 0) {
    console.error('? Missing required environment variables:', missingVars.join(', '));
    throw new Error(`Missing environment variables: ${missingVars.join(', ')}`);
}

console.log("? Ready to refresh token");
console.log("   Using refresh token:", refreshToken.substring(0, 20) + "...");
console.log("   Client ID:", pm.environment.get("client_id"));
console.log("   Realm:", pm.environment.get("realm"));