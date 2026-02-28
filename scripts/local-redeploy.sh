#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="${COMPOSE_FILE:-${ROOT_DIR}/docker-compose.local.yml}"
COMPOSE=(docker compose -f "$COMPOSE_FILE")

printf "Using compose file: %s\n" "$COMPOSE_FILE"

printf "\n[1/6] Stopping running local services...\n"
"${COMPOSE[@]}" stop -t 1 || true

printf "\n[2/6] Removing stopped local containers...\n"
"${COMPOSE[@]}" rm -f || true

printf "\n[3/6] Starting postgres...\n"
"${COMPOSE[@]}" up -d postgres

printf "\n[4/6] Waiting for postgres health...\n"
POSTGRES_CID="$("${COMPOSE[@]}" ps -q postgres)"
if [[ -z "$POSTGRES_CID" ]]; then
  echo "Postgres container not found" >&2
  exit 1
fi

for i in {1..60}; do
  HEALTH="$(docker inspect --format='{{if .State.Health}}{{.State.Health.Status}}{{else}}unknown{{end}}' "$POSTGRES_CID")"
  if [[ "$HEALTH" == "healthy" ]]; then
    echo "Postgres is healthy"
    break
  fi

  if [[ "$i" -eq 60 ]]; then
    echo "Postgres did not become healthy in time (status: $HEALTH)" >&2
    exit 1
  fi

  sleep 2
done

printf "\n[5/6] Running database migrations (--migrate-only)...\n"
"${COMPOSE[@]}" run --rm backend --migrate-only

printf "\n[6/6] Starting frontend, backend, and nginx...\n"
"${COMPOSE[@]}" up -d --build frontend backend nginx

printf "\nDone. Current service status:\n"
"${COMPOSE[@]}" ps
