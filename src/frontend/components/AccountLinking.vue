<!-- Account Linking Component -->
<template>
  <div class="account-linking-container">
    <div class="card">
      <div class="card-header">
        <h3>Link {{ providerName }} Account</h3>
      </div>
      <div class="card-body">
        <p class="mb-4">
          To link your {{ providerName }} account, please sign in to your existing account first.
        </p>

        <form @submit.prevent="handleLinking" v-if="!isLinked">
          <div class="mb-3">
            <label for="email" class="form-label">Email</label>
            <input
              id="email"
              v-model="loginForm.email"
              type="email"
              class="form-control"
              :class="{ 'is-invalid': errors.email }"
              required
            />
            <div v-if="errors.email" class="invalid-feedback">
              {{ errors.email }}
            </div>
          </div>

          <div class="mb-3">
            <label for="password" class="form-label">Password</label>
            <input
              id="password"
              v-model="loginForm.password"
              type="password"
              class="form-control"
              :class="{ 'is-invalid': errors.password }"
              required
            />
            <div v-if="errors.password" class="invalid-feedback">
              {{ errors.password }}
            </div>
          </div>

          <div class="d-flex justify-content-between">
            <button
              type="button"
              @click="cancelLinking"
              class="btn btn-outline-secondary"
            >
              Cancel
            </button>
            <button
              type="submit"
              :disabled="isLoading"
              class="btn btn-primary"
            >
              {{ isLoading ? 'Linking...' : 'Link Account' }}
            </button>
          </div>
        </form>

        <!-- Success State -->
        <div v-if="isLinked" class="text-center">
          <div class="alert alert-success">
            <i class="fas fa-check-circle fa-2x mb-2"></i>
            <h5>Account Linked Successfully!</h5>
            <p>Your {{ providerName }} account has been linked to your existing account.</p>
          </div>
          <button @click="goToDashboard" class="btn btn-primary">
            Continue to Dashboard
          </button>
        </div>

        <!-- Error Display -->
        <div v-if="error" class="alert alert-danger">
          {{ error }}
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import { externalAuthService } from '../services/externalAuthService';

// Composables
const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();

// Reactive state
const isLoading = ref(false);
const isLinked = ref(false);
const error = ref<string | null>(null);

const loginForm = ref({
  email: '',
  password: ''
});

const errors = ref<Record<string, string>>({});

// Computed
const provider = computed(() => route.query.provider as string);
const externalToken = computed(() => route.query.token as string);
const providerName = computed(() => {
  return provider.value === 'google' ? 'Google' : 'Facebook';
});

// Methods
const handleLinking = async () => {
  if (!validateForm()) return;

  isLoading.value = true;
  error.value = null;

  try {
    // First, authenticate with existing credentials
    const loginResult = await authStore.login({
      email: loginForm.value.email,
      password: loginForm.value.password
    });

    if (loginResult.success) {
      // Get the user ID from the login result
      const userId = loginResult.user?.id;
      
      if (!userId) {
        throw new Error('User ID not available');
      }

      // Link the external account
      const linkResult = await externalAuthService.linkExternalAccount(
        provider.value as 'google' | 'facebook',
        externalToken.value,
        userId
      );

      if (linkResult) {
        isLinked.value = true;
        
        // Update the stored tokens with the linked account tokens
        await authStore.setTokens({
          accessToken: linkResult.accessToken,
          refreshToken: linkResult.refreshToken,
          expiresIn: linkResult.expiresIn,
          tokenType: linkResult.tokenType
        });
      }
    }
  } catch (err: any) {
    handleLinkingError(err);
  } finally {
    isLoading.value = false;
  }
};

const handleLinkingError = (err: any) => {
  console.error('Account linking error:', err);
  
  if (err.statusCode === 401) {
    error.value = 'Invalid email or password. Please check your credentials.';
    errors.value = {
      email: 'Please check your email',
      password: 'Please check your password'
    };
  } else if (err.statusCode === 409) {
    error.value = 'This external account is already linked to another user.';
  } else if (err.statusCode === 404) {
    error.value = 'Account not found. Please make sure you have an existing account.';
  } else {
    error.value = err.message || 'Failed to link account. Please try again.';
  }
};

const validateForm = (): boolean => {
  errors.value = {};
  
  if (!loginForm.value.email) {
    errors.value.email = 'Email is required';
  } else if (!isValidEmail(loginForm.value.email)) {
    errors.value.email = 'Please enter a valid email address';
  }
  
  if (!loginForm.value.password) {
    errors.value.password = 'Password is required';
  } else if (loginForm.value.password.length < 6) {
    errors.value.password = 'Password must be at least 6 characters';
  }
  
  return Object.keys(errors.value).length === 0;
};

const isValidEmail = (email: string): boolean => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
};

const cancelLinking = () => {
  router.push('/login');
};

const goToDashboard = () => {
  router.push('/dashboard');
};

// Lifecycle
onMounted(() => {
  // Validate required query parameters
  if (!provider.value || !externalToken.value) {
    error.value = 'Invalid linking request. Missing required parameters.';
  }
  
  if (!['google', 'facebook'].includes(provider.value)) {
    error.value = 'Unsupported provider for account linking.';
  }
});
</script>

<style scoped>
.account-linking-container {
  max-width: 500px;
  margin: 2rem auto;
  padding: 0 1rem;
}

.card {
  border: 1px solid #dee2e6;
  border-radius: 0.5rem;
  box-shadow: 0 0.125rem 0.25rem rgba(0, 0, 0, 0.075);
}

.card-header {
  padding: 1.25rem;
  background-color: #f8f9fa;
  border-bottom: 1px solid #dee2e6;
  border-radius: 0.5rem 0.5rem 0 0;
}

.card-header h3 {
  margin: 0;
  color: #495057;
}

.card-body {
  padding: 1.5rem;
}

.form-label {
  font-weight: 500;
  color: #495057;
  margin-bottom: 0.5rem;
}

.form-control {
  display: block;
  width: 100%;
  padding: 0.375rem 0.75rem;
  font-size: 1rem;
  line-height: 1.5;
  color: #495057;
  background-color: #fff;
  border: 1px solid #ced4da;
  border-radius: 0.375rem;
  transition: border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;
}

.form-control:focus {
  border-color: #86b7fe;
  outline: 0;
  box-shadow: 0 0 0 0.2rem rgba(13, 110, 253, 0.25);
}

.form-control.is-invalid {
  border-color: #dc3545;
}

.invalid-feedback {
  color: #dc3545;
  font-size: 0.875rem;
  margin-top: 0.25rem;
}

.btn {
  display: inline-block;
  padding: 0.375rem 0.75rem;
  font-size: 1rem;
  font-weight: 400;
  text-align: center;
  white-space: nowrap;
  vertical-align: middle;
  cursor: pointer;
  border: 1px solid transparent;
  border-radius: 0.375rem;
  transition: color 0.15s ease-in-out, background-color 0.15s ease-in-out,
    border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;
}

.btn:disabled {
  opacity: 0.65;
  cursor: not-allowed;
}

.btn-primary {
  color: #fff;
  background-color: #0d6efd;
  border-color: #0d6efd;
}

.btn-primary:hover:not(:disabled) {
  background-color: #0b5ed7;
  border-color: #0a58ca;
}

.btn-outline-secondary {
  color: #6c757d;
  border-color: #6c757d;
  background-color: transparent;
}

.btn-outline-secondary:hover {
  color: #fff;
  background-color: #6c757d;
  border-color: #6c757d;
}

.alert {
  padding: 0.75rem 1.25rem;
  border: 1px solid transparent;
  border-radius: 0.375rem;
  margin-bottom: 1rem;
}

.alert-success {
  color: #0f5132;
  background-color: #d1e7dd;
  border-color: #badbcc;
}

.alert-danger {
  color: #721c24;
  background-color: #f8d7da;
  border-color: #f5c2c7;
}

.d-flex {
  display: flex;
}

.justify-content-between {
  justify-content: space-between;
}

.text-center {
  text-align: center;
}

.mb-3 {
  margin-bottom: 1rem;
}

.mb-4 {
  margin-bottom: 1.5rem;
}

.mb-2 {
  margin-bottom: 0.5rem;
}

.fa-2x {
  font-size: 2em;
}
</style>