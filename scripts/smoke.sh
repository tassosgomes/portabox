#!/usr/bin/env bash

set -euo pipefail

COMPOSE_FILE="docker-compose.dev.yml"

wait_for_port() {
  local host="$1"
  local port="$2"
  local retries="${3:-30}"

  for ((i=1; i<=retries; i++)); do
    if bash -c "exec 3<>/dev/tcp/${host}/${port}" 2>/dev/null; then
      exec 3>&-
      return 0
    fi

    sleep 2
  done

  echo "Port ${host}:${port} did not become available in time." >&2
  return 1
}

cleanup() {
  docker compose -f "${COMPOSE_FILE}" down >/dev/null 2>&1 || true
}

trap cleanup EXIT

pnpm install
dotnet build PortaBox.sln
docker compose -f "${COMPOSE_FILE}" config >/dev/null
docker compose -f "${COMPOSE_FILE}" up -d

wait_for_port 127.0.0.1 5432
wait_for_port 127.0.0.1 9000
wait_for_port 127.0.0.1 1025

echo "Smoke test completed successfully."
