import api from "../api";
const data = (promise) => promise.then((response) => response.data);
export const publicApi = {
  campaigns: (params = {}, signal) =>
    data(api.get("/campaigns", { params, signal })),
  campaign: (id, signal) =>
    data(api.get(`/campaigns/${encodeURIComponent(id)}`, { signal })),
  apply: (body) => data(api.post("/applications", body)),
  applicationStatus: (params) =>
    data(api.get("/applications/status", { params })),
  donate: (body) => data(api.post("/donations", body)),
  donation: (reference, signal) =>
    data(api.get(`/donations/${encodeURIComponent(reference)}`, { signal })),
  offer: (body) => data(api.post("/practical-help-offers", body)),
  outreach: (signal) => data(api.get("/outreach", { signal })),
  volunteerOutreach: (body) => data(api.post("/volunteers/outreach", body)),
  volunteerCommunity: (body) => data(api.post("/volunteers/community", body)),
  contact: (body) => data(api.post("/contact-messages", body)),
};
