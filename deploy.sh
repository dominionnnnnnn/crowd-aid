#!/bin/bash
# Run ON THE SERVER from the repo folder:  bash deploy.sh
# Pulls the latest main, rebuilds only what changed, restarts, and health-checks.
set -e
cd "$(dirname "$0")"

COMPOSE="docker compose -f docker-compose.prod.yml"

if [ "$1" = "--no-pull" ]; then
  echo "==> Skipping git pull (deploying what is checked out)"
else
  echo "==> Pulling latest code"
  git pull --ff-only
fi

echo "==> Building and starting"
$COMPOSE up -d --build

echo "==> Cleaning old images"
docker image prune -f >/dev/null

echo "==> Status"
$COMPOSE ps

# Health check through Caddy (same path a visitor takes)
DOMAIN=$(grep -E '^DOMAIN=' .env | cut -d= -f2-)
if [ -z "$DOMAIN" ] || [[ "$DOMAIN" == :* ]]; then URL="http://localhost/api/health"; else URL="https://$DOMAIN/api/health"; fi

echo "==> Health check: $URL"
for i in 1 2 3 4 5 6 7 8 9 10; do
  if curl -fsS "$URL"; then echo; echo "✅ DEPLOY OK"; exit 0; fi
  sleep 3
done
echo "❌ Health check failed. Run: $COMPOSE logs --tail=50 api web"
exit 1
