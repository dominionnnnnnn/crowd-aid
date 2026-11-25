# ---------- BUILD the React app ----------
FROM node:22-bookworm-slim AS build
WORKDIR /usr/src/app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

# ---------- SERVE with Caddy ----------
# Caddy serves the built files, proxies /api to the API container,
# and gets a free HTTPS certificate automatically once DOMAIN is a real domain.
FROM caddy:2-alpine
COPY Caddyfile /etc/caddy/Caddyfile
COPY --from=build /usr/src/app/dist /srv
