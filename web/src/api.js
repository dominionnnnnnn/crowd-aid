import axios from "axios";
import {
  getCampaignerToken,
  expireCampaignerSession,
} from "./api/campaignerSession";
const SESSION_KEY = "crowdaid.admin.session";
let token = null;
try {
  token = sessionStorage.getItem(SESSION_KEY);
} catch {
  /* Memory fallback. */
}
export const getToken = () => token;
export function setToken(value) {
  token = value;
  try {
    value
      ? sessionStorage.setItem(SESSION_KEY, value)
      : sessionStorage.removeItem(SESSION_KEY);
  } catch {
    /* Memory fallback. */
  }
}
export function expireSession() {
  setToken(null);
  window.dispatchEvent(new Event("crowdaid:session-expired"));
}
const api = axios.create({
  baseURL: "/api/v1",
  timeout: 20000,
  headers: { "Content-Type": "application/json" },
});
api.interceptors.request.use((config) => {
  const path = config.url || "";
  if (path.startsWith("/admin/") && path !== "/admin/auth/login" && token)
    config.headers.Authorization = `Bearer ${token}`;
  if ((path.startsWith("/me/") || path === "/auth/me") && getCampaignerToken())
    config.headers.Authorization = `Bearer ${getCampaignerToken()}`;
  return config;
});
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const path = error.config?.url || "";
    if (error.response?.status === 401) {
      if (path.startsWith("/admin/") && path !== "/admin/auth/login")
        expireSession();
      if (path.startsWith("/me/") || path === "/auth/me")
        expireCampaignerSession();
    }
    return Promise.reject(error);
  },
);
export default api;
