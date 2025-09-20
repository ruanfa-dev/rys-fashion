// Test script for Login User endpoint
// This sets up tokens and their expiry times correctly

pm.test("Login successful", function () {
    pm.response.to.have.status(200);
    
    const jsonData = pm.response.json();
    
    // Verify response structure
    pm.expect(jsonData).to.have.property('access_token');
    pm.expect(jsonData).to.have.property('refresh_token');
    pm.expect(jsonData).to.have.property('expires_in');
    
    // Store tokens with expiry tracking
    const now = Math.floor(Date.now() / 1000);
    const expiresAt = now + (jsonData.expires_in || 300);
    
    pm.environment.set("access_token", jsonData.access_token);
    pm.environment.set("refresh_token", jsonData.refresh_token);
    pm.environment.set("user_token_expiry", expiresAt.toString());
    pm.environment.set("token_type", jsonData.token_type || "Bearer");
    
    // Store additional user info if available
    if (jsonData.scope) {
        pm.environment.set("user_scope", jsonData.scope);
    }
    
    console.log("? Login successful:");
    console.log("   Access token expires at:", new Date(expiresAt * 1000).toISOString());
    console.log("   Token type:", jsonData.token_type || "Bearer");
    console.log("   Scope:", jsonData.scope || "default");
    
    // Log token preview (first 20 characters for security)
    console.log("   Access token preview:", jsonData.access_token.substring(0, 20) + "...");
    console.log("   Refresh token preview:", jsonData.refresh_token.substring(0, 20) + "...");
});

pm.test("Token expiry is reasonable", function () {
    const jsonData = pm.response.json();
    const expiresIn = jsonData.expires_in;
    
    // Token should expire between 1 minute and 24 hours
    pm.expect(expiresIn).to.be.above(60); // More than 1 minute
    pm.expect(expiresIn).to.be.below(86400); // Less than 24 hours
    
    console.log(`   Token expires in: ${expiresIn} seconds (${Math.round(expiresIn/60)} minutes)`);
});

pm.test("Tokens are properly formatted", function () {
    const jsonData = pm.response.json();
    
    // JWT tokens should have 3 parts separated by dots
    const accessTokenParts = jsonData.access_token.split('.');
    const refreshTokenParts = jsonData.refresh_token.split('.');
    
    pm.expect(accessTokenParts).to.have.lengthOf(3);
    pm.expect(refreshTokenParts).to.have.lengthOf(3);
    
    console.log("   Access token format: Valid JWT (3 parts)");
    console.log("   Refresh token format: Valid JWT (3 parts)");
});

// Optional: Decode and log JWT payload (for debugging)
pm.test("JWT payload is readable", function () {
    const jsonData = pm.response.json();
    
    try {
        // Decode JWT payload (base64url decode)
        const payloadB64 = jsonData.access_token.split('.')[1];
        const payloadJson = atob(payloadB64.replace(/-/g, '+').replace(/_/g, '/'));
        const payload = JSON.parse(payloadJson);
        
        console.log("   JWT payload preview:");
        console.log("     Subject (sub):", payload.sub || "N/A");
        console.log("     Username:", payload.preferred_username || payload.username || "N/A");
        console.log("     Email:", payload.email || "N/A");
        console.log("     Roles:", payload.realm_access?.roles || "N/A");
        console.log("     Issued at:", payload.iat ? new Date(payload.iat * 1000).toISOString() : "N/A");
        console.log("     Expires at:", payload.exp ? new Date(payload.exp * 1000).toISOString() : "N/A");
        
        // Store user info for later use
        if (payload.sub) pm.environment.set("user_id", payload.sub);
        if (payload.preferred_username) pm.environment.set("username", payload.preferred_username);
        if (payload.email) pm.environment.set("user_email", payload.email);
        
    } catch (error) {
        console.log("   Could not decode JWT payload:", error.message);
    }
});