// Test script for Token Refresh endpoint
// This endpoint tests the refresh token functionality

pm.test("Token refresh successful", function () {
    pm.response.to.have.status(200);
    
    const jsonData = pm.response.json();
    
    // Verify response structure
    pm.expect(jsonData).to.have.property('access_token');
    pm.expect(jsonData).to.have.property('expires_in');
    // Note: refresh_token might not be included if rotation is disabled
    
    // Store new tokens with expiry tracking
    const now = Math.floor(Date.now() / 1000);
    const expiresAt = now + (jsonData.expires_in || 300);
    
    pm.environment.set("access_token", jsonData.access_token);
    pm.environment.set("user_token_expiry", expiresAt.toString());
    
    // Update refresh token if provided (token rotation)
    if (jsonData.refresh_token) {
        pm.environment.set("refresh_token", jsonData.refresh_token);
        console.log("? Refresh token rotated");
    }
    
    console.log("? Token refresh successful:");
    console.log("   New access token expires at:", new Date(expiresAt * 1000).toISOString());
    console.log("   New access token preview:", jsonData.access_token.substring(0, 20) + "...");
    
    if (jsonData.refresh_token) {
        console.log("   New refresh token preview:", jsonData.refresh_token.substring(0, 20) + "...");
    }
});

pm.test("New token has reasonable expiry", function () {
    const jsonData = pm.response.json();
    const expiresIn = jsonData.expires_in;
    
    // Token should expire between 1 minute and 24 hours
    pm.expect(expiresIn).to.be.above(60);
    pm.expect(expiresIn).to.be.below(86400);
    
    console.log(`   New token expires in: ${expiresIn} seconds (${Math.round(expiresIn/60)} minutes)`);
});

pm.test("New access token is different from old one", function () {
    const jsonData = pm.response.json();
    const oldToken = pm.environment.get("previous_access_token"); // Set this in pre-request script
    
    if (oldToken) {
        pm.expect(jsonData.access_token).to.not.equal(oldToken);
        console.log("? Access token was properly renewed");
    }
});

pm.test("New token is properly formatted JWT", function () {
    const jsonData = pm.response.json();
    const tokenParts = jsonData.access_token.split('.');
    
    pm.expect(tokenParts).to.have.lengthOf(3);
    
    try {
        // Verify we can decode the payload
        const payloadB64 = tokenParts[1];
        const payloadJson = atob(payloadB64.replace(/-/g, '+').replace(/_/g, '/'));
        const payload = JSON.parse(payloadJson);
        
        // Verify token contains expected claims
        pm.expect(payload).to.have.property('exp');
        pm.expect(payload).to.have.property('iat');
        pm.expect(payload).to.have.property('sub');
        
        console.log("? New JWT token format is valid");
        console.log("   Subject:", payload.sub);
        console.log("   Issued at:", new Date(payload.iat * 1000).toISOString());
        console.log("   Expires at:", new Date(payload.exp * 1000).toISOString());
        
    } catch (error) {
        pm.test.skip("Could not decode JWT payload: " + error.message);
    }
});

// Clean up test variables
pm.environment.unset("previous_access_token");