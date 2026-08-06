#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

echo "Starting SQL Server..."
docker compose up -d

echo "Waiting for SQL Server to become healthy (this can take up to 60 seconds)..."
for i in $(seq 1 60); do
  status="$(docker inspect --format='{{.State.Health.Status}}' childcare-sqlserver 2>/dev/null || echo "missing")"
  if [ "$status" = "healthy" ]; then
    echo "SQL Server is healthy."
    break
  fi
  if [ "$i" -eq 60 ]; then
    echo "SQL Server did not become healthy in time. Status: $status"
    echo "Try: docker logs childcare-sqlserver --tail 30"
    exit 1
  fi
  sleep 2
done

if [ -f .env ]; then
  set -a
  # shellcheck disable=SC1091
  source .env
  set +a
fi

echo "Starting API..."
dotnet run --project src/ChildCareLicensing.Api
