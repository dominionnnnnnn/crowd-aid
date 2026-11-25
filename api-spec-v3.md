# CrowdAid MVP: API spec for the frontend (v3, 25 Nov 2026)

Matched to **CrowdAid Prototype v2** (Design System artifact) and Handoff v2. Replaces api-spec-v1, which was based on the wireframes only. This is a design contract only. Write all code fresh on hackathon day in the new repo.

**v3 = the backend dev's v2 plus the changes listed in the next section** (accounts for approved applicants, campaigner dashboard, withdrawals, required email). Where v3 changes or adds something it is labelled **CHANGED** or **NEW**.

**Labels:** CONFIRMED = in the approved designs. PROPOSED = my suggestion, change freely. OPEN = founder decision still pending.

---

## Changes in v3 (read this first)

1. **Email is now required on the application form** (`email` on POST /applications). It is the applicant's login and where the activation link is sent.
2. **Approve now creates the account.** Approving an application creates a `campaigner` user and a **one-time activation link** (single use, expires in 72 h). The system emails the link, and the admin also gets it in the Approve response (copy / WhatsApp fallback if email is off or fails).
3. **First login = set password.** New public endpoints check the link and set the password (§5b). No temporary password is ever emailed.
4. **Two roles in the JWT:** `admin` and `campaigner` (v2 had admin only). A campaigner can only read their own campaign. The campaign is always found from the token, never from the URL or body.
5. **Campaigner dashboard endpoints** (§5c): campaign status and funding, donations received, practical (non-money) help offered, payout details, withdrawals.
6. **Admin: withdrawals queue and activation resend** (§5d).
7. Optional **application status check** by reference + email (#34), because applicants have no account until they are approved.
8. Small corrections to v2 are listed in §10.

---

## 0. Where the prototype differs from wireframe v0.4.1 (decide before kickoff)

1. **Processing fee.** v0.4.1 showed a demo ₦75 processing fee (total ₦5,075). Prototype v2 shows ₦0 platform fee and **no processing fee** (total = donation). This spec follows v2: `total = amount`.
2. **Payment method** (Card / Bank transfer / USSD) is now a required choice. In demo mode it is recorded only. No card details are collected.
3. **Minimum donation** is ₦500 in v2 (it was open).
4. **New in v2:** Contact CrowdAid dialog with an admin Messages tab, a "volunteer community" signup next to outreach registration, admin statuses for offers, and a separate verification status on applications.
5. **Priority:** Handoff v2 marks Volunteer and Admin as P1, but the wireframes had all 8 pages as P0. This spec keeps the admin approve → publish flow as P0 because it proves the review-before-publish story to judges.

---

## 1. Conventions (PROPOSED)

- Base URL `/api/v1`, JSON in and out.
- Money is **whole naira integers** (`5000` = ₦5,000). Frontend formats with `₦` + en-NG grouping. CONFIRMED in handoff.
- Dates are ISO 8601. Frontend formats them ("20 Nov 2026").
- References (CONFIRMED prefixes from the prototype): `CA-REQ-0001` applications, `CA-DON-0001` donations, `CA-OFF-0001` practical help offers, `CA-VOL-0001` volunteers, `CA-MSG-0001` contact messages, `CA-WDR-0001` withdrawals (NEW).
- Phone validation (CONFIRMED): Nigerian mobile, `0` or `234`/`+234` followed by 10 digits. Store normalised as `+234XXXXXXXXXX`.
- Public endpoints need no login. Admin and campaigner endpoints need `Authorization: Bearer <JWT>` (see §5a). **CHANGED:** each route group accepts only its own role (`/admin/*` = admin, `/me/*` = campaigner).
- Error shape, which drives the prototype's error summary and field errors:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Please fix the highlighted fields.",
    "fields": { "phone": "Enter a Nigerian number, e.g. 0803 123 4567." }
  }
}
```

Status codes: `400` validation, `401` not signed in or token invalid/expired, `403` signed in but not allowed, `404` not found or not published, `409` action not allowed in the current status, `410` activation link expired or already used (NEW), `500` server error (frontend shows the failure banner and keeps answers).

---

## 2. Endpoint overview

| # | Method | Path | Screen | Login (JWT) | Priority |
|---|---|---|---|---|---|
| 1 | GET | `/campaigns` | Home (featured), Listing | No (public) | P0 |
| 2 | GET | `/campaigns/:id` | Campaign details | No (public) | P0 |
| 3 | POST | `/applications` | Get help application | No (public) | P0 |
| 4 | POST | `/applications/:ref/documents` | Upload box | No (public) | P1 (optional) |
| 5 | POST | `/donations` | Donation form | No (public) | P0 |
| 6 | GET | `/donations/:ref` | Result panel / recheck pending | No (public) | P0 |
| 7 | POST | `/practical-help-offers` | Practical help (campaign or general) | No (public) | P0 |
| 8 | GET | `/outreach` | Volunteer › Outreach | No (public) | P0 |
| 9 | POST | `/volunteers/outreach` | Volunteer › Register for outreach | No (public) | P0 |
| 10 | POST | `/volunteers/community` | Volunteer › Join community | No (public) | P1 |
| 11 | POST | `/contact-messages` | Contact CrowdAid dialog | No (public) | P1 |
| 12 | POST | `/admin/auth/login` | Admin sign-in | No (returns the token) | P0 |
| 13 | GET | `/admin/applications` | Applications tab | **Yes** (admin) | P0 |
| 14 | GET | `/admin/applications/:ref` | Application detail | **Yes** (admin) | P0 |
| 15 | PATCH | `/admin/applications/:ref/verification` | Verification status + method | **Yes** (admin) | P0 |
| 16 | POST | `/admin/applications/:ref/actions` | Request info / Approve / Reject / Info received | **Yes** (admin) | P0 |
| 17 | POST | `/admin/applications/:ref/publish` | Publish campaign | **Yes** (admin) | P0 |
| 18 | GET | `/admin/offers` | Practical help offers tab | **Yes** (admin) | P0 |
| 19 | PATCH | `/admin/offers/:ref` | Offer status | **Yes** (admin) | P1 |
| 20 | GET | `/admin/volunteers` | Volunteers tab | **Yes** (admin) | P0 |
| 21 | GET | `/admin/messages` | Messages tab | **Yes** (admin) | P1 |
| 22 | POST | `/auth/login` | Campaigner sign-in (NEW) | No (returns the token) | P0 |
| 23 | GET | `/auth/activation/:token` | Activation page: check link (NEW) | No (public) | P0 |
| 24 | POST | `/auth/activate` | Activation page: set password (NEW) | No (returns the token) | P0 |
| 25 | GET | `/auth/me` | Restore session (NEW) | **Yes** (any role) | P0 |
| 26 | GET | `/me/campaign` | Dashboard overview (NEW) | **Yes** (campaigner) | P0 |
| 27 | GET | `/me/campaign/donations` | Dashboard: donations (NEW) | **Yes** (campaigner) | P0 |
| 28 | GET | `/me/campaign/offers` | Dashboard: practical help offered (NEW) | **Yes** (campaigner) | P1 |
| 29 | GET, PUT | `/me/payout` | Withdraw: payout details (NEW) | **Yes** (campaigner) | P1 |
| 30 | GET, POST | `/me/withdrawals` | Withdraw: history and request (NEW) | **Yes** (campaigner) | P1 |
| 31 | GET | `/admin/withdrawals` | Admin: withdrawals tab (NEW) | **Yes** (admin) | P1 |
| 32 | POST | `/admin/withdrawals/:ref/actions` | Approve / reject / mark paid (NEW) | **Yes** (admin) | P1 |
| 33 | POST | `/admin/applications/:ref/activation` | Resend or reset activation link (NEW) | **Yes** (admin) | P1 |
| 34 | GET | `/applications/status` | Track my application (NEW) | No (public) | P1 |

**Summary (CHANGED):** the public endpoints (1–11, 23, 24, 34) and both sign-ins (12, 22) need no token. Campaigner endpoints (26–30) need a `campaigner` token. Admin endpoints (13–21, 31–33) need an `admin` token. #25 accepts either.

Two public endpoints need care without a login:
- **#4 upload (P1):** `POST /applications` should also return a one-time `uploadToken`, and #4 accepts files only with that token, so nobody can attach files to someone else's reference.
- **#6 donation status:** return only reference, amount, status and campaign title. Never the donor's email or name.

Campaign updates and outreach events are **seeded** (no admin screens to create them).

---

## 3. Shared enums (CONFIRMED from prototype)

```
APPLICANT_RELATIONSHIP = Parent | Child | Spouse | Sibling | Aunt or uncle | Grandparent | Friend | Neighbour | Community or religious leader | Other
OGUN_LGAS = Abeokuta North | Abeokuta South | Ado-Odo/Ota | Ewekoro | Ifo | Ijebu East | Ijebu North | Ijebu North East | Ijebu Ode | Ikenne | Imeko Afon | Ipokia | Obafemi Owode | Odeda | Odogbolu | Ogun Waterside | Remo North | Sagamu | Yewa North | Yewa South
SUPPORT_TYPES (application) = Financial support | Transportation | Medical equipment | Food | Blood donor coordination | Professional support
HELP_CATEGORIES (offers) = Transportation | Blood donor coordination | Medical equipment | Food support | Professional assistance | Other support
CAMPAIGN_NEEDS = Transport to hospital | Blood donors | Meals for family | Professional support | Medical equipment
AVAILABILITY = Weekdays | Weekends | Flexible
ABEOKUTA_AREAS = Adatan | Asero | Camp | Ibara | Isale-Igbein | Kuto | Lafenwa | Obantoko | Oke-Ilewo | Onikolobo | Panseke | Somewhere else
PAYMENT_METHOD = card | transfer | ussd
DONATION_PRESETS = 1000 | 2000 | 5000 | 10000 | 20000
OUTREACH_ROLES = Collecting donations | Sharing campaign stories | Setup & logistics | Wherever needed
OUTREACH_STAY = all | part          ("Yes, all of it" / "Part of it")
COMMUNITY_INTERESTS = Collecting donations at outreaches | Sharing campaign stories | Transport and logistics | Healthcare or professional skills | Admin and coordination
APPLICATION_STATUS = Received | In review | More info requested | Verified | Approved | Published | Rejected
VERIFICATION_STATUS = Not started | In progress | Verified | Could not verify
OFFER_STATUS = New | Contacted | Not needed
WITHDRAWAL_STATUS = Requested | Approved | Paid | Rejected      (NEW)
USER_ROLE = admin | campaigner                                    (NEW)
ACCOUNT_STATE = Not created | Link sent | Activated               (NEW, shown to admins)
```

PROPOSED: serve these from the frontend as constants. No endpoint needed.

Note: the campaign need labels and the offer categories don't match word for word. The prototype maps them (for example "Meals for family" → "Food support") when a need item opens the help form. Keep that mapping in one frontend helper.

---

## 4. Public endpoints

### 1. GET `/campaigns`
Query: `q` (name, condition or town), `location` (town), `need` (one of CAMPAIGN_NEEDS), `featured=true`.
Returns **published campaigns only**.

```json
{
  "data": [{
    "id": "usman",
    "title": "Help Usman get kidney surgery",
    "condition": "Kidney surgery",
    "town": "Abeokuta",
    "location": "Abeokuta, Ogun State",
    "imageUrl": null,
    "preview": "Usman is a 4-year-old boy in Abeokuta who needs kidney surgery. His family is asking the community for help with the cost.",
    "raised": 3000000,
    "goal": 5000000,
    "supporters": 12,
    "needs": ["Transport to hospital", "Blood donors", "Meals for family"],
    "reviewed": true
  }],
  "locations": ["Abeokuta", "Sagamu"]
}
```
- `locations` = distinct towns of published campaigns, for the Location filter.
- Frontend computes `% funded` and `remaining` from `raised` and `goal`.
- `imageUrl` can be `null`. The prototype uses a tinted tile, and the frontend can pick the tint.
- No paging needed for the demo (a handful of campaigns).

### 2. GET `/campaigns/:id`

```json
{
  "id": "usman",
  "title": "Help Usman get kidney surgery",
  "condition": "Kidney surgery",
  "town": "Abeokuta",
  "location": "Abeokuta, Ogun State",
  "reviewed": true,
  "raised": 3000000,
  "goal": 5000000,
  "supporters": 12,
  "story": ["Paragraph 1", "Paragraph 2", "Paragraph 3"],
  "needs": ["Transport to hospital", "Blood donors", "Meals for family"],
  "funds": [
    { "label": "Surgery", "amount": 3600000 },
    { "label": "Hospital stay", "amount": 900000 },
    { "label": "Medication and follow-up tests", "amount": 500000 }
  ],
  "updates": [
    { "date": "2026-11-20", "title": "Surgery date being confirmed", "text": "..." }
  ],
  "organizer": { "name": "Aminat Yusuf", "role": "Applicant · Usman's aunt" }
}
```
- Not found or not published → `404` → "This campaign isn't available".
- `story` is an array of paragraphs (matches the prototype).
- `funds` drives "How the funds will be used".
- **Never** return applicant phone, patient contact details, documents or verification notes. CONFIRMED.
- Share uses the frontend URL. No endpoint needed.

### 3. POST `/applications`
Mirrors the 3 prototype sections.

```json
{
  "who": "myself | someone_else",
  "name": "Applicant full name",
  "phone": "0803 123 4567",
  "email": "you@example.com",
  "relationship": "Parent",
  "patientName": "Patient full name",
  "patientAge": 4,
  "town": "Abeokuta",
  "lga": "Abeokuta South",
  "condition": "Medical condition or healthcare problem",
  "treatment": "Treatment or assistance required",
  "facility": "optional",
  "estimatedCost": 5000000,
  "amountNeeded": 5000000,
  "supportTypes": ["Financial support", "Transportation"],
  "privateReviewConsent": true,
  "accuracyConfirmed": true
}
```
Validation (CONFIRMED from prototype):
- Required: `who`, `name`, `phone`, `email` (valid format, **NEW in v3**), `patientAge` (whole number 0–120), `town`, `lga`, `condition`, `treatment`, at least 1 `supportTypes`, both consents `true`.
- When `who = someone_else`: `relationship` and `patientName` are also required. When `myself`: patient name = applicant name, and relationship is stored as "Myself".
- `facility`, `estimatedCost`, `amountNeeded` are optional.

Response `201`:
```json
{ "reference": "CA-REQ-0007", "status": "Received" }
```
- Always `Received`. The screen says "Request received", never approved. CONFIRMED.
- Publication consent is **not** collected here; it is asked only after approval. CONFIRMED.
- **NEW:** `email` is the **applicant's own** email (also when `who = someone_else`; it is not the patient's). Store it lower-case. It is used for sign-in and for the activation link.
- **NEW:** one account per email. If a user with this email already exists, Approve reuses it instead of creating a second account (PROPOSED).
- **CHANGED (frontend copy):** the design says "No account needed". The success panel should now say the applicant will get an email once the application is approved, with a link to set up their account. Applicants still need no account to apply.

### 4. POST `/applications/:ref/documents` (P1, optional)
`multipart/form-data`, `files[]`. JPG/PNG/PDF, max 5 (drawn). Size limit OPEN.
Response: `{ "files": ["treatment_estimate.pdf"] }`. If not built, hide the upload box and say documents may be requested privately.

### 5. POST `/donations`
Demo mode: the prototype has a **demo outcome selector** (Success / Pending / Failed). Send the chosen outcome with the donation, so one call does it all.

```json
{
  "campaignId": "usman",
  "amount": 5000,
  "name": "optional",
  "anonymous": false,
  "email": "you@example.com",
  "method": "card | transfer | ussd",
  "demoOutcome": "success | pending | failed"
}
```
Validation (CONFIRMED): `amount` is a whole number ≥ 500, `email` is required and valid, `method` is required. Campaign must be published.

Response `201`:
```json
{
  "reference": "CA-DON-0012",
  "campaignId": "usman",
  "campaignTitle": "Help Usman get kidney surgery",
  "amount": 5000,
  "platformFee": 0,
  "total": 5000,
  "method": "card",
  "status": "success | pending | failed",
  "demo": true,
  "campaign": { "raised": 3005000, "goal": 5000000, "supporters": 13 }
}
```
- `success`: add `amount` to `raised` and +1 to `supporters`. Return the new totals so the success screen and campaign page show the update (the prototype's "raised total updates" behaviour).
- `pending` / `failed`: totals unchanged. Failed → frontend offers retry with answers kept.
- Frontend shows the "Processing…" state for ~1–2 seconds before showing the result. No server delay needed.
- `demoOutcome` is accepted only while `DEMO_PAYMENTS=true`. With a real provider (P1), drop it and use the provider's hosted checkout + webhook to set the status.
- Anonymous donors: never show the name publicly.

### 6. GET `/donations/:ref`
Returns the same shape as #5 (without `campaign` totals). Used for "Waiting for confirmation" rechecks. In demo mode a pending donation stays pending.

### 7. POST `/practical-help-offers`
`campaignId` present = campaign mode. Absent or `null` = general mode.

```json
{
  "campaignId": "usman",
  "types": ["Transportation"],
  "details": "optional, e.g. I can drive in the mornings.",
  "availability": ["Weekdays"],
  "area": "Kuto",
  "name": "Tunde Bakare",
  "phone": "0803 123 4567",
  "contactConsent": true
}
```
Validation (CONFIRMED): at least 1 `types` (HELP_CATEGORIES), at least 1 `availability`, `area` from ABEOKUTA_AREAS, `name`, valid `phone`, `contactConsent = true`.

Response `201`: `{ "reference": "CA-OFF-0003", "mode": "campaign", "campaignTitle": "Help Usman get kidney surgery" }`.
Frontend picks the confirmation copy by `mode`. Offers are never public. No matching. CONFIRMED.

### 8. GET `/outreach`
```json
{
  "data": [{
    "id": "o1",
    "title": "Community fundraising outreach",
    "date": "2026-11-28",
    "time": "9:00am – 1:00pm",
    "venue": "Meeting point shared after you register",
    "supportingCampaign": { "id": "usman", "title": "Help Usman get kidney surgery" }
  }]
}
```
Empty `data` → "No upcoming outreaches right now".

### 9. POST `/volunteers/outreach`
```json
{
  "outreachId": "o1",
  "name": "Kemi Adebayo",
  "phone": "0803 123 4567",
  "area": "Oke-Ilewo",
  "roles": ["Collecting donations"],
  "stay": "all | part",
  "guidelinesConsent": true
}
```
Required (CONFIRMED): `outreachId`, `name`, `phone`, `area`, `stay`, `guidelinesConsent`. `roles` is optional.
Response `201`: `{ "reference": "CA-VOL-0003", "outreach": { "title": "...", "date": "...", "time": "..." } }`.
Minimum volunteer age is still OPEN; the prototype has no age field.

### 10. POST `/volunteers/community` (P1)
```json
{
  "name": "Seun Afolabi",
  "phone": "0907 123 4567",
  "email": "optional",
  "area": "Obantoko",
  "interests": ["Transport and logistics"],
  "availability": ["Weekends"],
  "contactConsent": true
}
```
Required (CONFIRMED): `name`, `phone`, `area`, at least 1 `interests`, at least 1 `availability`, `contactConsent`.
Response `201`: `{ "reference": "CA-VOL-0004" }`.
PROPOSED: store both volunteer kinds in one table with `kind = Outreach | Community`. The admin tab already shows them together.

### 11. POST `/contact-messages` (P1)
```json
{ "campaignId": "usman or null", "name": "...", "phone": "...", "message": "..." }
```
All required except `campaignId`. Response `201`: `{ "reference": "CA-MSG-0001" }`.

### 34. GET `/applications/status` (P1, NEW)
Applicants have no account until they are approved, so they check progress with the reference from the success panel plus the email they applied with.
Query: `reference`, `email`.
```json
{ "reference": "CA-REQ-0007", "status": "In review", "message": "We are reviewing your application." }
```
- Return only the status and one fixed message per status. Never notes, verification details, reasons or documents. `Rejected` gets a neutral message; the admin gives the reason by phone or email.
- If the reference and email don't match, return `404` and don't say which one was wrong.
- Rate-limit this endpoint (PROPOSED); references are sequential and guessable.

---

## 5. Admin and campaigner endpoints (Bearer token)

### 12. POST `/admin/auth/login`
Request:
```json
{ "email": "admin@crowdaid.demo", "password": "••••••••" }
```
Response `200`:
```json
{
  "token": "<JWT access token>",
  "expiresIn": 28800,
  "admin": { "id": "uuid", "name": "CrowdAid Admin", "role": "admin" }
}
```
Errors: `400` missing fields; `401 INVALID_CREDENTIALS` with one generic message ("Email or password is incorrect"). Never say which of the two was wrong.

See **§5a Authentication (JWT)** for the full rules.

### 5a. Authentication (JWT) (PROPOSED)

**Scope (CHANGED).** Two kinds of users sign in: `admin` (pre-created) and `campaigner` (created when an application is approved, activated through the one-time link). Every public endpoint (campaigns, applications, donations, offers, volunteers, contact, activation) stays open, because guest browsing and giving is CONFIRMED. All `/admin/*` routes except `/admin/auth/login` need an `admin` token. All `/me/*` routes need a `campaigner` token. `/auth/me` accepts either.

**Token.**
- Type: JWT signed with **HS256** using a server secret from an environment variable (`JWT_SECRET`, at least 32 random characters). Never commit it to the repo; set it in the server's `.env` (passed through `docker-compose.prod.yml`).
- Lifetime: **8 hours** (`expiresIn: 28800`), long enough for the hackathon day and judging. No refresh token in the MVP (P2).
- Claims:

```json
{
  "sub": "<admin id>",
  "name": "CrowdAid Admin",
  "role": "admin | campaigner",
  "iss": "crowdaid-api",
  "aud": "crowdaid-api",
  "iat": 1764064800,
  "exp": 1764093600
}
```
- **CHANGED:** one audience (`crowdaid-api`) for both roles; the `role` claim decides access. A campaigner token carries the user id only, never a campaign id.
- Keep the payload to identity only. No phone numbers, emails or application data in the token, since anyone holding it can decode it.

**Passwords.**
- Store admins with a **bcrypt** hash (cost 10–12), never plain text.
- **NEW:** campaigners use the same rules, with a **minimum of 8 characters** (PROPOSED). A campaigner has **no password until they activate**, so they can't sign in before that.
- Seed one admin account. Put the demo password in the team's private notes, not in the repo or the pitch slides.

**Middleware (every protected route).**
1. Read `Authorization: Bearer <token>`. Missing or malformed → `401 UNAUTHENTICATED`.
2. Verify signature, `exp`, `iss` and `aud`. Any failure → `401 UNAUTHENTICATED` (expired tokens can use `TOKEN_EXPIRED` so the UI can say "Your session expired").
3. Check the role the route group needs (`admin` for `/admin/*`, `campaigner` for `/me/*`). Otherwise → `403 FORBIDDEN`.
4. Attach `{ id, name }` to the request. The application log uses this for "by" (e.g. "Approved · CrowdAid Admin · 10:42").
5. **(NEW)** For `/me/*`, load the campaigner's own application and campaign from the user id in the token. **Never** take a campaign id or reference from the URL or body on these routes, so one campaigner can't read another's data.

Most stacks have this built in: ASP.NET Core `JwtBearer` + `[Authorize(Roles = "admin")]`, or `jsonwebtoken` + a small Express middleware in Node. Pick whichever your backend dev writes fastest.

**Frontend handling.**
- On login, keep the token **in memory** (app state), and optionally in `sessionStorage` so a page refresh during the demo doesn't log the admin out. Avoid `localStorage` (it survives closing the browser).
- Send `Authorization: Bearer <token>` on every `/admin/*` and `/me/*` call from one shared API helper.
- On any `401`: clear the token and show the matching sign-in again (the admin gate on `/admin`, `/login` for campaigners), with "Your session expired. Sign in again." if the code is `TOKEN_EXPIRED`.
- "Sign out" just clears the token client-side. No server endpoint needed for the MVP.

**Other basics.**
- CORS (CHANGED): in production the site and the API share one origin (Caddy proxies `/api/*`), so no CORS is needed. Allow `localhost` only in development.
- HTTPS (CHANGED) is handled by Caddy on the VPS (HACKATHON-PLAN section 6D), so tokens and passwords aren't sent in the clear. Test it before the demo.
- PROPOSED, P1: limit login attempts (for example 5 per minute per IP). Apply it to `/admin/auth/login`, `/auth/login` and `/auth/activate`.

**Out of scope for the MVP:** refresh tokens, self sign-up (accounts only come from Approve), self-service password reset (the admin sends a new link, #33), extra admin roles (approver vs publisher is still an OPEN founder decision; if it's decided, add `role: "reviewer" | "publisher"` to the claims and check it in step 3).

### 13. GET `/admin/applications`
Query: `status` (one of APPLICATION_STATUS, or omit for all).
```json
{
  "data": [{
    "reference": "CA-REQ-0004",
    "applicant": "Daniel Okafor",
    "patient": "Grace Okafor",
    "relationship": "Son",
    "town": "Abeokuta",
    "treatment": "Physiotherapy sessions",
    "amountNeeded": 600000,
    "status": "In review",
    "hasEvidence": true,
    "receivedAt": "2026-11-21T10:00:00Z",
    "isNew": false
  }]
}
```
`isNew` = submitted since the admin last opened the tab (PROPOSED; or simply received today).

### 14. GET `/admin/applications/:ref`
Full application (all fields from #3, including the **full phone number and email** for admins), plus (`account` is NEW):
```json
{
  "status": "In review",
  "verification": { "status": "In progress", "method": "Called the clinic" },
  "files": ["estimate.pdf"],
  "log": [{ "action": "Opened", "by": "Admin", "at": "...", "note": null }],
  "publication": null,
  "campaignId": null,
  "account": { "state": "Not created", "activationExpiresAt": null, "activatedAt": null }
}
```
PROPOSED (matches the prototype's "Opened" log line): opening a `Received` application moves it to `In review` and adds a log entry.

### 15. PATCH `/admin/applications/:ref/verification`
```json
{ "status": "Not started | In progress | Verified | Could not verify", "method": "How was it checked? (private)" }
```
Locked (`409`) once the application is Approved, Published or Rejected. CONFIRMED.
If `status = Verified`, also set application status to `Verified`. PROPOSED.

### 16. POST `/admin/applications/:ref/actions`
One endpoint for the four confirmation dialogs.

| `action` | Allowed from | Required fields | Result status |
|---|---|---|---|
| `request_info` | In review, Verified | `needed`, `message` | More info requested |
| `info_received` | More info requested | `note` (optional) | In review |
| `approve` | Verified only (CONFIRMED: prototype locks Approve until verification is Verified) | `note` (optional) | Approved **+ account and activation link (NEW, see below)** |
| `reject` | In review, Verified, More info requested | `reason` (private), `message` | Rejected |

```json
{ "action": "reject", "reason": "Not a healthcare need", "message": "Text the admin will send by phone/WhatsApp" }
```
- **CHANGED:** for `request_info` and `reject` the app still does **not** send the message. The admin contacts the applicant by phone, WhatsApp or email, and the message is only logged. CONFIRMED (dialog copy). The only message the system sends itself is the **activation email** on approve (below).
- Each action adds a log entry with admin name and time.
- Not allowed from the current status → `409`.
- Approving does **not** publish. CONFIRMED.

**What `approve` does now (NEW, all in one transaction):**
1. Status becomes `Approved` and a log entry is written (same as v2).
2. Look for a user with `application.email` (case-insensitive). If none, create a `campaigner` user with **no password yet**. If one exists, reuse it and do **not** create a second account.
3. Link the application to that user (`application.userId`). The campaign created later by Publish is owned by that user.
4. If the user has no password yet, create an **activation token**: 32 random bytes, URL-safe. Store only its **SHA-256 hash**. It expires in 72 hours and works once.
5. If `EMAIL_ENABLED=true`, email the link to `application.email`. If sending fails, the approval still succeeds and `emailSent` is `false`.

Response `200`:
```json
{
  "reference": "CA-REQ-0007",
  "status": "Approved",
  "account": {
    "state": "Link sent",
    "email": "aminat@example.com",
    "phone": "+2348031234567",
    "activationLink": "https://<site>/activate?token=...",
    "expiresAt": "2026-11-28T10:42:00Z",
    "emailSent": true
  }
}
```
- `activationLink` is returned **only** to the admin (here and in #33), so the admin can copy it or send it by WhatsApp when email is off or failed. Never log it, never store it in plain text, never return it again.
- If the user already has a password (for example a second application with the same email), `account.state` is `Activated` and no link is created.
- The admin screen needs: Copy button, "Send via WhatsApp" button (opens `wa.me/<phone>` with the link as text), and a line saying whether the email was sent.

### 17. POST `/admin/applications/:ref/publish`
Only from `Approved`.
```json
{
  "title": "Help Usman get kidney surgery",
  "location": "Abeokuta, Ogun State",
  "goal": 5000000,
  "organizer": "Aminat Yusuf · Usman's aunt",
  "story": "Only details the family consented to share",
  "consentRecorded": "23 Nov 2026 · by phone",
  "publicationConsent": true,
  "contentReviewed": true
}
```
- Required (CONFIRMED): `title`, `location`, `goal`, `story`, `consentRecorded`, and both checkboxes `true`.
- Creates the campaign (`raised: 0`, `supporters: 0`, `published: true`), copies **only** these public fields, sets status `Published`, logs it.
- PROPOSED: split `story` into paragraphs on blank lines. Default `funds` to one line ("Treatment and care" = goal) and `needs` from the application's support types (excluding Financial support), as the prototype does.
- Response: `{ "campaignId": "lateef", "status": "Published" }`.

### 18. GET `/admin/offers`
Query: `status`, `campaignId`.
Rows: `reference, name, phone, types, campaignTitle ("General (no campaign)" when none), availability, area, details, status, createdAt, isNew`.

### 19. PATCH `/admin/offers/:ref` (P1)
`{ "status": "New | Contacted | Not needed" }`.

### 20. GET `/admin/volunteers`
Query: `outreachId` (or `kind=Community`).
Rows: `reference, kind, name, phone, outreachLabel ("Community fundraising outreach · 28 Nov" or "Volunteer community (ongoing)"), roleOrInterests, availability (stay answer or days), area, createdAt, isNew`. Read-only. CONFIRMED.

### 21. GET `/admin/messages` (P1)
Rows: `reference, name, phone, about (campaign title or "General enquiry"), message, createdAt`.

**Phone masking:** the prototype shows phones as `+234 803 [•••]` in admin lists. PROPOSED: return the full number and mask in the UI, revealing it in the detail view.

### 5b. Campaigner sign-in and activation (NEW)

#### 22. POST `/auth/login`
```json
{ "email": "aminat@example.com", "password": "••••••••" }
```
Response `200`:
```json
{
  "token": "<JWT access token>",
  "expiresIn": 28800,
  "user": { "id": "uuid", "name": "Aminat Yusuf", "email": "aminat@example.com", "role": "campaigner" }
}
```
- Errors: `400` missing fields; `401 INVALID_CREDENTIALS` with the same generic message as #12. A user who hasn't activated yet gets the same `401`, so the response never reveals that the account exists.
- Admins keep using #12. A campaigner token can't call `/admin/*`.

#### 23. GET `/auth/activation/:token`
Called when the link is opened, before the set-password form is shown. It does **not** use up the token.
```json
{ "valid": true, "name": "Aminat Yusuf", "emailMasked": "a•••@example.com", "applicationRef": "CA-REQ-0007", "patientName": "Usman" }
```
- `404` unknown token, `410 TOKEN_EXPIRED`, `410 TOKEN_USED`. Frontend copy: "This link has expired. Ask CrowdAid to send a new one." / "This link was already used. Sign in instead."

#### 24. POST `/auth/activate`
```json
{ "token": "<from the link>", "password": "at least 8 characters" }
```
- In **one transaction**: check the token (exists, not expired, not used), save the bcrypt hash, set the token's `usedAt` and the user's `activatedAt`. Two quick clicks must not both succeed.
- Response `200`: same body as #22. The user is signed in straight away and the frontend goes to the dashboard.
- Errors: `400 VALIDATION_ERROR` (`fields.password`), `410 TOKEN_EXPIRED`, `410 TOKEN_USED`.
- "Confirm password" is checked in the frontend only.

#### 25. GET `/auth/me`
Any valid token. Returns `{ "id", "name", "email", "role" }`. Used to restore the session after a page refresh.

### 5c. Campaigner dashboard (NEW, `role = campaigner`)
Every `/me/*` route uses the campaign owned by the signed-in user (middleware step 5). There are no ids in these URLs.

#### 26. GET `/me/campaign` (P0)
```json
{
  "application": { "reference": "CA-REQ-0007", "status": "Approved", "patientName": "Usman" },
  "campaign": {
    "id": "usman",
    "title": "Help Usman get kidney surgery",
    "published": true,
    "raised": 3000000,
    "goal": 5000000,
    "supporters": 12,
    "withdrawn": 500000,
    "pendingWithdrawals": 300000,
    "available": 2200000,
    "needs": ["Transport to hospital", "Blood donors", "Meals for family"],
    "updates": [{ "date": "2026-11-20", "title": "Surgery date being confirmed", "text": "..." }]
  },
  "payoutSet": true
}
```
- Before Publish, `campaign` is `null`. The dashboard then shows the status steps (Received → In review → Verified → Approved → Published) and a message that CrowdAid will ask for consent before the campaign goes public. The steps are a frontend constant.
- `raised` and `supporters` count **confirmed donations only** (`status = success`).
- `available = raised − withdrawn (Paid) − pendingWithdrawals (Requested + Approved)`. Calculate it on the server every time; don't store it.

#### 27. GET `/me/campaign/donations` (P0)
Query: `page`, `pageSize` (default 20).
```json
{ "data": [{ "reference": "CA-DON-0012", "displayName": "Tunde B.", "amount": 5000, "createdAt": "2026-11-25T09:10:00Z" }], "total": 12 }
```
- Confirmed (`success`) donations only. Never return email, payment method or phone. Anonymous donors always show as `"Anonymous supporter"`, the same rule as the public pages.

#### 28. GET `/me/campaign/offers` (P1)
The practical (non-money) help people offered for **this** campaign. Query: `status`.
```json
{ "data": [{ "reference": "CA-OFF-0003", "types": ["Transportation"], "details": "I can drive in the mornings.", "availability": ["Weekdays"], "area": "Kuto", "status": "New", "createdAt": "2026-11-24T14:00:00Z" }] }
```
- Read only. General offers (no campaign) are not included.
- **Name and phone are not returned.** The design keeps supporter contact details admin-only and says CrowdAid does not match supporters to patients. CrowdAid contacts the supporter and passes the help on. OPEN (O-1): add a per-offer `contactShared` flag once the supporter agrees.
- `status` uses OFFER_STATUS and is visible to the campaigner but changed only by admins (#19).

#### 29. GET and PUT `/me/payout` (P1)
```json
{ "bankName": "Zenith Bank", "accountNumber": "0123456789", "accountName": "Aminat Yusuf" }
```
- All three are required. `accountNumber` is exactly 10 digits.
- GET returns the number masked (`"••••••6789"`). Admins see the full number on the withdrawal row (#31).
- This is sensitive data. Never log it. In the demo use fictional values.

#### 30. GET and POST `/me/withdrawals` (P1)
`GET` returns history plus the balance:
```json
{ "data": [{ "reference": "CA-WDR-0001", "amount": 500000, "status": "Paid", "requestedAt": "2026-11-24T10:00:00Z", "decidedAt": "2026-11-24T12:00:00Z", "note": null }], "available": 2200000 }
```
`POST` body `{ "amount": 300000 }`. Response `201`:
```json
{ "reference": "CA-WDR-0002", "amount": 300000, "status": "Requested", "available": 1900000, "demo": true }
```
Rules:
- The campaign must be Published, else `409 CAMPAIGN_NOT_PUBLISHED`.
- Payout details must exist, else `409 PAYOUT_DETAILS_REQUIRED`.
- `amount` is a whole number of naira, at least ₦1,000 (OPEN: minimum) and at most `available`. Invalid → `400 VALIDATION_ERROR`; more than available → `409 INSUFFICIENT_FUNDS`.
- Check the balance and insert the row **in one transaction with a lock**, so two quick clicks can't withdraw the same money twice.
- `demo: true` while payments are simulated. No money moves. `Paid` only means an admin marked it. The screen must say "Demo", the same rule as donations.
- When a withdrawal is `Rejected`, `note` holds the message the admin wrote for the campaigner.

### 5d. Admin: withdrawals and activation (NEW)

#### 31. GET `/admin/withdrawals` (P1)
Query: `status`.
Rows: `reference, campaignTitle, applicantName, amount, status, requestedAt, isNew, payout: { bankName, accountNumber, accountName }` (full account number, admins only).

#### 32. POST `/admin/withdrawals/:ref/actions` (P1)
| `action` | Allowed from | Required fields | Result status |
|---|---|---|---|
| `approve` | Requested | `note` (optional) | Approved |
| `reject` | Requested, Approved | `reason` (private), `message` (shown to the campaigner) | Rejected |
| `mark_paid` | Approved | `transferReference` (optional) | Paid |
- A reject returns the money to `available` automatically (it is always calculated, see #26).
- Every action writes a log entry with the admin's name and time. Not allowed from the current status → `409`.
- In demo mode, `mark_paid` only changes the status. The API makes no transfer.

#### 33. POST `/admin/applications/:ref/activation` (P1)
Creates a **new** activation link and invalidates all earlier ones. Use it when the link expired, the email never arrived, or the campaigner forgot their password (this replaces self-service reset).
- Allowed only when the application is `Approved` or `Published`, else `409`.
- If the user is already activated, the new link lets them set a new password. Their current password keeps working until they use the link.
- Response `200`: the same `account` object as the approve response (with `activationLink`, `expiresAt`, `emailSent`). Writes a log entry ("Activation link re-sent").

---

## 6. Data model (PROPOSED, minimal)

- **Campaign**: id (slug), applicationRef (null for seeds), ownerUserId (NEW, null for seeds without an account), title, condition, town, location, imageUrl, preview, story (JSON array), raised, goal, supporters, needs (JSON), funds (JSON), organizerName, organizerRole, reviewed, published, featured, publishedAt
- **CampaignUpdate**: id, campaignId, date, title, text
- **Application**: ref, who, name, phone, email (NEW, required), userId (NEW, set on approve), relationship, patientName, patientAge, town, lga, condition, treatment, facility, estimatedCost, amountNeeded, supportTypes (JSON), privateReviewConsent, accuracyConfirmed, status, verificationStatus, verificationMethod, publication (JSON), campaignId, receivedAt
- **ApplicationLog**: id, applicationRef, action, note, reason, message, by, at
- **ApplicationFile** (P1): id, applicationRef, fileName, storageKey
- **Donation**: ref, campaignId, amount, platformFee, total, name, anonymous, email, method, status, demo, createdAt
- **Offer**: ref, campaignId (nullable), types (JSON), details, availability (JSON), area, name, phone, status, createdAt
- **Outreach**: id, title, date, time, venue, campaignId
- **Volunteer**: ref, kind, outreachId (nullable), name, phone, email, area, roles/interests (JSON), stay, availability (JSON), createdAt
- **ContactMessage**: ref, campaignId (nullable), name, phone, message, createdAt
- **User** (CHANGED, replaces Admin): id, name, email (unique, lower-case), phone, passwordHash (bcrypt, null until activated), role (`admin` | `campaigner`), activatedAt, lastLoginAt, createdAt
- **ActivationToken** (NEW): id, userId, tokenHash (SHA-256), expiresAt, usedAt, createdBy (admin id), createdAt
- **PayoutDetails** (NEW): userId, bankName, accountNumber, accountName, updatedAt
- **Withdrawal** (NEW): ref, userId, campaignId, amount, status, note, reason, demo, requestedAt, decidedBy, decidedAt, paidAt
- **WithdrawalLog** (NEW): id, withdrawalRef, action, by, at, note

## 7. Seed data (fictional demo data, as in the prototype)
- Campaigns: `usman` (₦3,000,000 of ₦5,000,000, 12 supporters, 2 updates), `ronke` (cataract surgery, Sagamu), `daniel`, `tobi`. Copy text and figures from Prototype v2 so the app matches the design.
- Applications: CA-REQ-0001 to 0006 in mixed statuses (Published, In review with evidence, Received, etc.), matching the prototype.
- Offers CA-OFF-0001/0002, volunteers CA-VOL-0001/0002, outreach `o1` (Sat 28 Nov 2026).
- One admin and one campaigner (CHANGED): Aminat Yusuf (`aminat@crowdaid.demo`, already activated) who owns `usman`, so the dashboard demo works without running the whole approval flow. Add fictional payout details and two withdrawals: one `Paid` ₦500,000 and one `Requested` ₦300,000, so `available` isn't the full raised amount. Every seeded application gets a fictional email, because email is now required. Keep demo passwords in the team's private notes, not in the repo.
- Mark all seeds as demo; never present them as real cases or real money.

## 8. Build order for the day
1. Seed + `GET /campaigns`, `GET /campaigns/:id` → frontend unblocked.
2. `POST /donations` with demoOutcome → the demo's money moment (raised total updates).
3. `POST /applications`.
4. Admin: JWT login + auth middleware → list → detail → verification → actions → publish (new campaign appears in listing — the best live demo beat).
5. **(NEW) Accounts and dashboard:** approve creates the user + activation link → `GET /auth/activation/:token` → `POST /auth/activate` → `POST /auth/login` → `GET /auth/me` → `GET /me/campaign` and `/me/campaign/donations`.
6. Offers + outreach + volunteer registration (simple inserts) + admin read tabs.
7. **(NEW, P1)** `/me/campaign/offers`, payout, withdrawals + the admin withdrawals tab, activation resend, application status check, sending the activation email.
8. P1 if time: community volunteers, contact messages + Messages tab, offer status, uploads, real payment test mode.

**Cut first if behind:** withdrawals → application status check → offers view for campaigners → sending email (the admin copies the link instead) → community volunteers.

Agree these JSON shapes at kickoff; the frontend can run on mock JSON until each endpoint is live.

## 9. Open decisions that touch the API
Processing fee (v0.4.1 ₦75 vs v2 none) · payment provider · upload limits · minimum volunteer age · fund-handling policy text · real contact channels · who may approve vs publish.

**NEW open decisions:** is the activation email sent by the system (and which email service), or does the admin always copy/WhatsApp the link · may campaigners see supporter contact details (O-1) · minimum withdrawal amount · who may approve withdrawals · should the applicant, not only the admin, record publication consent in the dashboard · real payout provider (withdrawals are simulated in the MVP).

## 10. Corrections to v2 (NEW)
1. **JWT audience.** v2 used `aud: crowdaid-admin`. With two roles, use one audience (`crowdaid-api`) and let the `role` claim decide access (§5a).
2. **Hosting wording.** v2 mentions HTTPS and CORS for "Nobus Cloud". We deploy on a VPS with Docker; Caddy serves the site and proxies `/api/*`, so the site and API share one origin and CORS only matters in development.
3. **Admin table → User table.** Admins and campaigners live in one `User` table with a `role`.
4. **Raised and supporters can drift.** Update `raised` and `supporters` in the same transaction that saves a confirmed donation, or calculate them from confirmed donations, so the numbers can never disagree with the donation list.
5. **Spam on public POSTs.** Rate-limit `/applications`, `/practical-help-offers`, `/contact-messages` and `/volunteers/*` (PROPOSED, P1).
6. **Guessable references.** References are sequential (`CA-DON-0012`). `GET /donations/:ref` returns no personal data, so it is fine; the new status check (#34) needs the email as well for the same reason.
