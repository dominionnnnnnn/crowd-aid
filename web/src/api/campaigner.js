import api from "../api";
import { campaignerMock } from "../mocks/campaigner";
import {
  getCampaignerToken,
  expireCampaignerSession,
} from "./campaignerSession";
export const USE_MOCKS = true;
async function request(method, path, data, params) {
  if (!USE_MOCKS)
    return (await api.request({ method, url: path, data, params })).data;
  try {
    return await campaignerMock(
      method,
      path,
      data,
      params,
      getCampaignerToken(),
    );
  } catch (e) {
    if (e.response?.status === 401 && path !== "/auth/login")
      expireCampaignerSession();
    throw e;
  }
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
