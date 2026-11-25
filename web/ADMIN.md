# CrowdAid admin frontend

Run `npm install`, then `npm run dev`. Open `/admin`. The original API/database and email scaffold is at `/__check`.

## Demo

`src/api/admin.js` exports the only `USE_MOCKS` switch (currently `true`). In demo mode, sign in with a fictional valid email and any non-empty password; these are not real credentials. The demo session lasts eight hours. Only the session token is kept in sessionStorage. Records and mutations are in memory and reset on a full page reload. All records are fictional.

Applications and other queues use `src/mocks/` records adapted from the API seed section and Prototype v2. Opening a Received application moves it to In review. Save verification as Verified before approving. Approval creates a simulated activation link and reports that email was not sent. Publication fields start empty; both consent confirmations and a separate confirmation dialog are required. Publication changes the mock application only; this admin task does not implement public campaign pages. Withdrawals change demo statuses and never transfer money.

Activation links remain in the current detail view's memory only, and disappear when leaving it or reloading. They are not returned by application detail. Send a new link to obtain another one; earlier links are invalidated by the real API. Demo activation URLs are illustrative: this task does not implement the campaigner activation screen. Copy and WhatsApp actions are available when a link is returned. The app never automatically sends WhatsApp messages.

## Backend integration

Set `USE_MOCKS = false` to route every admin call through the single axios instance in `src/api.js`, with baseURL `/api/v1` and a Bearer token interceptor. Protected 401 responses clear the token and show “Your session expired. Sign in again.” Login 401 errors remain on the sign-in form. Production must implement role checks; the frontend gate alone is not authorization.

Implemented API paths and payloads follow endpoints 12–21 and 31–33 in `../api-spec-v3.md`. The implemented backend serves versioned endpoints under `/api/v1`; scaffold checks remain under `/api`.

- List queues use `{ data: [...] }`, following #13's list envelope. #18, #20, #21 and #31 list fields but do not explicitly give their envelopes. The adapter expects the same envelope. Volunteer `roleOrInterests` and `availability` render strings or arrays.
- Detail expects #3 application fields plus #14's status, verification, files, log, publication, campaignId and account. Opening a Received record should transition it to In review as proposed by #14.
- Mutation bodies that the spec leaves unspecified (#15, #19, #32 and non-approve #16) are deliberately not consumed; the UI reloads the detail/list or applies the submitted offer status after success.
- Approve consumes `{ reference, status, account }`. #33 consumes the account object directly, as specified. Activation email failure must not undo approval. Link delivery status and the raw link are only available in those responses; #14 has neither. The UI explains this after refresh and offers a new link.
- Enforce transition rules, verification lock, separate publication consent, one-time activation expiry/invalidation, and withdrawal balance/audit rules on the server.
- `files` currently contains filenames only. The contract has no secure document download endpoint, so the admin shows filenames without inventing download URLs.
- No account activation page, public campaign screens, real email, real payment or bank transfer implementation is included in this admin-only scope.

No tests were added. Use `npm run build` to verify the production bundle.
