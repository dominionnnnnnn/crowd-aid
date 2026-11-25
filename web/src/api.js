import axios from "axios";
const SESSION_KEY = "crowdaid.admin.session";
let token = null;
try {
  token = sessionStorage.getItem(SESSION_KEY);
} catch {
  /* Memory-only in restricted browsers. */
}
export const getToken = () => token;
export function setToken(value) {
  token = value;
  try {
    value
      ? sessionStorage.setItem(SESSION_KEY, value)
      : sessionStorage.removeItem(SESSION_KEY);
  } catch {
    /* Memory still works. */
  }
}
export function expireSession() {
  setToken(null);
  window.dispatchEvent(new Event("crowdaid:session-expired"));
}
const api = axios.create({
  baseURL: "/api",
  timeout: 20000,
  headers: { "Content-Type": "application/json" },
});
api.interceptors.request.use((config) => {
  if (token && config.url !== "/admin/auth/login")
    config.headers.Authorization = `Bearer ${token}`;
  return config;
});
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (
      error.response?.status === 401 &&
      error.config?.url !== "/admin/auth/login"
    )
      expireSession();
    return Promise.reject(error);
  },
);
export default api;
