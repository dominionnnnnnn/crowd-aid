# AGENTS.md

Instructions for Codex and other coding agents. **Read `CLAUDE.md` in this folder: its rules apply to you in full.** The short version:

- **CrowdAid**: community healthcare support platform (Ogun Innovate Hackathon 2026). Plan: `HACKATHON-PLAN.md`. API contract: `api-spec-v3.md`. Design (read-only): `design/`.
- `api/` = backend (ASP.NET Core + Postgres). `web/` = frontend (React + Vite). Shared root files (compose, Caddyfile, `deploy.sh`, `vps-setup.sh`, `.env.sample`) are changed only when the user asks.
- **Edit only the folder your task is for.** Never edit `design/`. If the other side needs a change, write it as a note in your final message.
- Build to `api-spec-v3.md` exactly; mock data must match its JSON shapes.
- Frontend calls the API with relative `/api/...` URLs only. Never hardcode `localhost`.
- No tests for now. Don't push, merge to `main` or deploy (`main` auto-deploys).
- Never commit secrets.
