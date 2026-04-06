import BaseApiService from './BaseApiService';

/**
 * AuthApiService — handles authentication endpoints.
 */
class AuthApiService extends BaseApiService {
  constructor() {
    super('/Auth');
  }

  /** Register a new account */
  register(displayName, email, password) {
    return this._post('/register', { displayName, email, password });
  }

  /** Login with email & password — returns { token, expiration, userId, email, displayName } */
  login(email, password) {
    return this._post('/login', { email, password });
  }
}

const authApiService = new AuthApiService();

// ── Backward-compatible named exports ──
export const register = (...args) => authApiService.register(...args);
export const login = (...args) => authApiService.login(...args);
export default authApiService;
