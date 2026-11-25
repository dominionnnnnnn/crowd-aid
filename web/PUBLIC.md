# CrowdAid frontend — live API

All browser-facing features now use the real C# API through the shared axios instance with baseURL `/api/v1`. JSON routes, field names and envelopes follow the controllers and DTOs. `USE_MOCKS = false` for admin and `DASHBOARD_MOCKS = false` for campaigner data. The existing mock files remain unused; there is no fallback to fabricated data when the API fails.

## Routes and endpoints

All paths below are relative to `/api/v1`, except the explicit scaffold paths.

| Route / feature                                  | Endpoints                                                                                                        | Mode                                                               |
| ------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------ |
| `/`, `/give`                                     | `GET /campaigns?featured=true`                                                                                   | Live data; static hero/FAQ; `/give` initially expands ways to help |
| `/campaigns`                                     | `GET /campaigns?q=&location=&need=`                                                                              | Live                                                               |
| `/campaigns/:id`                                 | `GET /campaigns/{id}`                                                                                            | Live                                                               |
| `/apply`                                         | `POST /applications`                                                                                             | Live                                                               |
| `/apply/status`                                  | `GET /applications/status?reference=&email=`                                                                     | Live                                                               |
| `/help?campaign=&type=`                          | Optional campaign lookup, `POST /practical-help-offers`                                                          | Live                                                               |
| `/volunteer?path=outreach`                       | `GET /outreach`, `POST /volunteers/outreach`                                                                     | Live                                                               |
| `/volunteer?path=community`                      | `POST /volunteers/community`                                                                                     | Live                                                               |
| `/contact?campaign=` and campaign contact dialog | Optional campaign lookup, `POST /contact-messages`                                                               | Live                                                               |
| `/donate?campaign=`                              | Campaign lookup/list, `POST /donations`                                                                          | Live Paystack initialization                                       |
| `/donate/result?ref=`                            | `GET /donations/{reference}`                                                                                     | Live verification                                                  |
| `/activate?token=`                               | `POST /auth/activation/check`, `POST /auth/activate`                                                             | Live                                                               |
| `/login`                                         | `POST /auth/login`                                                                                               | Live                                                               |
| `/dashboard`                                     | `GET /auth/me`, `/me/campaign`, `/me/campaign/donations`, `/me/campaign/offers`, `/me/payout`, `/me/withdrawals` | Live                                                               |
| `/dashboard/offers`                              | Same live workspace load; displays campaigner-safe offers                                                        | Live                                                               |
| `/dashboard/withdraw`                            | Same workspace load, `PUT /me/payout`, `POST /me/withdrawals`                                                    | Live; server-side demo withdrawals, no bank transfer               |
| `/admin`                                         | Admin applications, inbox, account links and withdrawals; see `ADMIN.md`                                         | Live                                                               |
| `/__check`                                       | Absolute `/api/health`, `/api/hello`, `/api/test-email` with per-request empty baseURL                           | Live scaffold                                                      |
| Unknown route                                    | Design-style 404                                                                                                 | Static                                                             |

## CTA and mobile navigation

Get help is the solid primary CTA. Give help uses the shared `.p-outline` variant in both the hero and nav: transparent background, 1.5px brand-green border/text, light green hover/focus tint, same dimensions/radius/padding as the primary.

At mobile widths, Menu opens a native modal. Give help is a button that toggles a visible nested section, with `aria-expanded` and `aria-controls`. Its links navigate to Campaigns, Practical help and Volunteer, and close the menu. Enter activates the button; Escape collapses the nested section and returns focus to Give help, while Escape on the closed section closes the modal. Native dialog focus trapping and restoration remain in place. Navigation displays Dashboard and Sign out when a campaigner token is present, Sign in otherwise; sign out clears only the campaigner session.

A manual browser check at **375px** confirmed tap-open, all three destinations closing the menu, Enter-open, and Escape-collapse/focus return. No test files or test suite were added.

## Live campaigner contracts

`GET /me/campaign` returns `{ application: { reference, status, patientName }, campaign: MeCampaignDto | null, payoutSet }`. A null campaign displays **Your campaign is being prepared for publication**, with the application reference/status and an explanation that the application is still private. Funding and withdrawal actions wait for publication; empty donations/offers/history stay empty.

A campaign contains its id, title, publication flag, raised/goal/supporters, withdrawn, pendingWithdrawals, available, needs and updates. These are server values, never hardcoded totals. Money uses whole naira formatted as ₦ with en-NG grouping; dates use en-NG.

- Donations: `{ data: [{ reference, displayName, amount, createdAt }], total }`; confirmed-only backend data, no phone/email/payment details.
- Offers: `{ data: MeOfferDto[] }`; types, optional details, availability, area, status, date and reference; no supporter names or phones.
- GET payout: masked `{ bankName, accountNumber, accountName }`, or JSON null. PUT sends those three fields with a full 10-digit accountNumber and consumes the masked response. Full account numbers are not logged or stored by the frontend.
- Withdrawals GET: `{ data: [{ reference, amount, status, requestedAt, decidedAt, note }], available }`.
- Withdrawals POST: `{ amount }` with a whole naira amount at least ₦1,000 and no more than available; requires payoutSet and a published campaign. Response: `{ reference, amount, status, available, demo }`.

Saving payout details immediately updates the masked display and payoutSet gate, then refreshes backend data. A successful withdrawal shows the returned reference/status/available and **Demo: no real money moves**. The balance updates immediately from the response, and history plus campaign balances refresh. Request/status records are live; the backend intentionally makes no actual payout. An error preserves the form and displays the server message/field errors.

## Authentication and errors

Login sends `{ email, password }` and consumes `{ token, expiresIn, user }`. Activation checks a token in a POST body, removes it from the address bar, displays the returned identity/application (nullable fields supported), then submits `{ token, password }`. Password confirmation stays local. `GET /auth/me` restores the real identity. Passwords and activation links are never logged or stored.

Campaigner token: `crowdaid.campaigner.session`. Admin token: `crowdaid.admin.session`. Each is held in memory plus its own sessionStorage entry. The axios interceptor selects the correct Bearer token, including when both roles use `/auth/me`. A protected campaigner 401 clears its session and redirects to `/login?expired=1` with “Your session expired. Sign in again.” Admin 401 behavior remains separate.

Backend `{ error: { code, message, fields } }` messages are shown directly and fields map to matching controls. Non-JSON HTTP errors get plain retry wording; axios's generic “Request failed with status code…” text is not used. Inputs preserve answers and have border + ⚠ errors, 44px targets and 3px focus rings.

## Other public behavior

Application email is required; optional amounts are null or integer naira. Approval-account copy and private document follow-up remain. Live event IDs populate outreach selectors. Success screens show returned references. Contact submits `{ campaignId, name, phone, message }`, with null campaignId for general enquiries.

Donation initialization shows the server's amount/platformFee/total before navigating to the HTTPS checkout URL. No card details are collected by CrowdAid. Return polls verification up to four times at 2.5-second intervals while pending, then offers a manual check without restarting payment. Campaign totals are authoritative; testMode controls the no-real-money badge. Donation drafts survive checkout return in sessionStorage and clear after confirmed success.

The responsive hero and all 12 supplied FAQ items remain in place, including links to `/apply`, `/volunteer` and `/contact`.

## Remaining endpoint gaps and validation

No reviewed backend endpoint exists for public evidence upload/secure document download, self-service password reset, creating outreach events, or editing campaign updates. The frontend does not invent those actions. Admin activation resend provides the supported password-reset route. Paystack webhooks are server-to-server and do not need a browser action. All currently implemented frontend data flows are live.

Live API calls need the running API/database; Paystack additionally needs backend test configuration. During the 375px local preview, API calls returned server errors, so authenticated withdrawal behavior was aligned to the C# contract but not exercised end-to-end with real credentials.

Run `npm run build`. No tests were created or run, and nothing was pushed, merged or deployed. Changes are confined to `web/`.
