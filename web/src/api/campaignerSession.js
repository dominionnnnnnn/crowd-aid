const KEY = "crowdaid.campaigner.session";
let token = null;
try {
  token = sessionStorage.getItem(KEY);
} catch {
  /* Memory fallback. */
}
export const getCampaignerToken = () => token;
export function setCampaignerToken(value) {
  token = value;
  try {
    value ? sessionStorage.setItem(KEY, value) : sessionStorage.removeItem(KEY);
  } catch {
    /* Memory fallback. */
  }
  window.dispatchEvent(new Event("crowdaid:campaigner-session-changed"));
}
export function expireCampaignerSession() {
  setCampaignerToken(null);
  window.dispatchEvent(new Event("crowdaid:campaigner-expired"));
}
