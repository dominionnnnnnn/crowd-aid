# Hackathon Playbook: scaffold, host, ship

Goal for the first 30 minutes: **the real app is live on the VPS**. After that, every change is
**test locally → push → one command to deploy**.

Server: `ubuntu@<SERVER_IP>` (key: `C:\Users\USER\Downloads\crowd-aid.pem`)
Repo folder on server: `~/crowd-aid`

---

## 0. Before you leave tonight: the checklist

- [ ] `ssh` into the server works with the key
- [ ] Ports 22, 80, 443 open in the security group (`Test-NetConnection IP -Port 22` is True)
- [ ] `docker --version` works on the server **without sudo**
- [ ] Starter deployed: `http://<SERVER_IP>` shows "✅ Hello World from Postgres"
- [ ] Saving a message in the page appears in the list (proves React → API → DB)
- [ ] Redeploy tested once with `bash deploy.sh` (this is the exact command you'll use all day)
- [ ] Deploy key added on GitHub so the server can `git pull` (see section 5)

---

## 1. First 30 minutes tomorrow: scaffold and host

### 1a. Put the real code into the starter's shape (on your laptop)

```
crowd-aid/
├── api/   <- your partner's backend (replace the sample)
└── web/   <- the React frontend (replace the sample)
```

Keep these from the starter, don't replace them: `docker-compose.yml`, `docker-compose.prod.yml`,
`web/Dockerfile`, `web/Dockerfile.dev`, `web/Caddyfile`, `.env.sample`, `deploy.sh`.

**Backend checklist (ask your partner or check his code):**
- [ ] `api/Dockerfile` last line uses **his** DLL name: `ENTRYPOINT ["dotnet", "HisProject.dll"]`
      (the DLL is named after his `.csproj` file)
- [ ] Database provider matches the compose `db` service. Postgres means the `Npgsql` package.
      SQL Server means swapping the `db` service (needs 2 GB+ RAM; this server has 8 GB, so it's fine).
- [ ] Connection string: compose sets `ConnectionStrings__Default`. If his code reads
      `ConnectionStrings:DefaultConnection`, rename the env var in **both** compose files.
- [ ] All routes start with `/api` (Caddy only forwards `/api/*` to the backend)
- [ ] Remove `app.UseHttpsRedirection()` (Caddy handles HTTPS; the API speaks plain HTTP inside Docker)
- [ ] DB tables: `EnsureCreated()` for a fresh DB, or `Migrate()` if he uses EF migrations
- [ ] Secrets (JWT key etc.) are read from env vars (`Jwt__Key`), not `appsettings.json`

**Frontend checklist:**
- [ ] Replace every hardcoded `http://localhost:5159` with `/api` (search the project for `localhost`).
      SimplyTodo has it in `src/api/axios.jsx` and `src/helpers/useAxiosPrivate.jsx`.
- [ ] `vite.config.js` keeps the `server.proxy` block for `/api` (dev) and `host: true`
- [ ] Not using Vite? Keep the same idea: the app calls relative `/api`, and a proxy forwards it

### 1b. Test locally first (always)

```powershell
cd C:\Users\USER\Documents\crowd-aid
docker compose up --build
```
- React: http://localhost:5173
- API: http://localhost:5159/api/health

Only push when this works.

### 1c. Add new secrets to `.env.sample` and `docker-compose.prod.yml`

Anything the backend needs (JWT key, API keys) goes:
1. into `.env.sample` as a placeholder (committed, no real values)
2. into `docker-compose.prod.yml` under `api → environment` (e.g. `- Jwt__Key=${JWT_KEY}`)
3. into the real `.env` **on the server** (never committed)

### 1d. Push and deploy

```powershell
git add .
git commit -m "real app"
git push
```
```bash
ssh -i C:\Users\USER\Downloads\crowd-aid.pem ubuntu@<SERVER_IP>
cd crowd-aid
nano .env            # add any new secrets
bash deploy.sh
```
`deploy.sh` pulls, rebuilds, restarts, and ends with **✅ DEPLOY OK** or tells you what failed.

---

## 2. The loop for the rest of the hackathon

```
edit → docker compose up --build (local test) → git push → deploy
```

One-line deploy from your laptop (no need to open an SSH session):
```powershell
ssh -i C:\Users\USER\Downloads\crowd-aid.pem ubuntu@<SERVER_IP> "cd crowd-aid && bash deploy.sh"
```
Save it as a PowerShell function so it's one word:
```powershell
function deploy { ssh -i C:\Users\USER\Downloads\crowd-aid.pem ubuntu@<SERVER_IP> "cd crowd-aid && bash deploy.sh" }
```

Team rules that prevent chaos:
- **Deploy only from `main`.** Work on branches, merge when it works.
- Pull before you push (`git pull --rebase`), and tell your partner before you deploy so you don't overwrite each other.
- Rebuilds are cached: frontend-only changes take about 30 s, backend about 1 min. The first build is slower.

---

## 3. HTTPS (do this if the backend uses cookies or tokens)

Plain HTTP on a bare IP works for demos, but **browsers drop `Secure` cookies over HTTP**. SimplyTodo's
refresh-token flow uses `withCredentials` cookies, so it may fail on the IP. Fix without buying a domain:

```bash
nano .env        # set:  DOMAIN=<SERVER_IP>.nip.io
bash deploy.sh
```
Caddy fetches a free certificate automatically; open `https://<SERVER_IP>.nip.io`.
(Ports 80 and 443 must be open. If nip.io is slow or rate-limited, try `<SERVER_IP>.sslip.io`.)
**Test this tonight** so tomorrow isn't the first time. With a real domain, point an A record at the IP
and set `DOMAIN=yourdomain.com`.

---

## 4. When something breaks

```bash
alias dc="docker compose -f docker-compose.prod.yml"
dc ps                       # what's running / restarting
dc logs --tail=100 api      # backend errors: start here
dc logs --tail=100 web      # Caddy / HTTPS errors
dc logs --tail=50 db
```

| Symptom | Likely cause | Fix |
|---|---|---|
| Page won't load at all | Security group lacks 80/443, or `web` container down | Check rules, `dc ps` |
| Page loads, API calls fail (404/502) | Backend crashed, or route isn't under `/api` | `dc logs api` |
| Browser console: `localhost:5159` refused | Hardcoded localhost left in the frontend | Replace with `/api`, redeploy |
| API restarts in a loop | Bad connection string or DB not ready | `dc logs api`, compare env var name with the code |
| `password authentication failed` | `.env` password changed after the DB was first created | `dc down -v` (⚠️ wipes DB) or change it inside Postgres |
| Login works locally, not live | `Secure` cookies over HTTP | Use HTTPS (section 3) |
| `git pull` asks for a password | Deploy key not set up | Section 5 |
| `permission denied` on docker | Didn't log out/in after setup | `exit` and SSH back in |
| `bad interpreter: \r` running a `.sh` | Windows line endings | `.gitattributes` in the repo fixes new commits; or `sed -i 's/\r$//' deploy.sh` |
| Build fails with out of memory | Rare with 8 GB | `free -h`, `docker system prune -af` |
| Disk full | Old Docker images | `docker system prune -af` |

**Rollback to the last working version:**
```bash
git log --oneline -5
git checkout <good-commit-hash>
bash deploy.sh --no-pull     # deploy exactly what's checked out
git checkout main            # later, when the fix is pushed: go back to main, then bash deploy.sh
```

**Back up the DB before a risky change:**
```bash
dc exec -T db pg_dump -U crowdaid crowdaid_db > backup_$(date +%F_%H%M).sql
```

---

## 5. Let the server pull your private repo (deploy key)

On the server:
```bash
ssh-keygen -t ed25519 -N "" -f ~/.ssh/id_ed25519
cat ~/.ssh/id_ed25519.pub
```
GitHub → repo → **Settings → Deploy keys → Add deploy key** → paste → leave "Allow write access" **off**.
```bash
ssh -T git@github.com                                   # type yes; should greet the repo
git clone git@github.com:<you>/crowd-aid.git
```

---

## 6. Security rules (keep these true all weekend)

- ❌ Never commit `.env`. It's in `.gitignore`; check `git status` before pushing.
- ❌ Never add `ports:` for `db` or `api` in `docker-compose.prod.yml`. Docker bypasses the firewall,
  so the DB would be public. Only `web` (80/443) publishes ports.
- ✅ Strong DB password in the server `.env` (`openssl rand -base64 24`), and a strong JWT secret.
- ✅ SSH is key-only; root login and passwords are disabled. Keep the `.pem` private (don't send it in chat or commit it).
- ✅ Turn off Swagger / developer exception pages in Production (`ASPNETCORE_ENVIRONMENT=Production` is already set).
- ✅ After the hackathon: delete the instance (or at least rotate secrets) and remove the deploy key from GitHub.

---

## 7. Command cheat sheet

```bash
# server
cd ~/crowd-aid && bash deploy.sh        # deploy latest main
dc logs -f api                                  # live backend logs
dc restart api                                  # restart one service
dc exec db psql -U crowdaid -d crowdaid_db              # SQL shell
docker stats --no-stream                        # CPU/RAM per container
sudo ufw status                                 # firewall
free -h; df -h                                  # RAM, disk
```
```powershell
# laptop
docker compose up --build                       # run everything locally
docker compose down                             # stop
docker compose logs -f api                      # local backend logs
```
