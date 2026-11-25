import api from "../api";
import {
  getCampaignerToken,
  expireCampaignerSession,
} from "./campaignerSession";
export const DASHBOARD_MOCKS = true;
async function request(method, path, data, params) {
  // Authentication and session restoration always use the real backend.
  if (!path.startsWith("/me/") || !DASHBOARD_MOCKS)
    return (await api.request({ method, url: path, data, params })).data;
  if (!getCampaignerToken()) {
    expireCampaignerSession();
    throw {
      response: {
        status: 401,
        data: {
          error: {
            code: "UNAUTHENTICATED",
            message: "Your session expired. Sign in again.",
            fields: {},
          },
        },
      },
    };
  }
  // The dashboard store is fictional; its old demo-token format is internal only.
  // Never pass a real JWT to the mock auth implementation.
  const { campaignerMock } = await import("../mocks/campaigner");
  return campaignerMock(
    method,
    path,
    data,
    params,
    `demo-campaigner.${Date.now() + 28800000}`,
  );
}
export const campaignerApi = {
  login: (body) => request("post", "/auth/login", body),
  checkActivation: (token) =>
    request("post", "/auth/activation/check", { token }),
  activate: (body) => request("post", "/auth/activate", body),
  me: () => request("get", "/auth/me"),
  campaign: () => request("get", "/me/campaign"),
  donations: (params = { page: 1, pageSize: 20 }) =>
    request("get", "/me/campaign/donations", undefined, params),
  offers: (status) =>
    request("get", "/me/campaign/offers", undefined, status ? { status } : {}),
  payout: () => request("get", "/me/payout"),
  savePayout: (body) => request("put", "/me/payout", body),
  withdrawals: () => request("get", "/me/withdrawals"),
  withdraw: (amount) => request("post", "/me/withdrawals", { amount }),
};
