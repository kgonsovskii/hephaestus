#!/usr/bin/env bash
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UPDATE="$SCRIPT_DIR/update.sh"
LOG=/var/log/hephaestus-update.log

mkdir -p "$(dirname "$LOG")" 2>/dev/null || true
touch "$LOG" 2>/dev/null || true

if [ ! -f "$UPDATE" ]; then
  echo "missing $UPDATE" >&2
  exit 1
fi
chmod +x "$UPDATE" 2>/dev/null || true

if [ "${EUID:-0}" -eq 0 ]; then
  nohup /bin/bash "$UPDATE" >>"$LOG" 2>&1 &
else
  nohup sudo /bin/bash "$UPDATE" >>"$LOG" 2>&1 &
fi
echo "scheduled pid $!"
