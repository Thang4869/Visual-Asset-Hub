import apiClient from './client';

const ENDPOINT = '/Auth';

/** Register a new account */
export const register = (displayName, email, password) =>
  apiClient
    .post(`${ENDPOINT}/register`, { displayName, email, password })
    .then((r) => r.data);

/** Login with email & password — returns { token, expiration, userId, email, displayName } */
export const login = (email, password) =>
  apiClient
    .post(`${ENDPOINT}/login`, { email, password })
    .then((r) => r.data);
