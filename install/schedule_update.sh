#!/usr/bin/env bash
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UPDATE="$SCRIPT_DIR/update.sh"
LOG="${HEPHAESTUS_UPDATE_LOG:-/var/log/hephaestus-update.log}"
export HEPHAESTUS_UPDATE_LOG="$LOG"

mkdir -p "$(dirname "$LOG")" 2>/dev/null || true
touch "$LOG" 2>/dev/null || true

if [ ! -f "$UPDATE" ]; then
  echo "missing $UPDATE" >&2
  exit 1
fi
chmod +x "$UPDATE" 2>/dev/null || true

if [ "${EUID:-0}" -eq 0 ]; then
  if command -v stdbuf >/dev/null 2>&1; then
    nohup stdbuf -oL -eL /bin/bash "$UPDATE" >>"$LOG" 2>&1 &
  else
    nohup /bin/bash "$UPDATE" >>"$LOG" 2>&1 &
  fi
else
  if command -v stdbuf >/dev/null 2>&1; then
    nohup stdbuf -oL -eL sudo -E /bin/bash "$UPDATE" >>"$LOG" 2>&1 &
  else
    nohup sudo -E /bin/bash "$UPDATE" >>"$LOG" 2>&1 &
  fi
fi
echo "scheduled pid $!"
