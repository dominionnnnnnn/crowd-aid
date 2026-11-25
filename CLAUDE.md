# CLAUDE.md

Guidance for Claude (and other coding agents) working in this repo. Codex reads the same rules from `AGENTS.md`.

## Project

**CrowdAid** (*Healthcare, Together.*): a community healthcare support platform for Abeokuta, built for the Ogun Innovate Hackathon 2026.
People apply for help; an admin reviews and verifies them; approved applicants get an account (a one-time activation link, set password on first login) and a dashboard; the public donates (simulated), offers practical help or volunteers.

- **Plan and task list:** `HACKATHON-PLAN.md`
- **API contract (source of truth):** `api-spec-v3.md`
- **Design (read-only reference):** `design/` (Prototype v2, Design System v1.1, Handoff v2, wireframes, logo and icons)
- **Deploy and troubleshooting:** `README.md`, `HACKATHON-PLAYBOOK.md`

## Folders and who owns them

| Folder / file | Owner | Stack |
|---|---|---|
| `api/` | **Backend dev** | ASP.NET Core 8, EF Core, PostgreSQL, SMTP to MailHog |
| `web/` | **Frontend dev** | React (Vite), served by Caddy in production |
| `design/` | Designer | Read-only reference. Never edit. |
| `docker-compose*.yml`, `web/Caddyfile`, `deploy.sh`, `vps-setup.sh`, `.env.sample` | **Shared** | Change only after the team agrees |
| `HACKATHON-PLAN.md`, `api-spec-v3.md` | Shared | Edit only when asked |

## Rules (follow strictly)

1. **Stay in your folder.** Work out which side the session is for:
   - Started from `api/`, on a `be/*` branch, or the task is backend → edit **only `api/`**.
   - Started from `web/`, on a `fe/*` branch, or the task is frontend → edit **only `web/`**.
   - If it isn't clear, **ask before editing anything**.
2. **Never edit the other side's folder**, not even a "small fix". If the other side needs a change (a field name, a new endpoint, a bug), write it in your final message as a short note for that person.
3. **Never touch shared files** (compose files, Caddyfile, `deploy.sh`, `vps-setup.sh`, `.env.sample`) unless the user explicitly asks. If you think one needs to change, say so and why.
4. **The API contract is `api-spec-v3.md`.** Build to it exactly: paths, JSON field names, status values, error shape. If something in it is wrong or missing, stop and tell the user. Don't silently change the shape.
5. **Never commit secrets.** No passwords, keys or tokens in code or committed config. They come from environment variables (`.env` on the server, which is gitignored).
6. **Don't add tests for now.** The team wants working code first.
7. **Don't run `git push`, merge to `main`, or deploy.** Anything on `main` goes live on the server within minutes. Leave committing and pushing to the user unless asked.
8. Keep changes small and focused on the task. Don't reformat or rename files you weren't asked to touch.

## Conventions both sides share

- All API routes live under **`/api`** (Caddy only forwards `/api/*` to the backend). The spec writes paths without the prefix; e.g. `GET /campaigns` is served at `/api/campaigns`. Agree on one prefix (`/api` or `/api/v1`) and use it on both sides.
- Money is **whole naira integers** (`5000` = ₦5,000). The frontend formats it as `₦` + en-NG grouping.
- Dates are ISO 8601 strings.
- References: `CA-REQ-0001` applications, `CA-DON-` donations, `CA-OFF-` offers, `CA-VOL-` volunteers, `CA-MSG-` messages, `CA-WDR-` withdrawals.
- Error body: `{ "error": { "code": "...", "message": "...", "fields": { "field": "how to fix" } } }`.
- Auth: `Authorization: Bearer <JWT>`. Roles `admin` and `campaigner`.
- All demo data is fictional and labelled as demo. Payments and withdrawals are simulated.

## Backend (`api/`)

- Runs in Docker on port 8080 inside the network; dev maps it to `localhost:5159`.
- Config comes from environment variables set in the compose files: `ConnectionStrings__Default`, `Smtp__Host`, `Smtp__Port`, `Smtp__From`, `App__BaseUrl`, `Jwt__Secret`, `Admin__Email`, `Admin__Password`.
- Email goes to **MailHog** (`mailhog:1025`), inbox at http://localhost:8025. Nothing real is sent.
- If the project/DLL name changes, update the `ENTRYPOINT` in `api/Dockerfile` (that file is inside `api/`, so it's yours).
- Don't call `app.UseHttpsRedirection()`; Caddy handles HTTPS.
- Private data (applications, phone numbers, emails, documents, verification notes, payout details) never appears in public endpoints.
- `POST /api/test-email` is a scaffold check. Delete it before any real email provider is used.

## Frontend (`web/`)

- Call the API with relative URLs (`/api/...`) through one shared helper. **Never hardcode `localhost`.**
- Vite proxies `/api` to the API in dev; Caddy does it in production.
- Follow the design in `design/`: tokens and rules are in `design/CrowdAid Handoff v2.md` (colours, Plus Jakarta Sans, radii, 44px tap targets, 3px focus ring, errors as border + ⚠ text).
- Copy any logo or icon you need from `design/assets/` into `web/public/`. Don't link into `design/`.
- Until an endpoint exists, use mock data shaped exactly like the spec, kept in one place per feature, so switching to the real API changes only that file.

## Commands

```bash
docker compose up --build              # whole stack locally
# React http://localhost:5173 · API http://localhost:5159/api/health · MailHog http://localhost:8025
docker compose up --build api          # rebuild only the API after code changes
docker compose logs -f api             # API logs
```

## Git

- Branches: `be/<feature>` for backend, `fe/<feature>` for frontend.
- Merge to `main` only when `docker compose up --build` works locally. `main` auto-deploys to the server.
