<!-- External Login Component for Vue.js -->
<template>
  <div class="external-auth-container">
    <div class="external-auth-buttons">
      <button 
        @click="handleGoogleLogin" 
        :disabled="isLoading"
        class="btn btn-google"
      >
        <i class="fab fa-google"></i>
        <span>{{ isLoading && currentProvider === 'google' ? 'Signing in...' : 'Sign in with Google' }}</span>
      </button>

      <button 
        @click="handleFacebookLogin" 
        :disabled="isLoading"
        class="btn btn-facebook"
      >
        <i class="fab fa-facebook-f"></i>
        <span>{{ isLoading && currentProvider === 'facebook' ? 'Signing in...' : 'Sign in with Facebook' }}</span>
      </button>
    </div>

    <!-- Error Display -->
    <div v-if="error" class="alert alert-danger mt-3">
      {{ error }}
      <button @click="clearError" class="btn-close" aria-label="Close"></button>
    </div>

    <!-- Success Message -->
    <div v-if="successMessage" class="alert alert-success mt-3">
      {{ successMessage }}
    </div>

    <!-- Account Linking Options -->
    <div v-if="showLinkingOptions" class="mt-4">
      <div class="card">
        <div class="card-header">
          <h5>Link External Account</h5>
        </div>
        <div class="card-body">
          <p>Do you want to link this {{ currentProvider }} account to your existing account?</p>
          <div class="d-flex gap-2">
            <button @click="linkToExistingAccount" class="btn btn-primary">
              Yes, Link Account
            </button>
            <button @click="createNewAccount" class="btn btn-secondary">
              Create New Account
            </button>
            <button @click="cancelLinking" class="btn btn-outline-secondary">
              Cancel
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import { externalAuthService, type ExternalAuthResult } from '../services/externalAuthService';

// Props
interface Props {
  showLinkingOption?: boolean;
  redirectAfterAuth?: string;
}

const props = withDefaults(defineProps<Props>(), {
  showLinkingOption: false,
  redirectAfterAuth: '/dashboard'
});

// Emits
interface Emits {
  (e: 'authSuccess', result: ExternalAuthResult): void;
  (e: 'authError', error: string): void;
}

const emit = defineEmits<Emits>();

// Composables
const router = useRouter();
const authStore = useAuthStore();

// Reactive state
const isLoading = ref(false);
const currentProvider = ref<'google' | 'facebook' | null>(null);
const error = ref<string | null>(null);
const successMessage = ref<string | null>(null);
const showLinkingOptions = ref(false);
const pendingAuthResult = ref<ExternalAuthResult | null>(null);

// Methods
const handleGoogleLogin = async () => {
  await handleExternalLogin('google');
};

const handleFacebookLogin = async () => {
  await handleExternalLogin('facebook');
};

const handleExternalLogin = async (provider: 'google' | 'facebook') => {
  isLoading.value = true;
  currentProvider.value = provider;
  error.value = null;
  successMessage.value = null;

  try {
    let result: ExternalAuthResult;

    if (provider === 'google') {
      result = await externalAuthService.handleGoogleSignIn();
    } else {
      result = await externalAuthService.handleFacebookLogin();
    }

    await handleAuthResult(result);
  } catch (err) {
    handleAuthError(err);
  } finally {
    isLoading.value = false;
    currentProvider.value = null;
  }
};

const handleAuthResult = async (result: ExternalAuthResult) => {
  try {
    // Store tokens
    await authStore.setTokens({
      accessToken: result.accessToken,
      refreshToken: result.refreshToken,
      expiresIn: result.expiresIn,
      tokenType: result.tokenType
    });

    // Set user info
    authStore.setUser(result.userInfo);

    // Emit success event
    emit('authSuccess', result);

    // Handle new user or account linking
    if (result.isNewUser && props.showLinkingOption) {
      showLinkingOptions.value = true;
      pendingAuthResult.value = result;
      return;
    }

    // Show success message
    successMessage.value = result.isNewUser 
      ? 'Welcome! Your account has been created successfully.'
      : 'Welcome back! You have been signed in successfully.';

    // Redirect after a short delay
    setTimeout(() => {
      router.push(props.redirectAfterAuth);
    }, 1500);

  } catch (err) {
    handleAuthError(err);
  }
};

const handleAuthError = (err: any) => {
  console.error('External authentication error:', err);
  
  let errorMessage = 'Authentication failed. Please try again.';
  
  if (err.statusCode === 404) {
    errorMessage = 'Account not found. Please complete the initial registration through our system first.';
  } else if (err.statusCode === 409) {
    errorMessage = 'This account is already linked to another user.';
  } else if (err.message) {
    errorMessage = err.message;
  }
  
  error.value = errorMessage;
  emit('authError', errorMessage);
};

const linkToExistingAccount = async () => {
  if (!pendingAuthResult.value || !currentProvider.value) return;

  try {
    isLoading.value = true;
    
    // This would typically show a login form first to get the existing user ID
    // For now, we'll redirect to a linking page
    router.push({
      name: 'AccountLinking',
      query: {
        provider: currentProvider.value,
        token: pendingAuthResult.value.accessToken
      }
    });
  } catch (err) {
    handleAuthError(err);
  }
};

const createNewAccount = () => {
  if (!pendingAuthResult.value) return;
  
  // Proceed with the new account creation
  successMessage.value = 'Welcome! Your new account has been created successfully.';
  showLinkingOptions.value = false;
  
  setTimeout(() => {
    router.push(props.redirectAfterAuth);
  }, 1500);
};

const cancelLinking = () => {
  showLinkingOptions.value = false;
  pendingAuthResult.value = null;
  authStore.logout();
};

const clearError = () => {
  error.value = null;
};

// Lifecycle
onMounted(async () => {
  // Check if we're handling a callback from external auth
  const urlParams = new URLSearchParams(window.location.search);
  const code = urlParams.get('code');
  
  if (code) {
    isLoading.value = true;
    try {
      const result = await externalAuthService.handleCallback();
      await handleAuthResult(result);
    } catch (err) {
      handleAuthError(err);
    } finally {
      isLoading.value = false;
    }
  }
});
</script>

<style scoped>
.external-auth-container {
  max-width: 400px;
  margin: 0 auto;
}

.external-auth-buttons {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 0.375rem;
  font-weight: 500;
  transition: all 0.2s;
  text-decoration: none;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-google {
  background-color: #4285f4;
  color: white;
}

.btn-google:hover:not(:disabled) {
  background-color: #3367d6;
}

.btn-facebook {
  background-color: #1877f2;
  color: white;
}

.btn-facebook:hover:not(:disabled) {
  background-color: #166fe5;
}

.alert {
  padding: 0.75rem 1rem;
  border-radius: 0.375rem;
  position: relative;
}

.alert-danger {
  background-color: #f8d7da;
  color: #721c24;
  border: 1px solid #f1aeb5;
}

.alert-success {
  background-color: #d1edff;
  color: #0f5132;
  border: 1px solid #a3cfbb;
}

.btn-close {
  position: absolute;
  top: 0.5rem;
  right: 0.5rem;
  background: none;
  border: none;
  font-size: 1.2rem;
  cursor: pointer;
}

.card {
  border: 1px solid #dee2e6;
  border-radius: 0.375rem;
}

.card-header {
  padding: 1rem;
  background-color: #f8f9fa;
  border-bottom: 1px solid #dee2e6;
}

.card-body {
  padding: 1rem;
}

.d-flex {
  display: flex;
}

.gap-2 {
  gap: 0.5rem;
}

.mt-3 {
  margin-top: 1rem;
}

.mt-4 {
  margin-top: 1.5rem;
}
</style>