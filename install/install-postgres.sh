#!/usr/bin/env bash
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

if [ "${EUID:-0}" -ne 0 ]; then
  echo "Run as root: sudo $0" >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SQL="${SCRIPT_DIR}/setup-postgres.sql"
if [ ! -f "$SQL" ]; then
  echo "Missing: $SQL" >&2
  exit 1
fi

. "$SCRIPT_DIR/wait.sh"

apt_get update
apt_get install -y postgresql postgresql-client

run_as_postgres() {
  if [ "${EUID:-0}" -eq 0 ]; then
    if command -v runuser >/dev/null 2>&1; then
      runuser -u postgres -- "$@"
    else
      sudo -u postgres "$@"
    fi
  else
    sudo -n -u postgres "$@"
  fi
}

run_as_postgres psql -d postgres -v ON_ERROR_STOP=1 < "$SQL"
