/**
 * TokenManager — encapsulates JWT token storage in localStorage.
 * Single Responsibility: manage auth token persistence.
 */
class TokenManager {
  #storageKey;

  /**
   * @param {string} storageKey - localStorage key for the token
   */
  constructor(storageKey = 'vah_token') {
    this.#storageKey = storageKey;
  }

  /** Retrieve the stored JWT token (or null). */
  getToken() {
    return localStorage.getItem(this.#storageKey);
  }

  /** Persist a JWT token. */
  setToken(token) {
    localStorage.setItem(this.#storageKey, token);
  }

  /** Remove the stored token (logout). */
  clearToken() {
    localStorage.removeItem(this.#storageKey);
  }

  /** Check whether a token is currently stored. */
  hasToken() {
    return !!this.getToken();
  }
}

/** Singleton instance used across the app. */
const tokenManager = new TokenManager();

export default tokenManager;
