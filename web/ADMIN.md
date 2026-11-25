# CrowdAid admin — live integration

All admin features now use the real backend through `src/api/admin.js` (`USE_MOCKS = false`) and the single axios instance (`baseURL: /api/v1`). Admin mock files remain in place but are unused. C# controllers and DTOs are the contract; JSON is camelCase.

## Authentication

`POST /admin/auth/login` sends `{ email, password }` and consumes `{ token, expiresIn, admin: { id, name, role } }`. The admin name appears in the top bar. `GET /auth/me` validates/restores the admin session, explicitly choosing the admin token even when a campaigner is also signed in.

The admin JWT is held in memory and sessionStorage under `crowdaid.admin.session`. Only protected admin calls and the admin session-restore request use it. A protected 401 clears only this session and shows “Your session expired. Sign in again.” Passwords are never persisted. Signing out leaves the campaigner session untouched.

## Live API inventory

Paths below are relative to `/api/v1`.

| Feature                 | Endpoint                                                 | Contract                                                                                                              |
| ----------------------- | -------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| Sign in                 | `POST /admin/auth/login`                                 | `AdminLoginResponse`                                                                                                  |
| Session restore         | `GET /auth/me`                                           | `UserSummaryDto`, role `admin`                                                                                        |
| Application list        | `GET /admin/applications?status=`                        | `{ data: AdminApplicationRowDto[] }`                                                                                  |
| Application detail      | `GET /admin/applications/{reference}`                    | Full `AdminApplicationDetailDto`, private contact details, verification, files, log, publication, campaignId, account |
| Verification            | `PATCH /admin/applications/{reference}/verification`     | `{ status, method }`; consumes the returned full detail                                                               |
| Decisions               | `POST /admin/applications/{reference}/actions`           | `action` plus applicable `needed`, `message`, `reason`, `note`; response `{ reference, status, account }`             |
| Publication             | `POST /admin/applications/{reference}/publish`           | Public fields, integer `goal`, `consentRecorded`, both consent/review flags; response `{ campaignId, status }`        |
| Activation resend       | `POST /admin/applications/{reference}/activation`        | Direct `AccountLinkDto`                                                                                               |
| Offers                  | `GET /admin/offers`                                      | `{ data: AdminOfferRowDto[] }`                                                                                        |
| Offer status            | `PATCH /admin/offers/{reference}`                        | `{ status }`; returned row replaces the displayed row                                                                 |
| Volunteers              | `GET /admin/volunteers?outreachId=` or `?kind=Community` | `{ data: AdminVolunteerRowDto[] }`                                                                                    |
| Outreach filter options | `GET /outreach`                                          | Actual event IDs, titles and dates                                                                                    |
| Messages                | `GET /admin/messages`                                    | `{ data: AdminMessageRowDto[] }`                                                                                      |
| Withdrawals             | `GET /admin/withdrawals?status=`                         | `{ data: AdminWithdrawalRowDto[] }`                                                                                   |
| Withdrawal decisions    | `POST /admin/withdrawals/{reference}/actions`            | `WithdrawalActionRequest`; response `{ reference, status }`                                                           |

## Withdrawals — verified against C#

`AdminWithdrawalsController` and the admin withdrawal DTOs are now present and integrated. Each row contains `reference`, `campaignTitle`, `applicantName`, `amount`, `status`, `requestedAt`, `isNew`, and `payout: { bankName, accountNumber, accountName }`. Full payout numbers are visible only to admins.

Decision bodies use `action` with applicable `note`, `reason`, `message`, `transferReference`:

- `approve` from Requested; note optional.
- `reject` from Requested or Approved; private reason and campaigner-visible message required.
- `mark_paid` from Approved; transfer reference optional.

The UI consumes the returned reference/status immediately, then reloads the filtered queue. It does not assume a full row is returned by an action. Server errors remain visible with retry; there is no “endpoint unavailable” fallback. The label is **Demo: no real money moves**. These are persisted server requests/statuses; marking paid makes no bank transfer.

## Review, account links and errors

Opening Received transitions it to In review on the backend. Save verification as Verified before approval. Verification locks after Approved/Published/Rejected. More info requested must be marked received before approval. Approval and publication remain separate; publication fields start empty, require both consent boxes and have their own confirmation dialog.

Approve's optional account and resend's direct account object contain `state`, `email`, `phone`, nullable `activationLink`, nullable `expiresAt`, nullable `emailSent`. Links stay in the current detail view's memory, never logs or sessionStorage. Copy includes manual-selection recovery; WhatsApp opens a composed link without sending automatically. A successful demo email is described as sent to MailHog, not the applicant's real inbox. Already-activated accounts with no new link and null emailSent are handled.

All error banners use the server's `{ error: { message, fields } }`. Field errors map to relevant inputs; non-JSON HTTP failures get plain retry wording rather than axios's generic status-code message. Phone numbers remain masked in lists and full in private detail views. Successful decisions retain their returned status/link even if a follow-up refresh fails.

## Endpoints not supplied by the backend

The reviewed controllers do not provide evidence upload or secure evidence download routes. The admin displays filenames without inventing document links. There are no campaign-update or outreach-creation admin endpoints/UI. All existing admin screens are live.

## Verification

Use `npm run build`. No tests were created or run. No backend, design, root, Caddyfile or Dockerfile edits; no push, merge or deployment.
