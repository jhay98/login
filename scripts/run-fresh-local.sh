#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

COMPOSE_FILE="docker-compose.local.yml"

if [[ ! -f "$COMPOSE_FILE" ]]; then
  echo "Missing $COMPOSE_FILE in project root." >&2
  exit 1
fi

compose() {
  docker compose -f "$COMPOSE_FILE" "$@"
}

echo "Stopping and removing local compose resources (containers, local build images, volumes)..."
compose down --volumes --rmi local --remove-orphans

echo "Starting local stack..."
compose up -d

echo "Local stack started in background."
echo "Frontend: http://localhost:8080"
