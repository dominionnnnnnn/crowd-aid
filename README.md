# CrowdAid

*Healthcare, Together.* Ogun Innovate Hackathon 2026.

- **Plan / task list:** `HACKATHON-PLAN.md`
- **API contract:** `api-spec-v3.md`
- **Deploy + troubleshooting:** `HACKATHON-PLAYBOOK.md`

```
crowd-aid/
├── api/                     ASP.NET Core 8 + EF Core (Postgres) + SMTP to MailHog
├── web/                     React (Vite); Caddy serves it in prod and proxies /api
├── docker-compose.yml       DEV: db, mailhog, api, web (all ports open on your laptop)
├── docker-compose.prod.yml  PROD: only 80/443 public; db, api, mailhog private
├── .env.sample              copy to .env on the server
├── deploy.sh                on the server: pull, rebuild, health-check
└── vps-setup.sh             paste into the instance's "User Data" box
```

```
Browser ──> Caddy (web :80/:443) ──/api/*──> api:8080 ──> db:5432
                                                  └──SMTP──> mailhog:1025  (inbox UI :8025)
```

## Run locally

```powershell
docker compose up --build
```
| What | URL |
|---|---|
| React (hot reload) | http://localhost:5173 |
| API health | http://localhost:5159/api/health |
| **MailHog inbox** | http://localhost:8025 |
| Postgres | localhost:5433 · user `crowdaid` · pass `crowdaid` · db `crowdaid_db` |

The scaffold page should show **✅ Hello from CrowdAid + Postgres**. Send a test email from it, and it appears in MailHog.

## Email (MailHog)

MailHog catches every email the API sends; nothing reaches a real inbox. The API reads:

| Setting (env var) | Value |
|---|---|
| `Smtp__Host` / `Smtp__Port` | `mailhog` / `1025` |
| `Smtp__From` | `CrowdAid <no-reply@crowdaid.demo>` |
| `App__BaseUrl` | site address used to build activation links (`PUBLIC_URL` in prod) |

Backend: send the activation email on Approve with these settings (see `api-spec-v3.md`, approve). Delete `POST /api/test-email` before switching to a real email provider.

### MailHog on the server

MailHog's inbox shows activation links, so it is **not** public. It listens only on the server's localhost.
Open it from your laptop through an SSH tunnel:

```powershell
ssh -i C:\Users\USER\Downloads\crowd-aid.pem -L 8025:localhost:8025 ubuntu@<SERVER_IP>
```
Keep that window open and browse to **http://localhost:8025**. (Stop local Docker first if it already uses port 8025, or use `-L 8026:localhost:8025` and open :8026.)
For the demo, the presenter's laptop opens the tunnel and shows the email arriving.

## Deploy (first time, on the server)

```bash
git clone git@github.com:<owner>/crowd-aid.git && cd crowd-aid
cp .env.sample .env
sed -i "s|CHANGE_ME_use_openssl_rand_hex_32|$(openssl rand -hex 32)|" .env
sed -i "s|CHANGE_ME_use_openssl_rand|$(openssl rand -hex 16)|" .env
nano .env        # set PUBLIC_URL (http://<SERVER_IP>) and ADMIN_PASSWORD
bash deploy.sh   # ends with ✅ DEPLOY OK
```
Then open `http://<SERVER_IP>`, check the database line, send a test email, and check it in MailHog through the tunnel.

HTTPS, auto-deploy, logs, rollback: see `HACKATHON-PLAYBOOK.md` and `HACKATHON-PLAN.md` section 6.
