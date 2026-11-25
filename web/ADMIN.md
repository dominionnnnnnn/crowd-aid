# CrowdAid admin — live API integration

`src/api/admin.js` has `USE_MOCKS = false`. All admin requests use the single axios instance in `src/api.js`, with baseURL `/api/v1`. The old admin mock modules remain in place but are not used in live mode.

## Authentication

- `POST /admin/auth/login` sends `{ email, password }` and reads `{ token, expiresIn, admin: { id, name, role } }`.
- The JWT is held in memory and `sessionStorage` at `crowdaid.admin.session`. Passwords are never persisted.
- The returned admin name appears in the top bar. `GET /auth/me` restores/validates the admin session on reload; it explicitly uses the admin token, even if a campaigner is signed in in the same browser.
- Only protected admin requests carry the admin Bearer token. Protected 401 responses clear the admin session and show “Your session expired. Sign in again.” Login errors show the backend's generic credential message.
- Signing out clears only the admin token. Campaigner sessions remain separate.

## Live screens and C# contracts

The source of truth is `CrowdAid.Api/Controllers/` and `CrowdAid.Data/DTOs/Admin/` / `Auth/`, read without modifying backend files. JSON is camelCase.

| Screen / action         | Relative endpoint                                           | Shape used                                                                                                                                               |
| ----------------------- | ----------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Sign in                 | `POST /admin/auth/login`                                    | `AdminLoginResponse`                                                                                                                                     |
| Restore session         | `GET /auth/me`                                              | `UserSummaryDto`; role must be `admin`                                                                                                                   |
| Applications filter     | `GET /admin/applications?status=`                           | `{ data: AdminApplicationRowDto[] }`                                                                                                                     |
| Private detail          | `GET /admin/applications/{reference}`                       | `AdminApplicationDetailDto`, including `reference`, `receivedAt`, application fields, verification, files, log, publication, campaignId and account      |
| Verification            | `PATCH /admin/applications/{reference}/verification`        | Body `{ status, method }`; response is the updated full detail, which the UI consumes directly                                                           |
| Decisions               | `POST /admin/applications/{reference}/actions`              | `ApplicationActionRequest`: `action`, and the relevant `needed`, `message`, `reason`, `note`; response `{ reference, status, account }`                  |
| Publish                 | `POST /admin/applications/{reference}/publish`              | `title`, `location`, integer `goal`, `organizer`, `story`, `consentRecorded`, `publicationConsent`, `contentReviewed`; response `{ campaignId, status }` |
| New activation link     | `POST /admin/applications/{reference}/activation`           | Direct `AccountLinkDto`, not an `{ account: ... }` envelope                                                                                              |
| Practical-help offers   | `GET /admin/offers`                                         | `{ data: AdminOfferRowDto[] }`                                                                                                                           |
| Offer status            | `PATCH /admin/offers/{reference}`                           | `{ status }`; the returned `AdminOfferRowDto` replaces the displayed row                                                                                 |
| Volunteers              | `GET /admin/volunteers?outreachId=...` or `?kind=Community` | `{ data: AdminVolunteerRowDto[] }`; `roleOrInterests` is an array, `availability` is a string for outreach or an array for community                     |
| Outreach filter options | `GET /outreach`                                             | Live event IDs, titles and dates; no hardcoded `o1`                                                                                                      |
| Messages                | `GET /admin/messages`                                       | `{ data: AdminMessageRowDto[] }`                                                                                                                         |
| Withdrawals queue       | `GET /admin/withdrawals?status=`                            | Live request configured; see current backend dependency below                                                                                            |
| Withdrawal decision     | `POST /admin/withdrawals/{reference}/actions`               | Live request configured with `action` and relevant `note`, `reason`, `message`, `transferReference`; see dependency below                                |

Application flow: opening Received moves it to In review on the server. Verification locks after Approved/Published/Rejected. More info requested must be marked received before approval. Approve requires application status Verified and saved verification Verified. Approve never publishes. Publication fields start empty, both consent/review boxes must be ticked, and publication has its own confirmation dialog.

Mutation success is retained if a follow-up detail refresh fails: the UI keeps the returned status/link and shows the refresh error instead of asking the admin to submit the same successful decision again.

## Account links and demo email

Approve's optional `account` and activation resend's direct response contain `state`, `email`, `phone`, nullable `activationLink`, nullable `expiresAt`, and nullable `emailSent`. The raw link stays in the current detail view's memory only. It is never logged or stored in sessionStorage. GET detail does not return it.

When `emailSent` is true, the panel explains that the activation email went to the **MailHog demo inbox**, not the applicant's real inbox. When false, it offers private sharing. An already-activated account can have no link and `emailSent: null`; the UI handles that explicitly. Copy has a manual-selection fallback if clipboard access is unavailable. WhatsApp uses the response's phone number and opens a composed `wa.me` link; it never sends automatically. A new link invalidates earlier links on the backend.

## Errors and privacy

Backend `{ error: { code, message, fields } }` messages appear in error banners. Field errors map to login, verification, decision, publication and offer-status controls. Publication errors are also retained on the underlying form so the admin can close the dialog and correct them. Errors use a border plus ⚠ text. Phone numbers remain masked in lists and full in private detail dialogs. Filenames are displayed without inventing a secure download URL that the backend does not provide.

Withdrawals remain visibly labelled Demo: status changes must not imply a real bank transfer.

## Current backend dependency

At integration time, this checkout contains `AdminApplicationsController`, `AdminInboxController`, and `AuthController`, but **no `AdminWithdrawalsController` or withdrawal DTOs**. The Withdrawals tab now calls the requested live URLs rather than using mock data. It shows the server error (or an endpoint-unavailable message for an empty 404). Its existing row/action UI is ready for the requested contract, but those response shapes cannot yet be checked against C# and the server must supply these routes. No backend files were changed.

## Validation

Run `npm run build`. No tests were added or run. Nothing was pushed, merged or deployed.
