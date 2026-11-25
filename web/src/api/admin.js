import api, { expireSession, getToken } from "../api";
export const USE_MOCKS = false;
async function request(method, path, data, params) {
  if (!USE_MOCKS)
    return (await api.request({ method, url: `/admin${path}`, data, params }))
      .data;
  const { mockRequest } = await import("../mocks/admin");
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
  me: () => api.get("/auth/me", { sessionRole: "admin" }).then((r) => r.data),
  applications: (status) =>
    request("get", "/applications", undefined, status ? { status } : {}),
  application: (reference) =>
    request("get", `/applications/${encodeURIComponent(reference)}`),
  verification: (reference, data) =>
    request(
      "patch",
      `/applications/${encodeURIComponent(reference)}/verification`,
      { status: data.status, method: data.method },
    ),
  action: (reference, data) =>
    request(
      "post",
      `/applications/${encodeURIComponent(reference)}/actions`,
      data,
    ),
  publish: (reference, data) =>
    request(
      "post",
      `/applications/${encodeURIComponent(reference)}/publish`,
      data,
    ),
  activation: (reference) =>
    request(
      "post",
      `/applications/${encodeURIComponent(reference)}/activation`,
    ),
  offers: (params) => request("get", "/offers", undefined, params),
  offerStatus: (reference, status) =>
    request("patch", `/offers/${encodeURIComponent(reference)}`, { status }),
  volunteers: (params) => request("get", "/volunteers", undefined, params),
  messages: () => request("get", "/messages"),
  withdrawals: (status) =>
    request("get", "/withdrawals", undefined, status ? { status } : {}),
  withdrawalAction: (reference, data) =>
    request(
      "post",
      `/withdrawals/${encodeURIComponent(reference)}/actions`,
      data,
    ),
};
