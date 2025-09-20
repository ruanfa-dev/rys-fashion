// External Authentication Service for Vue.js/React
export class ExternalAuthService {
  private apiBaseUrl: string;
  
  constructor(apiBaseUrl = '/api') {
    this.apiBaseUrl = apiBaseUrl;
  }

  /**
   * Redirect to Keycloak for external OAuth authentication
   */
  async redirectToExternalAuth(provider: 'google' | 'facebook', redirectUri?: string) {
    const currentUrl = redirectUri || `${window.location.origin}/auth/callback`;
    const state = this.generateState();
    
    // Store state for validation
    sessionStorage.setItem('oauth_state', state);
    sessionStorage.setItem('oauth_provider', provider);
    
    try {
      const response = await fetch(
        `${this.apiBaseUrl}/auth/external/authorize-url/${provider}?redirectUri=${encodeURIComponent(currentUrl)}&state=${state}`
      );
      
      if (!response.ok) {
        throw new Error(`Failed to get authorize URL: ${response.status}`);
      }
      
      const data = await response.json();
      window.location.href = data.authorizeUrl;
    } catch (error) {
      console.error('External auth redirect failed:', error);
      throw error;
    }
  }

  /**
   * Handle callback from external authentication
   */
  async handleCallback(): Promise<ExternalAuthResult> {
    const urlParams = new URLSearchParams(window.location.search);
    const code = urlParams.get('code');
    const state = urlParams.get('state');
    const error = urlParams.get('error');
    
    // Validate state parameter
    const storedState = sessionStorage.getItem('oauth_state');
    if (state !== storedState) {
      throw new Error('Invalid state parameter');
    }
    
    if (error) {
      throw new Error(`Authentication failed: ${error}`);
    }
    
    if (!code) {
      throw new Error('Authorization code missing');
    }
    
    // Exchange authorization code for tokens via Keycloak's token endpoint
    return await this.exchangeCodeForTokens(code);
  }

  /**
   * Exchange authorization code for access tokens
   */
  private async exchangeCodeForTokens(code: string): Promise<ExternalAuthResult> {
    const tokenEndpoint = await this.getKeycloakTokenEndpoint();
    const clientId = await this.getClientId();
    const redirectUri = `${window.location.origin}/auth/callback`;
    
    const tokenRequest = new URLSearchParams({
      grant_type: 'authorization_code',
      client_id: clientId,
      code: code,
      redirect_uri: redirectUri
    });
    
    try {
      const response = await fetch(tokenEndpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: tokenRequest
      });
      
      if (!response.ok) {
        const errorData = await response.text();
        throw new Error(`Token exchange failed: ${response.status} - ${errorData}`);
      }
      
      const tokens = await response.json();
      
      // Get user info from the access token
      const userInfo = await this.getUserInfo(tokens.access_token);
      
      return {
        accessToken: tokens.access_token,
        refreshToken: tokens.refresh_token,
        expiresIn: tokens.expires_in,
        tokenType: tokens.token_type || 'Bearer',
        userInfo,
        isNewUser: this.determineIfNewUser(userInfo)
      };
    } catch (error) {
      console.error('Token exchange failed:', error);
      throw error;
    }
  }

  /**
   * Alternative: Direct token exchange with external access token
   * Use this if you get the external token directly (e.g., via Google/Facebook SDK)
   */
  async exchangeExternalToken(
    provider: 'google' | 'facebook', 
    externalAccessToken: string,
    options: ExternalAuthOptions = {}
  ): Promise<ExternalAuthResult> {
    const request = {
      provider,
      accessToken: externalAccessToken,
      idToken: options.idToken,
      linkToExistingUser: options.linkToExistingUser || false,
      existingUserId: options.existingUserId
    };

    try {
      const response = await fetch(`${this.apiBaseUrl}/auth/external/exchange`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(request)
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new ExternalAuthError(errorData.message || 'Token exchange failed', response.status, errorData);
      }

      const result = await response.json();
      return {
        accessToken: result.accessToken,
        refreshToken: result.refreshToken,
        expiresIn: result.expiresIn,
        tokenType: result.tokenType,
        userInfo: result.userInfo,
        isNewUser: result.isNewUser,
        isLinkedAccount: result.isLinkedAccount
      };
    } catch (error) {
      if (error instanceof ExternalAuthError) {
        throw error;
      }
      console.error('External token exchange failed:', error);
      throw new ExternalAuthError('Network error during token exchange');
    }
  }

  /**
   * Google Sign-In integration
   */
  async handleGoogleSignIn(): Promise<ExternalAuthResult> {
    // This requires Google Identity Services library
    // Include: <script src="https://accounts.google.com/gsi/client"></script>
    
    return new Promise((resolve, reject) => {
      if (typeof google === 'undefined') {
        reject(new Error('Google Identity Services not loaded'));
        return;
      }

      google.accounts.id.initialize({
        client_id: process.env.VUE_APP_GOOGLE_CLIENT_ID || process.env.REACT_APP_GOOGLE_CLIENT_ID,
        callback: async (response) => {
          try {
            // response.credential contains the ID token
            const result = await this.exchangeExternalToken('google', response.credential);
            resolve(result);
          } catch (error) {
            reject(error);
          }
        }
      });

      google.accounts.id.prompt();
    });
  }

  /**
   * Facebook Login integration
   */
  async handleFacebookLogin(): Promise<ExternalAuthResult> {
    // This requires Facebook SDK
    // Include Facebook SDK in your HTML
    
    return new Promise((resolve, reject) => {
      if (typeof FB === 'undefined') {
        reject(new Error('Facebook SDK not loaded'));
        return;
      }

      FB.login((response) => {
        if (response.authResponse) {
          this.exchangeExternalToken('facebook', response.authResponse.accessToken)
            .then(resolve)
            .catch(reject);
        } else {
          reject(new Error('Facebook login cancelled'));
        }
      }, { scope: 'email,public_profile' });
    });
  }

  /**
   * Link external account to existing user
   */
  async linkExternalAccount(
    provider: 'google' | 'facebook',
    externalAccessToken: string,
    userId: string
  ): Promise<ExternalAuthResult> {
    return this.exchangeExternalToken(provider, externalAccessToken, {
      linkToExistingUser: true,
      existingUserId: userId
    });
  }

  // Helper methods
  private generateState(): string {
    return Math.random().toString(36).substring(2, 15) + Math.random().toString(36).substring(2, 15);
  }

  private async getKeycloakTokenEndpoint(): string {
    // Get from your configuration
    const keycloakUrl = process.env.VUE_APP_KEYCLOAK_URL || process.env.REACT_APP_KEYCLOAK_URL;
    const realm = process.env.VUE_APP_KEYCLOAK_REALM || process.env.REACT_APP_KEYCLOAK_REALM || 'rys-fashion';
    return `${keycloakUrl}/realms/${realm}/protocol/openid-connect/token`;
  }

  private async getClientId(): string {
    return process.env.VUE_APP_KEYCLOAK_CLIENT_ID || process.env.REACT_APP_KEYCLOAK_CLIENT_ID || 'rys-fashion-api';
  }

  private async getUserInfo(accessToken: string): Promise<UserInfo> {
    const keycloakUrl = process.env.VUE_APP_KEYCLOAK_URL || process.env.REACT_APP_KEYCLOAK_URL;
    const realm = process.env.VUE_APP_KEYCLOAK_REALM || process.env.REACT_APP_KEYCLOAK_REALM || 'rys-fashion';
    
    const response = await fetch(`${keycloakUrl}/realms/${realm}/protocol/openid-connect/userinfo`, {
      headers: {
        'Authorization': `Bearer ${accessToken}`
      }
    });

    if (!response.ok) {
      throw new Error('Failed to get user info');
    }

    return await response.json();
  }

  private determineIfNewUser(userInfo: any): boolean {
    // Implementation depends on your user model
    return !userInfo.roles || userInfo.roles.length === 0;
  }
}

// Types
export interface ExternalAuthResult {
  accessToken: string;
  refreshToken?: string;
  expiresIn: number;
  tokenType: string;
  userInfo: UserInfo;
  isNewUser: boolean;
  isLinkedAccount?: boolean;
}

export interface ExternalAuthOptions {
  idToken?: string;
  linkToExistingUser?: boolean;
  existingUserId?: string;
}

export interface UserInfo {
  sub: string;
  email: string;
  email_verified: boolean;
  name: string;
  given_name: string;
  family_name: string;
  picture?: string;
  preferred_username: string;
}

export class ExternalAuthError extends Error {
  constructor(
    message: string,
    public statusCode?: number,
    public details?: any
  ) {
    super(message);
    this.name = 'ExternalAuthError';
  }
}

// Usage example
export const externalAuthService = new ExternalAuthService();