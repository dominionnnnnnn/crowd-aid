# CrowdAid: Ogun Innovate Hackathon plan

*Healthcare, Together.* Community healthcare support in Abeokuta. Team: **FE** (Dominion) · **BE** (partner) · **DS** (designer).
Built from the design files: *Prototype v2*, *Design System v1.1*, *Handoff v2*, *Lo-fi wireframes v0.4.1*.

Tick boxes as we go. ⚠️ = decide in the first 15 minutes.

---

## 1. What the design already covers

8 pages, all in the design. Routes are mapped from the prototype's `#/` links to normal React routes.

| Page | Route | Pri | Key behaviour (from the design) |
|---|---|---|---|
| Homepage + Give help panel | `/` | P0 | "Get help" and "Give help" same size/style; Give help expands to Donate · Practical assistance · Volunteer |
| Campaign listing | `/campaigns` | P0 | Search (name/condition/location), filters (Location, Practical help needed), result count, empty state, loading skeletons |
| Campaign details | `/campaigns/:id` | P0 | Funding progress, practical needs, use of funds, updates (2 newest), organizer, sticky Donate bar, "Reviewed by CrowdAid" badge |
| Donation | `/donate?campaign=` | P0 | Presets ₦1k/2k/5k/10k/20k + custom (min ₦500), method (Card / Bank transfer / USSD), email required, **demo outcome selector** (Success / Pending / Failed), result panel replaces form |
| Get help application | `/apply` | P0 | 3 sections, optional evidence (JPG/PNG/PDF, max 5), two consents, success panel with ref `CA-REQ-0001` |
| Practical help offer | `/help?campaign=&type=` | P0 | Campaign mode vs general mode, 6 help types, availability, area, consent. **Blood group never collected.** |
| Volunteer | `/volunteer?path=outreach\|community` | P1 | Two paths, seeded outreach, registration form + success panel |
| Admin | `/admin` | P1 | Sign-in (pre-created accounts) · tabs: Applications · Practical help offers · Volunteer registrations · Messages |
| Contact dialog | modal (global) | P1 | Campaign-aware, goes to Admin › Messages |

**Rules the design sets (follow them):** money is `₦` + en-NG grouping, whole naira · tap targets ≥ 44px · 3px focus ring ·
errors = 3px border + ⚠ text, never colour alone · demo labels stay on all payment states · *only confirmed payments add to the
campaign total (never the fee)* · patient/family contact details and documents are never public.
Tokens (Handoff v2 §2): primary #0B6B3A · hover #095A31 · text #14271E · border #D5DFDA · tint #EAF6EE · error #B42318 · warning #8A5300 · info #1D5FA8 · font Plus Jakarta Sans 400–800 · radius 10 controls / 12–16 cards / 24 dialogs.

### The design's admin flow (already specified)
`Received → In review → Verified → Approved → Published`, plus `More info requested` and `Rejected`.
- Verification field: Not started · In progress · Verified · Could not verify, plus a private "how was it checked" note.
- **Approve** unlocks only when verification = Verified. **Approve never publishes.**
- **Publish** unlocks only when status = Approved *and* both boxes are ticked (publication consent obtained, public content reviewed). Admin types public title, location, practical needs, organizer, goal and story; nothing is copied from the private application.
- Request info / Approve / Reject open **one shared modal** (reason required), logged with admin name + time.

---

## 2. What is NOT in the design (our additions)

The design says *"Applying is free. No account needed"* and has no applicant login. Your idea (approved applicants get a one-time login,
set a password, then use a dashboard) is new, so these screens don't exist yet. **FE builds them first for the designer to review.**

| Screen | Route | What it needs |
|---|---|---|
| **Activate account** | `/activate?token=` | Check token, show campaign name, new password + confirm, then dashboard. States: expired, already used. |
| **Campaigner login** | `/login` | Email + password. Redirects to the dashboard. |
| **Campaigner dashboard** | `/dashboard` | Status stepper (Approved → Published), funding progress, supporters count, recent donations (name or "Anonymous supporter"), campaign updates. |
| **Offers** | `/dashboard/offers` | Practical-help offers for their campaign (type, availability, area, status). |
| **Withdraw** | `/dashboard/withdraw` | Available balance, payout details, request amount, history with status. **Labelled demo.** |
| **Admin: Withdrawals tab** | `/admin/withdrawals` | Queue with Approve / Reject / Mark paid (same reason modal). |
| **Admin: Approve screen addition** | inside `/admin` | After Approve: show whether the activation **email was sent**, plus the one-time link with **Copy** and **Send via WhatsApp** (fallback if email is off). Also a "Send a new link" button. |
| **Track my application** | `/apply/status` | Reference + phone → status (Received / In review / More info / Approved / Declined). P1. |
| **Updated apply success text** | `/apply` | Change "No account needed" and the next-steps copy: an email with an account link is sent once the application is approved. |
| **Apply form: add Email (required)** | `/apply` section 1 | The design only has phone. Add an email field, validation, and a hint such as "We'll send your account link here if you're approved." It is the applicant's own email, also when applying for someone else. |

### ⚠️ Decisions (where our idea meets the design)

| # | Decision | My recommendation |
|---|---|---|
| 1 | **How does the one-time link reach the applicant?** Email is now **required** on the application form. | The system **emails the link on Approve**. The admin Approve screen also shows it (Copy + WhatsApp) in case email isn't set up or fails. Needs an email service key (Resend, Mailgun or SMTP); if there isn't one, the admin copy/WhatsApp path is the plan. |
| 2 | **Login ID** | Email (now required on every application). |
| 3 | **When is the account created?** | On **Approve**. The applicant can then log in and see status. The campaign still goes public only on **Publish**. |
| 4 | **Publication consent** | Keep the design's admin-recorded consent ("23 Nov · by phone"). Optional extra: applicant ticks it in the dashboard. |
| 5 | **What can a campaigner see about offers?** The design keeps supporter contact **admin-only** and says CrowdAid does *not* match supporters to patients. | Dashboard shows only type, availability, area. Admin passes on contacts after the supporter agrees. Confirm with the founder. |
| 6 | **Withdrawals** | **Simulated and labelled demo** (same as payments in the design). Real payouts need a licensed payment provider and KYC, which won't be done in a hackathon. Don't present simulated money as real. |
| 7 | **Admin roles** (design open decision #6) | Hackathon: one admin role does everything; keep Approve and Publish as separate buttons. |
| 8 | Payment provider / fees (design open #1) | Simulated with the design's fictional breakdown. Real provider test mode only if time allows (P1). |
| 9 | Backend stack | Confirm with BE. The contract below is language-agnostic. |

---

## 3. End-to-end flow

```
Applicant: /apply ─► "Received" (CA-REQ-0001)
Admin:     In review ─► Verified ─► (More info / Reject)
           ─► APPROVE ─► system creates campaigner + one-time link ─► link emailed (admin can also copy or WhatsApp it)
Applicant: /activate?token ─► set password ─► /dashboard (status: Approved, not public yet)
Admin:     records publication consent ─► PUBLISH ─► campaign live on /campaigns
Public:    donate · offer practical help · volunteer
Applicant: dashboard shows progress, supporters, offers ─► requests withdrawal ─► admin approves ─► Paid (demo)
```

---

## 4. API contract

The contract is **`api-spec-v3.md`** (same folder): the backend dev's v2 spec plus our changes. FE mocks and BE endpoints both follow it. If something needs to change, change the spec first, then the code.

What v3 adds to v2:
- [ ] **Email required** on the application form
- [ ] **Approve creates the account** and a one-time activation link (72 h, single use), and emails it
- [ ] `GET /auth/activation/:token`, `POST /auth/activate` (set password), `POST /auth/login` (campaigner)
- [ ] Two JWT roles (`admin`, `campaigner`); `/me/*` routes only ever return the signed-in campaigner's own campaign
- [ ] Dashboard: `/me/campaign`, `/me/campaign/donations`, `/me/campaign/offers`, `/me/payout`, `/me/withdrawals`
- [ ] Admin: withdrawals queue and actions, resend activation link
- [ ] Optional: `GET /applications/status` (reference + email)

Priorities and the order to cut things are in the spec's §8. The open questions it raises are in section 2 above and the spec's §9.

---

## 5. Work plan

### Phase 0: scaffold and host (first 45 min) 🚀
- [ ] **FE:** launch the new VPS instance first (section 6, step A) so its setup runs while we code
- [ ] **BE:** create the new private GitHub repo `crowd-aid`
- [x] `crowd-aid` folder scaffolded with the Docker setup (db, api, web, **MailHog**), `deploy.sh`, `vps-setup.sh`, `.env.sample`
- [ ] FE: push the scaffold to the repo; check the scaffold page shows ✅ for the database and that a test email appears in MailHog
- [ ] FE: replace the scaffold page with the real React app in `web/` (Vite + React + routing + Tailwind), all API calls to relative `/api`, never `localhost`
- [ ] BE: put his project in `api/`, set the correct DLL name in `api/Dockerfile`, add a `/api/health` endpoint
- [ ] Everyone: run `docker compose up --build` locally once; push to `main`
- [ ] Deploy to the new server, then turn on auto-deploy (section 6) so DS tests live
- [ ] Settle the ⚠️ decisions and freeze the API contract

### Phase 1: FE builds the new screens, DS reviews them (hours 1–3)
Order matters. These are the screens the designer has never seen:
- [ ] Admin **Approve → one-time link** screen (Copy + WhatsApp button) inside `/admin`
- [ ] `/activate` and `/login`
- [ ] `/dashboard` (progress, supporters), `/dashboard/offers`, `/dashboard/withdraw`
- [ ] Admin Withdrawals tab; `/apply/status`
- [ ] Shared pieces they reuse from the design: reason modal, status pill, funding progress, form field, submit button, result panel
- [ ] **DS:** review each on the live link; send feedback in one shared list
- [ ] **BE in parallel:** DB + migrations, admin seed, auth (login/activate), `POST /applications`, admin list/detail/status endpoints, approve (creates user + token)

### Phase 2: the 8 designed pages (hours 2–5, FE and BE together)
Build in the design's priority order, mocks first, real endpoints as BE ships them:
- [ ] P0: Home → Campaigns → Campaign details → Donate → Apply → Practical help
- [ ] P1: Volunteer, Admin tabs (Applications, Offers, Volunteers, Messages), Contact dialog
- [ ] Switch each screen from mock data to the real endpoint as BE ships it

### Phase 3: first real vertical slice (hours 4–6)
**Apply → admin verifies → approves → link → activate → dashboard → publish → live on /campaigns.** No mocks on this path; test on the server.

### Phase 4: polish and demo (last 2 hours)
- [ ] **Feature freeze at T-2h**: bug fixes only
- [ ] Seed data and the demo script (section 7)
- [ ] Empty / loading / error states; 360px mobile check (design targets 360 / 768 / 1280)
- [ ] Full deploy from clean `main`; DB backup; screen recording as a fallback

**Cut first if behind:** withdrawals → track-application → volunteer → secure evidence upload (design already marks upload as P1).

---

## 6. Hosting: new instance, new repo, new deploy key

### A. New instance (lead account, bigger credit). Start now; it takes ~5 min to set itself up
1. **Keypair:** create `crowd-aid`, download the `.pem`, then fix permissions:
   ```powershell
   icacls C:\Users\USER\Downloads\crowd-aid.pem /inheritance:r
   icacls C:\Users\USER\Downloads\crowd-aid.pem /grant:r "$($env:USERNAME):R"
   ```
2. **Security group first** (Security Groups page): create `web`, add **Ingress TCP 22, 80, 443** from `0.0.0.0/0`. Keep egress allow-all.
3. **Launch:** Ubuntu 24 · Standard Instances · 2 vCPU / 8 GB or bigger (the last server with 8 GB built fine) · volume 50 GB · keypair `crowd-aid` · network `inet-pub1` · security group **`web`** · leave Recovery Password **empty**.
4. **User Data: paste the full contents of `vps-setup.sh`** (copy it from `Documents\crowd-aid\vps-setup.sh`; this was empty last time, so setup had to be run by hand). The file must start with `#!/bin/bash` as the very first characters, with no blank line above.
5. After ~5 min: `ssh -i C:\Users\USER\Downloads\crowd-aid.pem ubuntu@<SERVER_IP>`, then `sudo ls /root` should list `SETUP_DONE`. `exit` and log in again so Docker works without sudo. If `SETUP_DONE` is missing, check `sudo tail /var/log/vps-setup.log`.

### B. New repo + new deploy key
GitHub **will not let one deploy key be used on two repos**, so the new server makes its own key:
```bash
ssh-keygen -t ed25519 -N "" -f ~/.ssh/id_ed25519
cat ~/.ssh/id_ed25519.pub        # copy the WHOLE line
```
GitHub → **crowd-aid repo → Settings → Deploy keys → Add** (read-only) → paste. Then:
```bash
ssh -T git@github.com            # should greet crowd-aid
git clone git@github.com:<owner>/crowd-aid.git
cd crowd-aid
cp .env.sample .env
sed -i "s|CHANGE_ME_use_openssl_rand_hex_32|$(openssl rand -hex 32)|" .env
sed -i "s|CHANGE_ME_use_openssl_rand|$(openssl rand -hex 16)|" .env
nano .env                        # set PUBLIC_URL=http://<SERVER_IP> and ADMIN_PASSWORD
bash deploy.sh                   # ends with ✅ DEPLOY OK
```
Open `http://<SERVER_IP>`; the first page should load and `/api/health` should answer.
First push from the laptop (inside `Documents\crowd-aid`):
```powershell
git init -b main
git add .
git commit -m "scaffold"
git remote add origin git@github.com:<owner>/crowd-aid.git
git push -u origin main
```

### C. Auto-deploy so DS sees every push (add after the first manual deploy works)
```bash
crontab -e
* * * * * cd $HOME/crowd-aid && /usr/bin/flock -n /tmp/deploy.lock bash -c 'git fetch -q origin main && [ "$(git rev-parse HEAD)" != "$(git rev-parse origin/main)" ] && bash deploy.sh' >> $HOME/auto-deploy.log 2>&1
```
Watch with `tail -f ~/auto-deploy.log`; pushes go live in 1–3 min. If a build fails it won't retry on its own: fix and push again, or `bash deploy.sh --no-pull`.

### D. HTTPS
The apply/login flows use tokens and cookies. Set `DOMAIN=<SERVER_IP>.nip.io` in `.env` and run `bash deploy.sh` (playbook section 3). Test it early.

### Team rules
Branches per feature; merge to `main` only after `docker compose up --build` works locally; `git pull --rebase` before push; new secret → `.env.sample` + `docker-compose.prod.yml` + the server's `.env` (never in git). Troubleshooting and rollback: `HACKATHON-PLAYBOOK.md`.

---

## 7. Demo (from the wireframes' own journey, plus our additions)

**Seed:** campaign *Help Usman get kidney surgery* already **Published** (₦3,000,000 of ₦5,000,000 · 60% · 12 supporters, Abeokuta South, organizer Aminat Yusuf, needs: Transport, Blood donors, Meals); a second clearly fictional application `CA-REQ-0002` for the live run; one admin account; a seeded outreach (Sat 28 Nov, 9:00am–1:00pm); sample offers and a volunteer. Keep the "FICTIONAL DEMO DATA" and "Demo payment" labels.

**Live run (≈4 min):**
1. Public: home → campaign → donate ₦5,000 (success; total updates) → offer practical help
2. `/apply`: submit the second application, show the reference
3. `/admin`: open it → Verified → **Approve** (modal) → one-time link appears
4. Open the link → **set password** → campaigner dashboard
5. Admin **Publish** (consent recorded) → campaign appears on `/campaigns`
6. Campaigner requests a withdrawal → admin approves (demo)
Fallback: if anything breaks, steps 1 + 3–5 on the seeded data still tell the story.

---

## 8. Risks

| Risk | Mitigation |
|---|---|
| Our accounts idea contradicts the design's "no account needed" | Treated as a labelled extension; DS reviews first; only touches /apply copy, Admin Approve, and the new dashboard |
| Email sending isn't set up in time | The Approve response always carries the link; admin copies it or sends it by WhatsApp |
| Simulated money mistaken for real | Demo labels on every payment/withdrawal screen, as the design requires |
| Private data leaks into public endpoints | Separate public DTOs; contract lists public fields explicitly |
| Evidence files lost on redeploy | Uploads volume in prod compose; or hide upload (design allows) |
| Bad push breaks the live demo | Merge to `main` only after a local build; rollback in the playbook; freeze at T-2h |
| New server not ready in time | Start step 6A first; `vps-setup.sh` in User Data does the setup unattended |
