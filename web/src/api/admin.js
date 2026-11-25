import api, { expireSession, getToken } from "../api";
import { mockRequest } from "../mocks/admin";
export const USE_MOCKS = true;
async function request(method, path, data, params) {
  if (!USE_MOCKS)
    return (await api.request({ method, url: `/admin${path}`, data, params }))
      .data;
  try {
    return await mockRequest(method, path, data, params, getToken());
  } catch (error) {
    if (error.response?.status === 401 && path !== "/auth/login")
      expireSession();
    throw error;
  }
}
export const adminApi = {
  login: (data) => request("post", "/auth/login", data),
  applications: (status) =>
    request("get", "/applications", undefined, status ? { status } : {}),
  application: (ref) =>
    request("get", `/applications/${encodeURIComponent(ref)}`),
  verification: (ref, data) =>
    request(
      "patch",
      `/applications/${encodeURIComponent(ref)}/verification`,
      data,
    ),
  action: (ref, data) =>
    request("post", `/applications/${encodeURIComponent(ref)}/actions`, data),
  publish: (ref, data) =>
    request("post", `/applications/${encodeURIComponent(ref)}/publish`, data),
  activation: (ref) =>
    request("post", `/applications/${encodeURIComponent(ref)}/activation`),
  offers: (params) => request("get", "/offers", undefined, params),
  offerStatus: (ref, status) =>
    request("patch", `/offers/${encodeURIComponent(ref)}`, { status }),
  volunteers: (params) => request("get", "/volunteers", undefined, params),
  messages: () => request("get", "/messages"),
  withdrawals: (status) =>
    request("get", "/withdrawals", undefined, status ? { status } : {}),
  withdrawalAction: (ref, data) =>
    request("post", `/withdrawals/${encodeURIComponent(ref)}/actions`, data),
};
