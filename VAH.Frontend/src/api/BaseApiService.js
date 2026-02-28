import apiClient from './client';

/**
 * BaseApiService — abstract base class for all API service classes.
 *
 * Provides:
 * - Shared `apiClient` reference
 * - Common CRUD helpers (get, post, put, delete) with consistent `.data` unwrapping
 * - Subclasses only need to set `this.endpoint` and add domain-specific methods
 *
 * OOP: Encapsulation, Inheritance (DRY), Open/Closed (extend without modifying base)
 */
export default class BaseApiService {
  /** @type {string} API endpoint prefix, e.g. '/Assets' */
  endpoint;

  /** @type {import('axios').AxiosInstance} Shared axios instance */
  client;

  /**
   * @param {string} endpoint - The base API endpoint for this service
   */
  constructor(endpoint) {
    this.endpoint = endpoint;
    this.client = apiClient;
  }

  // ──── Generic CRUD helpers ────

  /**
   * GET request and return response data.
   * @param {string} [path=''] - Path appended to the endpoint
   * @param {object} [params={}] - Query params
   */
  async _get(path = '', params = {}) {
    const res = await this.client.get(`${this.endpoint}${path}`, { params });
    return res.data;
  }

  /**
   * POST request and return response data.
   * @param {string} [path=''] - Path appended to the endpoint
   * @param {*} data - Request body
   * @param {object} [config={}] - Extra axios config
   */
  async _post(path = '', data = {}, config = {}) {
    const res = await this.client.post(`${this.endpoint}${path}`, data, config);
    return res.data;
  }

  /**
   * PUT request and return response data.
   * @param {string} [path=''] - Path appended to the endpoint
   * @param {*} data - Request body
   */
  async _put(path = '', data = {}) {
    const res = await this.client.put(`${this.endpoint}${path}`, data);
    return res.data;
  }

  /**
   * DELETE request and return response data.
   * @param {string} [path=''] - Path appended to the endpoint
   */
  async _delete(path = '') {
    const res = await this.client.delete(`${this.endpoint}${path}`);
    return res.data;
  }
}
