#!/usr/bin/env bash
set -euo pipefail

# Brings up the dev stack via docker compose.
#
# Default: full stack — our api+db+cache+webserver plus the SE498-Capstone api+api-db,
#          merged via docker-compose.debug.yml + docker-compose.capstone.yml + docker-compose.integrated.yml,
#          under the `frontend` profile.
# --backend-only / -b: only our api+db (docker-compose.debug.yml, no profile, no capstone stack).
#
# Any non-flag args are forwarded to `docker compose`. If none are given, defaults to `up`.
# Examples:
#   ./scripts/dev-up.sh
#   ./scripts/dev-up.sh down
#   ./scripts/dev-up.sh logs -f webserver
#   ./scripts/dev-up.sh --backend-only
#   ./scripts/dev-up.sh -b down

cd "$(dirname "$0")/.."

# Container runtime — `docker` or `podman`.
# Resolution order:
#   1. $RUNTIME env var (one-off override or `export RUNTIME=...`)
#   2. saved choice in scripts/.runtime (gitignored)
#   3. interactive prompt on first run; choice gets saved
runtime_file="scripts/.runtime"
runtime="${RUNTIME:-}"
if [ -z "$runtime" ] && [ -f "$runtime_file" ]; then
  runtime="$(cat "$runtime_file")"
fi
if [ -z "$runtime" ]; then
  if [ -t 0 ] && [ -t 1 ]; then
    echo "First-time setup: choose your container runtime."
    echo "  1) docker"
    echo "  2) podman"
    while :; do
      read -r -p "Select [1-2, default 1]: " choice
      case "${choice:-1}" in
        1|docker) runtime="docker"; break ;;
        2|podman) runtime="podman"; break ;;
        *) echo "Please enter 1 or 2." ;;
      esac
    done
    echo "$runtime" > "$runtime_file"
    echo "Saved to $runtime_file. Override later with RUNTIME=... or by editing the file."
  else
    runtime="docker"
    echo "Non-interactive shell; defaulting to docker. Set RUNTIME=... to override." >&2
  fi
fi

mode="frontend"
args=()
for a in "$@"; do
  case "$a" in
    --backend-only|-b) mode="backend" ;;
    *) args+=("$a") ;;
  esac
done

if [ ${#args[@]} -eq 0 ]; then
  args=(up)
fi

if [ "$mode" = "backend" ]; then
  exec "$runtime" compose -f docker-compose.debug.yml "${args[@]}"
else
  exec "$runtime" compose \
    -f docker-compose.debug.yml \
    -f docker-compose.capstone.yml \
    -f docker-compose.integrated.yml \
    --profile frontend \
    "${args[@]}"
fi
