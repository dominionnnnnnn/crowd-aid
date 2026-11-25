# Public pages, live auth and demo dashboard

All edits are in `web/`. C# controllers and DTOs are the API contract. The shared axios instance has baseURL `/api/v1`; JSON is camelCase. Public requests, login and activation do not carry an unrelated admin token. Error messages and field errors use `{ error: { code, message, fields } }`.

## Routes and endpoints

Endpoints below are relative to `/api/v1`, except the absolute scaffold paths.

| Route / feature             | Endpoints                                                                                                                                   | Current mode                                                                |
| --------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------- |
| `/`                         | `GET /campaigns?featured=true`                                                                                                              | Live campaign data; responsive community hero and FAQ are static            |
| `/give`                     | Same Home page with Give help initially expanded                                                                                            | Live featured data                                                          |
| `/campaigns`                | `GET /campaigns?q=&location=&need=`                                                                                                         | Live                                                                        |
| `/campaigns/:id`            | `GET /campaigns/{id}`                                                                                                                       | Live                                                                        |
| `/apply`                    | `POST /applications`                                                                                                                        | Live                                                                        |
| `/apply/status`             | `GET /applications/status?reference=&email=`                                                                                                | Live                                                                        |
| `/help?campaign=&type=`     | Optional `GET /campaigns/{id}`, `POST /practical-help-offers`                                                                               | Live                                                                        |
| `/volunteer?path=outreach`  | `GET /outreach`, `POST /volunteers/outreach`                                                                                                | Live                                                                        |
| `/volunteer?path=community` | `POST /volunteers/community`                                                                                                                | Live                                                                        |
| `/donate?campaign=`         | Campaign lookup/list, `POST /donations`                                                                                                     | Live Paystack initialization                                                |
| `/donate/result?ref=`       | `GET /donations/{reference}`                                                                                                                | Live Paystack verification                                                  |
| `/contact?campaign=`        | Optional campaign lookup, `POST /contact-messages`                                                                                          | Live                                                                        |
| Campaign contact dialog     | `POST /contact-messages`                                                                                                                    | Live; shares the contact page's form component                              |
| `/activate?token=`          | `POST /auth/activation/check`, `POST /auth/activate`                                                                                        | **Live authentication**                                                     |
| `/login`                    | `POST /auth/login`                                                                                                                          | **Live authentication**                                                     |
| `/dashboard`                | `GET /auth/me` for identity; `/me/campaign`, `/me/campaign/donations`, `/me/campaign/offers`, `/me/payout`, `/me/withdrawals` for demo data | **Live identity; mocked dashboard data**                                    |
| `/dashboard/offers`         | Same workspace load, displaying practical-help responses without supporter names/phones                                                     | Live identity; mocked data                                                  |
| `/dashboard/withdraw`       | Same workspace load; `PUT /me/payout`, `POST /me/withdrawals`                                                                               | Live identity; mocked data/actions, labelled Demo                           |
| `/admin`                    | Admin login, applications, offers, volunteers, messages, activation links; withdrawals live URLs configured                                 | Live; withdrawal backend routes absent at integration time (see `ADMIN.md`) |
| `/__check`                  | Absolute `/api/health`, `/api/hello`, `/api/test-email` with per-request `baseURL: ""`                                                      | Live scaffold                                                               |
| Unmatched route             | Design-style 404                                                                                                                            | Static                                                                      |

`src/api/public.js` contains only live calls and no mock switch. All existing public controller routes already have matching frontend consumers. Paystack's webhook is server-to-server and has no browser action. No public campaign, donor total, submission reference or payment result is fabricated.

## Authentication and dashboard boundary

`src/api/campaigner.js` now always sends `/auth/login`, `/auth/activation/check`, `/auth/activate`, and `/auth/me` to the real API. Authentication has no mock switch. Only `/me/*` requests use `DASHBOARD_MOCKS = true` while those endpoints do not exist. Set that one flag to false when the dashboard backend is ready.

Login sends `{ email, password }` and consumes `{ token, expiresIn, user }`. Activation check sends `{ token }` and consumes `{ valid, name, emailMasked, applicationRef, patientName }`; application/patient fields may be null. Activation submits only `{ token, password }`, consumes the same login response, stores its JWT and opens the dashboard. Password confirmation is local. The new password is 8–128 characters. Expired, used and invalid-link errors display the backend's message.

The activation token is captured in memory, removed from the address bar, and sent only in request bodies. `index.html` supplies a no-referrer policy. Passwords and activation links are not stored or logged. Old demo tokens and made-up credentials are no longer accepted by the UI's auth adapter.

Campaigner session key: `crowdaid.campaigner.session`. Admin session key: `crowdaid.admin.session`. Tokens live in memory and separate sessionStorage entries. Each role sends its own Bearer token; the admin `/auth/me` request explicitly selects the admin session so simultaneous admin/campaigner sign-ins do not overwrite one another. Protected 401 responses expire only the corresponding session.

The dashboard fetches the real user with `GET /auth/me` and uses that user's name. All campaign figures, donations, practical offers, payout details and withdrawals are still fictional mock examples, clearly labelled “Demo: no real money moves”. The dashboard's mock store uses an internal demo marker rather than treating the real JWT as mock credentials. Mock data does not publish or mutate any live campaign and resets on page reload. Names and phones are absent from campaigner practical-help DTOs. Do not interpret mock campaign content as the logged-in person's real application.

## Home image and FAQ

The hero uses `/assets/hero-community.jpg` (1024×1536) and `/assets/hero-community-sm.jpg` (512×768) through `<picture>` and responsive `srcSet`/`sizes`. Explicit dimensions plus a reserved aspect ratio prevent image-loading layout shift. The image uses object-fit cover and 50% 30% positioning, a 16px radius, a subtle shadow and a soft offset green accent. Two small cards use the existing brand/icon system. Text is left/image right on desktop; the image follows the text on mobile. Alt text is exactly: “CrowdAid volunteers in white vests collecting donations and sharing flyers at a local market”.

Home includes all 12 user-supplied FAQ questions and answers unchanged. Native buttons expose aria-expanded/aria-controls, Enter/Space work normally, all answers start closed, and only one answer opens at a time. The answer phrases “Get Help”, “Volunteer section” and “Contact Us section” link to `/apply`, `/volunteer` and `/contact` respectively.

## Public form and payment behavior

- Application email is required. Fields match `CreateApplicationRequest`, optional amounts become null and supplied amounts remain integer naira. Application copy explains that approved applicants receive an account link by email. Documents are requested privately; no download/upload endpoint is invented.
- Offers and volunteer submissions use the exact C# request field names and lookup values. Outreach selectors use actual returned event IDs. Success screens show the returned references. The contact page and dialog submit `{ campaignId, name, phone, message }`; general enquiries use null campaignId.
- Donating sends `campaignId`, integer naira `amount`, `name`, `anonymous`, `email`, `method`. There is no simulated outcome selector. The server-returned amount/fee/total and reference are shown before navigating to the HTTPS authorizationUrl. No card or bank details are collected by CrowdAid.
- Donation return checks the backend up to four times at 2.5-second intervals while pending, then provides a manual recheck. It never retries payment automatically. Returned campaign totals are authoritative and the “Test mode, no real money” badge follows testMode. Donation drafts survive hosted-checkout return in sessionStorage and clear on confirmed success; they contain no card data or passwords.
- Errors map to fields, preserve answers, and use borders plus ⚠ text. Controls retain at least 44px targets and 3px focus rings. Unknown/unpublished campaigns use the unavailable state; empty campaign/update/outreach states remain truthful.

## Backend dependencies and validation

Live behavior requires the API/database and real account credentials. Approval/resend email goes to MailHog in demo, not the applicant's real inbox. Paystack requires the backend's test configuration and `/donate/result` callback. No secrets are included in the frontend.

Admin withdrawals: the live request paths are configured, but this checkout currently has no withdrawal controller/DTOs. Campaigner `/me/*` data remains intentionally mocked. Everything else exposed by the reviewed browser-facing controllers is wired live.

Run `npm run build`. No tests were added or run; nothing was pushed, merged or deployed.
