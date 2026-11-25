# Public pages and campaigner workspace

All implementation changes are inside `web/`. `api/`, `design/`, root configuration, Caddyfiles and Dockerfiles are untouched. No tests were added or run. `npm run build` completed successfully.

## API and source of truth

The implemented C# controllers and DTOs in `api/CrowdAid.Api/Controllers/` and `api/CrowdAid.Data/DTOs/` take precedence over older design/spec examples. `src/api.js` is the single axios instance, with `baseURL: "/api/v1"`. JSON request and response fields use camelCase.

`src/api/public.js` always calls the live API. It has no mock switch, seeded campaign fallback, simulated donation result selector, or fabricated success response. Missing data produces an empty state; request failures produce a retry state. Actual content depends on the backend and its database.

Errors read `{ error: { code, message, fields } }`. The API message is displayed, field messages map to the named input or choice group, errors have a 3px border and ⚠ text, and summaries link to the affected fields. Form answers remain when submission fails. Controls have 44px or larger targets and 3px focus rings. Public styling is scoped in `src/public.css` to preserve the existing admin presentation.

## Route inventory

All endpoint paths in the following table are relative to `/api/v1` unless an absolute `/api/...` path is written.

| Route                                            | Data / endpoints                                                                                                                                             | Mode                                         |
| ------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------- |
| `/`                                              | `GET /campaigns?featured=true`; expandable Give help panel                                                                                                   | Live                                         |
| `/give`                                          | Same Home page, with Give help expanded initially                                                                                                            | Live featured campaign                       |
| `/campaigns`                                     | `GET /campaigns` with `q`, `location`, `need`; `locations` supplies the location filter                                                                      | Live                                         |
| `/campaigns/:id`                                 | `GET /campaigns/{id}`; story, needs, fund breakdown, newest two updates, organizer, funding and supporters                                                   | Live                                         |
| `/apply`                                         | `POST /applications` using `CreateApplicationRequest`; three sections, required applicant email, integer naira amounts, private-review and accuracy consents | Live                                         |
| `/apply/status`                                  | `GET /applications/status?reference=&email=`; shows returned reference, status and message                                                                   | Live                                         |
| `/help?campaign=&type=`                          | Optional `GET /campaigns/{id}`; `POST /practical-help-offers` using `CreateOfferRequest`                                                                     | Live                                         |
| `/volunteer?path=outreach`                       | `GET /outreach`; `POST /volunteers/outreach` using `OutreachVolunteerRequest`                                                                                | Live                                         |
| `/volunteer?path=community`                      | `POST /volunteers/community` using `CommunityVolunteerRequest`                                                                                               | Live                                         |
| `/donate?campaign=`                              | `GET /campaigns/{id}` (or campaign list if none selected); `POST /donations` using `CreateDonationRequest`                                                   | Live Paystack initialization                 |
| `/donate/result?ref=`                            | `GET /donations/{reference}`; reads the returned status, `testMode`, and authoritative campaign totals                                                       | Live verification                            |
| Contact CrowdAid dialog (header/footer/campaign) | `POST /contact-messages` with `campaignId`, `name`, `phone`, `message`                                                                                       | Live                                         |
| `/activate?token=`                               | `POST /auth/activation/check` with `{ token }`, then `POST /auth/activate` with `{ token, password }`                                                        | Mocked                                       |
| `/login`                                         | `POST /auth/login` with `{ email, password }`                                                                                                                | Mocked                                       |
| `/dashboard`                                     | `GET /auth/me`, `/me/campaign`, `/me/campaign/donations`, `/me/campaign/offers`, `/me/payout`, `/me/withdrawals`; overview, donation list and updates        | Mocked                                       |
| `/dashboard/offers`                              | Same workspace loading; displays `/me/campaign/offers` without supporter name or phone fields                                                                | Mocked                                       |
| `/dashboard/withdraw`                            | Same workspace loading; `PUT /me/payout`, `POST /me/withdrawals` with `{ amount }`, history and available balance                                            | Mocked; labelled “Demo: no real money moves” |
| `/admin`                                         | Existing admin endpoints #12–21 and #31–33 in `src/api/admin.js`; paths remain `/admin/...` relative to the new axios base                                   | Mocked; `USE_MOCKS = true` preserved         |
| `/__check`                                       | Absolute `/api/health`, `/api/hello`, `/api/test-email`, using `{ baseURL: "" }` per request so axios does not prepend `/api/v1`                             | Live scaffold                                |
| Any unmatched route                              | Design-style 404 with Home and Campaigns links                                                                                                               | Static                                       |

## Public behavior and backend details

- Home and campaign pages use the actual API response. No campaigns, raised amounts or supporters are invented. Empty updates are hidden. Unknown/unpublished campaigns use the unavailable state. GETs can be retried without losing navigation.
- Public campaign pages never render application contact details, private evidence, verification notes or payout details. A mobile donation bar appears after the main Donate button scrolls above the viewport.
- Application amounts are submitted as whole naira numbers, optional amounts as `null`, and patient age as an integer. Applying for oneself supplies the applicant name as `patientName` and `Myself` as the relationship. The applicant's email is always required. Approved applicants are told they get a sign-in link by email. There is no document upload endpoint in the implemented controllers, so the form uses the design's private-document follow-up copy.
- Offer `types`, `availability`, area, relationship, support-type and LGA values match the C# lookup values. Campaign need labels are mapped to offer categories in one helper. General offer success correctly accepts a null `campaignTitle` from C#.
- Outreach registration uses actual event IDs and returned date/time values. With no events, the outreach form is hidden and the community option remains available. Optional community email is submitted as `null` when empty.
- Each successful application, offer, volunteer registration and contact message shows the backend-returned reference.
- Donation initialization sends only `campaignId`, `amount`, `name`, `anonymous`, `email`, `method`. There is no `demoOutcome`. Amounts stay in naira; the backend handles kobo conversion. The returned `accessCode` is not needed for hosted checkout.
- Donation initialization shows a review panel with the returned `amount`, `platformFee`, `total`, reference and `testMode` badge. “Continue to secure checkout” navigates with `window.location.assign(authorizationUrl)` after requiring HTTPS. No card/bank details are collected by CrowdAid.
- Returning from Paystack never implies success. `/donate/result` checks the API, polls up to four times at 2.5-second intervals while pending, then offers a manual check. Pending payments are never automatically restarted. Only API-confirmed success is described as confirmed; campaign totals come from the response. The badge reads “Test mode, no real money” whenever `testMode` is true.
- To preserve details when returning from a failed hosted checkout, donation form answers are kept in sessionStorage under `crowdaid.donation.draft`. They are cleared on confirmed success. No card details, passwords, checkout access codes or activation tokens are stored there. The form still works if storage is unavailable.

## Campaigner mocks and sessions

`src/api/campaigner.js` has its own `USE_MOCKS = true` switch. Its paths and payloads follow the revised spec endpoints #22–30; admin #31–33 stay in the existing admin adapter, while status check #34 is live in the public adapter. Mock response objects and mutations live in `src/mocks/campaigner.js` and are never used by public pages.

The fictional campaigner demo has a small donation balance, a masked payout account, a practical-help offer, and withdrawal history. It is labelled throughout. It does not create campaigns or donations in the live API. Mock changes reset on reload. Sign in with a fictional valid email and any non-empty password. These are demonstration credentials, not a real seeded password.

Activation tokens beginning with `demo-` (including those generated by the mocked admin) and the scaffold's `TEST-ONLY` token demonstrate activation. The tokens `expired` and `used` show recovery states; unknown tokens are rejected. A successful activation is single-use within the mock module's current page lifetime and opens an Approved/private dashboard with no published campaign. Ordinary demo login on a fresh reload shows the fictional published workspace. The admin and campaigner mocks are separate stores; admin publication does not publish a campaigner mock or a live campaign.

The activation token is captured in memory, removed from the address bar on load, and sent in the check/activate request body. `index.html` supplies a `no-referrer` policy. New password and confirmation are checked locally (at least 8 characters, equal); only `token` and `password` go to the adapter.

Admin token: `crowdaid.admin.session`. Campaigner token: `crowdaid.campaigner.session`. Each is kept in memory and its own sessionStorage entry. The shared axios interceptor attaches an admin token only to protected `/admin/...` calls and a campaigner token only to `/me/...` and `/auth/me`. Public requests and login/activation calls carry neither token. Protected 401 responses clear only the corresponding session and show the corresponding sign-in state. Admin is never linked in public navigation.

Demo withdrawal requests enforce whole naira, a minimum of ₦1,000, available balance, publication and payout details. Payout edits require a full 10-digit account number, while the GET/display response masks it. Campaigner practical-help responses contain no supporter names or phone numbers. No actual payout or email is sent by these mocks.

## Build and remaining integration

`npm run build` passed. No tests were created or run, and nothing was committed, pushed, merged or deployed.

Live public actions require the API/database to be running. Paystack initialization/verification additionally requires the backend's Paystack test configuration and callback URL ending in `/donate/result`; the frontend contains no Paystack secret key. Admin and campaigner auth/dashboard endpoints are not built in C# yet, so both adapters deliberately remain mocked. Change each adapter's single switch only when its backend endpoints exist.
